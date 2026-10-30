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
}
