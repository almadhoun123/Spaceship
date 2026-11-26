using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Spaceship
{
    /// <summary>
    /// Routes multi-touch input from the touch wall to the two players (GDD 1).
    /// A touch belongs to the half of the screen it started on, for its whole lifetime.
    /// Touches that start on UI (weapon / movement buttons) are left to the UI.
    /// In the Editor, mouse clicks are simulated as touches.
    /// </summary>
    public class SplitTouchInput : MonoBehaviour
    {
        [SerializeField] PlayerWeaponController leftPlayer;
        [SerializeField] PlayerWeaponController rightPlayer;

        // touchId -> owning player (null when the touch started on UI)
        readonly Dictionary<int, PlayerWeaponController> owners = new();
        readonly List<RaycastResult> uiHits = new();

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
#if UNITY_EDITOR
            TouchSimulation.Enable();
#endif
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            TouchSimulation.Disable();
#endif
            EnhancedTouchSupport.Disable();
            owners.Clear();
        }

        void Update()
        {
            foreach (var touch in Touch.activeTouches)
            {
                var pos = touch.screenPosition;
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        var owner = IsOverUI(pos) ? null : (pos.x < Screen.width * 0.5f ? leftPlayer : rightPlayer);
                        owners[touch.touchId] = owner;
                        if (owner) owner.PointerDown(pos);
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        if (owners.TryGetValue(touch.touchId, out var held) && held) held.PointerHeld(pos);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (owners.TryGetValue(touch.touchId, out var up) && up) up.PointerUp(pos);
                        owners.Remove(touch.touchId);
                        break;
                }
            }
        }

        bool IsOverUI(Vector2 screenPos)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            uiHits.Clear();
            es.RaycastAll(new PointerEventData(es) { position = screenPos }, uiHits);
            return uiHits.Count > 0;
        }
    }
}
