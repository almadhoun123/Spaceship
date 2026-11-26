using UnityEngine;

/// <summary>
/// Regular enemy: flies down the rail toward the player ship, stops at a set distance in
/// front of it, keeps that distance as the ship moves, and only then starts shooting.
/// </summary>
public class EnemyShip : EnemyBase
{
    EnemySettings settings;
    bool holding;
    float fireTimer;

    /// <summary>True once the enemy has reached its stop distance and can shoot.</summary>
    public bool IsHolding => holding;

    public void Init(EnemySpawner owner, float distanceAlongRail, Vector3 railOffset)
    {
        settings = owner.Settings;
        InitBase(owner, settings.maxHealth, distanceAlongRail, railOffset);
        holding = false;
        fireTimer = settings.firstShotDelay;
        PlaceFacingBehindShip(railDistance, KeepOutOfCentre(railDistance, offset), settings.lookBehindDistance);
    }

    void Update()
    {
        if (spawner == null) return;
        if (Leaving) { UpdateLeaving(); return; }

        float holdDistance = spawner.ShipDistance + settings.stopDistance;
        if (!holding)
        {
            railDistance -= settings.approachSpeed * Time.deltaTime;
            if (railDistance <= holdDistance) holding = true;
        }
        if (holding) railDistance = holdDistance;

        PlaceFacingBehindShip(railDistance, KeepOutOfCentre(railDistance, offset), settings.lookBehindDistance);

        if (!holding) return;
        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            FireAtShip(settings.projectileSpeed, settings.projectileLifetime, settings.projectileDamagePercent);
            fireTimer = settings.fireInterval + Random.Range(0f, settings.fireIntervalRandom);
        }
    }
}
