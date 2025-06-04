using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Base movement speed (units/sec).")]
    public float moveSpeed = 5f;

    [Tooltip("Multiplier applied to moveSpeed when sprinting.")]
    public float sprintMultiplier = 1.5f;

    [Tooltip("Jump impulse force.")]
    public float jumpForce = 7f;

    [Tooltip("How far below the capsule to check for ground.")]
    public float groundCheckOffset = 0.1f;

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private PlayerControls controls;

    // Raw input values:
    private Vector2 moveInput = Vector2.zero;
    private bool jumpPressed = false;
    private bool sprintPressed = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        // Prevent tipping over:
        rb.constraints = RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ;

        // Initialize Input System
        controls = new PlayerControls();

        // Move callbacks:
        controls.Player.Move.performed += ctx =>
        {
            moveInput = ctx.ReadValue<Vector2>();
        };
        controls.Player.Move.canceled += ctx =>
        {
            moveInput = Vector2.zero;
        };

        // Jump callback:
        controls.Player.Jump.performed += ctx =>
        {
            jumpPressed = true;
        };

        // Sprint callbacks:
        controls.Player.Sprint.performed += ctx =>
        {
            sprintPressed = true;
        };
        controls.Player.Sprint.canceled += ctx =>
        {
            sprintPressed = false;
        };
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void FixedUpdate()
    {
        // 1. Base horizontal move vector (local space)
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        // 2. Determine current speed (sprint or walk)
        float currentSpeed = moveSpeed;
        if (sprintPressed)
            currentSpeed *= sprintMultiplier;

        // 3. Convert to world space and scale by deltaTime after assigning to velocity
        Vector3 worldMove = transform.TransformDirection(moveDirection) * currentSpeed;

        // 4. Check if grounded
        bool isGrounded = CheckIfGrounded();

        // 5. Build new velocity (preserve existing vertical velocity until jump)
        Vector3 newVelocity = new Vector3(
            worldMove.x,
            rb.linearVelocity.y,
            worldMove.z
        );

        // 6. Jump if requested & grounded
        if (jumpPressed && isGrounded)
        {
            newVelocity.y = jumpForce;
            jumpPressed = false; // consume the jump
        }

        // 7. Assign to Rigidbody
        rb.linearVelocity = newVelocity;
    }

    private bool CheckIfGrounded()
    {
        Vector3 origin = transform.position + capsule.center;
        float rayLength = (capsule.height * 0.5f) + groundCheckOffset;

        return Physics.Raycast(origin, Vector3.down, rayLength);
    }
}
