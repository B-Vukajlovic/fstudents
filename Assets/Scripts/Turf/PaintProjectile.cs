using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PaintProjectile : MonoBehaviour
{
    [Header("Ground-Paint Settings")]
    public LayerMask groundLayerMask;      // which layers count as ground
    public GameObject groundPaintPrefab;   // your flat decal/blob prefab
    public float castHeight    = 0.6f;     // sphere radius + small epsilon
    public float stampInterval = 0.1f;     // seconds between stamps
    public float lifetime      = 5f;       // auto-destroy

    [Header("De-duplication & Overwrite")]
    [Tooltip("Skip stamping if friendly paint within this radius.")]
    public float minStampDistance = 0.5f;
    [Tooltip("Layer your paint-prefabs live on.")]
    public LayerMask paintLayerMask;

    // **Set these before you fire the projectile:**
    [HideInInspector] public int    teamId;
    [HideInInspector] public Material paintMaterial;

    private float stampTimer;

    private void Awake()
    {
        // Clean up projectile after its lifetime
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        stampTimer += Time.deltaTime;
        if (stampTimer < stampInterval) return;
        stampTimer = 0f;
        TryStamp();
    }

    private void TryStamp()
    {
        // 1) Raycast straight down to find the ground point
        Vector3 origin = transform.position + Vector3.up * castHeight;
        if (!Physics.Raycast(origin, Vector3.down, out var hit, castHeight * 2f, groundLayerMask))
            return;

        Vector3 hitPoint = hit.point;
        Vector3 spawnPos = hitPoint + Vector3.up * 0.05f;

        // 2) Gather ALL paint stamps within minStampDistance (including triggers)
        Collider[] nearby = Physics.OverlapSphere(
            hitPoint,
            minStampDistance,
            paintLayerMask,
            QueryTriggerInteraction.Collide
        );

        bool hasFriendly = false;
        // 3) Check each: destroy enemy paint, detect friendly paint
        foreach (var col in nearby)
        {
            var stamp = col.GetComponent<PaintStamp>();
            if (stamp == null) 
                continue;

            if (stamp.teamId == teamId)
            {
                // Already our color here ⇒ skip stamping entirely
                hasFriendly = true;
                break;
            }
            else
            {
                // Enemy color ⇒ remove it
                Destroy(stamp.gameObject);
            }
        }

        if (hasFriendly)
            return;

        // 4) No friendly here (and enemies removed) ⇒ place your decal
        var newStamp = Instantiate(groundPaintPrefab, spawnPos, Quaternion.identity);
        // Tag it
        var ps = newStamp.GetComponent<PaintStamp>();
        ps.teamId = teamId;
        // Color it
        var rend = newStamp.GetComponent<Renderer>();
        if (rend != null && paintMaterial != null)
            rend.material = paintMaterial;
    }

    private void OnTriggerEnter(Collider other)
    {
        // destroy the projectile on ground contact
        if ((groundLayerMask.value & (1 << other.gameObject.layer)) != 0)
            Destroy(gameObject);
    }
}
