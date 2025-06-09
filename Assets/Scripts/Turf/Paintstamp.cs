using UnityEngine;

// Attach to your ground-paint prefab.
public class PaintStamp : MonoBehaviour
{
    [Tooltip("0 = Team A, 1 = Team B, etc.")]
    public int teamId;
}
