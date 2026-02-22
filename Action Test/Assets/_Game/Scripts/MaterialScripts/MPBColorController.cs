using UnityEngine;

/// <summary>
/// Change an object's color per-renderer using MaterialPropertyBlock
/// (no material instancing, no creating new materials).
///
/// Works for MeshRenderer and SkinnedMeshRenderer.
/// Supports common color property names: _BaseColor (URP/HDRP), _Color (Built-in), etc.
/// </summary>
[DisallowMultipleComponent]
public class MPBColorController : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("If empty, will auto-grab Renderer on this GameObject.")]
    [SerializeField] private Renderer targetRenderer;

    [Header("Color Property")]
    [Tooltip("Try these in order until we find one that exists on the material.")]
    [SerializeField] private string[] colorPropertyCandidates = { "_BaseColor", "_Color", "_TintColor" };

    [Header("Apply Settings")]
    [Tooltip("Apply to this sub-material index. -1 applies to all material slots.")]
    [SerializeField] private int materialIndex = -1;

    [Header("Defaults")]
    [SerializeField] private bool applyOnEnable = true;
    [SerializeField] private Color initialColor = Color.white;

    private MaterialPropertyBlock _mpb;
    private int _resolvedColorId = -1;
    private bool _hasResolvedProperty;

    private void Reset()
    {
        targetRenderer = GetComponent<Renderer>();
    }

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer == null)
        {
            Debug.LogError($"[{nameof(MPBColorController)}] No Renderer found on '{name}'.", this);
            enabled = false;
            return;
        }

        _mpb = new MaterialPropertyBlock();
        ResolveColorProperty();
    }

    private void OnEnable()
    {
        if (applyOnEnable)
            SetColor(initialColor);
    }

    /// <summary>
    /// Set the per-instance color override (no new materials).
    /// </summary>
    public void SetColor(Color color)
    {
        if (!_hasResolvedProperty)
            ResolveColorProperty();

        if (!_hasResolvedProperty)
            return;

        if (materialIndex < 0)
        {
            // Apply to all material slots
            int count = targetRenderer.sharedMaterials != null ? targetRenderer.sharedMaterials.Length : 1;
            for (int i = 0; i < count; i++)
                ApplyColorToIndex(i, color);
        }
        else
        {
            ApplyColorToIndex(materialIndex, color);
        }
    }

    /// <summary>
    /// Clear the override so the renderer uses its original material color.
    /// </summary>
    public void Clear()
    {
        if (targetRenderer == null)
            return;

        if (materialIndex < 0)
        {
            int count = targetRenderer.sharedMaterials != null ? targetRenderer.sharedMaterials.Length : 1;
            for (int i = 0; i < count; i++)
                targetRenderer.SetPropertyBlock(null, i);
        }
        else
        {
            targetRenderer.SetPropertyBlock(null, materialIndex);
        }
    }

    private void ApplyColorToIndex(int index, Color color)
    {
        // Get the current MPB for this slot, modify, then re-apply.
        targetRenderer.GetPropertyBlock(_mpb, index);
        _mpb.SetColor(_resolvedColorId, color);
        targetRenderer.SetPropertyBlock(_mpb, index);
    }

    private void ResolveColorProperty()
    {
        _hasResolvedProperty = false;
        _resolvedColorId = -1;

        if (targetRenderer == null)
            return;

        var mats = targetRenderer.sharedMaterials;
        if (mats == null || mats.Length == 0)
        {
            Debug.LogWarning($"[{nameof(MPBColorController)}] Renderer on '{name}' has no materials.", this);
            return;
        }

        // Look at the material(s) we intend to affect and find a property that exists.
        int start = (materialIndex < 0) ? 0 : Mathf.Clamp(materialIndex, 0, mats.Length - 1);
        int end = (materialIndex < 0) ? mats.Length - 1 : start;

        for (int mi = start; mi <= end; mi++)
        {
            var m = mats[mi];
            if (m == null) continue;

            for (int pi = 0; pi < colorPropertyCandidates.Length; pi++)
            {
                string prop = colorPropertyCandidates[pi];
                if (string.IsNullOrWhiteSpace(prop)) continue;

                if (m.HasProperty(prop))
                {
                    _resolvedColorId = Shader.PropertyToID(prop);
                    _hasResolvedProperty = true;
                    return;
                }
            }
        }

        Debug.LogWarning(
            $"[{nameof(MPBColorController)}] Could not find a supported color property on '{name}'. " +
            $"Tried: {string.Join(", ", colorPropertyCandidates)}",
            this);
    }
}