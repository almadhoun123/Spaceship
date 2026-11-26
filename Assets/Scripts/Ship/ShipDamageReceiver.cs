using UnityEngine;

/// <summary>
/// Sits on the ship's hull trigger. Applies damage when the hull touches debris or an enemy ray,
/// and says where the hit came from so the view can be jolted away from it.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShipDamageReceiver : MonoBehaviour
{
    [SerializeField] ShipHealth health;
    [SerializeField] ShipSettings settings;
    [Tooltip("Jammed by yellow rock clouds.")]
    [SerializeField] WeaponLockout weaponLockout;
    [SerializeField] DebrisFieldSettings debrisSettings;

    void OnTriggerEnter(Collider other)
    {
        var ray = other.GetComponentInParent<EnemyProjectile>();
        if (ray != null && ray.isActiveAndEnabled)
        {
            // The ray came from where it was a moment ago: a little back along its flight.
            var from = ray.transform.position - ray.transform.forward * 5f;
            health.TakeDamagePercent(ray.DamagePercent, from);
            ray.Despawn();
            return;
        }

        var debris = other.GetComponentInParent<Debris>();
        if (debris == null || !debris.isActiveAndEnabled) return;

        // Yellow rock cloud: no damage, but the weapons are jammed for a while.
        if (debris is DustCloud)
        {
            if (weaponLockout != null) weaponLockout.Trigger(debrisSettings.weaponsDisabledSeconds);
            debris.Hit();
            return;
        }

        health.TakeDamagePercent(settings.debrisDamagePercent, debris.transform.position);
        debris.Hit();
    }
}
