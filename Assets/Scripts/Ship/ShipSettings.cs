using UnityEngine;

/// <summary>Tunable values for the player ship.</summary>
[CreateAssetMenu(fileName = "ShipSettings", menuName = "Spaceships/Ship Settings")]
public class ShipSettings : ScriptableObject
{
    [Header("Health")]
    [Min(1f)] public float maxHealth = 100f;
    [Tooltip("Percent of max health lost when the ship hits debris.")]
    [Range(0f, 100f)] public float debrisDamagePercent = 5f;
    [Tooltip("Seconds between the ship's health reaching zero and the level restarting.")]
    [Min(0f)] public float restartDelay = 1.5f;

    [Header("Energy (shared by both players)")]
    [Min(1f)] public float maxEnergy = 100f;
    [Tooltip("Energy regained per second.")]
    [Min(0f)] public float energyRegenPerSecond = 12f;
    [Tooltip("Seconds after spending energy before it starts to come back.")]
    [Min(0f)] public float energyRegenDelay = 0.6f;

    [Header("Positions (offsets from the rail)")]
    [Tooltip("Sideways distance of the Left / Right positions, in metres.")]
    [Min(0f)] public float horizontalOffset = 12f;
    [Tooltip("Vertical distance of the Up / Down positions, in metres.")]
    [Min(0f)] public float verticalOffset = 9f;
    [Tooltip("Roughly how long the ship takes to reach a new position, in seconds.")]
    [Min(0.01f)] public float moveSmoothTime = 0.15f;

    /// <summary>Local offset from the rail point for a position.</summary>
    public Vector3 GetOffset(ShipLane lane)
    {
        switch (lane)
        {
            case ShipLane.Up: return new Vector3(0f, verticalOffset, 0f);
            case ShipLane.Down: return new Vector3(0f, -verticalOffset, 0f);
            case ShipLane.Left: return new Vector3(-horizontalOffset, 0f, 0f);
            case ShipLane.Right: return new Vector3(horizontalOffset, 0f, 0f);
            default: return Vector3.zero;
        }
    }
}
