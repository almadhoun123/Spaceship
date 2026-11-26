using UnityEngine;

/// <summary>
/// One member of a swarm. All members of a swarm share the same rail distance and centre,
/// so they arrive and stop together, each circling the centre at its own spot. Weak, and
/// shoots weak rays once the swarm has stopped.
/// </summary>
public class SwarmEnemy : EnemyBase
{
    SwarmSettings settings;
    Vector3 formation;   // this member's spot around the swarm centre (x, y)
    float phase;
    bool holding;
    float fireTimer;

    public void Init(EnemySpawner owner, SwarmSettings swarm, float distanceAlongRail, Vector3 groupOffset, Vector2 formationSpot, float orbitPhase)
    {
        settings = swarm;
        InitBase(owner, swarm.maxHealth, distanceAlongRail, groupOffset);
        formation = formationSpot;
        phase = orbitPhase;
        holding = false;
        fireTimer = swarm.firstShotDelay + Random.Range(0f, swarm.fireIntervalRandom);
        Place();
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

        Place();

        if (!holding) return;
        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            FireAtShip(settings.projectileSpeed, settings.projectileLifetime, settings.projectileDamagePercent);
            fireTimer = settings.fireInterval + Random.Range(0f, settings.fireIntervalRandom);
        }
    }

    void Place()
    {
        float angle = phase + Time.time * settings.orbitSpeed;
        var spot = Quaternion.Euler(0f, 0f, angle) * formation;
        spot.y += Mathf.Sin(Time.time * 2.3f + phase) * settings.bobAmplitude;
        // The whole formation is pushed out of the middle together, so it keeps its shape.
        var centre = KeepOutOfCentre(railDistance, offset, settings.formationRadius + settings.bobAmplitude);
        PlaceFacingBehindShip(railDistance, centre + spot, settings.lookBehindDistance);
    }
}
