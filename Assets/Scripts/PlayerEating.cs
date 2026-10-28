using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerEating : MonoBehaviour
{
    [SerializeField] private float cookedFoodHungerReduction = 40f;
    [SerializeField] private float snackHungerReduction = 15f;

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
    }

    private void TryEatingFood()
    {
        FoodManager food = playerInteraction.GetFoodInHand();

        if (food == null)
        {
            Debug.Log("No food in hand to eat.");
            return;
        }

        if (food.foodType == FoodType.Snack)
        {
            EatFood(snackHungerReduction);
        }
        else if (food.isBurnt)
        {
            Debug.Log("Food is burnt and cannot be eaten.");
        }
        else if (food.isCooked)
        {
            EatFood(cookedFoodHungerReduction);
        }
        else
        {
            Debug.Log("Food is raw and needs to be cooked first.");
        }
    }

    private void EatFood(float hungerReduction)
    {
        playerHungerManager.ReduceHunger(hungerReduction);
        playerInteraction.RemoveFoodFromHand();
        Debug.Log("Food eaten.");
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
