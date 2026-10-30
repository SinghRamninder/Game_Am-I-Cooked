using Unity.Netcode;
using UnityEngine;

public enum FoodType
{
    Raw,
    Snack
}

public class FoodManager : HoldableItem
{
    public FoodType foodType = FoodType.Raw;

    public NetworkVariable<bool> isCooked = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> isBurnt = new NetworkVariable<bool>(false);
    [HideInInspector] public float cookingProgress = 0f;

    [SerializeField] private Color cookedColor = new Color(0.85f, 0.55f, 0.35f);
    [SerializeField] private Color burntColor = new Color(0.22f, 0.18f, 0.16f);
    [SerializeField] private float colorChangeSpeed = 2f;
    [SerializeField] private ParticleSystem burntSmoke;

    private MeshRenderer[] foodRenderers;
    private MaterialPropertyBlock propertyBlock;
    private Color currentColor = Color.white;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        foodRenderers = GetComponentsInChildren<MeshRenderer>();
        propertyBlock = new MaterialPropertyBlock();
        currentColor = GetTargetColor();
        ApplyColor();

        isBurnt.OnValueChanged += OnBurntChanged;
        UpdateBurntSmoke();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        isBurnt.OnValueChanged -= OnBurntChanged;
    }

    private void Update()
    {
        if (foodRenderers == null) return;

        Color targetColor = GetTargetColor();
        if (currentColor == targetColor) return;

        currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorChangeSpeed);
        if (Mathf.Abs(currentColor.r - targetColor.r) + Mathf.Abs(currentColor.g - targetColor.g) + Mathf.Abs(currentColor.b - targetColor.b) < 0.01f)
        {
            currentColor = targetColor;
        }

        ApplyColor();
    }

    private Color GetTargetColor()
    {
        if (isBurnt.Value)
        {
            return burntColor;
        }

        if (isCooked.Value)
        {
            return cookedColor;
        }

        return Color.white;
    }

    private void ApplyColor()
    {
        foreach (MeshRenderer foodRenderer in foodRenderers)
        {
            foodRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", currentColor);
            foodRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void OnBurntChanged(bool oldValue, bool newValue)
    {
        UpdateBurntSmoke();
    }

    private void UpdateBurntSmoke()
    {
        if (burntSmoke == null) return;

        if (isBurnt.Value)
        {
            burntSmoke.Play();
        }
        else
        {
            burntSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
