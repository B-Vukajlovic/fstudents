using UnityEngine;
using UnityEngine.SceneManagement; // only if you want to reload the scene

public class DeathZone : MonoBehaviour
{
    [Tooltip("Optional: if left empty, the player GameObject will simply be disabled on death.")]
    public string playerTag = "Player";

    [Tooltip("If true, the current scene will reload when the player dies.")]
    public bool reloadSceneOnDeath = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        // 1) Disable the player’s movement script (so they can’t move after “dying”)
        var movement = other.GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;

        // 2) (Optional) Play a death animation, VFX, sound, etc. here

        // 3) If you want to reload the scene:
        if (reloadSceneOnDeath)
        {
            // Wait a fraction of a second if you want a brief death freeze (optional).
            // You could also call this via a coroutine to add a delay.
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            // Otherwise, just disable the player GameObject entirely:
            other.gameObject.SetActive(false);
        }
    }
}
