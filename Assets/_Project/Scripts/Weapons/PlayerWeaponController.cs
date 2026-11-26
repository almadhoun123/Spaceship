using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spaceship
{
    /// <summary>
    /// One player's weapons (GDD 2.3): Laser (tap), Machine Gun (hold), Area Destruction (draw a shape).
    /// Receives pointer events from <see cref="SplitTouchInput"/>; weapon buttons call <see cref="SelectWeapon"/>.
    /// </summary>
    public class PlayerWeaponController : MonoBehaviour
    {
        [SerializeField] PlayerSide side;
        [SerializeField] Camera aimCamera;
        [SerializeField] ShipStatus ship;
        [Tooltip("Index 0 = Laser, 1 = Machine Gun, 2 = Area Destruction.")]
        [SerializeField] WeaponDefinition[] weapons = new WeaponDefinition[3];
        [SerializeField] LayerMask hitMask = ~0;
        [Tooltip("Optional: shows the shape being drawn with the area weapon (screen-space points are projected in front of the camera).")]
        [SerializeField] LineRenderer drawLine;
        [SerializeField] float drawLineDepth = 2f;

        public PlayerSide Side => side;
        public int SelectedIndex { get; private set; }
        public WeaponDefinition Selected => weapons != null && SelectedIndex < weapons.Length ? weapons[SelectedIndex] : null;

        /// <summary>Raised for each ray shot: weapon, world origin, world hit point (or max range), hit target (may be null).</summary>
        public event Action<WeaponDefinition, Vector3, Vector3, Damageable> Fired;
        /// <summary>Raised when an area shape is completed, with the targets it hit.</summary>
        public event Action<WeaponDefinition, IReadOnlyList<Damageable>> AreaFired;
        public event Action<int> WeaponSelected;
        public event Action OutOfEnergy;

        readonly List<Vector2> shape = new();
        readonly List<Damageable> areaHits = new();
        float nextFireTime;

        void Awake()
        {
            if (!aimCamera) aimCamera = Camera.main;
        }

        public void SelectWeapon(int index)
        {
            if (weapons == null || index < 0 || index >= weapons.Length) return;
            CancelShape();
            SelectedIndex = index;
            WeaponSelected?.Invoke(index);
        }

        public void PointerDown(Vector2 screenPos)
        {
            var w = Selected;
            if (!w) return;
            switch (w.type)
            {
                case WeaponType.Laser:
                case WeaponType.MachineGun:
                    nextFireTime = 0f;
                    TryFireRay(w, screenPos);
                    break;
                case WeaponType.AreaDestruction:
                    shape.Clear();
                    AddShapePoint(screenPos);
                    break;
            }
        }

        public void PointerHeld(Vector2 screenPos)
        {
            var w = Selected;
            if (!w) return;
            if (w.type == WeaponType.MachineGun) TryFireRay(w, screenPos);
            else if (w.type == WeaponType.AreaDestruction) AddShapePoint(screenPos);
        }

        public void PointerUp(Vector2 screenPos)
        {
            var w = Selected;
            if (!w || w.type != WeaponType.AreaDestruction) return;
            AddShapePoint(screenPos);
            if (shape.Count >= w.minShapePoints) FireArea(w);
            CancelShape();
        }

        void TryFireRay(WeaponDefinition w, Vector2 screenPos)
        {
            if (Time.time < nextFireTime) return;
            nextFireTime = Time.time + w.fireInterval;
            if (ship && !ship.TrySpendEnergy(w.energyCost)) { OutOfEnergy?.Invoke(); return; }

            var ray = aimCamera.ScreenPointToRay(screenPos);
            Damageable target = null;
            var end = ray.origin + ray.direction * w.range;
            if (Physics.Raycast(ray, out var hit, w.range, hitMask, QueryTriggerInteraction.Collide))
            {
                end = hit.point;
                target = hit.collider.GetComponentInParent<Damageable>();
                if (target) target.TakeDamage(w.damage, side);
            }
            Fired?.Invoke(w, ray.origin, end, target);
        }

        void FireArea(WeaponDefinition w)
        {
            if (ship && !ship.TrySpendEnergy(w.energyCost)) { OutOfEnergy?.Invoke(); return; }
            areaHits.Clear();
            // Copy first: destroying targets modifies Damageable.Active.
            areaHits.AddRange(Damageable.Active);
            for (int i = areaHits.Count - 1; i >= 0; i--)
            {
                var sp = aimCamera.WorldToScreenPoint(areaHits[i].transform.position);
                if (sp.z <= 0f || !ScreenShape.Contains(shape, sp)) areaHits.RemoveAt(i);
            }
            AreaFired?.Invoke(w, areaHits);
            foreach (var d in areaHits) if (d) d.TakeDamage(w.damage, side);
        }

        void AddShapePoint(Vector2 p)
        {
            if (shape.Count > 0 && (shape[^1] - p).sqrMagnitude < 25f) return; // ignore < 5px jitter
            shape.Add(p);
            if (!drawLine) return;
            drawLine.positionCount = shape.Count;
            drawLine.SetPosition(shape.Count - 1, aimCamera.ScreenToWorldPoint(new Vector3(p.x, p.y, drawLineDepth)));
        }

        void CancelShape()
        {
            shape.Clear();
            if (drawLine) drawLine.positionCount = 0;
        }
    }

    public static class ScreenShape
    {
        /// <summary>Even-odd point-in-polygon test; the polygon is closed implicitly.</summary>
        public static bool Contains(IReadOnlyList<Vector2> poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                var a = poly[i]; var b = poly[j];
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }
    }
}
