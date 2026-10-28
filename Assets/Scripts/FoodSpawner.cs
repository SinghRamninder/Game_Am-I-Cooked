using System.Collections;
using UnityEngine;

public class FoodSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] rawFoodPrefab;
    [SerializeField] private GameObject[] snackPrefab;
    [SerializeField] private int totalAvailableFood = 50;
    [SerializeField] private bool spawnRawFood = true;

    private GameObject[] storedFood;
    private float nextSpawnTime = 4f;
    private int currentFoodIndex = 0;
    private bool isContainerEmpty = false;
    private bool isContainerLoading = false;

    private void Start()
    {
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
            StartCoroutine(nextFoodSpawn());

            return storedFood[currentFoodIndex];
        }
    }

    private IEnumerator nextFoodSpawn()
    {
        currentFoodIndex++;
        if (currentFoodIndex >= storedFood.Length)
        {
            isContainerEmpty = true;
            yield return null;
        }
        else
        {
            yield return new WaitForSeconds(nextSpawnTime);

            storedFood[currentFoodIndex].SetActive(true);
            isContainerLoading = false;
        }
    }
}
