using UnityEngine;

public class PlayerHungerManager : MonoBehaviour
{
    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float hungerIncreaseRate = 2f;

    [HideInInspector] public bool isDead = false;

    private float hungerLevel = 0f;

    private void Update()
    {
        if (isDead) return;

        hungerLevel += hungerIncreaseRate * Time.deltaTime;

        if (hungerLevel >= maxHunger)
        {
            hungerLevel = maxHunger;
            Die();
        }
    }

    public void ReduceHunger(float amount)
    {
        if (isDead) return;

        hungerLevel = Mathf.Clamp(hungerLevel - amount, 0f, maxHunger);
        Debug.Log(gameObject.name + " hunger reduced to " + hungerLevel);
    }

    public void IncreaseHunger(float amount)
    {
        if (isDead) return;

        hungerLevel = Mathf.Clamp(hungerLevel + amount, 0f, maxHunger);
        Debug.Log(gameObject.name + " hunger increased to " + hungerLevel);
    }

    public float GetHungerLevel()
    {
        return hungerLevel;
    }

    public float GetHungerPercentage()
    {
        return hungerLevel / maxHunger;
    }

    private void Die()
    {
        isDead = true;
        Debug.Log(gameObject.name + " died of hunger.");

        PlayerInteraction playerInteraction = GetComponent<PlayerInteraction>();
        if (playerInteraction != null)
        {
            playerInteraction.TryDroppingFood(false);
            playerInteraction.enabled = false;
        }

        PlayerEating playerEating = GetComponent<PlayerEating>();
        if (playerEating != null)
        {
            playerEating.enabled = false;
        }

        PlayerMovement playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }
    }
}
