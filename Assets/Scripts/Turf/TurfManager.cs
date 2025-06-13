using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TurfManager : MonoBehaviour
{
    [Header("UI Setup")]
    [Tooltip("Drag your PlayerUI_Panel prefab here")]
    public GameObject playerUIPrefab;
    [Tooltip("Drag your Canvas (or a child of it) here")]
    public RectTransform uiParent;
    [Tooltip("Inset from each corner (X right, Y down) in pixels")]
    public Vector2 uiOffset = new Vector2(10f, -10f);

    private PaintableSurface[] allTiles;
    private int totalTiles;

    private class PlayerEntry
    {
        public Color   color;
        public Image   swatch;
        public TMP_Text percentText;
    }
    private readonly List<PlayerEntry> entries = new List<PlayerEntry>();

    void Awake()
    {
        // Cache all paintable surfaces
        allTiles = Object.FindObjectsByType<PaintableSurface>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );
        totalTiles = allTiles.Length;

        // Find players and spawn UI panels
        var players = GameObject.FindGameObjectsWithTag("Player");
        int count = Mathf.Min(players.Length, 4);

        for (int i = 0; i < count; i++)
        {
            // Get player color
            Transform bodyTransform = players[i].transform.Find("Body.008");
            SkinnedMeshRenderer bodyRend = bodyTransform.GetComponentInChildren<SkinnedMeshRenderer>();
            Color col = bodyRend.material.color;

            // Instantiate UI under the Canvas
            var uiObj = Instantiate(playerUIPrefab, uiParent);
            var rect  = uiObj.GetComponent<RectTransform>();

            // Anchor panel to corner
            SetAnchor(rect, i);

            // Calculate directional offset
            Vector2 offset = uiOffset;
            if (i == 1 || i == 3) offset.x = -offset.x;  // right corners
            if (i == 2 || i == 3) offset.y = -offset.y;  // bottom corners
            rect.anchoredPosition = offset;

            // Configure swatch & text
            var swatch      = uiObj.transform.Find("Swatch").GetComponent<Image>();
            var percentText = uiObj.transform.Find("PercentText").GetComponent<TMP_Text>();
            swatch.color        = col;
            percentText.text    = "0.0%";

            entries.Add(new PlayerEntry { color = col, swatch = swatch, percentText = percentText });
        }
    }

    void Update()
    {
        // Recount tiles and update UI
        foreach (var e in entries)
        {
            int owned = 0;
            foreach (var tile in allTiles)
            {
                if (tile.GetComponent<Renderer>().material.color == e.color)
                    owned++;
            }
            float pct = totalTiles > 0 ? (owned / (float)totalTiles) * 100f : 0f;
            e.percentText.text = $"{pct:0.0}%";
        }
    }

    private void SetAnchor(RectTransform rect, int index)
    {
        switch (index)
        {
            case 0: // top-left
                rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
                rect.pivot     = new Vector2(0, 1);
                break;
            case 1: // top-right
                rect.anchorMin = rect.anchorMax = new Vector2(1, 1);
                rect.pivot     = new Vector2(1, 1);
                break;
            case 2: // bottom-left
                rect.anchorMin = rect.anchorMax = new Vector2(0, 0);
                rect.pivot     = new Vector2(0, 0);
                break;
            case 3: // bottom-right
                rect.anchorMin = rect.anchorMax = new Vector2(1, 0);
                rect.pivot     = new Vector2(1, 0);
                break;
        }
    }
}
