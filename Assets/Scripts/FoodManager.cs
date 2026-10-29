using UnityEngine;

public enum FoodType
{
    Raw,
    Snack
}

public class FoodManager : MonoBehaviour
{
    public FoodType foodType = FoodType.Raw;

    [HideInInspector] public bool isCooked = false;
    [HideInInspector] public bool isBurnt = false;
    [HideInInspector] public float cookingProgress = 0f;
}
