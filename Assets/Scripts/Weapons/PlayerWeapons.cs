using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>
/// One player's weapons: the three selector buttons on their side of the console
/// (the selected one stays pressed down) and what their touches on their half do:
///   1. Laser: tap to fire one strong, fast bolt. Costs energy.
///   2. Machine gun: hold to fire a stream of fast, weak bolts. No energy, but it heats up and locks when overheated.
///   3. Area (circle select): press and drag a circle over targets to mark them; lift to hit every
///      marked one (less damage the more of the screen was painted). Costs energy.
/// </summary>
public class PlayerWeapons : MonoBehaviour
{
    [Tooltip("Buttons 1, 2, 3 on this player's side.")]
    [SerializeField] CockpitButton[] weaponButtons = new CockpitButton[3];
    [SerializeField] WeaponSettings laser;
    [SerializeField] MachineGunSettings machineGun;
    [SerializeField] AreaWeaponSettings area;

    [Header("Scene")]
    [Tooltip("Where this player's shots start: the corner of the window on their side.")]
    [SerializeField] Transform muzzle;
    [Tooltip("Player colour material, used for the shots and the circle.")]
    [SerializeField] Material beamMaterial;
    [FormerlySerializedAs("beamPrefab")]
    [SerializeField] PlayerBolt boltPrefab;
    [SerializeField] ShipEnergy energy;
    [SerializeField] Camera cockpitCamera;
    [Tooltip("Draws the circle brush. Child of the camera; also copied to mark selected targets.")]
    [SerializeField] LineRenderer areaLine;
    [Tooltip("\"Weapon is overheated\" message on this player's side.")]
    [SerializeField] GameObject overheatMessage;
    [Tooltip("When jammed (yellow rock cloud), nothing fires.")]
    [SerializeField] WeaponLockout lockout;
    [FormerlySerializedAs("beamPoolSize")]
    [SerializeField, Min(1)] int boltPoolSize = 30;

    const int NoPointer = int.MinValue;
    const int CircleSegments = 40;

    readonly List<PlayerBolt> bolts = new List<PlayerBolt>();
    UnityAction[] buttonHandlers;

    float nextLaserTime;
    float nextGunTime;
    int holdPointer = NoPointer;
    Vector2 holdPosition;

    // Circle select
    int brushPointer = NoPointer;
    Vector2 brushPosition;
    readonly HashSet<Vector2Int> paintedCells = new HashSet<Vector2Int>();
    readonly List<Component> selected = new List<Component>();
    readonly List<LineRenderer> marks = new List<LineRenderer>();
    float hideBrushAt = -1f;

    public int SelectedWeapon { get; private set; }
    /// <summary>Which half of the screen is this player's. Set by <see cref="CockpitPointerInput"/>.</summary>
    public bool OnRightHalf { get; set; }
    /// <summary>True while a yellow rock cloud has jammed the weapons.</summary>
    public bool Jammed => lockout != null && lockout.Active;
    /// <summary>Machine gun heat, 0 (cold) to 1 (overheated).</summary>
    public float Heat { get; private set; }
    public bool Overheated { get; private set; }
    float overheatEndTime;

    // ---------- setup ----------

    void Awake()
    {
        for (int i = 0; i < boltPoolSize; i++) CreateBolt();
        EnsureButtonHandlers();
    }

    // Built on demand: when scripts recompile during Play Mode, Unity clears this unsaved
    // array and calls OnEnable/OnDisable again without Awake.
    void EnsureButtonHandlers()
    {
        if (buttonHandlers != null && buttonHandlers.Length == weaponButtons.Length) return;
        buttonHandlers = new UnityAction[weaponButtons.Length];
        for (int i = 0; i < weaponButtons.Length; i++)
        {
            int index = i;
            buttonHandlers[i] = () => Select(index);
        }
    }

    void OnEnable()
    {
        EnsureButtonHandlers();
        for (int i = 0; i < weaponButtons.Length; i++)
            if (weaponButtons[i] != null) weaponButtons[i].Pressed.AddListener(buttonHandlers[i]);
    }

    void OnDisable()
    {
        if (buttonHandlers == null) return;
        for (int i = 0; i < weaponButtons.Length; i++)
            if (weaponButtons[i] != null) weaponButtons[i].Pressed.RemoveListener(buttonHandlers[i]);
    }

