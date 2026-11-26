using UnityEngine;

/// <summary>
/// Arrow (or center) button: moves the ship one step that way from where it is now,
/// so Up while Down goes to the center. The center button always goes to the center.
/// </summary>
[RequireComponent(typeof(CockpitButton))]
public class ShipLaneButton : MonoBehaviour
{
    [SerializeField] ShipPositionController ship;
    [SerializeField] ShipLane lane;

    CockpitButton button;

    void Awake() => button = GetComponent<CockpitButton>();
    void OnEnable() => button.Pressed.AddListener(OnPressed);
    void OnDisable() => button.Pressed.RemoveListener(OnPressed);

    void OnPressed() => ship.Step(lane);
}
