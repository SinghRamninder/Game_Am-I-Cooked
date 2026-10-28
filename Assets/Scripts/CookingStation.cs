using System.Collections;
using UnityEngine;

public class CookingStation : MonoBehaviour
{
    [SerializeField] private float cookingTime = 5f;
    [SerializeField] private float foodBurningTime = 10f;

    private bool isCooking = false;
    private FoodManager foodOnStation;
    private Coroutine cookingCoroutine;

    public bool TryCookingFood(FoodManager food)
    {
        if (isCooking)
        {
            Debug.Log("Cooking station is already in use.");
            return false;
        }

        if (food.isBurnt)
        {
            Debug.Log("Food is already burnt.");
            return false;
        }
        else if (food.isCooked)
        {
            Debug.Log("Food is already cooked.");
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
            Debug.Log("Food is not raw and cannot be cooked.");
            return false;
        }
    }

    public GameObject TryTakingFood()
    {
        if (foodOnStation == null)
        {
            Debug.Log("No food on the cooking station.");
            return null;
        }

        if (cookingCoroutine != null)
        {
            StopCoroutine(cookingCoroutine);
            cookingCoroutine = null;
        }

        GameObject foodToReturn = foodOnStation.gameObject;
        foodOnStation = null;
        isCooking = false;
        Debug.Log("Food taken from cooking station.");

        return foodToReturn;
    }

    private IEnumerator CookFood (FoodManager food)
    {
        Debug.Log("Cooking started.");
        yield return new WaitForSeconds(cookingTime);
        food.isCooked = true;
        Debug.Log("Food is cooked.");
        yield return new WaitForSeconds(foodBurningTime);
        food.isBurnt = true;
        Debug.Log("Food is burnt.");
        cookingCoroutine = null;
    }
}
