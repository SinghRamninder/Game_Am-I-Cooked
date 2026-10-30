using System.Collections;
using Unity.Netcode;
using UnityEngine;

public enum CookingState
{
    Idle,
    Cooking,
    Burning
}

public class CookingStation : NetworkBehaviour
{
    [SerializeField] private float cookingTime = 5f;
    [SerializeField] private float foodBurningTime = 10f;
    [SerializeField] private AudioClip sizzleSound;
    [SerializeField] private float sizzleVolume = 1f;
    [SerializeField] private float soundMinDistance = 2f;
    [SerializeField] private float soundMaxDistance = 22f;

    private bool isCooking = false;
    private AudioSource audioSource;
    private FoodManager foodOnStation;
    private Coroutine cookingCoroutine;
    private FloatingMessageUI messageUI;
    private CookingProgressUI progressUI;
    private NetworkVariable<CookingState> cookingState = new NetworkVariable<CookingState>(CookingState.Idle);
    private NetworkVariable<float> stationProgress = new NetworkVariable<float>(0f);

    private void Awake()
    {
        messageUI = GetComponentInChildren<FloatingMessageUI>();
        progressUI = GetComponentInChildren<CookingProgressUI>();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = soundMinDistance;
        audioSource.maxDistance = soundMaxDistance;
        audioSource.dopplerLevel = 0f;
    }

    public override void OnNetworkSpawn()
    {
        cookingState.OnValueChanged += OnCookingStateChanged;
        stationProgress.OnValueChanged += OnStationProgressChanged;
        UpdateProgressUI();
    }

    public override void OnNetworkDespawn()
    {
        cookingState.OnValueChanged -= OnCookingStateChanged;
        stationProgress.OnValueChanged -= OnStationProgressChanged;
    }

    public bool TryCookingFood(FoodManager food, ulong clientId)
    {
        if (isCooking)
        {
            ShowMessage(clientId, "Cooking station is already in use.");
            return false;
        }

        if (food.isBurnt.Value)
        {
            ShowMessage(clientId, "Food is already burnt.");
            return false;
        }
        else if (food.isCooked.Value)
        {
            ShowMessage(clientId, "Food is already cooked.");
            return false;
        }

        if (food.foodType == FoodType.Raw)
        {
            isCooking = true;
            foodOnStation = food;
            food.SetHolder(NetworkObject);
            cookingCoroutine = StartCoroutine(CookFood(food));
            PlaySizzleRpc();

            return true;
        }
        else
        {
            ShowMessage(clientId, "Food is not raw and cannot be cooked.");
            return false;
        }
    }

    public FoodManager TryTakingFood(ulong clientId)
    {
        if (foodOnStation == null)
        {
            ShowMessage(clientId, "No food on the cooking station.");
            return null;
        }

        if (cookingCoroutine != null)
        {
            StopCoroutine(cookingCoroutine);
            cookingCoroutine = null;
        }

        cookingState.Value = CookingState.Idle;

        FoodManager foodToReturn = foodOnStation;
        foodOnStation = null;
        isCooking = false;

        return foodToReturn;
    }

    private IEnumerator CookFood(FoodManager food)
    {
        Debug.Log("Cooking started.");
        cookingState.Value = CookingState.Cooking;

        while (food.cookingProgress < 1f)
        {
            food.cookingProgress += Time.deltaTime / cookingTime;
            stationProgress.Value = food.cookingProgress;
            yield return null;
        }

        food.cookingProgress = 1f;
        food.isCooked.Value = true;
        Debug.Log("Food is cooked.");

        cookingState.Value = CookingState.Burning;
        float burningTimer = 0f;
        while (burningTimer < foodBurningTime)
        {
            burningTimer += Time.deltaTime;
            stationProgress.Value = burningTimer / foodBurningTime;
            yield return null;
        }

        food.isBurnt.Value = true;
        Debug.Log("Food is burnt.");
        cookingState.Value = CookingState.Idle;
        cookingCoroutine = null;
    }

    private void ShowMessage(ulong clientId, string message)
    {
        ShowMessageRpc(message, RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void ShowMessageRpc(string message, RpcParams rpcParams)
    {
        messageUI.ShowMessage(message);
    }

    [Rpc(SendTo.Everyone)]
    private void PlaySizzleRpc()
    {
        audioSource.PlayOneShot(sizzleSound, sizzleVolume);
    }

    private void OnCookingStateChanged(CookingState oldState, CookingState newState)
    {
        UpdateProgressUI();
    }

    private void OnStationProgressChanged(float oldProgress, float newProgress)
    {
        UpdateProgressUI();
    }

    private void UpdateProgressUI()
    {
        if (cookingState.Value == CookingState.Cooking)
        {
            progressUI.ShowCookingProgress(stationProgress.Value);
        }
        else if (cookingState.Value == CookingState.Burning)
        {
            progressUI.ShowBurningProgress(stationProgress.Value);
        }
        else
        {
            progressUI.HideProgress();
        }
    }
}
