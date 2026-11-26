using UnityEngine;

/// <summary>
/// Shares the cloud layer's shape settings with every shader as global values, so the Earth's cloud
/// shadows match the clouds exactly (same noise, same coverage, same drift). Put it on the Clouds layer.
/// Turn the shadows themselves on or off with "Cloud Shadows" on the Earth material.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class CloudShadowSync : MonoBehaviour
{
    static readonly string[] Shared =
    {
        "_Coverage", "_CoverageScale", "_CoverageSoftness",
        "_DetailScale", "_Warp", "_Puffiness", "_PuffScale", "_Threshold", "_EdgeSoftness",
        "_Speed", "_Evolve", "_Opacity",
        "_HighScale", "_HighStretch", "_HighThreshold", "_HighSpeed", "_HighOpacity", "_HighParallax",
    };

    Renderer cloudRenderer;

    void OnEnable()
    {
        cloudRenderer = GetComponent<Renderer>();
        Push();
    }

    void OnValidate()
    {
        cloudRenderer = GetComponent<Renderer>();
        Push();
    }

    void LateUpdate() => Push();

    void Push()
    {
        var mat = cloudRenderer != null ? cloudRenderer.sharedMaterial : null;
        if (mat == null) return;
        foreach (var name in Shared)
            if (mat.HasProperty(name)) Shader.SetGlobalFloat("_CG" + name, mat.GetFloat(name));
        // Cloud shell radius relative to the planet (both use the same unit mesh).
        Shader.SetGlobalFloat("_CG_ShellScale", transform.localScale.x);
    }
}
