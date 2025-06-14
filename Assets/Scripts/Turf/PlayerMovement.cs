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

    [Tooltip("Jump impulse force.")]
    public float jumpForce = 7f;

    [Tooltip("Rotation speed (degrees/sec) for smoothing.")]
    public float rotationSpeed = 360f;

    [Header("Ground Check")]
    [Tooltip("Layers considered as ground.")]
    public LayerMask groundLayerMask;

    [Tooltip("How far below the capsule to check for ground.")]
    public float groundCheckOffset = 0.1f;

    [Header("Turf Penalty Settings")]
    [Tooltip("How far down to check turf colour.")]
    public float turfCheckDistance = 1f;

    [Tooltip("Speed multiplier when off own turf.")]
    public float turfSpeedPenalty = 0.5f;

    [Tooltip("Jump multiplier when off own turf.")]
    public float turfJumpPenalty = 0.5f;

    private Animator       animator;
    private Rigidbody      rb;
    private CapsuleCollider capsule;

    // INPUT via PlayerInput:
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;

    private Vector2 moveInput   = Vector2.zero;
    private bool    jumpPressed = false;

    // Cached player color for turf checks
    private SkinnedMeshRenderer playerRenderer;
    private Color    playerColor;

    private void Awake()
    {
        rb       = GetComponent<Rigidbody>();
        capsule  = GetComponent<CapsuleCollider>();
        animator = GetComponent<Animator>();

        // Freeze all rotations so physics doesn't topple us
        rb.constraints = RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationY
                       | RigidbodyConstraints.FreezeRotationZ;

        // Cache our colour
        Transform bodyTransform = transform.Find("Body.008");
        playerRenderer = bodyTransform.GetComponentInChildren<SkinnedMeshRenderer>();
        playerColor = playerRenderer.material.color;

        // Set up InputSystem actions
        playerInput  = GetComponent<PlayerInput>();
        moveAction   = playerInput.actions.FindAction("Move");
        jumpAction   = playerInput.actions.FindAction("Jump");

        moveAction.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        moveAction.canceled  += ctx => moveInput = Vector2.zero;
        jumpAction.performed += ctx => jumpPressed = true;
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
    }

    private void Update()
    {
        bool isMoving = moveInput.sqrMagnitude > 0.001f;
        bool grounded = IsGrounded();
        animator.SetBool("IsRunning", isMoving);
        animator.SetBool("IsGrounded", grounded);
    }

    private void FixedUpdate()
    {
        // Determine if we’re on our own turf
        bool onOwnTurf = IsOnOwnTurf();

        // Apply penalty multipliers if not on our turf
        float speedMul = onOwnTurf ? 1f : turfSpeedPenalty;
        float jumpMul  = onOwnTurf ? 1f : turfJumpPenalty;

        Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y);

        // Compute final move speed
        float speed = moveSpeed * speedMul;
        Vector3 moveVec = (inputDir.sqrMagnitude > 1f)
                         ? inputDir.normalized * speed
                         : inputDir * speed;

        // Smooth rotation toward movement direction
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

        if (groundedNow)
        {
            rb.linearVelocity = new Vector3(moveVec.x, rb.linearVelocity.y, moveVec.z);
            if (jumpPressed)
            {
                animator.SetTrigger("Jump");
                rb.AddForce(Vector3.up * (jumpForce * jumpMul), ForceMode.Impulse);
            }
        }
        else
        {
            // Preserve in-air velocity
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, rb.linearVelocity.z);
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

    private bool IsOnOwnTurf()
    {
        // Cast from just above the player's feet to avoid hitting the player's own collider
        Vector3 origin      = transform.position + Vector3.up * 0.1f;
        float   maxDistance = turfCheckDistance + 0.1f;

        RaycastHit hit;
        if (Physics.Raycast(
                origin,
                Vector3.down,
                out hit,
                maxDistance,
                groundLayerMask,
                QueryTriggerInteraction.Ignore))
        {
            // Only consider objects that have been painted
            var paintable = hit.collider.GetComponent<PaintableSurface>();
            if (paintable == null)
                return false;

            // Compare the painted colour to the player's colour
            var rend = hit.collider.GetComponent<Renderer>();
            return rend != null && rend.material.color == playerColor;
        }
        return false;
    }
}
