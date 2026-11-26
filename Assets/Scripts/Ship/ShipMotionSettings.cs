using UnityEngine;

/// <summary>
/// Tunable values for the cockpit's motion effects: banking in curves, tilting when the ship
/// changes position, shaking when hit, and a kick when the shared weapon fires.
/// Kept small on purpose: this is a big shared screen and the players are 8 to 14.
/// </summary>
[CreateAssetMenu(fileName = "ShipMotionSettings", menuName = "Spaceships/Ship Motion Settings")]
public class ShipMotionSettings : ScriptableObject
{
    [Tooltip("Scales every effect. 0 turns them all off.")]
    [Range(0f, 2f)] public float intensity = 1f;

    [Header("Curves: bank into turns")]
    [Tooltip("Degrees of roll per degree per second of turning.")]
    [Min(0f)] public float bankPerTurnRate = 0.6f;
    [Min(0f)] public float maxCurveBank = 6f;
    [Tooltip("Degrees of nose pitch per degree per second of climbing or diving.")]
    [Min(0f)] public float pitchPerClimbRate = 0.25f;
    [Min(0f)] public float maxCurvePitch = 2.5f;
    [Tooltip("How slowly the bank follows the curve (s). Higher is softer.")]
    [Min(0.01f)] public float curveSmoothTime = 0.45f;

    [Header("Changing position: lean into the move")]
    [Tooltip("Degrees of roll per m/s of sideways movement.")]
    [Min(0f)] public float rollPerSideSpeed = 0.3f;
    [Min(0f)] public float maxMoveRoll = 6f;
    [Tooltip("Degrees of pitch per m/s of up/down movement.")]
    [Min(0f)] public float pitchPerVerticalSpeed = 0.25f;
    [Min(0f)] public float maxMovePitch = 4f;
    [Min(0.01f)] public float moveSmoothTime = 0.08f;

    [Header("Jolt when hit: pushed away from the hit, then springs back")]
    [Tooltip("Push speed per percent of health lost (m/s). 5% of damage gives about a 10 cm push.")]
    [Min(0f)] public float joltSpeedPerPercent = 0.37f;
    [Tooltip("Turn speed per percent of health lost (degrees/s). 5% gives about 1.5 degrees.")]
    [Min(0f)] public float joltAngleSpeedPerPercent = 5.5f;
    [Tooltip("Hits bigger than this many percent jolt no harder.")]
    [Min(0f)] public float maxJoltPercent = 20f;
    [Tooltip("How hard the view springs back.")]
    [Min(1f)] public float joltStiffness = 120f;
    [Tooltip("How quickly the spring calms down (higher = less wobble).")]
    [Min(0f)] public float joltDamping = 14f;

    [Header("Shake when hit")]
    [Tooltip("Shake added per percent of health lost (shake runs 0 to 1).")]
    [Min(0f)] public float shakePerDamagePercent = 0.05f;
    [Tooltip("Shake fades by this much per second.")]
    [Min(0.01f)] public float shakeDecay = 1.6f;
    [Tooltip("Strongest shake rotation (degrees).")]
    [Min(0f)] public float maxShakeAngle = 1.5f;
    [Tooltip("Strongest shake movement (m).")]
    [Min(0f)] public float maxShakeOffset = 0.06f;
    [Tooltip("How fast the shake wobbles.")]
    [Min(0.1f)] public float shakeFrequency = 16f;

    [Header("Shared weapon kick")]
    [Tooltip("Shake added when the shared weapon fires.")]
    [Range(0f, 1f)] public float blastShake = 0.55f;
    [Tooltip("How far the cockpit is pushed back (m).")]
    [Min(0f)] public float blastKickDistance = 0.25f;
    [Tooltip("Nose lifts this much (degrees).")]
    [Min(0f)] public float blastKickPitch = 2f;
    [Tooltip("How fast the kick settles (per second).")]
    [Min(0.1f)] public float blastKickRecovery = 5f;
}
