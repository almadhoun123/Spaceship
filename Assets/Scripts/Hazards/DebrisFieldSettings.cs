using UnityEngine;

/// <summary>Tunable values for the random debris placed along the rail.</summary>
[CreateAssetMenu(fileName = "DebrisFieldSettings", menuName = "Spaceships/Debris Field Settings")]
public class DebrisFieldSettings : ScriptableObject
{
    [Min(0)] public int count = 40;
    [Tooltip("Damage a piece can take before it breaks.")]
    [Min(1f)] public float health = 30f;
    [Tooltip("No debris closer than this to the start of the rail, in metres.")]
    [Min(0f)] public float startDistance = 80f;
    [Tooltip("No debris closer than this to the end of the rail, in metres.")]
    [Min(0f)] public float endMargin = 20f;
    [Min(0.1f)] public float minDiameter = 2f;
    [Min(0.1f)] public float maxDiameter = 4f;
    [Tooltip("Random wobble around the chosen position, in metres.")]
    [Min(0f)] public float positionJitter = 1f;

    [Header("Yellow rock clouds (jam the weapons instead of damaging the ship)")]
    [Min(0)] public int cloudCount = 3;
    [Tooltip("Overall size of a cloud (m). A little bigger than the biggest rock.")]
    [Min(0.1f)] public float cloudDiameter = 5f;
    [Tooltip("Damage a cloud can take before it breaks up.")]
    [Min(1f)] public float cloudHealth = 30f;
    [Tooltip("How long flying into a cloud jams all weapons (s).")]
    [Min(0f)] public float weaponsDisabledSeconds = 3f;
}