    void Start()
    {
        Select(0);
        areaLine.enabled = false;
        overheatMessage.SetActive(false);
    }

    PlayerBolt CreateBolt()
    {
        // Bolts live in world space, not under the moving cockpit.
        var b = Instantiate(boltPrefab);
        b.name = $"{name}_Bolt_{bolts.Count:00}";
        b.gameObject.SetActive(false);
        bolts.Add(b);
        return b;
    }

    /// <summary>Selects weapon 0, 1 or 2. Its button stays pressed until another is chosen.</summary>
    public void Select(int index)
    {
        SelectedWeapon = index;
        for (int i = 0; i < weaponButtons.Length; i++) weaponButtons[i].Latched = i == index;
        holdPointer = NoPointer;
        CancelBrush();
    }

    // ---------- pointer events from CockpitPointerInput ----------

    public void PointerDown(int pointerId, Vector2 screenPosition)
    {
        // Paused (start menu or test mode): the game is frozen, so no weapon fires.
        if (Time.timeScale <= 0f) return;
        if (Jammed) return;

        switch (SelectedWeapon)
        {
            case 0:
                FireLaser(screenPosition);
                break;
            case 1:
                holdPointer = pointerId;
                holdPosition = KeepInHalf(screenPosition, 0f);
                break;
            case 2:
                if (brushPointer != NoPointer) break;   // already painting with another finger
                CancelBrush();
                brushPointer = pointerId;
                brushPosition = KeepInHalf(screenPosition, BrushRadiusPixels);
                Paint(brushPosition);
                break;
        }
    }

    public void PointerMove(int pointerId, Vector2 screenPosition)
    {
        if (pointerId == holdPointer) holdPosition = KeepInHalf(screenPosition, 0f);

        if (pointerId == brushPointer)
        {
            // The whole ring stays on this player's half, even if the finger crosses the middle.
            screenPosition = KeepInHalf(screenPosition, BrushRadiusPixels);
            // Stamp along the way, so a quick swipe doesn't skip what it passed over.
            float step = Mathf.Max(1f, BrushRadiusPixels * 0.3f);
            float dist = Vector2.Distance(brushPosition, screenPosition);
            int n = Mathf.CeilToInt(dist / step);
            for (int i = 1; i <= n; i++) Paint(Vector2.Lerp(brushPosition, screenPosition, i / (float)n));
            brushPosition = screenPosition;
        }
    }

    public void PointerUp(int pointerId, Vector2 screenPosition)
    {
        if (pointerId == holdPointer) holdPointer = NoPointer;
        if (pointerId == brushPointer)
        {
            PointerMove(pointerId, screenPosition);
            FireBrush();
        }
    }

    // ---------- per frame: machine gun fire and heat, circle brush ----------

    void Update()
    {
        // Paused: drop any held trigger or half-painted selection so nothing fires or lingers on resume.
        if (Time.timeScale <= 0f)
        {
            holdPointer = NoPointer;
            if (brushPointer != NoPointer) CancelBrush();
            return;
        }

        // Jammed: let go of the trigger and drop the selection; the gun still cools down.
        if (Jammed)
        {
            holdPointer = NoPointer;
            if (brushPointer != NoPointer) CancelBrush();
        }

        bool firedThisFrame = false;

        if (Overheated)
        {
            if (Time.time >= overheatEndTime)
            {
                Overheated = false;
                Heat = 0f;
                overheatMessage.SetActive(false);
            }
        }
        else if (holdPointer != NoPointer && SelectedWeapon == 1)
        {
            if (Time.time >= nextGunTime)
            {
                nextGunTime = Time.time + 1f / machineGun.fireRate;
                FireGun(holdPosition);
            }
            firedThisFrame = true; // holding the trigger: no cooling between shots
        }

        if (!firedThisFrame && !Overheated && Heat > 0f)
            Heat = Mathf.Max(0f, Heat - machineGun.coolPerSecond * Time.deltaTime);

        // Targets keep moving while painting, so keep marking anything that slides under the circle.
        if (brushPointer != NoPointer) Paint(brushPosition, paintArea: false);

        if (hideBrushAt > 0f && Time.time >= hideBrushAt) CancelBrush();
    }

