using System;
using UnityEngine;

/// <summary>Pulses only the Y-button triangles of the tour's left controller visuals.</summary>
[DisallowMultipleComponent]
public class TourInspectionButtonHighlight : MonoBehaviour
{
    [Serializable]
    public class Target
    {
        public SkinnedMeshRenderer renderer;
        public Mesh originalMesh;
        public Mesh highlightMesh;
        public Material[] originalMaterials;
        public Material[] highlightMaterials;
        public int[] buttonMaterialIndices;
    }

    [SerializeField] private Target[] targets = Array.Empty<Target>();
    [SerializeField] private Color brown = new Color(83f / 255f, 58f / 255f, 45f / 255f, 1f);
    [SerializeField] private Color gold = new Color(187f / 255f, 146f / 255f, 92f / 255f, 1f);
    [SerializeField, Min(0.5f)] private float blinkPeriod = 1.6f;
    public Target[] Targets => targets;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private MaterialPropertyBlock properties;
    private float elapsed;

    private void OnEnable()
    {
        properties = new MaterialPropertyBlock();
        elapsed = 0f;
        foreach (var target in targets)
        {
            if (target.renderer == null || target.highlightMesh == null) continue;
            target.renderer.sharedMesh = target.highlightMesh;
            target.renderer.sharedMaterials = target.highlightMaterials;
        }
        ApplyColor(brown);
    }

    private void Update()
    {
        elapsed = Mathf.Repeat(elapsed + Time.unscaledDeltaTime, Mathf.Max(0.5f, blinkPeriod));
        float pulse = 0.5f - 0.5f * Mathf.Cos(elapsed / Mathf.Max(0.5f, blinkPeriod) * Mathf.PI * 2f);
        ApplyColor(Color.Lerp(brown, gold, pulse));
    }

    private void ApplyColor(Color color)
    {
        foreach (var target in targets)
        {
            if (target.renderer == null) continue;
            foreach (int index in target.buttonMaterialIndices)
            {
                target.renderer.GetPropertyBlock(properties, index);
                properties.SetColor(BaseColorId, color);
                // The controller texture is dark; a little emission makes the blink readable.
                properties.SetColor(EmissionColorId, color.linear * 0.75f);
                target.renderer.SetPropertyBlock(properties, index);
            }
        }
    }

    private void OnDisable()
    {
        foreach (var target in targets)
        {
            if (target.renderer == null) continue;
            foreach (int index in target.buttonMaterialIndices)
                target.renderer.SetPropertyBlock(null, index);
            target.renderer.sharedMesh = target.originalMesh;
            target.renderer.sharedMaterials = target.originalMaterials;
        }
    }
}
