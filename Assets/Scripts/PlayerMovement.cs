// PlayerMovement.cs
// Handles player movement (walking, sprinting), jumping, rotating to face movement direction,
// and a one‐time air dash/dive feature that stays tilted until landing.

using UnityEngine;

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

    [Tooltip("Rotation speed (degrees/sec) for smoothing.")]
    public float rotationSpeed = 360f;

    [Header("Ground Check")]
    [Tooltip("Layers considered as ground.")]
    public LayerMask groundLayerMask;

    [Tooltip("How far below the capsule to check for ground.")]
    public float groundCheckOffset = 0.1f;

    [Header("Dash (Dive) Settings")]
    [Tooltip("Speed of the dash (units/sec).")]
    public float dashSpeed = 15f;

    [Tooltip("Duration of the dash in seconds.")]
    public float dashDuration = 0.2f;

    [Tooltip("When dashing in the air, what fraction of dashSpeed is applied upward.")]
    [Range(0f, 1f)]
    public float upwardDashFactor = 0.3f;

    [Tooltip("Tilt angle (in degrees) forward while dashing.")]
    public float dashTiltAngle = 15f;

    private Rigidbody       rb;
    private CapsuleCollider capsule;
    private InputSystem_Actions controls;

    private Vector2 moveInput     = Vector2.zero;
    private bool    jumpPressed   = false;
    private bool    sprintPressed = false;

    // Dash state:
    private bool    isDashing     = false;
    private float   dashTimeLeft  = 0f;
    private Vector3 dashDirection = Vector3.zero;
    private bool    hasDashed     = false;

    private void Awake()
    {
        rb      = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        // Freeze rotation on X/Z so we only rotate around Y manually
        rb.constraints = RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ;

        controls = new InputSystem_Actions();

        // Move input
        controls.Player.Move.performed += ctx => moveInput     = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled  += ctx => moveInput     = Vector2.zero;

        // Jump input
        controls.Player.Jump.performed += ctx => jumpPressed   = true;

        // Sprint input
        controls.Player.Sprint.performed += ctx => sprintPressed = true;
        controls.Player.Sprint.canceled  += ctx => sprintPressed = false;
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
        // If we are currently in a dash, handle dash movement & tilt until grounded
        if (isDashing)
        {
            HandleDashing();
            return;
        }

        // Otherwise, normal movement:

        // Build world‐space input direction
        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        // Calculate current move speed (account for sprint)
        float currentSpeed = moveSpeed * (sprintPressed ? sprintMultiplier : 1f);
        Vector3 worldMove = (inputDirection.sqrMagnitude > 1f)
            ? inputDirection.normalized * currentSpeed
            : inputDirection * currentSpeed;

        // Smoothly rotate toward movement direction (if any)
        if (inputDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(inputDirection.normalized);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.fixedDeltaTime
            );
        }

        // Ground check
        bool isGrounded = IsGrounded();

        // Reset dash availability when grounded
        if (isGrounded)
        {
            hasDashed = false;
        }

        if (isGrounded)
        {
            // On ground: apply horizontal movement and allow jumping
            rb.linearVelocity = new Vector3(worldMove.x, rb.linearVelocity.y, worldMove.z);

            if (jumpPressed)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }
        else
        {
            // In air: preserve existing velocity
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, rb.linearVelocity.z);

            // If Jump was pressed while airborne, attempt dash instead of jump
            if (jumpPressed)
            {
                TryStartDash();
            }
        }

        // Reset jumpPressed for next frame
        jumpPressed = false;
    }

    private void TryStartDash()
    {
        // Only allow a dash if:
        // - player is in the air (not grounded)
        // - player hasn’t dashed yet since last landing
        // - not already mid-dash
        if (IsGrounded() || hasDashed || isDashing)
        {
            return;
        }

        // Begin a new dash:
        isDashing    = true;
        dashTimeLeft = dashDuration;
        hasDashed    = true;

        // Determine dash direction: forward + small upward component
        Vector3 forward = transform.forward.normalized;
        dashDirection = (forward + Vector3.up * upwardDashFactor).normalized;

        // Immediately set velocity so dash “feels” instant:
        rb.linearVelocity = dashDirection * dashSpeed;
    }

    private void HandleDashing()
    {
        // If dashDuration not over, continue overriding velocity:
        if (dashTimeLeft > 0f)
        {
            rb.linearVelocity = dashDirection * dashSpeed;
            dashTimeLeft -= Time.fixedDeltaTime;
        }

        // Always tilt to face the dash direction while airborne:
        if (!IsGrounded())
        {
            // Compute yaw from dashDirection (so facing horizontal)
            float yaw = Mathf.Atan2(dashDirection.x, dashDirection.z) * Mathf.Rad2Deg;
            // Apply a forward-facing pitch of dashTiltAngle (negative for nose-down)
            transform.rotation = Quaternion.Euler(-dashTiltAngle, yaw, 0f);
        }
        else
        {
            // Landed: end the dash/tilt state
            isDashing = false;
        }
    }

    private bool IsGrounded()
    {
        Vector3 worldCenter   = transform.TransformPoint(capsule.center);
        float   bottomOffset  = (capsule.height * 0.5f) - capsule.radius;
        Vector3 bottomOrigin  = worldCenter + Vector3.down * bottomOffset;
        float   sphereRadius  = capsule.radius + groundCheckOffset;

        return Physics.CheckSphere(
            bottomOrigin,
            sphereRadius,
            groundLayerMask,
            QueryTriggerInteraction.Ignore
        );
    }
}
