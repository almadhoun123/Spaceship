using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spaceship
{
    /// <summary>Anything players can shoot: enemy ships, debris, asteroids, bosses (GDD 2.1).</summary>
    public class Damageable : MonoBehaviour
    {
        static readonly List<Damageable> active = new();
        public static IReadOnlyList<Damageable> Active => active;

        [SerializeField] float maxHealth = 10f;
        [SerializeField] int scoreValue = 100;
        [Tooltip("Optional effect spawned when destroyed.")]
        [SerializeField] GameObject destroyEffect;

        public float Health { get; private set; }
        public int ScoreValue => scoreValue;

        /// <summary>Raised with the destroyed target and the side that landed the final hit (null = shared weapon).</summary>
        public static event Action<Damageable, PlayerSide?> AnyDestroyed;

        void Awake() => Health = maxHealth;
        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);

        public void TakeDamage(float amount, PlayerSide? source = null)
        {
            if (Health <= 0f) return;
            Health -= amount;
            if (Health > 0f) return;
            AnyDestroyed?.Invoke(this, source);
            if (destroyEffect) Instantiate(destroyEffect, transform.position, transform.rotation);
            Destroy(gameObject);
        }
    }
}
