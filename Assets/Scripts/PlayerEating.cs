using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerEating : MonoBehaviour
{
    [SerializeField] private float cookedFoodHungerReduction = 40f;
    [SerializeField] private float snackHungerReduction = 15f;
    [SerializeField] private FloatingMessageUI messageUI;
    [SerializeField] private FloatingMessageUI hungerPopupUI;

    private InputSystem_Actions controls;
    private PlayerInteraction playerInteraction;
    private PlayerHungerManager playerHungerManager;

    private void Awake()
    {
        controls = new InputSystem_Actions();
        playerInteraction = GetComponent<PlayerInteraction>();
        playerHungerManager = GetComponent<PlayerHungerManager>();
    }

    private void Update()
    {
        if (controls.Player.Eat.WasPressedThisFrame())
        {
            TryEatingFood();
        }

        if (controls.Player.Drop.WasPressedThisFrame())
        {
            playerInteraction.TryDroppingFood(false);
        }
    }

    private void TryEatingFood()
    {
        FoodManager food = playerInteraction.GetFoodInHand();

        if (food == null)
        {
            messageUI.ShowMessage("No food in hand to eat.");
            return;
        }

        if (food.foodType == FoodType.Snack)
        {
            EatFood(snackHungerReduction);
        }
        else if (food.isBurnt)
        {
            messageUI.ShowMessage("Food is burnt and cannot be eaten.");
        }
        else if (food.isCooked)
        {
            EatFood(cookedFoodHungerReduction);
        }
        else
        {
            messageUI.ShowMessage("Food is raw and needs to be cooked first.");
        }
    }

    private void EatFood(float hungerReduction)
    {
        float reducedPercentage = playerHungerManager.ReduceHunger(hungerReduction);
        playerInteraction.RemoveFoodFromHand();
        messageUI.HideMessage();
        hungerPopupUI.ShowMessage("-" + Mathf.RoundToInt(reducedPercentage) + "%");
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
