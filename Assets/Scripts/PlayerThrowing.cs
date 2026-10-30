using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PlayerThrowing : NetworkBehaviour
{
    [SerializeField] private LineRenderer throwLine;
    [SerializeField] private float chargeTime = 1f;
    [SerializeField] private float minThrowDistance = 1.5f;
    [SerializeField] private float maxThrowDistance = 12f;
    [SerializeField] private float massDistanceFactor = 0.25f;
    [SerializeField] private float baseFlightTime = 0.3f;
    [SerializeField] private float flightTimePerMeter = 0.07f;
    [SerializeField] private int linePointCount = 30;

    private InputSystem_Actions controls;
    private PlayerInteraction playerInteraction;
    private PlayerCombat playerCombat;
    private PlayerHungerManager playerHungerManager;
    private Animator animator;
    private Vector3[] linePoints;
    private int lineMask;
    private bool isCharging = false;
    private float chargeStartTime = 0f;
    private int cancelledFrame = -1;

    private void Awake()
    {
        controls = new InputSystem_Actions();
        playerInteraction = GetComponent<PlayerInteraction>();
        playerCombat = GetComponent<PlayerCombat>();
        playerHungerManager = GetComponent<PlayerHungerManager>();
        animator = GetComponentInChildren<Animator>();
        linePoints = new Vector3[linePointCount];

        int itemLayer = LayerMask.NameToLayer("Food");
        for (int layer = 0; layer < 32; layer++)
        {
            if (!Physics.GetIgnoreLayerCollision(itemLayer, layer))
            {
                lineMask |= 1 << layer;
            }
        }

        throwLine.enabled = false;
    }

    private void Update()
    {
        if (IsLocalPlayer)
        {
            ReadThrowInput();
        }

        if (!isCharging) return;

        if (CanThrow())
        {
            UpdateThrowLine();
        }
        else
        {
            CancelThrow();
        }
    }

    private void ReadThrowInput()
    {
        if (controls.Player.Throw.WasPressedThisFrame())
        {
            StartCharging();
            return;
        }

        if (!isCharging) return;

        if (IsOtherButtonPressed())
        {
            CancelThrow();
        }
        else if (controls.Player.Throw.WasReleasedThisFrame())
        {
            ReleaseThrow();
        }
    }

    private bool IsOtherButtonPressed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            foreach (KeyControl key in keyboard.allKeys)
            {
                if (key != null && key.wasPressedThisFrame && !IsMoveKey(key))
                {
                    return true;
                }
            }
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame || mouse.backButton.wasPressedThisFrame || mouse.forwardButton.wasPressedThisFrame)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsMoveKey(InputControl key)
    {
        foreach (InputControl moveControl in controls.Player.Move.controls)
        {
            if (moveControl == key)
            {
                return true;
            }
        }

        return false;
    }

    public void StartCharging()
    {
        if (!CanThrow()) return;

        isCharging = true;
        chargeStartTime = Time.time;
        throwLine.enabled = true;
        UpdateThrowLine();
    }

    public void ReleaseThrow()
    {
        if (!isCharging) return;

        float charge = GetCharge();
        Vector3 direction = transform.forward;
        StopCharging();

        ThrowRpc(direction, charge);
    }

    public void CancelThrow()
    {
        if (!isCharging) return;

        StopCharging();
        cancelledFrame = Time.frameCount;
    }

    public bool IsCharging()
    {
        return isCharging;
    }

    public bool IsBlockingInput()
    {
        return isCharging || cancelledFrame == Time.frameCount;
    }

    private void StopCharging()
    {
        isCharging = false;
        throwLine.enabled = false;
    }

    private bool CanThrow()
    {
        return playerInteraction.GetHeldKitchenObject() != null && !playerCombat.IsStunned() && !playerHungerManager.isDead.Value;
    }

    private float GetCharge()
    {
        return Mathf.Clamp01((Time.time - chargeStartTime) / chargeTime);
    }

    private float GetThrowDistance(float mass, float charge)
    {
        float distanceForMass = maxThrowDistance / (1f + mass * massDistanceFactor);
        distanceForMass = Mathf.Max(distanceForMass, minThrowDistance);

        return Mathf.Lerp(minThrowDistance, distanceForMass, charge);
    }

    private float GetFlightTime(float distance)
    {
        return baseFlightTime + distance * flightTimePerMeter;
    }

    private Vector3 GetThrowVelocity(Vector3 direction, float distance, float startHeight)
    {
        float gravity = -Physics.gravity.y;
        float flightTime = GetFlightTime(distance);
        float upSpeed = (0.5f * gravity * flightTime * flightTime - startHeight) / flightTime;

        return direction * (distance / flightTime) + Vector3.up * upSpeed;
    }

    private Vector3 GetFlatDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = transform.forward;
            direction.y = 0f;
        }

        return direction.normalized;
    }

    private void UpdateThrowLine()
    {
        KitchenObject item = playerInteraction.GetHeldKitchenObject();
        Vector3 start = playerInteraction.GetHoldPoint().position;
        float distance = GetThrowDistance(item.GetMass(), GetCharge());
        float flightTime = GetFlightTime(distance);
        Vector3 velocity = GetThrowVelocity(GetFlatDirection(transform.forward), distance, start.y - transform.position.y);

        int pointCount = linePoints.Length;
        linePoints[0] = start;

        for (int i = 1; i < linePoints.Length; i++)
        {
            float time = flightTime * i / (linePoints.Length - 1);
            Vector3 point = start + velocity * time + 0.5f * Physics.gravity * time * time;

            Vector3 previous = linePoints[i - 1];
            RaycastHit hit;
            if (Physics.Linecast(previous, point, out hit, lineMask, QueryTriggerInteraction.Ignore))
            {
                linePoints[i] = hit.point;
                pointCount = i + 1;
                break;
            }

            linePoints[i] = point;
        }

        throwLine.positionCount = pointCount;
        throwLine.SetPositions(linePoints);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void ThrowRpc(Vector3 direction, float charge)
    {
        if (playerHungerManager.isDead.Value || playerCombat.IsStunned()) return;

        KitchenObject item = playerInteraction.GetObjectInHand();
        if (item == null) return;

        Vector3 throwDirection = GetFlatDirection(direction);
        Vector3 start = playerInteraction.GetHoldPoint().position;
        float distance = GetThrowDistance(item.GetMass(), Mathf.Clamp01(charge));
        Vector3 velocity = GetThrowVelocity(throwDirection, distance, start.y - transform.position.y);

        playerInteraction.TryDroppingItem(Vector3.zero);
        item.Throw(playerCombat, start, velocity);
        PlayThrowRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void PlayThrowRpc()
    {
        animator.SetTrigger("slap");
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
        StopCharging();
    }
}
