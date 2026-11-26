using UnityEngine;

/// <summary>
/// Moves the cockpit and camera together (the players' view) for a sense of motion:
/// banks into curves of the rail, leans into position changes, shakes when the ship is hit,
/// and kicks back when the shared weapon fires. Sits between ShipOffset and the cockpit/camera,
/// so the hull and its collisions are not affected, and the buttons stay put on screen.
/// </summary>
public class ShipMotion : MonoBehaviour
{
    [SerializeField] ShipMotionSettings settings;
    [Tooltip("The object that follows the rail (its turning drives the banking).")]
    [SerializeField] Transform railFollower;
    [Tooltip("The object the position buttons move (its movement drives the lean).")]
    [SerializeField] Transform positionOffset;
    [SerializeField] ShipHealth health;
    [SerializeField] SharedWeapon sharedWeapon;

    Quaternion lastRailRotation;
    Vector3 lastOffset;

    float curveRoll, curveRollVel, curvePitch, curvePitchVel;
    float moveRoll, moveRollVel, movePitch, movePitchVel;
    float trauma;
    float kickTime = -1f;
    float seed;
    // Hit jolt: a spring for position (m) and one for rotation (degrees: pitch, yaw, roll).
    Vector3 joltPos, joltPosVel, joltAng, joltAngVel;

    void Awake() => seed = Random.Range(0f, 100f);

    void OnEnable()
    {
        lastRailRotation = railFollower.rotation;
        lastOffset = positionOffset.localPosition;
        health.Hit += OnHit;
        if (sharedWeapon != null) sharedWeapon.Fired += OnBlast;
    }

    void OnDisable()
    {
        health.Hit -= OnHit;
        if (sharedWeapon != null) sharedWeapon.Fired -= OnBlast;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// Bigger hits shake and jolt more. With a known source the view is pushed away from it
    /// (hit on the left: pushed and tipped right); without one it is pushed back.
    /// </summary>
    void OnHit(float percent, Vector3? source)
    {
        AddShake(percent * settings.shakePerDamagePercent);

        float p = Mathf.Min(percent, settings.maxJoltPercent);
        var push = Vector3.back;
        if (source.HasValue)
        {
            var away = positionOffset.InverseTransformDirection(positionOffset.position - source.Value);
            if (away.sqrMagnitude > 0.0001f) push = away.normalized;
        }
        joltPosVel += push * (p * settings.joltSpeedPerPercent);
        // Pitch: nose up when pushed up or back. Yaw: turn a little with the push. Roll: tip toward the push.
        var spin = new Vector3(-push.y - Mathf.Min(push.z, 0f) * 0.5f, push.x * 0.5f, -push.x);
        joltAngVel += spin * (p * settings.joltAngleSpeedPerPercent);
    }

    void OnBlast()
    {
        AddShake(settings.blastShake);
        kickTime = 0f;
    }

    /// <summary>Adds shake (0 to 1). Several hits close together stack up to the maximum.</summary>
    public void AddShake(float amount) => trauma = Mathf.Clamp01(trauma + amount);

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        float k = settings.intensity;

        // --- Curves: how fast the rail is turning, in the ship's own frame. Where last frame's
        // forward points now gives the turn (atan2 stays precise for the tiny per-frame angles).
        var lastForward = lastRailRotation * Vector3.forward;
        lastRailRotation = railFollower.rotation;
        var seen = Quaternion.Inverse(railFollower.rotation) * lastForward;
        float yawStep = -Mathf.Atan2(seen.x, seen.z) * Mathf.Rad2Deg;   // + = turning right
        float pitchStep = Mathf.Atan2(seen.y, seen.z) * Mathf.Rad2Deg;  // + = nose going down
        // A big jump means the ship looped back to the start: ignore it.
        bool jump = Mathf.Abs(yawStep) > 20f || Mathf.Abs(pitchStep) > 20f;
        float yawRate = jump ? 0f : yawStep / dt;
        float pitchRate = jump ? 0f : pitchStep / dt;

        float rollTarget = Mathf.Clamp(-yawRate * settings.bankPerTurnRate, -settings.maxCurveBank, settings.maxCurveBank);
        float pitchTarget = Mathf.Clamp(pitchRate * settings.pitchPerClimbRate, -settings.maxCurvePitch, settings.maxCurvePitch);
        curveRoll = Mathf.SmoothDamp(curveRoll, rollTarget, ref curveRollVel, settings.curveSmoothTime);
        curvePitch = Mathf.SmoothDamp(curvePitch, pitchTarget, ref curvePitchVel, settings.curveSmoothTime);

        // --- Changing position: lean the way the ship is moving.
        var offset = positionOffset.localPosition;
        var velocity = (offset - lastOffset) / dt;
        lastOffset = offset;
        float moveRollTarget = Mathf.Clamp(-velocity.x * settings.rollPerSideSpeed, -settings.maxMoveRoll, settings.maxMoveRoll);
        float movePitchTarget = Mathf.Clamp(-velocity.y * settings.pitchPerVerticalSpeed, -settings.maxMovePitch, settings.maxMovePitch);
        moveRoll = Mathf.SmoothDamp(moveRoll, moveRollTarget, ref moveRollVel, settings.moveSmoothTime);
        movePitch = Mathf.SmoothDamp(movePitch, movePitchTarget, ref movePitchVel, settings.moveSmoothTime);

        // --- Shake: grows with how much damage was taken, then fades.
        trauma = Mathf.Max(0f, trauma - settings.shakeDecay * dt);
        float s = trauma;
        float t = Time.time * settings.shakeFrequency;
        var shakeAngles = new Vector3(Noise(t, 0f), Noise(t, 1f), Noise(t, 2f)) * (settings.maxShakeAngle * s);
        var shakeOffset = new Vector3(Noise(t, 3f), Noise(t, 4f), 0f) * (settings.maxShakeOffset * s);

        // --- Shared weapon kick: quick push back, then settle.
        float kick = 0f;
        if (kickTime >= 0f)
        {
            kickTime += dt;
            kick = kickTime < 0.05f ? kickTime / 0.05f : Mathf.Exp(-(kickTime - 0.05f) * settings.blastKickRecovery);
            if (kick < 0.01f) { kick = 0f; kickTime = -1f; }
        }

        // --- Hit jolt: damped springs pull the push and tip back to zero.
        StepSpring(ref joltPos, ref joltPosVel, dt);
        StepSpring(ref joltAng, ref joltAngVel, dt);

        float pitch = curvePitch + movePitch - kick * settings.blastKickPitch;
        float roll = curveRoll + moveRoll;
        transform.localRotation = Quaternion.Euler((new Vector3(pitch, 0f, roll) + shakeAngles + joltAng) * k);
        transform.localPosition = (shakeOffset + joltPos + Vector3.back * (kick * settings.blastKickDistance)) * k;
    }

    void StepSpring(ref Vector3 x, ref Vector3 v, float dt)
    {
        // Small sub-steps keep the spring stable at low frame rates.
        int steps = Mathf.CeilToInt(dt / 0.01f);
        float h = dt / steps;
        for (int i = 0; i < steps; i++)
        {
            v += (-settings.joltStiffness * x - settings.joltDamping * v) * h;
            x += v * h;
        }
    }

    float Noise(float t, float channel) => Mathf.PerlinNoise(seed + channel * 17.3f, t) * 2f - 1f;
}
