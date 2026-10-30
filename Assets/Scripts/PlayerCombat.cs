using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public enum StunState
{
    None,
    Stunned,
    KnockedDown
}

public class PlayerCombat : NetworkBehaviour
{
    [SerializeField] private float slapRange = 1.6f;
    [SerializeField] private float objectHitRange = 2f;
    [SerializeField] private float attackCooldown = 0.45f;
    [SerializeField] private float slapForce = 4f;
    [SerializeField] private float objectForcePerMass = 2.5f;
    [SerializeField] private float heavyObjectMass = 5f;
    [SerializeField] private int slapsToStun = 4;
    [SerializeField] private float slapStunWindow = 3f;
    [SerializeField] private float stunDuration = 1.5f;
    [SerializeField] private float knockdownDuration = 2.2f;
    [SerializeField] private float dropForce = 4f;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private Transform modelTransform;
    [SerializeField] private ParticleSystem stunStars;
    [SerializeField] private float shakeAmount = 0.06f;
    [SerializeField] private float shakeSpeed = 45f;

    public NetworkVariable<StunState> stunState = new NetworkVariable<StunState>(StunState.None);

    private InputSystem_Actions controls;
    private PlayerInteraction playerInteraction;
    private PlayerMovement playerMovement;
    private PlayerHungerManager playerHungerManager;
    private Animator animator;
    private PlayerRagdoll playerRagdoll;
    private PlayerThrowing playerThrowing;
    private Vector3 modelStartPosition;
    private List<float> recentSlapTimes = new List<float>();
    private float nextAttackTime = 0f;
    private float stunEndTime = 0f;

    private void Awake()
    {
        controls = new InputSystem_Actions();
        playerInteraction = GetComponent<PlayerInteraction>();
        playerMovement = GetComponent<PlayerMovement>();
        playerHungerManager = GetComponent<PlayerHungerManager>();
        animator = GetComponentInChildren<Animator>();
        playerRagdoll = GetComponent<PlayerRagdoll>();
        playerThrowing = GetComponent<PlayerThrowing>();
        modelStartPosition = modelTransform.localPosition;
    }

    public override void OnNetworkSpawn()
    {
        stunState.OnValueChanged += OnStunStateChanged;
        ShowStunEffect(stunState.Value == StunState.Stunned);
    }

    public override void OnNetworkDespawn()
    {
        stunState.OnValueChanged -= OnStunStateChanged;
    }

    private void Update()
    {
        if (IsLocalPlayer && controls.Player.Attack.WasPressedThisFrame() && !playerThrowing.IsBlockingInput())
        {
            TryAttacking();
        }

        if (IsServer && IsStunned() && Time.time >= stunEndTime)
        {
            stunState.Value = StunState.None;
        }

        if (stunState.Value == StunState.Stunned)
        {
            float time = Time.time * shakeSpeed;
            modelTransform.localPosition = modelStartPosition + new Vector3(Mathf.Sin(time) * shakeAmount, 0f, Mathf.Cos(time * 1.3f) * shakeAmount * 0.5f);
        }
    }

    public bool IsStunned()
    {
        return stunState.Value != StunState.None;
    }

    public void TryAttacking()
    {
        if (IsStunned()) return;

        PlayerCombat target = FindClosestTarget();
        NetworkObject targetObject = null;

        if (target != null)
        {
            targetObject = target.NetworkObject;

            Vector3 lookDirection = target.transform.position - transform.position;
            lookDirection.y = 0f;
            if (lookDirection != Vector3.zero)
            {
                playerMovement.FaceDirection(lookDirection);
            }
        }

        playerInteraction.SendRequest(InteractionType.Attack, targetObject);
    }

    private PlayerCombat FindClosestTarget()
    {
        float range = slapRange;
        if (playerInteraction.IsHoldingObject())
        {
            range = objectHitRange;
        }

        PlayerCombat closestTarget = null;
        float closestDistance = range;

        foreach (PlayerHungerManager player in PlayerHungerManager.allPlayers)
        {
            if (player == null || player.gameObject == gameObject || player.isDead.Value) continue;

            Vector3 offset = player.transform.position - transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = player.GetComponent<PlayerCombat>();
            }
        }

