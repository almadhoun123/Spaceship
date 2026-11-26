using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using Random = UnityEngine.Random;

/// <summary>
/// Spawns pooled enemies ahead of the player ship on the rail: regular ships, swarms and
/// kamikazes, picked at random by each type's weight. Also works out rail positions for them,
/// and clears every enemy when the ship starts the rail again.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] SplineContainer rail;
    [SerializeField] SplineAnimate ship;
    [Tooltip("The ship's hull: enemies aim at this.")]
    [SerializeField] Transform shipTarget;
    [SerializeField] ShipHealth shipHealth;
    [Tooltip("The object the position buttons move (ShipOffset): its local position is the ship's dodge position.")]
    [SerializeField] Transform shipOffset;
    [SerializeField] EnemyProjectilePool projectiles;

    [Header("Regular (also holds the shared spawn timing)")]
    [SerializeField] EnemyShip enemyPrefab;
    [SerializeField] EnemySettings settings;

    [Header("Swarm")]
    [SerializeField] SwarmEnemy swarmPrefab;
    [SerializeField] SwarmSettings swarmSettings;

    [Header("Kamikaze")]
    [SerializeField] KamikazeEnemy kamikazePrefab;
    [SerializeField] KamikazeSettings kamikazeSettings;

    [Header("Segments (optional)")]
    [Tooltip("The rail's segments. With a plan, each segment sets how often enemies come, " +
             "and leftover enemies leave when the ship enters a segment without enemies.")]
    [SerializeField] RailSegments segments;
    [SerializeField] SegmentSpawnPlan plan;

    readonly List<EnemyShip> regulars = new List<EnemyShip>();
    readonly List<SwarmEnemy> swarmers = new List<SwarmEnemy>();
    readonly List<KamikazeEnemy> kamikazes = new List<KamikazeEnemy>();
    readonly List<EnemyBase> all = new List<EnemyBase>();

    RailSampler sampler;
    float railLength;
    float lastShipProgress;
    float spawnTimer;
    float spawnCredit;
    Vector3 lastTargetPosition;

    public EnemySettings Settings => settings;
    public EnemyProjectilePool Projectiles => projectiles;
    public Transform ShipTarget => shipTarget;
    public ShipHealth ShipHealth => shipHealth;
    public Vector3 ShipVelocity { get; private set; }

    /// <summary>The ship's distance along the rail in the current lap (m).</summary>
    public float ShipDistance => ShipProgress * railLength;
    float ShipProgress => Mathf.Repeat(ship.NormalizedTime, 1f);

    /// <summary>Where the ship's hull sits relative to the rail right now (its dodge position).</summary>
    public Vector3 ShipOffsetFromRail
    {
        get
        {
            if (shipOffset != null)
            {
                var o = shipOffset.localPosition;
                o.z = 0f;
                return o;
            }
            EvaluateRail(ShipDistance, out var railPos, out var railRot);
            var local = Quaternion.Inverse(railRot) * (shipTarget.position - railPos);
            local.z = 0f;
            return local;
        }
    }

    void Start()
    {
        sampler = rail.GetComponent<RailSampler>();
        railLength = sampler.Length;
        Fill(regulars, enemyPrefab, settings.maxActive, "Enemy");
        Fill(swarmers, swarmPrefab, swarmSettings.groupSize * swarmSettings.maxGroups, "Swarm");
        Fill(kamikazes, kamikazePrefab, kamikazeSettings.maxActive, "Kamikaze");

        spawnTimer = settings.firstSpawnDelay;
        lastShipProgress = ShipProgress;
        lastTargetPosition = shipTarget.position;
    }

    void Fill<T>(List<T> list, T prefab, int count, string label) where T : EnemyBase
    {
        for (int i = 0; i < count; i++)
        {
            var e = Instantiate(prefab, transform);
            e.name = $"{label}_{i:00}";
            e.gameObject.SetActive(false);
            list.Add(e);
            all.Add(e);
        }
    }

    void Update()
    {
        // Ship velocity, used to aim shots where the ship will be.
        if (Time.deltaTime > 0f)
            ShipVelocity = (shipTarget.position - lastTargetPosition) / Time.deltaTime;
        lastTargetPosition = shipTarget.position;

        // The ship looped back to the start: clear the sky.
        float progress = ShipProgress;
        if (progress < lastShipProgress - 0.5f)
        {
            DespawnAll();
            spawnTimer = settings.firstSpawnDelay;
            spawnCredit = 0f;
            ShipVelocity = Vector3.zero;
        }
        lastShipProgress = progress;

        if (plan != null && segments != null) UpdateBySegment();
        else
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                spawnTimer = settings.spawnInterval;
                TrySpawn();
            }
        }
    }

    void UpdateBySegment()
    {
        float speed = ship.MaxSpeed;

        // Spawn rate follows the segment's strength where new enemies appear (ahead of the ship),
        // so they build up, peak late in the segment and taper off.
        float spawnAt = ShipDistance + settings.spawnDistance;
        float strength = plan.Evaluate(segments, spawnAt, railLength, speed, out var seg);
        float rate = seg != null ? seg.enemyRate * strength : 0f;
        if (rate > 0f)
        {
            spawnCredit += Time.deltaTime * rate / settings.spawnInterval;
            if (spawnCredit >= 1f)
            {
                spawnCredit -= 1f;
                TrySpawn();
            }
        }
        else
        {
            spawnCredit = 0f;
            // Nothing is coming and the ship is in a segment without enemies: send the rest away.
            plan.Evaluate(segments, ShipDistance, railLength, speed, out var segHere);
            if (segHere == null || segHere.enemyRate <= 0f) LeaveAll();
        }
    }

    void LeaveAll()
    {
        foreach (var e in all) if (e.gameObject.activeSelf) e.Leave();
    }

    void TrySpawn()
    {
        float distance = ShipDistance + settings.spawnDistance;
        if (distance > railLength - settings.endMargin) return;

        // Pick a type by weight among the ones that have room.
        float wRegular = Free(regulars) >= 1 ? settings.spawnWeight : 0f;
        float wSwarm = Free(swarmers) >= swarmSettings.groupSize ? swarmSettings.spawnWeight : 0f;
        float wKamikaze = Free(kamikazes) >= 1 ? kamikazeSettings.spawnWeight : 0f;
        float total = wRegular + wSwarm + wKamikaze;
        if (total <= 0f) return;

        float r = Random.value * total;
        if (r < wRegular) SpawnRegular(distance);
        else if (r < wRegular + wSwarm) SpawnSwarm(distance);
        else SpawnKamikaze(distance);
    }

    static int Free<T>(List<T> list) where T : EnemyBase
    {
        int n = 0;
        foreach (var e in list) if (!e.gameObject.activeSelf) n++;
        return n;
    }

    static T TakeFree<T>(List<T> list) where T : EnemyBase => list.Find(e => !e.gameObject.activeSelf);

    void SpawnRegular(float distance)
    {
        var e = TakeFree(regulars);
        e.gameObject.SetActive(true);
        e.Init(this, distance, PickOffset());
    }

    void SpawnSwarm(float distance)
    {
        var centre = PickOffset();
        int n = swarmSettings.groupSize;
        for (int i = 0; i < n; i++)
        {
            var e = TakeFree(swarmers);
            // Spread members evenly around the centre, a little randomly.
            float a = (i / (float)n) * 360f + Random.Range(-15f, 15f);
            float r = swarmSettings.formationRadius * Random.Range(0.6f, 1f);
            var spot = (Vector2)(Quaternion.Euler(0f, 0f, a) * Vector3.right * r);
            e.gameObject.SetActive(true);
            e.Init(this, swarmSettings, distance + Random.Range(-2f, 2f), centre, spot, Random.Range(0f, 360f));
        }
    }

    void SpawnKamikaze(float distance)
    {
        var e = TakeFree(kamikazes);
        e.gameObject.SetActive(true);
        e.Init(this, kamikazeSettings, distance, PickOffset());
    }

    /// <summary>
    /// Mostly on the left or right side (one per player), rarely in the middle.
    /// The side with fewer enemies right now gets the next one, so both players stay busy.
    /// </summary>
    Vector3 PickOffset()
    {
        float x;
        if (Random.value < settings.centerChance)
        {
            x = Random.Range(-settings.centerHalfWidth, settings.centerHalfWidth);
        }
        else
        {
            int left = 0, right = 0;
            foreach (var e in all)
            {
                if (!e.gameObject.activeSelf) continue;
                if (e.Offset.x < -settings.centerHalfWidth) left++;
                else if (e.Offset.x > settings.centerHalfWidth) right++;
            }
            float side = left < right ? -1f : right < left ? 1f : (Random.value < 0.5f ? -1f : 1f);
            x = side * Random.Range(settings.sideOffsetMin, settings.sideOffsetMax);
        }
        float y = Random.Range(-settings.verticalSpread, settings.verticalSpread);
        return new Vector3(x, y, 0f);
    }

    /// <summary>
    /// How far (m, along the rail's sideways axis, toward <paramref name="side"/>) a point must move
    /// so it sits outside the clear band in the middle of the screen. 0 when it is already clear.
    /// Worked out from the camera, so it follows the ship's position and curves in the rail.
    /// </summary>
    public float ClearBandPush(float distance, Vector3 railOffset, float side, float extraMargin)
    {
        var cam = Camera.main;
        if (cam == null || settings.clearBandHalfWidth <= 0f) return 0f;

        sampler.Sample(distance, out var railPos, out var railRot);
        var local = cam.transform.InverseTransformPoint(railPos + railRot * railOffset);
        if (local.z <= 0.5f) return 0f;   // beside or behind the camera: nothing to keep clear

        // Sideways distance (camera space) the band covers at this depth, plus room for the body.
        float tanHalf = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * cam.aspect;
        float required = local.z * tanHalf * settings.clearBandHalfWidth * 2f + settings.clearBandMargin + extraMargin;
        float deficit = required - side * local.x;
        if (deficit <= 0f) return 0f;

        // Moving along the rail's right axis moves it this much along the camera's right axis.
        float along = Vector3.Dot(cam.transform.right, railRot * Vector3.right);
        float push = deficit / Mathf.Max(0.3f, along);

        // Stay inside the tunnel.
        float room = settings.maxSideOffset - side * railOffset.x;
        return Mathf.Clamp(push, 0f, Mathf.Max(0f, room));
    }

    public void DespawnAll()
    {
        foreach (var e in all) e.gameObject.SetActive(false);
        projectiles.DespawnAll();
    }

    /// <summary>
    /// World position and rail-facing rotation at a distance along the rail: the same even-speed,
    /// smoothed frame the ship uses, so enemy offsets line up with the ship's positions.
    /// </summary>
    public void EvaluateRail(float distance, out Vector3 position, out Quaternion railRotation)
        => sampler.Sample(distance, out position, out railRotation);
}
