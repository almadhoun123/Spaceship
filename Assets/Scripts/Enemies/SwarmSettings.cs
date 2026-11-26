using UnityEngine;

/// <summary>Tunable values for the swarm: a small group of weak enemies that fly together and shoot weak rays.</summary>
[CreateAssetMenu(fileName = "SwarmSettings", menuName = "Spaceships/Swarm Settings")]
public class SwarmSettings : ScriptableObject
{
    [Header("Spawning")]
    [Tooltip("How likely a swarm is, compared with the other enemy types' weights.")]
    [Min(0f)] public float spawnWeight = 1f;
    [Min(1)] public int groupSize = 5;
    [Tooltip("Most swarms on screen at once.")]
    [Min(1)] public int maxGroups = 2;

    [Header("Each member")]
    [Min(1f)] public float maxHealth = 20f;

    [Header("Movement")]
    [Min(0f)] public float approachSpeed = 30f;
    [Min(1f)] public float stopDistance = 45f;
    [Min(0f)] public float lookBehindDistance = 25f;
    [Tooltip("How far members spread around the swarm's centre (m).")]
    [Min(0f)] public float formationRadius = 3.5f;
    [Tooltip("How fast members circle the centre (degrees per second).")]
    public float orbitSpeed = 70f;
    [Tooltip("Up/down bobbing of each member (m).")]
    [Min(0f)] public float bobAmplitude = 0.6f;

    [Header("Shooting (only once stopped)")]
    [Min(0f)] public float firstShotDelay = 0.5f;
    [Min(0.1f)] public float fireInterval = 3f;
    [Min(0f)] public float fireIntervalRandom = 1.5f;
    [Min(1f)] public float projectileSpeed = 60f;
    [Min(0.1f)] public float projectileLifetime = 3f;
    [Tooltip("Percent of max ship health lost per ray that hits.")]
    [Range(0f, 100f)] public float projectileDamagePercent = 1f;
}
