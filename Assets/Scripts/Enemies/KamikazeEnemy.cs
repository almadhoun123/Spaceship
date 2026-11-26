using UnityEngine;

/// <summary>
/// Kamikaze: weaves toward the ship from its own side of the screen, kept out of the middle.
/// At the commit distance it
/// locks onto where the ship is and dives there in a straight line, so changing position
/// after that dodges it. Hitting the ship deals heavy damage. Shot down close to the ship,
/// its blast still hurts, less the further away it is.
/// </summary>
public class KamikazeEnemy : EnemyBase
{
    KamikazeSettings settings;
    float weaveTime;
    Vector2 weavePhase;
    bool committed;
    Vector3 commitStartOffset;
    Vector3 commitTargetOffset;
    float commitStartDistance;
    float spawnAhead;
    Vector3 lastPosition;

    public bool Committed => committed;

    public void Init(EnemySpawner owner, KamikazeSettings kamikaze, float distanceAlongRail, Vector3 railOffset)
    {
        settings = kamikaze;
        InitBase(owner, kamikaze.maxHealth, distanceAlongRail, railOffset);
        weaveTime = 0f;
        weavePhase = new Vector2(Random.Range(0f, Mathf.PI * 2f), Random.Range(0f, Mathf.PI * 2f));
        committed = false;
        spawnAhead = Mathf.Max(distanceAlongRail - owner.ShipDistance, kamikaze.commitDistance + 1f);
        Place(CurrentOffset());
        lastPosition = transform.position;
    }

    void Update()
    {
        if (spawner == null) return;
        // Already diving: it finishes the dive. Otherwise it peels off like the others.
        if (Leaving && !committed) { UpdateLeaving(); return; }

        railDistance -= settings.approachSpeed * Time.deltaTime;
        weaveTime += Time.deltaTime;
        float ahead = railDistance - spawner.ShipDistance;

        if (!committed && ahead <= settings.commitDistance)
        {
            // Lock onto where the ship is right now; from here on it flies straight.
            // (Read the current spot before switching mode, so there is no jump.)
            commitStartOffset = CurrentOffset();
            commitTargetOffset = spawner.ShipOffsetFromRail;
            commitStartDistance = Mathf.Max(ahead, 0.01f);
            committed = true;
        }

        Place(CurrentOffset());

        // Hit the ship?
        if (Vector3.Distance(transform.position, spawner.ShipTarget.position) <= settings.impactRadius)
        {
            spawner.ShipHealth.TakeDamagePercent(settings.impactDamagePercent, transform.position);
            Kill();
            return;
        }

        // Missed and flew past.
        if (ahead < -settings.missDespawnDistance) Kill();
    }

    Vector3 CurrentOffset()
    {
        if (committed)
        {
            float ahead = railDistance - spawner.ShipDistance;
            float k = 1f - Mathf.Clamp01(ahead / commitStartDistance);
            return Vector3.Lerp(commitStartOffset, commitTargetOffset, k);
        }
        // Before committing: weave around a centre that drifts from where it appeared toward the
        // ship (following it if it moves). The weave also calms down, so the lock-on is no jump.
        float aheadNow = railDistance - spawner.ShipDistance;
        float p = Mathf.Clamp01((spawnAhead - aheadNow) / (spawnAhead - settings.commitDistance));
        p = p * p * (3f - 2f * p);   // ease in and out
        var centre = Vector3.Lerp(offset, spawner.ShipOffsetFromRail, p);

        float tau = Mathf.PI * 2f;
        float weaveScale = Mathf.Lerp(1f, settings.weaveAtCommit, p);
        var weave = new Vector3(
            Mathf.Sin(weaveTime * settings.weaveFrequency.x * tau + weavePhase.x) * settings.weaveAmplitude.x,
            Mathf.Sin(weaveTime * settings.weaveFrequency.y * tau + weavePhase.y) * settings.weaveAmplitude.y,
            0f);

        // It comes in from its own side, staying out of the middle of the screen (weave included),
        // and only crosses in during the final straight dive.
        centre = KeepOutOfCentre(railDistance, centre, Mathf.Abs(settings.weaveAmplitude.x) * weaveScale);
        return centre + weave * weaveScale;
    }

    void Place(Vector3 railOffset)
    {
        spawner.EvaluateRail(railDistance, out var railPos, out var railRot);
        var position = railPos + railRot * railOffset;

        // Point the way it is actually moving, so the weaving shows.
        var moved = position - lastPosition;
        var rotation = moved.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(moved, railRot * Vector3.up)
            : railRot * Quaternion.Euler(0f, 180f, 0f);
        transform.SetPositionAndRotation(position, rotation);
        lastPosition = position;
    }

    protected override void OnShotDown()
    {
        float d = Vector3.Distance(transform.position, spawner.ShipTarget.position);
        if (d < settings.closeBlastRadius)
            spawner.ShipHealth.TakeDamagePercent(settings.closeBlastMaxPercent * (1f - d / settings.closeBlastRadius), transform.position);
    }
}
