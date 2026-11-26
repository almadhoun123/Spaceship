using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// Turns touches (and the mouse, for testing in the Editor) into cockpit actions.
/// A touch that starts on a cockpit button presses it. Any other touch belongs, for its whole
/// life, to the player whose half of the screen it started on: its down, move and up go to
/// that player's weapons (tap, hold and draw). Every touch is tracked on its own, so the
/// two players never block each other.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CockpitPointerInput : MonoBehaviour
{
    [Tooltip("The cockpit's buttons and panels.")]
    [SerializeField] LayerMask buttonLayers;
    [SerializeField] float maxDistance = 10f;
    [Tooltip("Player 1: left half of the screen.")]
    [SerializeField] PlayerWeapons leftPlayer;
    [Tooltip("Player 2: right half of the screen.")]
    [SerializeField] PlayerWeapons rightPlayer;

    const int MouseId = -1;

    Camera cam;
    readonly Dictionary<int, PlayerWeapons> owners = new Dictionary<int, PlayerWeapons>();
    readonly Dictionary<int, CockpitButton> heldButtons = new Dictionary<int, CockpitButton>();

    void Awake()
    {
        cam = GetComponent<Camera>();
        // Each player's aiming and brush stay on their own half.
        if (leftPlayer != null) leftPlayer.OnRightHalf = false;
        if (rightPlayer != null) rightPlayer.OnRightHalf = true;
    }
    void OnEnable() => EnhancedTouchSupport.Enable();
    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        owners.Clear();
        foreach (var kv in heldButtons) kv.Value.Release(kv.Key);
        heldButtons.Clear();
    }

    void Update()
    {
        foreach (var touch in Touch.activeTouches)
        {
            switch (touch.phase)
            {
                case TouchPhase.Began: PointerDown(touch.touchId, touch.screenPosition); break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary: PointerMove(touch.touchId, touch.screenPosition); break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled: PointerUp(touch.touchId, touch.screenPosition); break;
            }
        }

        var mouse = Mouse.current;
        if (mouse != null)
        {
            var pos = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame) PointerDown(MouseId, pos);
            else if (mouse.leftButton.isPressed) PointerMove(MouseId, pos);
            if (mouse.leftButton.wasReleasedThisFrame) PointerUp(MouseId, pos);
        }
    }

    public void PointerDown(int id, Vector2 screenPosition)
    {
        // A touch on a screen menu (start menu, test panel) belongs to the menu, not the cockpit.
        if (IsOverScreenUI(screenPosition)) return;

        // Landed on the cockpit: press the button (held until this finger lifts), not a weapon touch.
        if (TryHitCockpit(screenPosition, out var button))
        {
            if (button != null)
            {
                button.PressDown(id);
                heldButtons[id] = button;
            }
            return;
        }

        var player = screenPosition.x < Screen.width * 0.5f ? leftPlayer : rightPlayer;
        if (player == null) return;
        owners[id] = player;
        player.PointerDown(id, screenPosition);
    }

    public void PointerMove(int id, Vector2 screenPosition)
    {
        if (owners.TryGetValue(id, out var player)) player.PointerMove(id, screenPosition);
    }

    public void PointerUp(int id, Vector2 screenPosition)
    {
        if (heldButtons.TryGetValue(id, out var button))
        {
            heldButtons.Remove(id);
            button.Release(id);
        }
        if (!owners.TryGetValue(id, out var player)) return;
        owners.Remove(id);
        player.PointerUp(id, screenPosition);
    }

    /// <summary>A quick tap: down and up at the same spot.</summary>
    public void Tap(Vector2 screenPosition, int id = 1000)
    {
        PointerDown(id, screenPosition);
        PointerUp(id, screenPosition);
    }

    /// <summary>
    /// Presses the cockpit button under a screen position, if there is one.
    /// Returns true when the touch landed on the cockpit (a button or a panel).
    /// </summary>
    public bool TryPressAt(Vector2 screenPosition)
    {
        if (!TryHitCockpit(screenPosition, out var button)) return false;
        if (button != null) button.Press();
        return true;
    }

    static readonly List<UnityEngine.EventSystems.RaycastResult> uiHits = new List<UnityEngine.EventSystems.RaycastResult>();

    static bool IsOverScreenUI(Vector2 screenPosition)
    {
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es == null) return false;
        uiHits.Clear();
        es.RaycastAll(new UnityEngine.EventSystems.PointerEventData(es) { position = screenPosition }, uiHits);
        return uiHits.Count > 0;
    }

    /// <summary>Is there cockpit (a panel or a button) under this screen point? Gives the button, if any.</summary>
    bool TryHitCockpit(Vector2 screenPosition, out CockpitButton button)
    {
        button = null;
        // The cockpit moves with the ship every frame, but colliders only follow at the
        // physics step. Bring them up to date so the ray hits what the player sees.
        Physics.SyncTransforms();

        var ray = cam.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out var hit, maxDistance, buttonLayers, QueryTriggerInteraction.Collide))
            return false;

        button = hit.collider.GetComponentInParent<CockpitButton>();
        return true;
    }
}