        return closestTarget;
    }

    public bool TryAttackingOnServer(NetworkObject target)
    {
        if (Time.time < nextAttackTime) return false;

        if (playerInteraction.GetFoodInHand() != null)
        {
            playerInteraction.ShowMessage("You can't slap with food in your hands.");
            return false;
        }

        nextAttackTime = Time.time + attackCooldown;

        KitchenObject weapon = playerInteraction.GetObjectInHand();
        float range = slapRange;
        float weaponMass = 0f;
        if (weapon != null)
        {
            range = objectHitRange;
            weaponMass = weapon.GetMass();
        }

        PlayerCombat targetCombat = null;
        if (target != null)
        {
            targetCombat = target.GetComponent<PlayerCombat>();
        }

        bool isHit = targetCombat != null && targetCombat != this && !targetCombat.playerHungerManager.isDead.Value && IsInReach(target.transform, range);

        Vector3 hitPoint = transform.position + transform.forward * 0.8f + Vector3.up * 1.1f;
        Vector3 hitDirection = transform.forward;
        if (isHit)
        {
            hitDirection = target.transform.position - transform.position;
            hitDirection.y = 0f;
            hitDirection.Normalize();
            hitPoint = target.transform.position - hitDirection * 0.3f + Vector3.up * 1.1f;
        }

        PlayAttackRpc(isHit, hitPoint, weaponMass);

        if (!isHit) return false;

        if (weapon == null)
        {
            targetCombat.ReceiveSlap(hitDirection);
        }
        else
        {
            targetCombat.ReceiveObjectHit(hitDirection, weaponMass);
        }

        return true;
    }

    private bool IsInReach(Transform target, float range)
    {
        Vector3 offset = target.position - transform.position;
        offset.y = 0f;
        float allowedReach = NetworkLagHandler.Instance.GetAllowedReach(OwnerClientId, range, playerMovement.moveSpeed);

        return offset.magnitude <= allowedReach;
    }

    private void ReceiveSlap(Vector3 direction)
    {
        recentSlapTimes.Add(Time.time);
        for (int i = recentSlapTimes.Count - 1; i >= 0; i--)
        {
            if (Time.time - recentSlapTimes[i] > slapStunWindow)
            {
                recentSlapTimes.RemoveAt(i);
            }
        }

        ApplyKnockbackRpc(direction * slapForce);

        if (recentSlapTimes.Count >= slapsToStun)
        {
            recentSlapTimes.Clear();
            playerInteraction.TryDroppingItem(direction * dropForce + Vector3.up * dropForce);
            Stun(stunDuration);
        }
    }

    private void ReceiveObjectHit(Vector3 direction, float weaponMass)
    {
        Vector3 force = direction * (slapForce + weaponMass * objectForcePerMass);

        if (weaponMass >= heavyObjectMass)
        {
            playerInteraction.TryDroppingItem(direction * dropForce + Vector3.up * dropForce);
            KnockDown(force);
        }
        else
        {
            ApplyKnockbackRpc(force);
            playerInteraction.TryDroppingFoodWithForce(direction * dropForce + Vector3.up * dropForce);
        }
    }

    public void ReceiveThrownHit(Vector3 direction, float objectMass, Vector3 hitPoint)
    {
        PlayHitEffectRpc(hitPoint, objectMass);
        ReceiveObjectHit(direction, objectMass);
    }

    private void Stun(float duration)
    {
        stunEndTime = Time.time + duration;
        stunState.Value = StunState.Stunned;
    }

    private void KnockDown(Vector3 force)
    {
        stunEndTime = Time.time + knockdownDuration;
        stunState.Value = StunState.KnockedDown;
        KnockDownRpc(force);
    }

    [Rpc(SendTo.Everyone)]
    private void KnockDownRpc(Vector3 force)
    {
        playerRagdoll.Fall(force, IsOwner);
    }

    [Rpc(SendTo.Owner)]
    private void ApplyKnockbackRpc(Vector3 force)
    {
        playerMovement.AddKnockback(force);
    }

    [Rpc(SendTo.Everyone)]
    private void PlayAttackRpc(bool isHit, Vector3 hitPoint, float weaponMass)
    {
        animator.SetTrigger("slap");

        if (isHit)
        {
            SpawnHitEffect(hitPoint, weaponMass);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void PlayHitEffectRpc(Vector3 hitPoint, float weaponMass)
    {
        SpawnHitEffect(hitPoint, weaponMass);
    }

    private void SpawnHitEffect(Vector3 hitPoint, float weaponMass)
    {
        GameObject hitEffect = Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
        hitEffect.transform.localScale = Vector3.one * (1f + weaponMass * 0.15f);
    }

    private void OnStunStateChanged(StunState oldState, StunState newState)
    {
        ShowStunEffect(newState == StunState.Stunned);
    }

    private void ShowStunEffect(bool show)
    {
        if (show)
        {
            stunStars.Play();
        }
        else
        {
            stunStars.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            modelTransform.localPosition = modelStartPosition;
        }
    }

    public override void OnDestroy()
    {
        controls.Dispose();
        base.OnDestroy();
    }

    void OnEnable()
    {
        controls.Player.Enable();
    }

    void OnDisable()
    {
        controls.Player.Disable();
    }
}
