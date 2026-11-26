using UnityEngine;

/// <summary>
/// Tunable values for weapon 3, circle select: press and drag a circle over targets to mark them,
/// lift to hit everything marked. The more of the screen painted, the less damage. Costs energy,
/// only when at least one target is marked.
/// </summary>
[CreateAssetMenu(fileName = "AreaWeaponSettings", menuName = "Spaceships/Area Weapon Settings")]
public class AreaWeaponSettings : ScriptableObject
{
    [Min(0f)] public float energyCost = 20f;

    [Header("Damage by painted size (size = fraction of the whole screen)")]
    [Min(0f)] public float maxDamage = 100f;
    [Min(0f)] public float minDamage = 10f;
    [Tooltip("Painting this much or less does max damage. Twice this much does half, and so on.")]
    [Range(0.001f, 1f)] public float fullDamageArea = 0.03f;
    [Tooltip("Targets further away than this can't be marked (m).")]
    [Min(1f)] public float range = 400f;

    [Header("Circle brush")]
    [Tooltip("Radius of the circle, as a fraction of the screen height.")]
    [Range(0.01f, 0.4f)] public float brushRadius = 0.08f;
    [Tooltip("Distance in front of the camera the circle and the marks are drawn at (m).")]
    [Min(0.06f)] public float lineDepth = 0.5f;
    [Min(0.0001f)] public float lineWidth = 0.004f;
    [Tooltip("Marks around selected targets are this much bigger than the target (pixels).")]
    [Min(0f)] public float markPadding = 10f;
    [Tooltip("How long the circle and marks stay on screen after it fires (s).")]
    [Min(0f)] public float showAfterFire = 0.35f;

    /// <summary>Damage for painting this fraction of the screen.</summary>
    public float DamageForArea(float areaFraction) =>
        Mathf.Clamp(maxDamage * fullDamageArea / Mathf.Max(areaFraction, 0.0001f), minDamage, maxDamage);
}
