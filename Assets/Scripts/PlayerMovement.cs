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

    [Tooltip("Dive Settings")]
    public float diveForce = 10f;
    public float diveCooldown = 1f;
    public float diveDuration = 0.4f;


    private Rigidbody rb;
    private CapsuleCollider capsule;
    private PlayerControls controls;

    // Raw input values:
    private Vector2 moveInput = Vector2.zero;
    private bool jumpPressed = false;
    private bool sprintPressed = false;
    private bool diveRequested = false;    // Has player just pressed dive?
    private bool isDiving = false;         // Are we currently diving?
    private float diveEndTime = 0f;        // When the dive should stop
    private float lastDiveTime = -Mathf.Infinity; // When the last dive happened

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

        controls.Player.Dive.performed += ctx =>
        {
            if (Time.time >= diveCooldown + lastDiveTime)
            {
                diveRequested = true;

            }
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

        // Are we in the middle of a dive?
        if (isDiving)
        {
            if (Time.time < diveEndTime)
                return; // still diving — skip normal movement
            else
                isDiving = false; // dive ended
        }
        if (diveRequested)
        {
            isDiving = true;
            diveRequested = false;
            lastDiveTime = Time.time;
            diveEndTime = Time.time + diveDuration;

            Vector3 diveDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
            if (diveDirection.magnitude == 0f)
            {
                // Default direction if no input (e.g., falling straight down)
                diveDirection = transform.forward;
            }
            diveDirection = diveDirection.normalized;

            Vector3 finalVelocity = diveDirection * diveForce;

            if (!CheckIfGrounded())
            {
                // Aerial dive – add downward force to simulate dive drop
                finalVelocity.y = -diveForce * 0.2f;
            }
            else
            {
                // Ground dash – preserve current Y velocity (jumping etc.)
                finalVelocity.y = rb.linearVelocity.y;
            }

            rb.linearVelocity = finalVelocity;
            return;
        }



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
