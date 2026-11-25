using UnityEngine;
using UnityEngine.UI;

namespace Spaceship
{
    /// <summary>Top information bar (GDD 2.5): drives health and energy fill Images (Image Type = Filled).</summary>
    public class ShipStatusBar : MonoBehaviour
    {
        [SerializeField] ShipStatus ship;
        [SerializeField] Image healthFill;
        [SerializeField] Image energyFill;

        void OnEnable()
        {
            if (!ship) return;
            ship.Changed += Refresh;
            Refresh(ship);
        }

        void OnDisable()
        {
            if (ship) ship.Changed -= Refresh;
        }

        void Refresh(ShipStatus s)
        {
            if (healthFill) healthFill.fillAmount = s.MaxHealth > 0 ? s.Health / s.MaxHealth : 0;
            if (energyFill) energyFill.fillAmount = s.MaxEnergy > 0 ? s.Energy / s.MaxEnergy : 0;
        }
    }
}
