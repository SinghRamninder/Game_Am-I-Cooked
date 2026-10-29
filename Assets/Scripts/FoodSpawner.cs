using System.Collections;
using Unity.Netcode;
using UnityEngine;

public enum StockState
{
    InStock,
    LowStock,
    OutOfStock
}

public class FoodSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject[] rawFoodPrefab;
    [SerializeField] private GameObject[] snackPrefab;
    [SerializeField] private int totalAvailableFood = 50;
    [SerializeField] private bool spawnRawFood = true;
    [SerializeField] private float nextSpawnTime = 5f;
    [SerializeField] private float lowStockPercentage = 0.8f;

    private FoodManager foodInContainer;
    private int currentFoodIndex = 0;
    private bool isContainerEmpty = false;
    private bool isContainerLoading = false;
    private FoodContainerUI containerUI;
    private NetworkVariable<StockState> stockState = new NetworkVariable<StockState>(StockState.InStock);

    private void Awake()
    {
        containerUI = GetComponentInChildren<FoodContainerUI>();
    }

    public override void OnNetworkSpawn()
    {
        stockState.OnValueChanged += OnStockStateChanged;
        ShowStockState(stockState.Value);
    }

    public void StartSpawning()
    {
        if (!IsServer || foodInContainer != null || isContainerEmpty) return;

        SpawnNextFood();
    }

    public override void OnNetworkDespawn()
    {
        stockState.OnValueChanged -= OnStockStateChanged;
    }

    private void SpawnNextFood()
    {
        GameObject foodPrefab;
        if (spawnRawFood)
        {
            foodPrefab = rawFoodPrefab[Random.Range(0, rawFoodPrefab.Length)];
        }
        else
        {
            foodPrefab = snackPrefab[Random.Range(0, snackPrefab.Length)];
        }

        GameObject food = Instantiate(foodPrefab, transform.position, Quaternion.identity);
        food.GetComponent<NetworkObject>().Spawn(true);

        foodInContainer = food.GetComponent<FoodManager>();
        foodInContainer.SetHolder(NetworkObject);
    }

    public FoodManager GetFood()
    {
        if (isContainerEmpty)
        {
            Debug.Log("Container is empty.");
            return null;
        }

        if (isContainerLoading || foodInContainer == null)
        {
            Debug.Log("Container is loading...");
            return null;
        }
        else
        {
            isContainerLoading = true;
            FoodManager foodToReturn = foodInContainer;
            foodInContainer = null;
            StartCoroutine(nextFoodSpawn());

            return foodToReturn;
        }
    }

    public bool IsContainerEmpty()
    {
        return isContainerEmpty;
    }

    private IEnumerator nextFoodSpawn()
    {
        currentFoodIndex++;
        if (currentFoodIndex >= totalAvailableFood)
        {
            isContainerEmpty = true;
            stockState.Value = StockState.OutOfStock;
            yield return null;
        }
        else
        {
            if (stockState.Value == StockState.InStock && currentFoodIndex >= Mathf.RoundToInt(totalAvailableFood * lowStockPercentage))
            {
                stockState.Value = StockState.LowStock;
            }

            yield return new WaitForSeconds(nextSpawnTime);

            SpawnNextFood();
            isContainerLoading = false;
        }
    }

    private void OnStockStateChanged(StockState oldState, StockState newState)
    {
        ShowStockState(newState);
    }

    private void ShowStockState(StockState state)
    {
        if (state == StockState.LowStock)
        {
            containerUI.ShowLowStock();
        }
        else if (state == StockState.OutOfStock)
        {
            containerUI.ShowOutOfStock();
        }
    }
}
