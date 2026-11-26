using UnityEngine;

/// <summary>Tunable values for the basic enemy ship, its shots and how often it appears.</summary>
[CreateAssetMenu(fileName = "EnemySettings", menuName = "Spaceships/Enemy Settings")]
public class EnemySettings : ScriptableObject
{
    [Header("Health")]
    [Min(1f)] public float maxHealth = 100f;

    [Header("Movement")]
    [Tooltip("How far ahead of the ship, along the rail, enemies appear (m).")]
    [Min(0f)] public float spawnDistance = 180f;
    [Tooltip("Enemies fly toward the ship at this speed (m/s), on top of the ship's own speed.")]
    [Min(0f)] public float approachSpeed = 25f;
    [Tooltip("Enemies stop and hold at this distance in front of the ship (m).")]
    [Min(1f)] public float stopDistance = 35f;
    [Tooltip("Enemies point at a spot this far behind the ship (m), so they face roughly past it, not dead at it.")]
    [Min(0f)] public float lookBehindDistance = 25f;

    [Header("Where enemies sit (offsets from the rail, so each player gets their own)")]
    [Tooltip("Chance an enemy sits in the middle instead of on the left or right side.")]
    [Range(0f, 1f)] public float centerChance = 0.03f;
    [Tooltip("Middle enemies: sideways range either side of the rail (m).")]
    [Min(0f)] public float centerHalfWidth = 5f;
    [Tooltip("Side enemies: closest sideways distance from the rail (m). Keep beyond the ship's Left/Right positions.")]
    [Min(0f)] public float sideOffsetMin = 18f;
    [Tooltip("Side enemies: furthest sideways distance from the rail (m). Keep inside the tunnel.")]
    [Min(0f)] public float sideOffsetMax = 26f;
    [Tooltip("Up/down range either side of the rail (m). About the ship's Up/Down distance or a little more.")]
    [Min(0f)] public float verticalSpread = 11f;

    [Header("Keep the middle of the screen clear (shared by all enemy types)")]
    [Tooltip("Half-width of the clear band in the middle of the screen, as a fraction of the screen width " +
             "(0.06 = from 44% to 56% across, the width of the shared weapon pad). Enemies are pushed out to their side.")]
    [Range(0f, 0.3f)] public float clearBandHalfWidth = 0.065f;
    [Tooltip("Extra room between the band edge and an enemy's centre (m), so its body stays out too.")]
    [Min(0f)] public float clearBandMargin = 2.5f;
    [Tooltip("Enemies are never pushed further than this from the rail sideways (m). Keep inside the tunnel.")]
    [Min(0f)] public float maxSideOffset = 26f;
    [Tooltip("How quickly enemies slide out of the way when the ship changes position (s).")]
    [Min(0.01f)] public float clearBandSmoothTime = 0.25f;

    [Header("Shooting (only once stopped)")]
    [Tooltip("Delay between reaching the stop distance and the first shot (s).")]
    [Min(0f)] public float firstShotDelay = 0.75f;
    [Min(0.1f)] public float fireInterval = 2.5f;
    [Tooltip("Random extra time added to each interval, so enemies don't fire in sync (s).")]
    [Min(0f)] public float fireIntervalRandom = 0.75f;
    [Min(1f)] public float projectileSpeed = 70f;
    [Min(0.1f)] public float projectileLifetime = 3f;
    [Tooltip("Percent of max ship health lost per ray that hits.")]
    [Range(0f, 100f)] public float projectileDamagePercent = 3f;

    [Header("Spawning (timing is shared by all enemy types)")]
    [Tooltip("How likely a regular enemy is, compared with the other enemy types' weights.")]
    [Min(0f)] public float spawnWeight = 2f;
    [Min(0f)] public float firstSpawnDelay = 3f;
    [Min(0.1f)] public float spawnInterval = 5f;
    [Min(1)] public int maxActive = 3;
    [Tooltip("No new enemies when the spawn point would be closer than this to the end of the rail (m).")]
    [Min(0f)] public float endMargin = 20f;

    [Header("Leaving (entering a segment without enemies; shared by all enemy types)")]
    [Tooltip("How fast a leaving enemy moves along the rail (m/s). Faster than the ship, so it pulls away ahead.")]
    [Min(0f)] public float leaveSpeed = 70f;
    [Tooltip("How fast it also drifts out to its side and up (m/s).")]
    [Min(0f)] public float leaveSideSpeed = 12f;
    [Tooltip("After this long (s) it is gone.")]
    [Min(0.1f)] public float leaveDuration = 2.5f;
}
