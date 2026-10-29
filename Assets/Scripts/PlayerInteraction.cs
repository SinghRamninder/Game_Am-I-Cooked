using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : NetworkBehaviour
{
    [SerializeField] private Transform holdPoint;
    [SerializeField] private float interactRange = 2.5f;
    [SerializeField] private float dropForce = 5f;

    private InputSystem_Actions controls;
    private NetworkVariable<NetworkObjectReference> heldFood = new NetworkVariable<NetworkObjectReference>();
    private FoodManager foodManager;
    private FoodManager nearbyFood;
    private FoodSpawner foodSpawner;
    private CookingStation cookingStation;
    private PlayerEating playerEating;
    private PlayerMessages playerMessages;
    private PlayerMovement playerMovement;
    private PlayerIdentity playerIdentity;

    private void Awake()
    {
        controls = new InputSystem_Actions();
        playerEating = GetComponent<PlayerEating>();
        playerMessages = GetComponent<PlayerMessages>();
        playerMovement = GetComponent<PlayerMovement>();
        playerIdentity = GetComponent<PlayerIdentity>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (controls.Player.Interact.WasPressedThisFrame())
        {
            TryInteracting();
        }
    }

    private void TryInteracting()
    {
        NetworkObject target = null;

        if (IsHoldingFood())
        {
            if (cookingStation != null)
            {
                target = cookingStation.NetworkObject;
            }
        }
        else if (nearbyFood != null && !nearbyFood.HasHolder())
        {
            target = nearbyFood.NetworkObject;
        }
        else if (foodSpawner != null)
        {
            target = foodSpawner.NetworkObject;
        }
        else if (cookingStation != null)
        {
            target = cookingStation.NetworkObject;
        }

        if (target != null)
        {
            SendRequest(InteractionType.Interact, target);
        }
    }

    public void SendRequest(InteractionType type, NetworkObject target)
    {
        NetworkObjectReference targetReference = new NetworkObjectReference();
        if (target != null)
        {
            targetReference = target;
        }

        SendRequestRpc(type, targetReference, NetworkManager.ServerTime.Time);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void SendRequestRpc(InteractionType type, NetworkObjectReference target, double pressTime)
    {
        NetworkDecisionMaker.Instance.AddRequest(this, type, target, pressTime);
    }

    public bool ExecuteRequest(InteractionType type, NetworkObject target)
    {
        if (type == InteractionType.Eat)
        {
            return playerEating.TryEatingFood();
        }

        if (type == InteractionType.Drop)
        {
            return TryDroppingFood(false);
        }

        if (target == null) return false;

        if (!IsInReach(target.transform))
        {
            playerMessages.ShowMessage("Too far away.");
            return false;
        }

        FoodSpawner spawner = target.GetComponent<FoodSpawner>();
        if (spawner != null)
        {
            return TryTakingFromSpawner(spawner);
        }

        CookingStation station = target.GetComponent<CookingStation>();
        if (station != null)
        {
            return TryUsingCookingStation(station);
        }

        FoodManager food = target.GetComponent<FoodManager>();
        if (food != null)
        {
            return TryPickingUpFood(food);
        }

        return false;
    }

    private bool IsInReach(Transform target)
    {
        Vector3 offset = target.position - transform.position;
        offset.y = 0f;
        float allowedReach = NetworkLagHandler.Instance.GetAllowedReach(OwnerClientId, interactRange, playerMovement.moveSpeed);

        return offset.magnitude <= allowedReach;
    }

    private bool TryTakingFromSpawner(FoodSpawner spawner)
    {
        if (foodManager != null) return false;

        if (spawner.IsContainerEmpty())
        {
            playerMessages.ShowMessage("Container is out of stock.");
            return false;
        }

        FoodManager food = spawner.GetFood();
        if (food == null)
        {
            playerMessages.ShowMessage("Container is loading...");
            return false;
        }

        PickupFood(food);
        return true;
    }

    private bool TryUsingCookingStation(CookingStation station)
    {
        if (foodManager != null)
        {
            if (station.TryCookingFood(foodManager, OwnerClientId))
            {
                foodManager = null;
                heldFood.Value = new NetworkObjectReference();
                return true;
            }
            return false;
        }

        FoodManager food = station.TryTakingFood(OwnerClientId);
        if (food == null) return false;

        PickupFood(food);
        return true;
    }

    private bool TryPickingUpFood(FoodManager food)
    {
        if (foodManager != null) return false;

        if (food.HasHolder())
        {
            playerMessages.ShowMessage("Someone already took that food.");
            return false;
        }

        PickupFood(food);
        return true;
    }

    private void PickupFood(FoodManager food)
    {
        foodManager = food;
        heldFood.Value = food.NetworkObject;
        food.SetHolder(NetworkObject);
    }

    public bool TryDroppingFood(bool isWithForce)
    {
        if (foodManager == null) return false;

        FoodManager food = foodManager;
        foodManager = null;
        heldFood.Value = new NetworkObjectReference();
        food.SetHolder(null);

        if (isWithForce)
        {
            food.AddImpulse(Vector3.up * dropForce);
        }

        return true;
    }

    public FoodManager GetFoodInHand()
    {
        return foodManager;
    }

    public void RemoveFoodFromHand()
    {
        if (foodManager == null) return;

        NetworkObject foodObject = foodManager.NetworkObject;
        foodManager = null;
        heldFood.Value = new NetworkObjectReference();
        foodObject.Despawn();
    }

    public bool IsHoldingFood()
    {
        NetworkObject foodObject;
        return heldFood.Value.TryGet(out foodObject);
    }

    public Transform GetHoldPoint()
    {
        return holdPoint;
    }

    public void ShowMessage(string message)
    {
        playerMessages.ShowMessage(message);
    }

    public string GetPlayerName()
    {
        return playerIdentity.GetPlayerName();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && foodManager != null && foodManager.IsSpawned)
        {
            foodManager.SetHolder(null);
        }

        foodManager = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;

        if (other.CompareTag("Food"))
        {
            nearbyFood = other.GetComponent<FoodManager>();
        }

        if (other.CompareTag("FoodSpawner"))
        {
            foodSpawner = other.GetComponent<FoodSpawner>();
            Debug.Log("FoodSpawner detected");
        }

        if (other.CompareTag("CookingStation"))
        {
            cookingStation = other.GetComponent<CookingStation>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsOwner) return;

        if (other.CompareTag("Food") && nearbyFood != null && other.gameObject == nearbyFood.gameObject)
        {
            nearbyFood = null;
        }

        if (other.CompareTag("FoodSpawner"))
        {
            foodSpawner = null;
            Debug.Log("FoodSpawner left");
        }

        if (other.CompareTag("CookingStation"))
        {
            cookingStation = null;
        }
    }

    public override void OnDestroy()
    {
        controls.Dispose();
        base.OnDestroy();
    }

    void OnEnable()
    {
        controls.Player.Enable();
    }

    void OnDisable()
    {
        controls.Player.Disable();
    }
}
