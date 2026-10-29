using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 15f;

    private Rigidbody rb;
    private Animator animator;
    private InputSystem_Actions controls;
    private Vector2 movement;
    private NetworkVariable<bool> isWalking = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        controls = new InputSystem_Actions();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            rb.position = transform.position;
            rb.rotation = transform.rotation;

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

    void Update()
    {
        if (IsOwner)
        {
            movement = controls.Player.Move.ReadValue<Vector2>();

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

        if (direction != Vector3.zero)
        {
            rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }
    }
}
