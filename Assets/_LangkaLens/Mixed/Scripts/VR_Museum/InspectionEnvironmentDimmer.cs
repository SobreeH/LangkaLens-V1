using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Owns temporary material copies so baked lighting can dim without changing the exhibit or assets.
internal sealed class InspectionEnvironmentDimmer
{
    private static readonly int[] ColorProperties =
    {
        Shader.PropertyToID("_BaseColor"), Shader.PropertyToID("_Color"),
        Shader.PropertyToID("_EmissionColor"), Shader.PropertyToID("_SpecColor"),
        Shader.PropertyToID("baseColorFactor"), Shader.PropertyToID("emissiveFactor")
    };
    private static readonly int Exposure = Shader.PropertyToID("_Exposure");
    private readonly Dictionary<Renderer, Material[]> originals = new Dictionary<Renderer, Material[]>();
    private readonly Dictionary<Material, Material> copies = new Dictionary<Material, Material>();
    private readonly List<(Material material, int property, Color color)> colors =
        new List<(Material, int, Color)>();
    private Material originalSky;
    private Material skyCopy;
    private float skyExposure;
    private float brightness = 1f;
    private float startBrightness;
    private float targetBrightness;
    private float elapsed;
    private float duration;
    private bool fading;
    private bool restoring;

    public void Begin(Scene scene, Transform artifact, Transform rig, Transform canvas,
        float target, float fadeDuration)
    {
        Restore();
        foreach (var root in scene.GetRootGameObjects())
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var t = renderer.transform;
            if (t.IsChildOf(artifact) || (rig != null && t.IsChildOf(rig)) ||
                (canvas != null && t.IsChildOf(canvas))) continue;

            var materials = renderer.sharedMaterials;
            originals.Add(renderer, materials);
            var dimmed = (Material[])materials.Clone();
            for (int i = 0; i < dimmed.Length; i++)
            {
                var source = materials[i];
                if (source == null) continue;
                if (!copies.TryGetValue(source, out var copy))
                {
                    copy = new Material(source) { hideFlags = HideFlags.HideAndDontSave };
                    copies.Add(source, copy);
                    foreach (int property in ColorProperties)
                        if (source.HasProperty(property))
                            colors.Add((copy, property, source.GetColor(property)));
                }
                dimmed[i] = copy;
            }
            renderer.sharedMaterials = dimmed;
        }

        originalSky = RenderSettings.skybox;
        if (originalSky != null && originalSky.HasProperty(Exposure))
        {
            skyExposure = originalSky.GetFloat(Exposure);
            skyCopy = new Material(originalSky) { hideFlags = HideFlags.HideAndDontSave };
            RenderSettings.skybox = skyCopy;
        }
        SetTarget(Mathf.Clamp01(target), fadeDuration);
    }

    public void End(float fadeDuration)
    {
        if (originals.Count == 0 && skyCopy == null) return;
        restoring = true;
        SetTarget(1f, fadeDuration);
    }

    private void SetTarget(float target, float fadeDuration)
    {
        startBrightness = brightness;
        targetBrightness = target;
        duration = Mathf.Max(0f, fadeDuration);
        elapsed = 0f;
        fading = true;
        if (duration == 0f) Tick(0f);
    }

    public void Tick(float deltaTime)
    {
        if (!fading) return;
        elapsed += deltaTime;
        float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
        brightness = Mathf.Lerp(startBrightness, targetBrightness, Mathf.SmoothStep(0f, 1f, t));
        foreach (var entry in colors)
        {
            Color color = entry.color * brightness;
            color.a = entry.color.a;
            entry.material.SetColor(entry.property, color);
        }
        if (skyCopy != null) skyCopy.SetFloat(Exposure, skyExposure * brightness);
        if (t < 1f) return;
        fading = false;
        if (restoring) Restore();
    }

    public void Restore()
    {
        foreach (var entry in originals)
            if (entry.Key != null) entry.Key.sharedMaterials = entry.Value;
        originals.Clear();
        if (skyCopy != null && RenderSettings.skybox == skyCopy) RenderSettings.skybox = originalSky;
        foreach (var copy in copies.Values) Release(copy);
        copies.Clear();
        colors.Clear();
        Release(skyCopy);
        skyCopy = null;
        originalSky = null;
        brightness = 1f;
        fading = false;
        restoring = false;
    }

    private static void Release(Material material)
    {
        if (material == null) return;
        if (Application.isPlaying) Object.Destroy(material);
        else Object.DestroyImmediate(material);
    }
}
