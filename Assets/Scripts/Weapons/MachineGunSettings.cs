using UnityEngine;

/// <summary>Tunable values for weapon 2: rapid low-damage beams while held. Uses heat, not energy.</summary>
[CreateAssetMenu(fileName = "MachineGunSettings", menuName = "Spaceships/Machine Gun Settings")]
public class MachineGunSettings : ScriptableObject
{
    [Header("Damage")]
    [Min(0f)] public float damage = 10f;
    [Tooltip("Shots per second while held.")]
    [Min(0.1f)] public float fireRate = 10f;

    [Header("Aiming")]
    [Tooltip("How far a shot flies before it disappears (m). Also how far aiming looks for a target.")]
    [Min(1f)] public float range = 220f;
    [Tooltip("Aim assist: how far from the held line a target can be and still be picked (m).")]
    [Min(0f)] public float aimAssistRadius = 1f;
    public LayerMask targetLayers;

    [Header("Heat")]
    [Tooltip("Heat added per shot. The gun overheats at 1, so holding lasts 1 / (heatPerShot x fireRate) seconds " +
             "(1/70 at 10 shots/s = 7 s).")]
    [Range(0f, 1f)] public float heatPerShot = 1f / 70f;
    [Tooltip("Heat lost per second while not firing.")]
    [Min(0f)] public float coolPerSecond = 0.4f;
    [Tooltip("Seconds the gun is locked after overheating.")]
    [Min(0f)] public float overheatDuration = 3f;

    [Header("Projectile")]
    public Color coldColor = new Color(1f, 0.9f, 0.1f);
    public Color hotColor = new Color(1f, 0.1f, 0.05f);
    [Tooltip("How fast each shot flies (m/s).")]
    [Min(1f)] public float boltSpeed = 350f;
    [Tooltip("Length of each streak (m).")]
    [Min(0.05f)] public float boltLength = 3f;
    [Tooltip("Thickness of each streak (m).")]
    [Min(0.01f)] public float beamWidth = 0.12f;
    [Tooltip("How close a shot has to pass to something to hit it (m).")]
    [Min(0f)] public float boltHitRadius = 0.5f;
}
