using UnityEngine;

/// <summary>
/// Tunable values for the kamikaze: weaves toward the ship, then commits to a straight dive at
/// the ship's position. Shoot it or change position to dodge. Exploding close to the ship hurts.
/// </summary>
[CreateAssetMenu(fileName = "KamikazeSettings", menuName = "Spaceships/Kamikaze Settings")]
public class KamikazeSettings : ScriptableObject
{
    [Header("Spawning")]
    [Tooltip("How likely a kamikaze is, compared with the other enemy types' weights.")]
    [Min(0f)] public float spawnWeight = 1f;
    [Min(1)] public int maxActive = 2;

    [Header("Health")]
    [Min(1f)] public float maxHealth = 30f;

    [Header("Movement")]
    [Tooltip("Speed toward the ship (m/s), on top of the ship's own speed.")]
    [Min(0f)] public float approachSpeed = 18f;
    [Tooltip("Side-to-side (x) and up-down (y) weaving size (m).")]
    public Vector2 weaveAmplitude = new Vector2(6f, 4f);
    [Tooltip("How much of the weave is left when it locks on (0 = none, 1 = all). Small keeps the lock-on smooth.")]
    [Range(0f, 1f)] public float weaveAtCommit = 0.3f;
    [Tooltip("Weaving speed for x and y (cycles per second).")]
    public Vector2 weaveFrequency = new Vector2(0.45f, 0.7f);
    [Tooltip("At this distance ahead it stops steering and flies straight at where the ship was (m). Moving after that dodges it.")]
    [Min(1f)] public float commitDistance = 35f;

    [Header("Damage")]
    [Tooltip("Hits the ship if it passes this close to the hull (m).")]
    [Min(0.1f)] public float impactRadius = 3.5f;
    [Tooltip("Percent of max ship health lost when it hits.")]
    [Range(0f, 100f)] public float impactDamagePercent = 15f;
    [Tooltip("Shot down closer than this (m), its explosion still hurts the ship.")]
    [Min(0f)] public float closeBlastRadius = 20f;
    [Tooltip("Percent of max ship health lost if shot down right next to the ship. Falls to 0 at the blast radius.")]
    [Range(0f, 100f)] public float closeBlastMaxPercent = 8f;
    [Tooltip("Removed once this far behind the ship after a miss (m).")]
    [Min(0f)] public float missDespawnDistance = 15f;
}
