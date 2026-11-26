using System;
using UnityEngine;

namespace Spaceship
{
    /// <summary>Shared ship health and energy (GDD 2.5). One instance on the ship.</summary>
    public class ShipStatus : MonoBehaviour
    {
        [SerializeField] float maxHealth = 100f;
        [SerializeField] float maxEnergy = 100f;
        [Tooltip("Energy regained per second.")]
        [SerializeField] float energyRegen = 4f;

        public float Health { get; private set; }
        public float Energy { get; private set; }
        public float MaxHealth => maxHealth;
        public float MaxEnergy => maxEnergy;
        public bool IsDestroyed => Health <= 0f;

        public event Action<ShipStatus> Changed;
        public event Action ShipDestroyed;

        void Awake()
        {
            Health = maxHealth;
            Energy = maxEnergy;
        }

        void Update()
        {
            if (IsDestroyed || Energy >= maxEnergy) return;
            Energy = Mathf.Min(maxEnergy, Energy + energyRegen * Time.deltaTime);
            Changed?.Invoke(this);
        }

        public bool TrySpendEnergy(float amount)
        {
            if (IsDestroyed || Energy < amount) return false;
            Energy -= amount;
            Changed?.Invoke(this);
            return true;
        }

        public void TakeDamage(float amount)
        {
            if (IsDestroyed) return;
            Health = Mathf.Max(0f, Health - amount);
            Changed?.Invoke(this);
            if (IsDestroyed) ShipDestroyed?.Invoke();
        }

        public void Repair(float amount)
        {
            if (IsDestroyed) return;
            Health = Mathf.Min(maxHealth, Health + amount);
            Changed?.Invoke(this);
        }
    }
}