    void LateUpdate()
    {
        // Redrawn every frame: the ship and the targets move.
        bool showing = brushPointer != NoPointer || hideBrushAt > 0f;
        if (!showing) return;
        DrawCircle(areaLine, brushPosition, BrushRadiusPixels);
        DrawMarks();
    }

    // ---------- weapon 1: laser ----------

    bool FireLaser(Vector2 screenPosition)
    {
        if (Time.time < nextLaserTime) return false;
        if (!energy.TrySpend(laser.energyCost)) return false;
        nextLaserTime = Time.time + laser.cooldown;

        var spec = new PlayerBoltSpec
        {
            speed = laser.boltSpeed, maxDistance = laser.range, damage = laser.damage,
            hitRadius = laser.boltHitRadius, length = laser.boltLength, width = laser.beamWidth,
        };
        Shoot(screenPosition, laser.range, laser.aimAssistRadius, laser.targetLayers, spec, beamMaterial.GetColor("_BaseColor"));
        return true;
    }

    // ---------- weapon 2: machine gun ----------

    void FireGun(Vector2 screenPosition)
    {
        var spec = new PlayerBoltSpec
        {
            speed = machineGun.boltSpeed, maxDistance = machineGun.range, damage = machineGun.damage,
            hitRadius = machineGun.boltHitRadius, length = machineGun.boltLength, width = machineGun.beamWidth,
        };
        var color = Color.Lerp(machineGun.coldColor, machineGun.hotColor, Heat);
        Shoot(screenPosition, machineGun.range, machineGun.aimAssistRadius, machineGun.targetLayers, spec, color);

        Heat = Mathf.Min(1f, Heat + machineGun.heatPerShot);
        if (Heat >= 1f)
        {
            Overheated = true;
            overheatEndTime = Time.time + machineGun.overheatDuration;
            overheatMessage.SetActive(true);
        }
    }

    // ---------- weapon 3: circle select ----------

    float BrushRadiusPixels => area.brushRadius * Screen.height;
    float CellSize => Mathf.Max(2f, BrushRadiusPixels * 0.25f);

    /// <summary>Marks every target under the circle at this point, and (optionally) adds it to the painted area.</summary>
    void Paint(Vector2 point, bool paintArea = true)
    {
        float r = BrushRadiusPixels;

        if (paintArea)
        {
            // Painted area is counted on a coarse grid, so going over the same spot twice doesn't count twice.
            float cell = CellSize;
            int reach = Mathf.CeilToInt(r / cell);
            var c = new Vector2Int(Mathf.FloorToInt(point.x / cell), Mathf.FloorToInt(point.y / cell));
            for (int x = -reach; x <= reach; x++)
            for (int y = -reach; y <= reach; y++)
            {
                var k = new Vector2Int(c.x + x, c.y + y);
                var centre = new Vector2((k.x + 0.5f) * cell, (k.y + 0.5f) * cell);
                if ((centre - point).sqrMagnitude <= r * r) paintedCells.Add(k);
            }
        }

        foreach (var enemy in FindObjectsByType<EnemyBase>()) TrySelect(enemy, point, r);
        foreach (var debris in FindObjectsByType<Debris>()) TrySelect(debris, point, r);
    }

    /// <summary>
    /// Stops a point at the middle of the screen so it stays on this player's half,
    /// <paramref name="margin"/> pixels in from the middle line.
    /// </summary>
    Vector2 KeepInHalf(Vector2 point, float margin)
    {
        float mid = Screen.width * 0.5f;
        if (OnRightHalf) point.x = Mathf.Max(point.x, mid + margin);
        else point.x = Mathf.Min(point.x, mid - margin);
        return point;
    }

    bool InOwnHalf(Vector2 point) => OnRightHalf ? point.x >= Screen.width * 0.5f : point.x < Screen.width * 0.5f;

    void TrySelect(Component target, Vector2 point, float brushRadius)
    {
        if (selected.Contains(target)) return;
        if (!ScreenCircle(target, out var centre, out var radius)) return;
        if (!InOwnHalf(centre)) return;   // the other player's side
        if (Vector2.Distance(centre, point) <= brushRadius + radius) selected.Add(target);
    }

