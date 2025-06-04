using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class HexTile : MonoBehaviour
{
    [Header("Falling & Respawn Settings")]
    [Tooltip("Seconds to wait after the player steps on before the tile falls")]
    public float fallDelay = 0.8f;

    [Tooltip("Seconds to wait after the tile has started falling before it begins respawning")]
    public float respawnDelay = 3f;

    [Tooltip("Time (in seconds) it takes for the tile to grow from zero scale to full scale")]
    public float growDuration = 1f;

    // (Optional) If you want to play a sound or VFX right when it finishes growing, you can add fields here.

    private Rigidbody    rb;
    private Collider     col;
    private Vector3      originalPosition;
    private Quaternion   originalRotation;
    private Vector3      originalScale;
    private Transform    originalParent;

    private bool isScheduledToFall = false;   // Prevent multiple concurrent fall coroutines

    void Awake()
    {
        rb  = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        // Record the “spawned” transform state exactly as it was instantiated by the grid generator:
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalScale    = transform.localScale;
        originalParent   = transform.parent;

        // Start as a “static” tile: no gravity, kinematic
        rb.isKinematic  = true;
        rb.useGravity   = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Only trigger if a Player (tagged “Player”) steps on it, and we’re not already in the middle of a fall/respawn cycle.
        if (!isScheduledToFall && collision.collider.CompareTag("Player"))
        {
            isScheduledToFall = true;
            StartCoroutine(FallAndRespawnRoutine());
        }
    }

    private IEnumerator FallAndRespawnRoutine()
    {
        // 1) Wait for the configured fallDelay before actually dropping
        yield return new WaitForSeconds(fallDelay);

        // 2) Turn on physics so the tile falls
        rb.isKinematic = false;
        rb.useGravity  = true;

        // 3) OPTIONAL: If you want to disable the collider while it’s in free-fall so it doesn’t hit other tiles,
        //    you could uncomment the next line. But leaving it on also works if you want falling tiles to bump each other.
        // col.enabled = false;

        // 4) Wait until respawnDelay has elapsed (giving it time to fall offscreen, etc.)
        yield return new WaitForSeconds(respawnDelay);

        // 5) Begin respawn: reset physics, transform, scale, and start growing
        RespawnReset();
        StartCoroutine(GrowFromZeroToFull());
    }

    private void RespawnReset()
    {
        // a) Stop any existing physics motion
        rb.linearVelocity         = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic     = true;
        rb.useGravity      = false;

        // b) Snap back to the original position & rotation & parent
        transform.SetParent(originalParent, worldPositionStays: true);
        transform.position = originalPosition;
        transform.rotation = originalRotation;

        // c) Reduce scale to zero so it “pops in” tiny
        transform.localScale = Vector3.zero;

        // d) Ensure collider is enabled during the growth phase (so the player can step on it while it grows)
        col.enabled = true;

        // e) Allow a new fall to be triggered once it’s big enough (or even mid-growth).
        //    We keep isScheduledToFall = true here to prevent immediately falling again before it’s visible,
        //    but we’ll clear it after a brief lag so that stepping on mid-growth works.
        StartCoroutine(ClearFallFlagAfterDelay(0.1f));
    }

    private IEnumerator ClearFallFlagAfterDelay(float delay)
    {
        // Give a tiny bit of time (just one frame or two) before allowing OnCollisionEnter to queue up a new fall
        yield return new WaitForSeconds(delay);
        isScheduledToFall = false;
    }

    private IEnumerator GrowFromZeroToFull()
    {
        float elapsed = 0f;

        while (elapsed < growDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / growDuration);
            transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, t);
            yield return null;
        }

        // Make absolutely sure we end at the exact original scale
        transform.localScale = originalScale;
    }
}
