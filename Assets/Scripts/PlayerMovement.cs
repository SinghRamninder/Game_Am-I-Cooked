using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 15f;

    private Rigidbody rb;
    private Animator animator;
    private InputSystem_Actions controls;
    private Vector2 movement;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        controls = new InputSystem_Actions();
    }

    void OnEnable()
    {
        controls.Player.Enable();
    }

    void OnDisable()
    {
        controls.Player.Disable();

        if (animator != null)
        {
            animator.SetBool("isWalking", false);
        }
    }

    void Update()
    {
        movement = controls.Player.Move.ReadValue<Vector2>();

        if (animator != null)
        {
            animator.SetBool("isWalking", movement != Vector2.zero);
        }
    }

    void FixedUpdate()
    {
        Vector3 direction = new Vector3(movement.x, 0f, movement.y);
        rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }
    }
}
