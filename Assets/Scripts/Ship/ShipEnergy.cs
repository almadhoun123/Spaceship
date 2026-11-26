using System;
using UnityEngine;

/// <summary>
/// One shared energy pool for both players. Weapons spend it; it refills on its own
/// a short moment after the last spend.
/// </summary>
public class ShipEnergy : ShipStat
{
    [SerializeField] ShipSettings settings;

    float current;
    float regenResumeTime;

    public override float Current => current;
    public override float Max => settings.maxEnergy;

    /// <summary>Raised when something tried to spend more energy than is left.</summary>
    public event Action SpendFailed;

    void Awake() => current = settings.maxEnergy;

    void Start() => RaiseChanged();

    void Update()
    {
        if (current >= Max || Time.time < regenResumeTime) return;
        current = Mathf.Min(Max, current + settings.energyRegenPerSecond * Time.deltaTime);
        RaiseChanged();
    }

    /// <summary>Spends energy if there is enough. Returns false (and spends nothing) if not.</summary>
    public bool TrySpend(float amount)
    {
        if (amount > current)
        {
            SpendFailed?.Invoke();
            return false;
        }
        current -= amount;
        regenResumeTime = Time.time + settings.energyRegenDelay;
        RaiseChanged();
        return true;
    }
}
