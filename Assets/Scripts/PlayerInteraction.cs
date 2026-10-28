using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    private InputSystem_Actions controls;
    private GameObject foodInHand;
    private FoodManager foodManager;
    private GameObject nearbyFood;
    private FoodSpawner foodSpawner;
    private bool canPickUpFood = false;
    private bool nearCookingStation = false;
    private CookingStation cookingStation;

    private void Awake()
    {
        controls = new InputSystem_Actions();
    }

    private void Update()
    {
        if (controls.Player.Interact.WasPressedThisFrame())
        {
            if (canPickUpFood)
            {
                if (foodSpawner != null)
                {
                    foodInHand = foodSpawner.GetFood();
                    if (foodInHand != null)
                    {
                        PickupFood();
                    }
                    foodSpawner = null;
                    canPickUpFood = false;
                }
                else if (nearbyFood != null)
                {
                    foodInHand = nearbyFood;
                    nearbyFood = null;
                    PickupFood();
                }
            }
            else if (nearCookingStation && cookingStation != null)
            {
                if (foodManager != null)
                {
                    if (cookingStation.TryCookingFood(foodManager))
                    {
                        foodInHand = null;
                        foodManager = null;
                    }
                }
                else
                {
                    foodInHand = cookingStation.TryTakingFood();
                    if (foodInHand != null)
                    {
                        PickupFood();
                    }
                }
            }
        }
    }

    private void PickupFood()
    {
        canPickUpFood = false;
        foodManager = foodInHand.GetComponent<FoodManager>();

        foodInHand.transform.SetParent(transform);
        foodInHand.transform.localPosition = Vector3.zero;
        foodInHand.transform.localRotation = Quaternion.identity;

        foreach (Collider col in foodInHand.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        Rigidbody rb = foodInHand.GetComponentInChildren<Rigidbody>();
        rb.isKinematic = true;
        rb.detectCollisions = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void TryDroppingFood(bool isWithForce)
    {
        if (foodInHand == null) return;

        foodInHand.transform.SetParent(null);

        foreach (Collider col in foodInHand.GetComponentsInChildren<Collider>())
        {
            col.enabled = true;
        }

        Rigidbody rb = foodInHand.GetComponentInChildren<Rigidbody>();
        rb.isKinematic = false;
        rb.detectCollisions = true;

        if (isWithForce)
        {
            rb.AddForce(Vector3.up * 2f, ForceMode.Impulse);
        }

        foodInHand = null;
        foodManager = null;
    }

    public FoodManager GetFoodInHand()
    {
        return foodManager;
    }

    public void RemoveFoodFromHand()
    {
        if (foodInHand == null) return;

        Destroy(foodInHand);
        foodInHand = null;
        foodManager = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Food") && foodInHand == null && !canPickUpFood)
        {
            nearbyFood = other.gameObject;
            canPickUpFood = true;
        }

        if (other.CompareTag("FoodSpawner") && foodInHand == null && !canPickUpFood)
        {
            foodSpawner = other.gameObject.GetComponent<FoodSpawner>();
            canPickUpFood = true;
            Debug.Log("FoodSpawner detected");
        }

        if (other.CompareTag("CookingStation"))
        {
            cookingStation = other.gameObject.GetComponent<CookingStation>();
            nearCookingStation = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Food") && foodInHand == null && canPickUpFood)
        {
            nearbyFood = null;
            canPickUpFood = false;
        }

        if (other.CompareTag("FoodSpawner") && foodInHand == null && canPickUpFood)
        {
            foodSpawner = null;
            canPickUpFood = false;
            Debug.Log("FoodSpawner left");
        }

        if (other.CompareTag("CookingStation"))
        {
            cookingStation = null;
            nearCookingStation = false;
        }
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
