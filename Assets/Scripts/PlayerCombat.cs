using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Knockback Settings")]
    [Tooltip("Distance (in world units) from the player’s center to check for attack hits.")]
    public float attackRange = 2f;

    [Tooltip("Offset (in local-space) to move the sphere's origin (e.g. in front of the player).")]
    public Vector3 attackOffset = new Vector3(0f, 0.5f, 1f);

    [Tooltip("Horizontal impulse force applied to other players when hit.")]
    public float knockbackForce = 10f;

    [Tooltip("Vertical impulse force added on top of horizontal knockback.")]
    public float upwardForce = 4f;

    [Tooltip("LayerMask for what counts as a ‘player’ to knock back.")]
    public LayerMask playerLayer;

    [Header("Attack Visual (Runtime)")]
    [Tooltip("When true, spawns a temporary sphere to show attack range in Game view.")]
    public bool showAttackVisual = true;

    [Tooltip("Material used by the temporary sphere (should be transparent/unlit).")]
    public Material attackVisualMaterial;

    [Tooltip("How long (in seconds) the visual sphere remains before disappearing.")]
    public float visualDuration = 0.2f;

    private PlayerControls controls;
    private Rigidbody    rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Initialize the generated Input System C# class
        controls = new PlayerControls();

        // Subscribe to Attack.performed
        controls.Player.Attack.performed += ctx => OnAttack();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void OnAttack()
    {
        // 1) Compute world-space position of the “attack origin”:
        Vector3 origin = transform.position + transform.TransformDirection(attackOffset);

        // 2) (Optional) Spawn a temporary sphere to visualize the attack area
        if (showAttackVisual)
            StartCoroutine(SpawnVisualSphere(origin, attackRange, visualDuration));

        // 3) Detect all colliders within attackRange on the playerLayer
        Collider[] hits = Physics.OverlapSphere(origin, attackRange, playerLayer);

        foreach (Collider c in hits)
        {
            // Skip ourselves
            if (c.attachedRigidbody == rb)
                continue;

            // Compute horizontal knockback direction (normalized vector from attacker to target)
            Vector3 horizontalDir = (c.transform.position - transform.position)
                                    .WithY(0f) // ignore any vertical difference
                                    .normalized;

            Rigidbody otherRb = c.attachedRigidbody;
            if (otherRb != null && !otherRb.isKinematic)
            {
                // 4) Build an impulse that combines horizontal push + upward lift
                Vector3 impulse = horizontalDir * knockbackForce 
                                  + Vector3.up * upwardForce;

                otherRb.AddForce(impulse, ForceMode.Impulse);
            }
        }

        // (Optional) Play attack animation or VFX here
        // e.g. animator.SetTrigger("Attack");
    }

    private IEnumerator SpawnVisualSphere(Vector3 center, float range, float lifetime)
    {
        // 1) Create a new GameObject with a Sphere mesh
        GameObject sphereVis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphereVis.name = "AttackVisual";
        sphereVis.transform.position = center;

        // 2) Scale the sphere so its radius = 'range'
        //    Primitive Sphere has diameter = 1 unit by default, so scale = range * 2 on each axis
        float diameter = range * 2f;
        sphereVis.transform.localScale = new Vector3(diameter, diameter, diameter);

        // 3) Assign the transparent attackVisualMaterial (if provided)
        if (attackVisualMaterial != null)
        {
            MeshRenderer mr = sphereVis.GetComponent<MeshRenderer>();
            mr.material = attackVisualMaterial;
        }
        else
        {
            MeshRenderer mr = sphereVis.GetComponent<MeshRenderer>();
            Material tempMat = new Material(Shader.Find("Unlit/Color"));
            tempMat.SetColor("_Color", new Color(1f, 0f, 0f, 0.3f));
            mr.material = tempMat;
        }

        // 4) Disable its collider so it doesn't interact with the world
        Collider col = sphereVis.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // 5) Wait for 'lifetime' seconds, then destroy the visual
        yield return new WaitForSeconds(lifetime);
        Destroy(sphereVis);
    }
}

public static class Vector3Extensions
{
    public static Vector3 WithY(this Vector3 v, float newY)
    {
        return new Vector3(v.x, newY, v.z);
    }
}
