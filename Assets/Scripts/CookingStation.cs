using System.Collections;
using UnityEngine;

public class CookingStation : MonoBehaviour
{
    [SerializeField] private float cookingTime = 5f;
    [SerializeField] private float foodBurningTime = 10f;

    private bool isCooking = false;
    private FoodManager foodOnStation;
    private Coroutine cookingCoroutine;
    private FloatingMessageUI messageUI;
    private CookingProgressUI progressUI;

    private void Awake()
    {
        messageUI = GetComponentInChildren<FloatingMessageUI>();
        progressUI = GetComponentInChildren<CookingProgressUI>();
    }

    public bool TryCookingFood(FoodManager food)
    {
        if (isCooking)
        {
            messageUI.ShowMessage("Cooking station is already in use.");
            return false;
        }

        if (food.isBurnt)
        {
            messageUI.ShowMessage("Food is already burnt.");
            return false;
        }
        else if (food.isCooked)
        {
            messageUI.ShowMessage("Food is already cooked.");
            return false;
        }

        if (food.foodType == FoodType.Raw)
        {
            isCooking = true;
            foodOnStation = food;
            food.GetComponent<Transform>().SetParent(transform);
            food.GetComponent<Transform>().localPosition = Vector3.zero;
            food.GetComponent<Transform>().localRotation = Quaternion.identity;
            cookingCoroutine = StartCoroutine(CookFood(food));

            return true;
        }
        else
        {
            messageUI.ShowMessage("Food is not raw and cannot be cooked.");
            return false;
        }
    }

    public GameObject TryTakingFood()
    {
        if (foodOnStation == null)
        {
            messageUI.ShowMessage("No food on the cooking station.");
            return null;
        }

        if (cookingCoroutine != null)
        {
            StopCoroutine(cookingCoroutine);
            cookingCoroutine = null;
        }

        progressUI.HideProgress();

        GameObject foodToReturn = foodOnStation.gameObject;
        foodOnStation = null;
        isCooking = false;

        return foodToReturn;
    }

    private IEnumerator CookFood(FoodManager food)
    {
        Debug.Log("Cooking started.");

        while (food.cookingProgress < 1f)
        {
            food.cookingProgress += Time.deltaTime / cookingTime;
            progressUI.ShowCookingProgress(food.cookingProgress);
            yield return null;
        }

        food.cookingProgress = 1f;
        food.isCooked = true;
        Debug.Log("Food is cooked.");

        float burningTimer = 0f;
        while (burningTimer < foodBurningTime)
        {
            burningTimer += Time.deltaTime;
            progressUI.ShowBurningProgress(burningTimer / foodBurningTime);
            yield return null;
        }

        food.isBurnt = true;
        Debug.Log("Food is burnt.");
        progressUI.HideProgress();
        cookingCoroutine = null;
    }
}
