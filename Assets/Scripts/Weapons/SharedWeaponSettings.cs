using UnityEngine;

/// <summary>
/// Tunable values for the shared weapon in the middle of the console: both players arm it
/// with their side pad, then the centre button fires a blast around the ship.
/// </summary>
[CreateAssetMenu(fileName = "SharedWeaponSettings", menuName = "Spaceships/Shared Weapon Settings")]
public class SharedWeaponSettings : ScriptableObject
{
    [Header("Arming (one pad per player)")]
    [Tooltip("Off: a tap arms a pad, and it stays pushed down for Arm Duration.\n" +
             "On: a pad is armed only while a finger holds it (the intended two-player version; needs two touches).")]
    public bool requireHold = false;
    [Tooltip("How long a tapped pad stays armed (s). Not used when Require Hold is on.")]
    [Min(0.1f)] public float armDuration = 1f;

    [Header("Blast")]
    [Min(0f)] public float energyCost = 60f;
    [Tooltip("Damage to every enemy and piece of debris in range. High enough to destroy anything.")]
    [Min(0f)] public float damage = 1000f;
    [Tooltip("Everything this close to the ship is hit (m).")]
    [Min(1f)] public float radius = 90f;
    [Tooltip("Also clears enemy rays already flying.")]
    public bool clearEnemyRays = true;
    [Tooltip("Shortest time between two blasts (s).")]
    [Min(0f)] public float cooldown = 1f;

    [Header("Look")]
    [Tooltip("Centre button colour while both pads are armed and it can fire.")]
    public Color readyColor = new Color(0.75f, 1f, 0.35f);
    public Color blastColor = new Color(0.6f, 0.95f, 1f, 0.45f);
    [Min(0.05f)] public float blastDuration = 0.5f;
}
