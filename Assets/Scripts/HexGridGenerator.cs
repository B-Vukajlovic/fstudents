using UnityEngine;

public class HexGridGenerator : MonoBehaviour
{
    [Header("Circle Fill Settings")]
    public float fillRadius = 10f;      // Circle radius in world units

    [Header("Hexagon Settings")]
    public GameObject hexTilePrefab;    // Prefab must have HexTile.cs on it
    public float hexRadius = 1f;        // Distance from center to any vertex

    [Header("Spacing (Gap between tiles)")]
    [Tooltip("Extra distance (in world units) between the edges of adjacent hexes.")]
    public float spacing = 0.1f;

    [Header("Height Settings")]
    public float baseHeight = 2f;       // Y-position to spawn all tiles

    void Start()
    {
        if (hexTilePrefab == null)
        {
            Debug.LogError("HexGridGenerator: No hexTilePrefab assigned!");
            enabled = false;
            return;
        }

        // 1) Compute hex dimensions (flat-topped):
        float hexWidth  = hexRadius * 2f;                // from leftmost to rightmost vertex
        float hexHeight = Mathf.Sqrt(3f) * hexRadius;    // from top to bottom vertex

        // 2) Compute center‐to‐center spacings, adding the extra 'spacing':
        float horizontalSpacing = (hexWidth * 0.75f) + spacing;
        float verticalSpacing   = hexHeight + spacing;

        // 3) Determine how many columns/rows are needed to cover the circle:
        int maxCols = Mathf.CeilToInt(fillRadius / horizontalSpacing);
        int maxRows = Mathf.CeilToInt(fillRadius / verticalSpacing);

        // Preserve any rotation the prefab had originally
        Quaternion prefabRotation = hexTilePrefab.transform.rotation;

        for (int col = -maxCols; col <= maxCols; col++)
        {
            for (int row = -maxRows; row <= maxRows; row++)
            {
                // Compute the un‐offset x position for this column
                float xPos = col * horizontalSpacing;

                // In a flat-topped layout, odd columns sit half a row down
                float zOffset = (Mathf.Abs(col) % 2 == 1) ? (verticalSpacing * 0.5f) : 0f;
                float zPos    = row * verticalSpacing + zOffset;

                // Only instantiate if inside the circle of radius 'fillRadius'
                Vector2 flatPos = new Vector2(xPos, zPos);
                if (flatPos.magnitude <= fillRadius)
                {
                    Vector3 worldPos = new Vector3(xPos, baseHeight, zPos);
                    GameObject hexGO = Instantiate(
                        hexTilePrefab,
                        worldPos,
                        prefabRotation,
                        transform    // parent under this generator
                    );
                    hexGO.name = $"Hex_{col}_{row}";
                    // The HexTile component on the prefab will handle falling/respawning
                }
            }
        }
    }
}
