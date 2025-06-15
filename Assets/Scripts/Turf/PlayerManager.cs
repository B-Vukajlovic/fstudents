// PlayerManager.cs
using UnityEngine;
using UnityEngine.InputSystem;
using System;

[RequireComponent(typeof(PlayerInput))]
public class PlayerManager : MonoBehaviour
{
    [Header("Shared Player State")]
    [Tooltip("This player’s team / paint color")]
    [SerializeField] private Color initialColor = Color.white;

    [Tooltip("Which layers this player can paint")]
    [SerializeField] private LayerMask paintLayerMask;

    public PlayerInput PlayerInput   { get; private set; }
    public int         PlayerIndex   { get; private set; }
    public Color       Color         { get; private set; }
    public LayerMask   PaintLayerMask{ get; private set; }

    public event Action<Color> OnColorChanged;

    private SkinnedMeshRenderer bodyRenderer;

    private void Awake()
    {
        PlayerInput  = GetComponent<PlayerInput>();
        PlayerIndex  = PlayerInput.playerIndex;

        var body = transform.Find("Body.008");
        if (body == null)
            Debug.LogError("PlayerManager: could not find child 'Body.008'!");
        else
            bodyRenderer = body.GetComponentInChildren<SkinnedMeshRenderer>();

        PaintLayerMask = paintLayerMask;
        SetColor(initialColor);
    }

    public void SetColor(Color newColor)
    {
        if (newColor == Color) return;

        Color = newColor;

        if (bodyRenderer != null)
            bodyRenderer.material.color = newColor;

        OnColorChanged?.Invoke(newColor);
    }
}
