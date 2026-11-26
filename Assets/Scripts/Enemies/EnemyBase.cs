using UnityEngine;

/// <summary>
/// Shared parts of every enemy type: health, its place relative to the rail, facing, aiming
/// and firing a ray at the ship. Weapons and the health bar talk to enemies through this.
/// </summary>
public abstract class EnemyBase : MonoBehaviour, IHasHealth
{
    [Tooltip("Where shots leave the enemy. Defaults to this transform.")]
    [SerializeField] protected Transform muzzle;

    protected EnemySpawner spawner;
    protected float railDistance;
    protected Vector3 offset;
    float health;
    float maxHealth = 1f;
    float leaveTimer;
    float leaveSide;
    float side;
    float clearPush;
    float clearPushVelocity;

    /// <summary>Where it sits relative to the rail (x: left/right, y: up/down).</summary>
    public Vector3 Offset => offset;
    public float HealthFraction => maxHealth > 0f ? health / maxHealth : 1f;
    /// <summary>True while it is flying off (the segment it belongs to is ending).</summary>
    public bool Leaving { get; private set; }

    protected void InitBase(EnemySpawner owner, float maxHp, float distanceAlongRail, Vector3 railOffset)
    {
        spawner = owner;
        maxHealth = Mathf.Max(0.01f, maxHp);
        health = maxHealth;
        railDistance = distanceAlongRail;
        offset = railOffset;
        Leaving = false;
        if (muzzle == null) muzzle = transform;
        side = Mathf.Abs(railOffset.x) > 0.01f ? Mathf.Sign(railOffset.x) : (Random.value < 0.5f ? -1f : 1f);
        clearPushVelocity = 0f;
        clearPush = spawner.ClearBandPush(distanceAlongRail, railOffset, side, 0f);
    }

    /// <summary>Which side of the screen this enemy belongs to: -1 left, +1 right.</summary>
    public float Side => side;

    /// <summary>
    /// Returns <paramref name="railOffset"/> moved out to this enemy's side, far enough that it stays
    /// out of the clear band in the middle of the screen. The push eases in and out, so a position
    /// change of the ship slides enemies aside instead of snapping them.
    /// </summary>
    /// <param name="extraMargin">More room for things drawn around the offset (a swarm's formation, a weave).</param>
    protected Vector3 KeepOutOfCentre(float distance, Vector3 railOffset, float extraMargin = 0f)
    {
        float target = spawner.ClearBandPush(distance, railOffset, side, extraMargin);
        clearPush = Mathf.SmoothDamp(clearPush, target, ref clearPushVelocity, spawner.Settings.clearBandSmoothTime);
        return railOffset + new Vector3(side * clearPush, 0f, 0f);
    }

    /// <summary>Stop fighting and fly off ahead and out to the side, then disappear.</summary>
    public void Leave()
    {
        if (Leaving || !gameObject.activeSelf) return;
        Leaving = true;
        leaveTimer = 0f;
        leaveSide = side;
    }

    /// <summary>Moves a leaving enemy; subclasses call this instead of their normal update.</summary>
    protected void UpdateLeaving()
    {
        var s = spawner.Settings;
        leaveTimer += Time.deltaTime;
        railDistance += s.leaveSpeed * Time.deltaTime;
        offset += new Vector3(leaveSide, 0.5f, 0f) * (s.leaveSideSpeed * Time.deltaTime);

        // Flying off ahead pulls it toward the middle of the view: keep it out to its side.
        var placed = KeepOutOfCentre(railDistance, offset);
        spawner.EvaluateRail(railDistance, out var railPos, out var railRot);
        var position = railPos + railRot * placed;
        var moved = position - transform.position;
        var rotation = moved.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(moved, railRot * Vector3.up) : transform.rotation;
        transform.SetPositionAndRotation(position, rotation);

        if (leaveTimer >= s.leaveDuration) Kill();
    }

    public void TakeDamage(float amount) => TakeDamage(amount, true);

    /// <param name="allowShotDownEffects">False skips side effects of being destroyed (the kamikaze's blast).</param>
    public void TakeDamage(float amount, bool allowShotDownEffects)
    {
        if (health <= 0f || !gameObject.activeSelf) return;
        health -= amount;
        if (health <= 0f)
        {
            if (allowShotDownEffects) OnShotDown();
            Kill();
        }
    }

    /// <summary>Called when weapons destroy this enemy (not when it despawns).</summary>
    protected virtual void OnShotDown() { }

    /// <summary>Removes this enemy (back to the pool).</summary>
    public void Kill() => gameObject.SetActive(false);

    /// <summary>
    /// Places the enemy at a rail position plus offset, pointing at a spot behind the ship:
    /// back down the rail, angled in toward the player.
    /// </summary>
    protected void PlaceFacingBehindShip(float distance, Vector3 railOffset, float lookBehindDistance)
    {
        spawner.EvaluateRail(distance, out var railPos, out var railRot);
        var position = railPos + railRot * railOffset;

        var railForward = railRot * Vector3.forward;
        var lookTarget = spawner.ShipTarget.position - railForward * lookBehindDistance;
        var dir = lookTarget - position;
        var rotation = dir.sqrMagnitude > 0.01f
            ? Quaternion.LookRotation(dir, railRot * Vector3.up)
            : railRot * Quaternion.Euler(0f, 180f, 0f);

        transform.SetPositionAndRotation(position, rotation);
    }

    /// <summary>Fires a straight ray at where the ship will be when it arrives (so it can be dodged).</summary>
    protected void FireAtShip(float speed, float lifetime, float damagePercent)
    {
        var target = spawner.ShipTarget.position;
        var from = muzzle.position;
        var aim = target;
        for (int i = 0; i < 2; i++)
        {
            float time = Vector3.Distance(from, aim) / speed;
            aim = target + spawner.ShipVelocity * time;
        }
        spawner.Projectiles.Fire(from, (aim - from).normalized, speed, lifetime, damagePercent);
    }
}
