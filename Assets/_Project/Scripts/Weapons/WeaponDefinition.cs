using UnityEngine;

namespace Spaceship
{
    /// <summary>Tunable weapon stats (GDD 2.3, Weapon Balancing).</summary>
    [CreateAssetMenu(menuName = "Spaceship/Weapon Definition", fileName = "Weapon_")]
    public class WeaponDefinition : ScriptableObject
    {
        public WeaponType type;
        public string displayName;
        public Sprite icon;
        [Min(0)] public float damage = 10f;
        [Tooltip("Energy per shot (laser/machine gun) or per use (area).")]
        [Min(0)] public float energyCost = 2f;
        [Tooltip("Seconds between shots while holding (machine gun).")]
        [Min(0.01f)] public float fireInterval = 0.1f;
        [Tooltip("Max hit distance for ray weapons.")]
        public float range = 500f;
        [Tooltip("Area weapon: minimum drawn points before the shape counts.")]
        public int minShapePoints = 8;
    }
}
