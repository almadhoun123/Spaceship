using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A pressable 3D button on the cockpit panel. Sinks into the panel when pressed, then springs back.
/// A latched button (such as the selected weapon) stays pressed down until it is released.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CockpitButton : MonoBehaviour
{
    [SerializeField] CockpitButtonSettings settings;
    public UnityEvent Pressed = new UnityEvent();

    Vector3 restPosition;
    float animTime = -1f;
    bool latched;

    /// <summary>When true the button stays pushed into the panel.</summary>
    public bool Latched
    {
        get => latched;
        set
        {
            latched = value;
            if (animTime < 0f) SetDepth(latched ? 1f : 0f);
        }
    }

    readonly System.Collections.Generic.HashSet<int> holders = new System.Collections.Generic.HashSet<int>();

    /// <summary>True while at least one finger (or the mouse) is still on the button.</summary>
    public bool IsHeld => holders.Count > 0;

    void Awake() => restPosition = transform.localPosition;

    void OnDisable() => holders.Clear();

    /// <summary>A press with no hold (a tap, or code).</summary>
    public void Press()
    {
        animTime = 0f;
        Pressed.Invoke();
    }

    /// <summary>A finger came down on the button. It counts as held until <see cref="Release"/>.</summary>
    public void PressDown(int pointerId)
    {
        holders.Add(pointerId);
        Press();
    }

    /// <summary>That finger lifted.</summary>
    public void Release(int pointerId) => holders.Remove(pointerId);

    void Update()
    {
        if (animTime < 0f) return;

        animTime += Time.deltaTime;
        float down = settings.pressDuration, up = settings.releaseDuration;
        float rest = latched ? 1f : 0f;
        float depth;
        if (animTime < down) depth = animTime / down;
        else if (animTime < down + up) depth = Mathf.Lerp(1f, rest, (animTime - down) / up);
        else { depth = rest; animTime = -1f; }

        SetDepth(depth);
    }

    // +Z in the cockpit's space points away from the players, into the panel.
    void SetDepth(float depth) =>
        transform.localPosition = restPosition + Vector3.forward * (settings.pressDepth * depth);
}
