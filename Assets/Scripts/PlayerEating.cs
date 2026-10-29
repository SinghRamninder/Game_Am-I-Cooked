using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerEating : NetworkBehaviour
{
    [SerializeField] private float cookedFoodHungerReduction = 40f;
    [SerializeField] private float snackHungerReduction = 15f;

    private InputSystem_Actions controls;
    private PlayerInteraction playerInteraction;
    private PlayerHungerManager playerHungerManager;
    private PlayerMessages playerMessages;

    private void Awake()
    {
        controls = new InputSystem_Actions();
        playerInteraction = GetComponent<PlayerInteraction>();
        playerHungerManager = GetComponent<PlayerHungerManager>();
        playerMessages = GetComponent<PlayerMessages>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (controls.Player.Eat.WasPressedThisFrame())
        {
            playerInteraction.SendRequest(InteractionType.Eat, null);
        }

        if (controls.Player.Drop.WasPressedThisFrame())
        {
            playerInteraction.SendRequest(InteractionType.Drop, null);
        }
    }

    public bool TryEatingFood()
    {
        FoodManager food = playerInteraction.GetFoodInHand();

        if (food == null)
        {
            playerMessages.ShowMessage("No food in hand to eat.");
            return false;
        }

        if (food.foodType == FoodType.Snack)
        {
            EatFood(snackHungerReduction);
            return true;
        }
        else if (food.isBurnt.Value)
        {
            playerMessages.ShowMessage("Food is burnt and cannot be eaten.");
            return false;
        }
        else if (food.isCooked.Value)
        {
            EatFood(cookedFoodHungerReduction);
            return true;
        }
        else
        {
            playerMessages.ShowMessage("Food is raw and needs to be cooked first.");
            return false;
        }
    }

    private void EatFood(float hungerReduction)
    {
        float reducedPercentage = playerHungerManager.ReduceHunger(hungerReduction);
        playerInteraction.RemoveFoodFromHand();
        playerMessages.ShowHungerPopup(Mathf.RoundToInt(reducedPercentage));
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
