using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spaceship
{
    /// <summary>
    /// The center Shared Weapon (GDD 2.3): both players must confirm within a short window,
    /// then it hits every target in view at a large energy cost.
    /// Wire the left player's button to ConfirmLeft and the right player's to ConfirmRight.
    /// </summary>
    public class SharedWeaponController : MonoBehaviour
    {
        [SerializeField] ShipStatus ship;
        [SerializeField] Camera viewCamera;
        [SerializeField] float energyCost = 60f;
        [SerializeField] float damage = 100f;
        [Tooltip("Seconds the second player has to confirm after the first.")]
        [SerializeField] float confirmWindow = 3f;
        [SerializeField] float cooldown = 10f;

        public bool LeftConfirmed { get; private set; }
        public bool RightConfirmed { get; private set; }
        public float CooldownRemaining => Mathf.Max(0f, readyTime - Time.time);
        public bool IsReady => CooldownRemaining <= 0f && ship && ship.Energy >= energyCost;

        public event Action<bool, bool> ConfirmationChanged;
        public event Action<IReadOnlyList<Damageable>> Fired;
        public event Action NotEnoughEnergy;

        readonly List<Damageable> hits = new();
        float firstConfirmTime;
        float readyTime;

        void Awake()
        {
            if (!viewCamera) viewCamera = Camera.main;
        }

        public void ConfirmLeft() => Confirm(PlayerSide.Left);
        public void ConfirmRight() => Confirm(PlayerSide.Right);

        public void Confirm(PlayerSide side)
        {
            if (CooldownRemaining > 0f) return;
            if (!LeftConfirmed && !RightConfirmed) firstConfirmTime = Time.time;
            if (side == PlayerSide.Left) LeftConfirmed = true; else RightConfirmed = true;
            ConfirmationChanged?.Invoke(LeftConfirmed, RightConfirmed);
            if (LeftConfirmed && RightConfirmed) Fire();
        }

        void Update()
        {
            if ((LeftConfirmed ^ RightConfirmed) && Time.time - firstConfirmTime > confirmWindow) ResetConfirm();
        }

        void Fire()
        {
            ResetConfirm();
            if (!ship || !ship.TrySpendEnergy(energyCost)) { NotEnoughEnergy?.Invoke(); return; }
            readyTime = Time.time + cooldown;

            hits.Clear();
            hits.AddRange(Damageable.Active);
            for (int i = hits.Count - 1; i >= 0; i--)
            {
                var vp = viewCamera.WorldToViewportPoint(hits[i].transform.position);
                if (vp.z <= 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f) hits.RemoveAt(i);
            }
            Fired?.Invoke(hits);
            foreach (var d in hits) if (d) d.TakeDamage(damage, null);
        }

        void ResetConfirm()
        {
            LeftConfirmed = RightConfirmed = false;
            ConfirmationChanged?.Invoke(false, false);
        }
    }
}
