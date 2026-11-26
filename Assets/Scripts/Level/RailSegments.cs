using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Splits the level rail into segments (by fraction of its length, so by time at constant speed)
/// and shows each one as its own colored grid tunnel. Move the split points or randomize the
/// colors in the Inspector; the tunnel pieces rebuild themselves. Gameplay can ask which segment
/// a point on the rail is in.
/// </summary>
[ExecuteAlways]
public class RailSegments : MonoBehaviour
{
    [SerializeField] SplineContainer rail;
    [Tooltip("Grid material for the tunnel pieces; each piece tints it with its segment color.")]
    [SerializeField] Material gridMaterial;
    [Tooltip("Where each segment ends, as a fraction of the rail (0-1), in order. The last one should be 1.")]
    [SerializeField] float[] segmentEnds = { 1f / 3f, 2f / 3f, 1f };
    [SerializeField] Color[] colors = new Color[0];

    [Header("Tunnel shape")]
    [SerializeField] float radius = 30f;
    [SerializeField] int sides = 8;
    [SerializeField] float segmentsPerUnit = 0.5f;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    public int SegmentCount => segmentEnds.Length;

    /// <summary>Which segment (0-based) a normalized rail position (0-1) is in.</summary>
    public int SegmentAt(float normalizedT)
    {
        for (int i = 0; i < segmentEnds.Length; i++)
            if (normalizedT <= segmentEnds[i]) return i;
        return segmentEnds.Length - 1;
    }

    /// <summary>Start and end of a segment, as fractions of the rail.</summary>
    public Vector2 SegmentRange(int index) =>
        new Vector2(index == 0 ? 0f : segmentEnds[index - 1], segmentEnds[index]);

    /// <summary>The segment's tunnel color (white if none is set yet).</summary>
    public Color SegmentColor(int index) =>
        colors != null && index >= 0 && index < colors.Length ? colors[index] : Color.white;

    void OnEnable() => Rebuild();
    void OnValidate() => needsRebuild = true;

    bool needsRebuild;

    void Update()
    {
        if (!needsRebuild) return;
        needsRebuild = false;
        Rebuild();
    }

    [ContextMenu("Randomize Colors")]
    public void RandomizeColors()
    {
        // Random, but spread around the color wheel so neighbouring segments never look alike.
        colors = new Color[segmentEnds.Length];
        float hue = Random.value;
        for (int i = 0; i < colors.Length; i++)
            colors[i] = Color.HSVToRGB(Mathf.Repeat(hue + i / (float)colors.Length + Random.Range(-0.05f, 0.05f), 1f), Random.Range(0.7f, 0.9f), 1f);
        Rebuild();
    }

    [ContextMenu("Rebuild")]
    public void Rebuild()
    {
        if (rail == null || gridMaterial == null || segmentEnds == null || segmentEnds.Length == 0) return;
        if (colors == null || colors.Length != segmentEnds.Length) RandomizeColors();

        var pieces = new List<Transform>();
        foreach (Transform child in transform) pieces.Add(child);

        for (int i = 0; i < segmentEnds.Length; i++)
        {
            GameObject piece;
            if (i < pieces.Count) piece = pieces[i].gameObject;
            else
            {
                piece = new GameObject();
                piece.transform.SetParent(transform, false);
                piece.AddComponent<MeshFilter>();
                piece.AddComponent<MeshRenderer>();
                piece.AddComponent<SplineExtrude>();
            }
            piece.name = $"Segment{i + 1}";

            var extrude = piece.GetComponent<SplineExtrude>();
            extrude.Container = rail;
            extrude.Radius = radius;
            extrude.Sides = sides;
            extrude.SegmentsPerUnit = segmentsPerUnit;
            extrude.Capped = false;
            extrude.Range = SegmentRange(i);
            extrude.Rebuild();

            var renderer = piece.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = gridMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, colors[i]);
            renderer.SetPropertyBlock(block);
        }

        // Fewer segments than before: remove the leftover pieces.
        for (int i = pieces.Count - 1; i >= segmentEnds.Length; i--)
        {
            if (Application.isPlaying) Destroy(pieces[i].gameObject);
            else DestroyImmediate(pieces[i].gameObject);
        }
    }
}
