using UnityEngine;
using UnityEngine.InputSystem;  // For PlayerInput/InputAction

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerMovementTurf : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Base movement speed (units/sec).")]
    public float moveSpeed = 5f;

    [Tooltip("Multiplier applied to moveSpeed when sprinting.")]
    public float sprintMultiplier = 1.5f;

    [Tooltip("Time in seconds for sprint to reach full speed.")]
    public float sprintRampUpTime = 2f;

    [Tooltip("Jump impulse force.")]
    public float jumpForce = 7f;

    [Tooltip("Rotation speed (degrees/sec) for smoothing.")]
    public float rotationSpeed = 360f;

    [Header("Ground Check")]
    [Tooltip("Layers considered as ground.")]
    public LayerMask groundLayerMask;

    [Tooltip("How far below the capsule to check for ground.")]
    public float groundCheckOffset = 0.1f;

    private Animator animator;
    private Rigidbody rb;
    private CapsuleCollider capsule;

    // INPUT via PlayerInput:
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    private Vector2 moveInput = Vector2.zero;
    private bool jumpPressed = false;
    private bool sprintPressed = false;

    // Sprint acceleration state:
    private float currentSprintMultiplier = 1f;
    private float sprintAccelerationRate;

    private void Awake()
    {
        rb       = GetComponent<Rigidbody>();
        capsule  = GetComponent<CapsuleCollider>();
        animator = GetComponent<Animator>();

        // Freeze X/Z rotation so we only rotate around Y
        rb.constraints = RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationY
                       | RigidbodyConstraints.FreezeRotationZ;

        // Calculate how fast we interpolate from 1 → sprintMultiplier
        sprintAccelerationRate = (sprintMultiplier - 1f) / sprintRampUpTime;

        // Set up PlayerInput and actions
        playerInput  = GetComponent<PlayerInput>();
        moveAction   = playerInput.actions.FindAction("Move");
        jumpAction   = playerInput.actions.FindAction("Jump");
        sprintAction = playerInput.actions.FindAction("Sprint");

        moveAction.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        moveAction.canceled  += ctx => moveInput = Vector2.zero;

        jumpAction.performed += ctx => jumpPressed = true;

        sprintAction.performed += ctx => sprintPressed = true;
        sprintAction.canceled  += ctx => sprintPressed = false;
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        sprintAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        sprintAction.Disable();
    }

    private void Update()
    {
        // Update animator flags
        bool isMoving  = moveInput.sqrMagnitude > 0.001f;
        bool grounded  = IsGrounded();
        animator.SetBool("IsRunning", isMoving);
        animator.SetBool("IsGrounded", grounded);
    }

    private void FixedUpdate()
    {
        // Build world‐space input vector
        Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y);

        // Handle sprint (now unlimited)
        if (sprintPressed && inputDir.sqrMagnitude > 0.01f)
        {
            currentSprintMultiplier += sprintAccelerationRate * Time.fixedDeltaTime;
            currentSprintMultiplier = Mathf.Min(currentSprintMultiplier, sprintMultiplier);
        }
        else
        {
            currentSprintMultiplier = 1f;
        }

        float speed     = moveSpeed * currentSprintMultiplier;
        Vector3 moveVec = (inputDir.sqrMagnitude > 1f)
                         ? inputDir.normalized * speed
                         : inputDir * speed;

        // Smooth rotation toward movement
        if (inputDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(inputDir.normalized);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.fixedDeltaTime
            );
        }

        bool groundedNow = IsGrounded();

        // Reset jump state on ground
        if (groundedNow)
            rb.linearVelocity = new Vector3(moveVec.x, rb.linearVelocity.y, moveVec.z);

        // Jump or apply gravity
        if (groundedNow)
        {
            if (jumpPressed)
            {
                animator.SetTrigger("Jump");
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }
        else
        {
            // In air, preserve horizontal/vertical separately
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, rb.linearVelocity.z);
            // no dash logic here anymore
        }

        jumpPressed = false;
    }

    private bool IsGrounded()
    {
        Vector3 worldCenter  = transform.TransformPoint(capsule.center);
        float   bottomOffset = (capsule.height * 0.5f) - capsule.radius;
        Vector3 bottomOrigin = worldCenter + Vector3.down * bottomOffset;
        float   radius       = capsule.radius + groundCheckOffset;

        return Physics.CheckSphere(
            bottomOrigin,
            radius,
            groundLayerMask,
            QueryTriggerInteraction.Ignore
        );
    }
}
