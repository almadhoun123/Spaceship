using UnityEngine;

/// <summary>Tunable values for one weapon.</summary>
[CreateAssetMenu(fileName = "WeaponSettings", menuName = "Spaceships/Weapon Settings")]
public class WeaponSettings : ScriptableObject
{
    public string displayName = "Laser";

    [Header("Cost and damage")]
    [Min(0f)] public float energyCost = 5f;
    [Min(0f)] public float damage = 100f;
    [Tooltip("Shortest time between two shots from the same player (s).")]
    [Min(0f)] public float cooldown = 0.2f;

    [Header("Aiming")]
    [Tooltip("How far a shot flies before it disappears (m). Also how far aiming looks for a target.")]
    [Min(1f)] public float range = 250f;
    [Tooltip("Aim assist: how far from the tapped line a target can be and still be picked (m). " +
             "The shot then curves toward it.")]
    [Min(0f)] public float aimAssistRadius = 1.5f;
    [Tooltip("What the weapon can hit.")]
    public LayerMask targetLayers;

    [Header("Projectile")]
    [Tooltip("How fast the shot flies (m/s).")]
    [Min(1f)] public float boltSpeed = 400f;
    [Tooltip("Length of the glowing streak (m).")]
    [Min(0.05f)] public float boltLength = 14f;
    [Tooltip("Thickness of the streak (m).")]
    [Min(0.01f)] public float beamWidth = 0.3f;
    [Tooltip("How close the shot has to pass to something to hit it (m).")]
    [Min(0f)] public float boltHitRadius = 0.6f;
}
