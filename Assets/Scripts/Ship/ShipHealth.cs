using System;
using UnityEngine;

/// <summary>Shared ship health for both players.</summary>
public class ShipHealth : ShipStat
{
    [SerializeField] ShipSettings settings;

    float current;

    public override float Current => current;
    public override float Max => settings.maxHealth;

    /// <summary>
    /// Raised for every hit with the percent of max health lost and, when known,
    /// where the hit came from (for effects that push the view away from it).
    /// </summary>
    public event Action<float, Vector3?> Hit;

    /// <summary>Raised once when health reaches zero.</summary>
    public event Action Died;

    public bool IsDead => current <= 0f;

    /// <summary>
    /// When true (test mode) the ship still takes hits, but a hit that would kill it
    /// refills health to full instead.
    /// </summary>
    public bool CannotDie { get; set; }

    void Awake() => current = settings.maxHealth;

    void Start() => RaiseChanged();

    public void TakeDamage(float amount, Vector3? source = null)
    {
        if (amount <= 0f || current <= 0f) return;
        float lost = Mathf.Min(amount, current);
        current -= lost;
        if (current <= 0f && CannotDie) current = Max;
        RaiseChanged();
        Hit?.Invoke(Max > 0f ? lost / Max * 100f : 0f, source);
        if (current <= 0f) Died?.Invoke();
    }

    /// <summary>Removes a percentage of max health. Pass where the hit came from if known.</summary>
    public void TakeDamagePercent(float percent, Vector3? source = null) => TakeDamage(Max * percent / 100f, source);

    public void ResetHealth()
    {
        current = Max;
        RaiseChanged();
    }
}
