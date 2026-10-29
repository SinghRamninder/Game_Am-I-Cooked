using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerHungerManager : NetworkBehaviour
{
    public static List<PlayerHungerManager> allPlayers = new List<PlayerHungerManager>();

    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float hungerIncreaseRate = 2f;

    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(false);

    private NetworkVariable<float> hungerLevel = new NetworkVariable<float>(0f);

    public override void OnNetworkSpawn()
    {
        allPlayers.Add(this);
        isDead.OnValueChanged += OnDeadChanged;

        if (isDead.Value)
        {
            DisablePlayer();
        }
    }

    public override void OnNetworkDespawn()
    {
        allPlayers.Remove(this);
        isDead.OnValueChanged -= OnDeadChanged;
    }

    private void Update()
    {
        if (!IsServer || isDead.Value) return;

        hungerLevel.Value += hungerIncreaseRate * Time.deltaTime;

        if (hungerLevel.Value >= maxHunger)
        {
            hungerLevel.Value = maxHunger;
            Die();
        }
    }

    public float ReduceHunger(float amount)
    {
        if (isDead.Value) return 0f;

        hungerLevel.Value = Mathf.Clamp(hungerLevel.Value - amount, 0f, maxHunger);
        Debug.Log(gameObject.name + " hunger reduced to " + hungerLevel.Value);

        return amount / maxHunger * 100f;
    }

    public void IncreaseHunger(float amount)
    {
        if (isDead.Value) return;

        hungerLevel.Value = Mathf.Clamp(hungerLevel.Value + amount, 0f, maxHunger);
        Debug.Log(gameObject.name + " hunger increased to " + hungerLevel.Value);
    }

    public float GetHungerLevel()
    {
        return hungerLevel.Value;
    }

    public float GetHungerPercentage()
    {
        return hungerLevel.Value / maxHunger;
    }

    private void Die()
    {
        PlayerInteraction playerInteraction = GetComponent<PlayerInteraction>();
        if (playerInteraction != null)
        {
            playerInteraction.TryDroppingFood(false);
        }

        isDead.Value = true;
    }

    private void OnDeadChanged(bool wasDead, bool nowDead)
    {
        if (nowDead)
        {
            DisablePlayer();
        }
    }

    private void DisablePlayer()
    {
        Debug.Log(gameObject.name + " died of hunger.");

        PlayerInteraction playerInteraction = GetComponent<PlayerInteraction>();
        if (playerInteraction != null)
        {
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
