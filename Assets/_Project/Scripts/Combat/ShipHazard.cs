using UnityEngine;

namespace Spaceship
{
    /// <summary>
    /// Damages the ship on contact (debris, asteroids, rammers).
    /// Needs a trigger collider; the ship needs a kinematic Rigidbody.
    /// </summary>
    public class ShipHazard : MonoBehaviour
    {
        [SerializeField] float contactDamage = 15f;
        [SerializeField] bool destroyOnHit = true;

        void OnTriggerEnter(Collider other)
        {
            var ship = other.GetComponentInParent<ShipStatus>();
            if (ship == null) return;
            ship.TakeDamage(contactDamage);
            if (destroyOnHit) Destroy(gameObject);
        }
    }
}
