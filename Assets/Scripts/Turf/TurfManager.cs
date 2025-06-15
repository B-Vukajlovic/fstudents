// TurfManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using System.Collections.Generic;

public class TurfManager : MonoBehaviour
{
    [Header("UI Setup")]
    public GameObject    playerUIPrefab;
    public RectTransform uiParent;
    public Vector2       uiOffset = new Vector2(10f, -10f);

    private PaintableSurface[] allTiles;
    private int                totalTiles;

    private class PlayerEntry
    {
        public PlayerManager pm;
        public Image         swatch;
        public TMP_Text      percentText;
    }
    private readonly List<PlayerEntry> entries = new();

    private void Awake()
    {
        allTiles   = FindObjectsByType<PaintableSurface>(
                         FindObjectsInactive.Exclude,
                         FindObjectsSortMode.None
                     );
        totalTiles = allTiles.Length;

        var players = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < players.Length && i < 4; i++)
        {
            var pm = players[i].GetComponent<PlayerManager>();

            // create UI
            var uiObj      = Instantiate(playerUIPrefab, uiParent);
            var rect       = uiObj.GetComponent<RectTransform>();
            SetAnchor(rect, i);
            var offset     = uiOffset;
            if (i == 1 || i == 3) offset.x = -offset.x;
            if (i == 2 || i == 3) offset.y = -offset.y;
            rect.anchoredPosition = offset;

            var swatch      = uiObj.transform.Find("Swatch").GetComponent<Image>();
            var percentText = uiObj.transform.Find("PercentText").GetComponent<TMP_Text>();
            swatch.color     = pm.Color;
            percentText.text = "0.0%";

            // track and subscribe
            pm.OnColorChanged += newCol => swatch.color = newCol;
            entries.Add(new PlayerEntry { pm = pm, swatch = swatch, percentText = percentText });
        }
    }

    private void Update()
    {
        // existing UI update loop
        foreach (var e in entries)
        {
            float pct = ComputeCoverage(e.pm);
            e.percentText.text = $"{pct:0.0}%";
        }
    }

    private void SetAnchor(RectTransform rect, int index)
    {
        switch (index)
        {
            case 0:
                rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
                rect.pivot     = new Vector2(0, 1);
                break;
            case 1:
                rect.anchorMin = rect.anchorMax = new Vector2(1, 1);
                rect.pivot     = new Vector2(1, 1);
                break;
            case 2:
                rect.anchorMin = rect.anchorMax = new Vector2(0, 0);
                rect.pivot     = new Vector2(0, 0);
                break;
            case 3:
                rect.anchorMin = rect.anchorMax = new Vector2(1, 0);
                rect.pivot     = new Vector2(1, 0);
                break;
        }
    }

    private float ComputeCoverage(PlayerManager pm)
    {
        int owned = 0;
        foreach (var tile in allTiles)
            if (tile.GetComponent<Renderer>().material.color == pm.Color)
                owned++;
        return totalTiles > 0
            ? (owned / (float)totalTiles) * 100f
            : 0f;
    }

    public List<(int playerIndex, float percent)> GetSortedCoverage()
    {
        return entries
            .Select(e => (
                playerIndex: e.pm.PlayerIndex,
                percent:     ComputeCoverage(e.pm)
            ))
            .OrderBy(entry => entry.percent)
            .ToList();
    }
}
