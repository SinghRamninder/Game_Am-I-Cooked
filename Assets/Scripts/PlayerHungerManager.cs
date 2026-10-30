using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerHungerManager : NetworkBehaviour
{
    public static List<PlayerHungerManager> allPlayers = new List<PlayerHungerManager>();

    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float hungerIncreaseRate = 2f;
    [SerializeField] private float hungerPerCarriedMass = 0.15f;

    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(false);
    [HideInInspector] public int deathFrame = -1;

    private NetworkVariable<float> hungerLevel = new NetworkVariable<float>(0f);
    private PlayerInteraction playerInteraction;
    private PlayerRagdoll playerRagdoll;
    private PlayerCombat playerCombat;
    private Collider bodyCollider;

    private void Awake()
    {
        playerInteraction = GetComponent<PlayerInteraction>();
        playerRagdoll = GetComponent<PlayerRagdoll>();
        playerCombat = GetComponent<PlayerCombat>();
        bodyCollider = GetComponent<Collider>();
    }

    public override void OnNetworkSpawn()
    {
        allPlayers.Add(this);
        isDead.OnValueChanged += OnDeadChanged;

        if (isDead.Value)
        {
            DisablePlayer();
        }
        else if (IntroSequence.Instance != null && IntroSequence.Instance.IsLocked())
        {
            FreezeControls();
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
        if (GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver()) return;
        if (IntroSequence.Instance != null && IntroSequence.Instance.IsLocked()) return;

        float carriedMass = playerInteraction.GetCarriedObjectMass();
        hungerLevel.Value += (hungerIncreaseRate + carriedMass * hungerPerCarriedMass) * Time.deltaTime;

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

        return amount / maxHunger * 100f;
    }

    public void IncreaseHunger(float amount)
    {
        if (isDead.Value) return;

        hungerLevel.Value = Mathf.Clamp(hungerLevel.Value + amount, 0f, maxHunger);
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

        playerCombat.ClearStun();
        deathFrame = Time.frameCount;
        isDead.Value = true;
    }

    public void Revive()
    {
        if (!IsServer) return;

        hungerLevel.Value = maxHunger - 1f;
        deathFrame = -1;
        isDead.Value = false;
    }

    private void OnDeadChanged(bool wasDead, bool nowDead)
    {
        if (nowDead)
        {
            DisablePlayer();
        }
        else
        {
            SetPlayerEnabled(true);
            playerRagdoll.PlayRevive();
        }
    }

    private void DisablePlayer()
    {
        SetPlayerEnabled(false);
        playerRagdoll.PlayDeath();
    }

    public void FreezeControls()
    {
        SetControlsEnabled(false);
    }

    public void UnfreezeControls()
    {
        SetControlsEnabled(!isDead.Value);
    }

    private void SetPlayerEnabled(bool isEnabled)
    {
        bodyCollider.enabled = isEnabled;
        SetControlsEnabled(isEnabled);
    }

    private void SetControlsEnabled(bool isEnabled)
    {
        if (isEnabled && GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver())
        {
            isEnabled = false;
        }

        if (isEnabled && IntroSequence.Instance != null && IntroSequence.Instance.IsLocked())
        {
            isEnabled = false;
        }

        PlayerThrowing playerThrowing = GetComponent<PlayerThrowing>();
        if (playerThrowing != null)
        {
            playerThrowing.enabled = isEnabled;
        }

        PlayerInteraction playerInteraction = GetComponent<PlayerInteraction>();
        if (playerInteraction != null)
        {
            playerInteraction.enabled = isEnabled;
        }

        PlayerEating playerEating = GetComponent<PlayerEating>();
        if (playerEating != null)
        {
            playerEating.enabled = isEnabled;
        }

        PlayerMovement playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = isEnabled;
        }

        PlayerCombat playerCombat = GetComponent<PlayerCombat>();
        if (playerCombat != null)
        {
            playerCombat.enabled = isEnabled;
        }
    }
}
