using System;
using UnityEngine;

/// <summary>Base for a ship value shown on a cockpit bar (health, energy).</summary>
public abstract class ShipStat : MonoBehaviour
{
    public abstract float Current { get; }
    public abstract float Max { get; }
    public float Normalized => Max > 0f ? Current / Max : 0f;

    /// <summary>Raised with (current, max) whenever the value changes.</summary>
    public event Action<float, float> Changed;

    protected void RaiseChanged() => Changed?.Invoke(Current, Max);
}
