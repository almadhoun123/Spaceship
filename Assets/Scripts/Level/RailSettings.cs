using UnityEngine;

/// <summary>Tunable values for how the ship (and everything placed on the rail) follows the spline.</summary>
[CreateAssetMenu(menuName = "Spaceships/Rail Settings", fileName = "RailSettings")]
public class RailSettings : ScriptableObject
{
    [Tooltip("Distance samples per metre for the even-speed lookup. Higher = more exact, more memory.")]
    [Min(0.1f)] public float samplesPerMetre = 2f;

    [Tooltip("The ship faces along a line from this far behind to this far ahead (m). " +
             "Bigger = turns start earlier and are softer; kinks and sharp knots are ironed out.")]
    [Min(0.1f)] public float lookDistance = 12f;

    [Tooltip("Keep the horizon level (ignore the knots' roll). Turn off to follow each knot's tilt.")]
    public bool levelHorizon = true;

    [Tooltip("How long the ship takes to catch up with the rail's direction (s). 0 = no extra smoothing.")]
    [Min(0f)] public float rotationSmoothTime = 0.25f;

    [Tooltip("A jump along the rail longer than this (m) in one frame, such as the test slider or a new lap, " +
             "snaps the ship instead of swinging it round.")]
    [Min(1f)] public float snapDistance = 15f;
}
