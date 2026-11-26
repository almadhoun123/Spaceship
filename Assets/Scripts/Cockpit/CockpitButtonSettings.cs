using UnityEngine;

/// <summary>Tunable values for how cockpit buttons press in and spring back.</summary>
[CreateAssetMenu(fileName = "CockpitButtonSettings", menuName = "Spaceships/Cockpit Button Settings")]
public class CockpitButtonSettings : ScriptableObject
{
    [Tooltip("How far the button sinks into the panel, in the cockpit's local units.")]
    [Min(0f)] public float pressDepth = 0.015f;
    [Min(0.01f)] public float pressDuration = 0.05f;
    [Min(0.01f)] public float releaseDuration = 0.12f;
}
