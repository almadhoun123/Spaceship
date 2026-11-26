using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Places rocks and yellow clouds along the rail, each on one of the five ship positions.
/// With a segment plan, each segment gets its own density and cloud/rock mix, building up,
/// peaking late in the segment and tapering off. Without one, a fixed number
/// of each is scattered over the whole rail. Re-rolls every time the ship starts the rail again.
/// </summary>
public class DebrisField : MonoBehaviour
{
    [SerializeField] SplineContainer rail;
    [SerializeField] SplineAnimate ship;
    [SerializeField] Debris debrisPrefab;
    [Tooltip("Yellow cloud of small rocks: jams the weapons on contact.")]
    [SerializeField] DustCloud cloudPrefab;
    [SerializeField] DebrisFieldSettings settings;
    [SerializeField] ShipSettings shipSettings;

    [Header("Segments (optional)")]
    [Tooltip("The rail's segments. With a plan, hazards follow each segment's density and mix.")]
    [SerializeField] RailSegments segments;
    [SerializeField] SegmentSpawnPlan plan;

    readonly List<Debris> pool = new List<Debris>();
    readonly List<DustCloud> clouds = new List<DustCloud>();
    float lastShipTime;
    ShipLane[] lanes;

    void Start()
    {
        lanes = (ShipLane[])System.Enum.GetValues(typeof(ShipLane));
        // Without a plan the counts are fixed, so fill the pools up front.
        if (!UsePlan)
        {
            for (int i = 0; i < settings.count; i++) Grow(pool, debrisPrefab, "Debris");
            if (cloudPrefab != null)
                for (int i = 0; i < settings.cloudCount; i++) Grow(clouds, cloudPrefab, "DustCloud");
        }
        Scatter();
        lastShipTime = Mathf.Repeat(ship.NormalizedTime, 1f);
    }

    bool UsePlan => plan != null && segments != null;

    void Update()
    {
        // The ship loops back to the start of the rail: bring every piece back.
        // NormalizedTime keeps counting up across laps (1.2, 2.5...); keep only this lap's part.
        float t = Mathf.Repeat(ship.NormalizedTime, 1f);
        if (t < lastShipTime - 0.5f) Scatter();
        lastShipTime = t;
    }

    public void Scatter()
    {
        if (lanes == null) lanes = (ShipLane[])System.Enum.GetValues(typeof(ShipLane));
        if (UsePlan) ScatterBySegment();
        else ScatterEverywhere();
    }

    void ScatterEverywhere()
    {
        float length = rail.GetComponent<RailSampler>().Length;
        float from = settings.startDistance, to = Mathf.Max(settings.startDistance, length - settings.endMargin);
        foreach (var d in pool)
            Place(d, Random.Range(from, to), Random.Range(settings.minDiameter, settings.maxDiameter), settings.health);
        foreach (var c in clouds)
            Place(c, Random.Range(from, to), settings.cloudDiameter, settings.cloudHealth);
    }

    void ScatterBySegment()
    {
        float length = rail.GetComponent<RailSampler>().Length;
        float speed = ship.MaxSpeed;
        int rocksUsed = 0, cloudsUsed = 0;

        for (int i = 0; i < segments.SegmentCount; i++)
        {
            var seg = plan.Get(i);
            if (seg == null || seg.hazardsPer100m <= 0f) continue;

            var range = segments.SegmentRange(i);
            float start = range.x * length, end = range.y * length;
            float spacing = 100f / seg.hazardsPer100m;

            // A slot every 'spacing' metres, jittered; each slot is kept with a chance equal to the
            // segment's strength there, so hazards build up, peak late in the segment and taper off.
            for (float slot = start + Random.Range(0f, spacing); slot < end; slot += spacing)
            {
                float distance = slot + Random.Range(-0.4f, 0.4f) * spacing;
                if (Random.value >= plan.Envelope(i, distance, start, end, speed)) continue;

                if (cloudPrefab != null && Random.value < seg.cloudShare)
                {
                    if (cloudsUsed >= clouds.Count) Grow(clouds, cloudPrefab, "DustCloud");
                    Place(clouds[cloudsUsed++], distance, settings.cloudDiameter, settings.cloudHealth);
                }
                else
                {
                    if (rocksUsed >= pool.Count) Grow(pool, debrisPrefab, "Debris");
                    Place(pool[rocksUsed++], distance, Random.Range(settings.minDiameter, settings.maxDiameter), settings.health);
                }
            }
        }

        // Hide whatever this roll did not need.
        for (int i = rocksUsed; i < pool.Count; i++) pool[i].gameObject.SetActive(false);
        for (int i = cloudsUsed; i < clouds.Count; i++) clouds[i].gameObject.SetActive(false);
    }

    void Grow<T>(List<T> list, T prefab, string label) where T : Debris
    {
        // Only when the field is laid out (start of a lap), never per frame.
        var piece = Instantiate(prefab, transform);
        piece.name = $"{label}_{list.Count:00}";
        piece.gameObject.SetActive(false);
        list.Add(piece);
    }

    void Place(Debris piece, float distance, float diameter, float health)
    {
        // Same distance and facing the ship uses, so the piece sits exactly on its lane.
        rail.GetComponent<RailSampler>().Sample(distance, out var pos, out var rotation);
        var lane = lanes[Random.Range(0, lanes.Length)];
        var offset = shipSettings.GetOffset(lane) + Random.insideUnitSphere * settings.positionJitter;

        piece.transform.SetPositionAndRotation(pos + rotation * offset, Random.rotation);
        piece.transform.localScale = Vector3.one * diameter;
        piece.ResetHealth(health);
        piece.gameObject.SetActive(true);
    }
}
