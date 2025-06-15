// PlayerMovementTurf.cs
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(PlayerInput))]
[RequireComponent(typeof(PlayerManager))]
public class PlayerMovementTurf : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed         = 5f;
    public float jumpForce         = 7f;
    public float rotationSpeed     = 360f;

    [Header("Ground Check")]
    public LayerMask groundLayerMask;
    public float     groundCheckOffset = 0.1f;

    [Header("Turf Penalty Settings")]
    public float turfCheckDistance = 1f;
    public float turfSpeedPenalty  = 0.5f;
    public float turfJumpPenalty   = 0.5f;

    // internal
    private Animator      animator;
    private Rigidbody     rb;
    private CapsuleCollider capsule;
    private PlayerInput    playerInput;
    private InputAction    moveAction;
    private InputAction    jumpAction;
    private Vector2        moveInput;
    private bool           jumpPressed;
    private PlayerManager  pm;

    private void Awake()
    {
        rb        = GetComponent<Rigidbody>();
        capsule   = GetComponent<CapsuleCollider>();
        animator  = GetComponent<Animator>();
        pm        = GetComponent<PlayerManager>();

        rb.constraints = RigidbodyConstraints.FreezeRotation;

        playerInput = GetComponent<PlayerInput>();
        moveAction  = playerInput.actions.FindAction("Move");
        jumpAction  = playerInput.actions.FindAction("Jump");

        moveAction.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        moveAction.canceled  += _   => moveInput = Vector2.zero;
        jumpAction.performed += _   => jumpPressed = true;
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
        animator.SetBool("IsRunning", moveInput.sqrMagnitude > 0.001f);
        animator.SetBool("IsGrounded", IsGrounded());
    }

    private void FixedUpdate()
    {
        bool onOwnTurf = TurfUtilities.IsOnOwnTurf(
                              transform,
                              pm.Color,
                              groundLayerMask,
                              turfCheckDistance
                          );

        float speedMul = onOwnTurf ? 1f : turfSpeedPenalty;
        float jumpMul  = onOwnTurf ? 1f : turfJumpPenalty;

        var inputDir = new Vector3(moveInput.x, 0f, moveInput.y);
        var moveVec  = (inputDir.sqrMagnitude > 1f ? inputDir.normalized : inputDir)
                         * (moveSpeed * speedMul);

        if (inputDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(inputDir.normalized);
            transform.rotation = Quaternion.RotateTowards(
                                     transform.rotation,
                                     targetRot,
                                     rotationSpeed * Time.fixedDeltaTime
                                 );
        }

        if (IsGrounded())
        {
            rb.linearVelocity = new Vector3(moveVec.x, rb.linearVelocity.y, moveVec.z);
            if (jumpPressed)
            {
                animator.SetTrigger("Jump");
                rb.AddForce(Vector3.up * (jumpForce * jumpMul), ForceMode.Impulse);
            }
        }

        jumpPressed = false;
    }

    private bool IsGrounded()
    {
        var worldCenter  = transform.TransformPoint(capsule.center);
        var bottomOffset = (capsule.height * 0.5f) - capsule.radius;
        var origin       = worldCenter + Vector3.down * bottomOffset;
        var radius       = capsule.radius + groundCheckOffset;

        return Physics.CheckSphere(
                   origin,
                   radius,
                   groundLayerMask,
                   QueryTriggerInteraction.Ignore
               );
    }
}