    /// <summary>Where a target is on screen and roughly how big (pixels). False if behind or out of range.</summary>
    bool ScreenCircle(Component target, out Vector2 centre, out float radius)
    {
        centre = default;
        radius = 0f;
        if (target == null || !target.gameObject.activeInHierarchy) return false;

        var sp = cockpitCamera.WorldToScreenPoint(target.transform.position);
        if (sp.z <= 0.1f || sp.z > area.range) return false;
        centre = sp;

        var rend = target.GetComponentInChildren<Renderer>();
        float worldRadius = rend != null ? rend.bounds.extents.magnitude * 0.8f : 1f;
        float pixelsPerMetre = cockpitCamera.pixelHeight * 0.5f / Mathf.Tan(cockpitCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) / sp.z;
        radius = worldRadius * pixelsPerMetre;
        return true;
    }

    void FireBrush()
    {
        brushPointer = NoPointer;

        // Drop anything that was destroyed while painting.
        selected.RemoveAll(t => t == null || !t.gameObject.activeInHierarchy);

        // Nothing marked: no cost, nothing happens.
        if (selected.Count == 0 || !energy.TrySpend(area.energyCost))
        {
            CancelBrush();
            return;
        }

        float cell = CellSize;
        float areaFraction = paintedCells.Count * cell * cell / (Screen.width * (float)Screen.height);
        float damage = area.DamageForArea(areaFraction);

        // Keep the marks on screen for a moment, then hit everything.
        var hits = new List<Component>(selected);
        hideBrushAt = Time.time + area.showAfterFire;
        foreach (var t in hits)
        {
            if (t is EnemyBase enemy) enemy.TakeDamage(damage);
            else if (t is Debris debris) debris.TakeDamage(damage);
        }
    }

    void CancelBrush()
    {
        brushPointer = NoPointer;
        paintedCells.Clear();
        selected.Clear();
        hideBrushAt = -1f;
        if (areaLine != null) areaLine.enabled = false;
        foreach (var m in marks) m.enabled = false;
    }

    void DrawMarks()
    {
        int used = 0;
        foreach (var t in selected)
        {
            if (!ScreenCircle(t, out var centre, out var radius)) continue;
            if (used >= marks.Count)
            {
                var m = Instantiate(areaLine, areaLine.transform.parent);
                m.name = $"{areaLine.name}_Mark_{marks.Count:00}";
                marks.Add(m);
            }
            DrawCircle(marks[used++], centre, radius + area.markPadding);
        }
        for (int i = used; i < marks.Count; i++) marks[i].enabled = false;
    }

    void DrawCircle(LineRenderer line, Vector2 centre, float radiusPixels)
    {
        // The line is a child of the camera, so it stays fixed on screen while the ship moves.
        line.enabled = true;
        line.useWorldSpace = false;
        line.loop = true;
        line.widthMultiplier = area.lineWidth;
        line.positionCount = CircleSegments;
        for (int i = 0; i < CircleSegments; i++)
        {
            float a = i / (float)CircleSegments * Mathf.PI * 2f;
            var p = centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radiusPixels;
            var world = cockpitCamera.ScreenToWorldPoint(new Vector3(p.x, p.y, area.lineDepth));
            line.SetPosition(i, line.transform.InverseTransformPoint(world));
        }
    }

    // ---------- shared ----------

    /// <summary>
    /// Aims from the camera through a screen point (with aim assist) and fires a bolt from the
    /// muzzle toward what it found, or straight out to the weapon's range.
    /// </summary>
    void Shoot(Vector2 screenPosition, float range, float radius, LayerMask mask, PlayerBoltSpec spec, Color color)
    {
        Physics.SyncTransforms(); // enemies and debris move by transform
        var ray = cockpitCamera.ScreenPointToRay(screenPosition);
        Vector3 aim = ray.GetPoint(range);
        Transform target = null;
        if (Physics.SphereCast(ray, radius, out var hit, range, mask, QueryTriggerInteraction.Ignore))
        {
            Component t = hit.collider.GetComponentInParent<EnemyBase>();
            if (t == null) t = hit.collider.GetComponentInParent<Debris>();
            target = t != null ? t.transform : null;
            aim = target != null ? target.position : hit.point;
        }

        var bolt = bolts.Find(b => !b.gameObject.activeSelf);
        if (bolt == null) bolt = CreateBolt();
        bolt.Fire(muzzle.position, aim, target, spec, beamMaterial, color, mask);
    }
}
