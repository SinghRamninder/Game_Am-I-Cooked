using System.Collections;
using UnityEngine;

public class FoodSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] rawFoodPrefab;
    [SerializeField] private GameObject[] snackPrefab;
    [SerializeField] private int totalAvailableFood = 50;
    [SerializeField] private bool spawnRawFood = true;
    [SerializeField] private float nextSpawnTime = 5f;
    [SerializeField] private float lowStockPercentage = 0.8f;

    private GameObject[] storedFood;
    private int currentFoodIndex = 0;
    private bool isContainerEmpty = false;
    private bool isContainerLoading = false;
    private bool isLowStockShown = false;
    private FoodContainerUI containerUI;

    private void Start()
    {
        containerUI = GetComponentInChildren<FoodContainerUI>();
        storedFood = new GameObject[totalAvailableFood];

        if (spawnRawFood)
        {
            for (int i = 0; i < totalAvailableFood; i++)
            {
                int randomIndex = Random.Range(0, rawFoodPrefab.Length);
                storedFood[i] = Instantiate(rawFoodPrefab[randomIndex], transform.position, Quaternion.identity);
                storedFood[i].SetActive(false);
            }

        }
        else
        {
            for (int i = 0; i < totalAvailableFood; i++)
            {
                int randomIndex = Random.Range(0, snackPrefab.Length);
                storedFood[i] = Instantiate(snackPrefab[randomIndex], transform.position, Quaternion.identity);
                storedFood[i].SetActive(false);
            }
        }

        storedFood[currentFoodIndex].SetActive(true);
    }

    public GameObject GetFood()
    {
        if (isContainerEmpty)
        {
            Debug.Log("Container is empty.");
            return null;
        }

        if (isContainerLoading)
        {
            Debug.Log("Container is loading...");
            return null;
        }
        else
        {
            isContainerLoading = true;
            GameObject foodToReturn = storedFood[currentFoodIndex];
            StartCoroutine(nextFoodSpawn());

            return foodToReturn;
        }
    }

    private IEnumerator nextFoodSpawn()
    {
        currentFoodIndex++;
        if (currentFoodIndex >= storedFood.Length)
        {
            isContainerEmpty = true;
            containerUI.ShowOutOfStock();
            yield return null;
        }
        else
        {
            if (!isLowStockShown && currentFoodIndex >= Mathf.RoundToInt(storedFood.Length * lowStockPercentage))
            {
                isLowStockShown = true;
                containerUI.ShowLowStock();
            }

            yield return new WaitForSeconds(nextSpawnTime);

            storedFood[currentFoodIndex].SetActive(true);
            isContainerLoading = false;
        }
    }
}
