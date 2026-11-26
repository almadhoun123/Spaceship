using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// What each rail segment contains (see <see cref="RailSegments"/>): how dense the hazards are,
/// the mix of clouds and rocks, and how often enemies come. Within a segment the density and
/// spawn rate build up gradually, peak late in the segment, then taper off into the next one.
/// Only the very start of the level has an empty stretch.
/// </summary>
[CreateAssetMenu(menuName = "Spaceships/Segment Spawn Plan", fileName = "SegmentSpawnPlan")]
public class SegmentSpawnPlan : ScriptableObject
{
    [System.Serializable]
    public class Segment
    {
        public string label = "Segment";
        [Tooltip("Rocks and clouds per 100 m of rail at the peak. 0 = no hazards.")]
        [Min(0f)] public float hazardsPer100m = 0f;
        [Tooltip("Share of the hazards that are yellow clouds; the rest are rocks. 0.5 = half and half.")]
        [Range(0f, 1f)] public float cloudShare = 0.5f;
        [Tooltip("Enemy spawns at the peak: 1 = one every EnemySettings.spawnInterval, 2 = twice as often. 0 = no enemies.")]
        [Min(0f)] public float enemyRate = 0f;
        [Tooltip("Where in the segment things peak (0 = start, 1 = end). Before it they build up slowly, after it they taper off.")]
        [Range(0.05f, 0.95f)] public float peakAt = 0.65f;
        [Tooltip("Strength the segment already has the moment it starts (0 to 1 of the peak). " +
                 "0 = builds up from nothing; higher = things are there straight away, then build up to the peak.")]
        [Range(0f, 1f)] public float startStrength = 0f;
    }

    [Tooltip("Empty time at the very start of the level (s), before anything appears. Only the first segment has it.")]
    [FormerlySerializedAs("quietSeconds")]
    [Min(0f)] public float startQuietSeconds = 8f;
    [Tooltip("One entry per rail segment, in order.")]
    public Segment[] segments = new Segment[0];

    public Segment Get(int index) =>
        segments != null && index >= 0 && index < segments.Length ? segments[index] : null;

    /// <summary>
    /// Strength (0 to 1) at a distance inside segment 'index', which runs from start to end (m),
    /// for a ship moving at speed (m/s): eases up from the start strength to 1 at the peak, then back down to 0 at the end.
    /// </summary>
    public float Envelope(int index, float distance, float start, float end, float speed)
    {
        var seg = Get(index);
        if (seg == null || end <= start) return 0f;

        float rise = index == 0 ? start + startQuietSeconds * speed : start;
        float peak = Mathf.Max(rise + 0.01f, Mathf.Lerp(start, end, seg.peakAt));
        if (distance <= rise || distance >= end) return 0f;

        if (distance < peak)
        {
            // Build-up: from the start strength to the peak.
            float u = Mathf.Clamp01((distance - rise) / (peak - rise));
            return Mathf.Lerp(seg.startStrength, 1f, u * u * (3f - 2f * u));
        }
        // Taper: from the peak slowly down to nothing at the end of the segment.
        float k = Mathf.Clamp01((end - distance) / Mathf.Max(0.01f, end - peak));
        return k * k * (3f - 2f * k);
    }

    /// <summary>
    /// Strength (0 to 1) at a distance along the whole rail (m), and the segment it falls in
    /// (null if the plan has no entry for it).
    /// </summary>
    public float Evaluate(RailSegments railSegments, float distance, float railLength, float speed, out Segment segment)
    {
        segment = null;
        if (railSegments == null || railLength <= 0f) return 0f;
        int index = railSegments.SegmentAt(distance / railLength);
        segment = Get(index);
        if (segment == null) return 0f;
        var range = railSegments.SegmentRange(index);
        return Envelope(index, distance, range.x * railLength, range.y * railLength, speed);
    }
}
