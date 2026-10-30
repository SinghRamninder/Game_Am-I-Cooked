using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 15f;
    [SerializeField] private float knockbackDamping = 6f;
    [SerializeField] private float skinWidth = 0.05f;
    [SerializeField] private float stepHeight = 0.15f;

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private int collisionMask;
    private Animator animator;
    private InputSystem_Actions controls;
    private PlayerCombat playerCombat;
    private Vector2 movement;
    private Vector3 knockbackVelocity;
    private NetworkVariable<bool> isWalking = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        animator = GetComponentInChildren<Animator>();
        playerCombat = GetComponent<PlayerCombat>();
        controls = new InputSystem_Actions();

        for (int layer = 0; layer < 32; layer++)
        {
            if (!Physics.GetIgnoreLayerCollision(gameObject.layer, layer))
            {
                collisionMask |= 1 << layer;
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        rb.isKinematic = true;

        if (IsOwner)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.position = transform.position;
            rb.rotation = transform.rotation;
        }
        else
        {
            rb.interpolation = RigidbodyInterpolation.None;
        }

        if (IsLocalPlayer)
        {
            PlayerCamera playerCamera = Camera.main.GetComponent<PlayerCamera>();
            if (playerCamera != null)
            {
                playerCamera.SetTarget(transform);
            }
        }
    }

    void OnEnable()
    {
        controls.Player.Enable();
    }

    void OnDisable()
    {
        controls.Player.Disable();
        movement = Vector2.zero;

        if (IsSpawned && IsOwner)
        {
            isWalking.Value = false;
        }

        if (animator != null)
        {
            animator.SetBool("isWalking", false);
        }
    }

    public override void OnDestroy()
    {
        controls.Dispose();
        base.OnDestroy();
    }

    public void SetMoveInput(Vector2 input)
    {
        movement = input;
    }

    public void AddKnockback(Vector3 force)
    {
        force.y = 0f;
        knockbackVelocity += force;
    }

    public void FaceDirection(Vector3 direction)
    {
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        rb.rotation = lookRotation;
        transform.rotation = lookRotation;
    }

    void Update()
    {
        if (IsLocalPlayer)
        {
            movement = controls.Player.Move.ReadValue<Vector2>();
        }

        if (playerCombat.IsStunned())
        {
            movement = Vector2.zero;
        }

        if (IsOwner)
        {
            bool walking = movement != Vector2.zero;
            if (isWalking.Value != walking)
            {
                isWalking.Value = walking;
            }
        }

        if (animator != null)
        {
            animator.SetBool("isWalking", isWalking.Value);
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        Vector3 direction = new Vector3(movement.x, 0f, movement.y);
        Vector3 velocity = direction * moveSpeed + knockbackVelocity;

        if (velocity != Vector3.zero)
        {
            rb.MovePosition(SlideMove(rb.position, velocity * Time.fixedDeltaTime));
        }

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }

        knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDamping * Time.fixedDeltaTime);
        if (knockbackVelocity.magnitude < 0.05f)
        {
            knockbackVelocity = Vector3.zero;
        }
    }

    private Vector3 SlideMove(Vector3 startPosition, Vector3 move)
    {
        Vector3 position = startPosition;

        for (int i = 0; i < 3; i++)
        {
            float distance = move.magnitude;
            if (distance < 0.0001f) break;

            Vector3 direction = move / distance;
            RaycastHit hit;
            if (!CastCapsule(position, direction, distance + skinWidth, out hit))
            {
                position += move;
                break;
            }

            float allowedDistance = Mathf.Max(hit.distance - skinWidth, 0f);
            position += direction * allowedDistance;

            Vector3 wallNormal = hit.normal;
            wallNormal.y = 0f;
            if (wallNormal == Vector3.zero) break;

            move = Vector3.ProjectOnPlane(move - direction * allowedDistance, wallNormal.normalized);
        }

        return position;
    }

    private bool CastCapsule(Vector3 position, Vector3 direction, float distance, out RaycastHit hit)
    {
        float radius = capsule.radius * 0.95f;
        Vector3 center = position + capsule.center;
        float halfLine = capsule.height * 0.5f - capsule.radius;
        Vector3 top = center + Vector3.up * halfLine;
        Vector3 bottom = center - Vector3.up * halfLine + Vector3.up * stepHeight;

        return Physics.CapsuleCast(bottom, top, radius, direction, out hit, distance, collisionMask, QueryTriggerInteraction.Ignore);
    }
}
