using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : NetworkBehaviour
{
    [SerializeField] private Transform holdPoint;
    [SerializeField] private float interactRange = 2.5f;
    [SerializeField] private float dropForce = 5f;

    private InputSystem_Actions controls;
    private NetworkVariable<NetworkObjectReference> heldItemReference = new NetworkVariable<NetworkObjectReference>();
    private HoldableItem heldItem;
    private FoodManager nearbyFood;
    private KitchenObject nearbyObject;
    private FoodSpawner foodSpawner;
    private CookingStation cookingStation;
    private Dustbin dustbin;
    private PlayerEating playerEating;
    private PlayerCombat playerCombat;
    private PlayerMessages playerMessages;
    private PlayerMovement playerMovement;
    private PlayerIdentity playerIdentity;
    private PlayerThrowing playerThrowing;

    private void Awake()
    {
        controls = new InputSystem_Actions();
        playerEating = GetComponent<PlayerEating>();
        playerCombat = GetComponent<PlayerCombat>();
        playerMessages = GetComponent<PlayerMessages>();
        playerMovement = GetComponent<PlayerMovement>();
        playerIdentity = GetComponent<PlayerIdentity>();
        playerThrowing = GetComponent<PlayerThrowing>();
    }

    private void Update()
    {
        if (!IsLocalPlayer || playerThrowing.IsBlockingInput()) return;

        if (controls.Player.Interact.WasPressedThisFrame())
        {
            TryInteracting();
        }
    }

    public void TryInteracting()
    {
        NetworkObject target = null;

        if (IsHoldingItem())
        {
            if (IsHoldingFood() && cookingStation != null)
            {
                target = cookingStation.NetworkObject;
            }
            else if (IsHoldingFood() && dustbin != null)
            {
                target = dustbin.NetworkObject;
            }
        }
        else if (nearbyFood != null && !nearbyFood.HasHolder())
        {
            target = nearbyFood.NetworkObject;
        }
        else if (nearbyObject != null && !nearbyObject.HasHolder())
        {
            target = nearbyObject.NetworkObject;
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

        if (type == InteractionType.Attack)
        {
            return playerCombat.TryAttackingOnServer(target);
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

        Dustbin dustbinTarget = target.GetComponent<Dustbin>();
        if (dustbinTarget != null)
        {
            return TryDiscardingFood(dustbinTarget);
        }

        HoldableItem item = target.GetComponent<HoldableItem>();
        if (item != null)
        {
            return TryPickingUpItem(item);
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
        if (heldItem != null) return false;

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

        PickupItem(food);
        return true;
    }

    private bool TryUsingCookingStation(CookingStation station)
    {
        if (heldItem != null)
        {
            FoodManager food = GetFoodInHand();
            if (food == null) return false;

            if (station.TryCookingFood(food, OwnerClientId))
            {
                heldItem = null;
                heldItemReference.Value = new NetworkObjectReference();
                return true;
            }
            return false;
        }

        FoodManager foodOnStation = station.TryTakingFood(OwnerClientId);
        if (foodOnStation == null) return false;

        PickupItem(foodOnStation);
        return true;
    }

    private bool TryDiscardingFood(Dustbin dustbinTarget)
    {
        if (GetFoodInHand() == null)
        {
            playerMessages.ShowMessage("Only food can go in the dustbin.");
            return false;
        }

        RemoveFoodFromHand();
        dustbinTarget.PlayDiscard();
        return true;
    }

    private bool TryPickingUpItem(HoldableItem item)
    {
        if (heldItem != null) return false;

        if (item.HasHolder())
        {
            playerMessages.ShowMessage("Someone already took that.");
            return false;
        }

        PickupItem(item);
        return true;
    }

    private void PickupItem(HoldableItem item)
    {
        heldItem = item;
        heldItemReference.Value = item.NetworkObject;
        item.SetHolder(NetworkObject);
    }

    public bool TryDroppingFood(bool isWithForce)
    {
        if (isWithForce)
        {
            return TryDroppingItem(Vector3.up * dropForce);
        }

        return TryDroppingItem(Vector3.zero);
    }

    public bool TryDroppingItem(Vector3 force)
    {
        if (heldItem == null) return false;

        HoldableItem item = heldItem;
        heldItem = null;
        heldItemReference.Value = new NetworkObjectReference();
        item.SetHolder(null);

        if (force != Vector3.zero)
        {
            item.AddImpulse(force);
        }

        return true;
    }

    public bool TryDroppingFoodWithForce(Vector3 force)
    {
        if (GetFoodInHand() == null) return false;

        return TryDroppingItem(force);
    }

    public FoodManager GetFoodInHand()
    {
        return heldItem as FoodManager;
    }

    public KitchenObject GetObjectInHand()
    {
        return heldItem as KitchenObject;
    }

    public HoldableItem GetHeldItem()
    {
        NetworkObject itemObject;
        if (!heldItemReference.Value.TryGet(out itemObject)) return null;

        return itemObject.GetComponent<HoldableItem>();
    }

    public KitchenObject GetHeldKitchenObject()
    {
        NetworkObject itemObject;
        if (!heldItemReference.Value.TryGet(out itemObject)) return null;

        return itemObject.GetComponent<KitchenObject>();
    }

    public float GetCarriedObjectMass()
    {
        KitchenObject kitchenObject = GetObjectInHand();
        if (kitchenObject == null) return 0f;

        return kitchenObject.GetMass();
    }

    public void RemoveFoodFromHand()
    {
        if (GetFoodInHand() == null) return;

        NetworkObject foodObject = heldItem.NetworkObject;
        heldItem = null;
        heldItemReference.Value = new NetworkObjectReference();
        foodObject.Despawn();
    }

    public bool IsHoldingItem()
    {
        NetworkObject itemObject;
        return heldItemReference.Value.TryGet(out itemObject);
    }

    public bool IsHoldingFood()
    {
        NetworkObject itemObject;
        if (!heldItemReference.Value.TryGet(out itemObject)) return false;

        return itemObject.GetComponent<FoodManager>() != null;
    }

    public bool IsHoldingObject()
    {
        NetworkObject itemObject;
        if (!heldItemReference.Value.TryGet(out itemObject)) return false;

        return itemObject.GetComponent<KitchenObject>() != null;
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
        if (IsServer && heldItem != null && heldItem.IsSpawned)
        {
            heldItem.SetHolder(null);
        }

        heldItem = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;

        if (other.CompareTag("Food"))
        {
            nearbyFood = other.GetComponent<FoodManager>();
        }

        if (other.CompareTag("KitchenObject"))
        {
            nearbyObject = other.GetComponent<KitchenObject>();
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

        if (other.CompareTag("Dustbin"))
        {
            dustbin = other.GetComponent<Dustbin>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsOwner) return;

        if (other.CompareTag("Food") && nearbyFood != null && other.gameObject == nearbyFood.gameObject)
        {
            nearbyFood = null;
        }

        if (other.CompareTag("KitchenObject") && nearbyObject != null && other.gameObject == nearbyObject.gameObject)
        {
            nearbyObject = null;
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

        if (other.CompareTag("Dustbin"))
        {
            dustbin = null;
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
