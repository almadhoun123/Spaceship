using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Places the ship on the rail by real distance, so it moves at an even speed, and eases its
/// facing toward the rail's smoothed direction. SplineAnimate still keeps the time (play, pause,
/// speed, looping and the test slider); this replaces where it puts the ship and how it turns.
/// Runs before ShipMotion, which banks the view from this rotation.
/// </summary>
[DefaultExecutionOrder(-100)]
public class ShipRailFollower : MonoBehaviour
{
    [SerializeField] SplineAnimate timeKeeper;
    [SerializeField] RailSampler rail;

    float lastDistance = -1f;
    bool smoothing = true;

    /// <summary>The ship's distance along the rail in the current lap (m).</summary>
    public float Distance => Mathf.Repeat(timeKeeper.NormalizedTime, 1f) * rail.Length;

    /// <summary>
    /// On (normal play): even speed and eased, smoothed facing. Off (test mode): the ship follows the
    /// raw spline exactly as authored, knot tilts and kinks included, so the spline can be judged as it is.
    /// </summary>
    public bool Smoothing
    {
        get => smoothing;
        set
        {
            smoothing = value;
            timeKeeper.Alignment = value ? SplineAnimate.AlignmentMode.None : SplineAnimate.AlignmentMode.SplineElement;
            if (value) Snap();
        }
    }

    void Start() => Snap();

    void LateUpdate()
    {
        // Smoothing off: SplineAnimate places and turns the ship itself.
        if (!smoothing) return;

        float distance = Distance;
        rail.Sample(distance, out var position, out var target);

        var s = rail.Settings;
        bool jumped = lastDistance < 0f || Mathf.Abs(distance - lastDistance) > s.snapDistance;
        lastDistance = distance;

        transform.position = position;
        if (jumped || s.rotationSmoothTime <= 0f)
        {
            transform.rotation = target;
        }
        else if (Time.deltaTime > 0f)
        {
            float k = 1f - Mathf.Exp(-Time.deltaTime / s.rotationSmoothTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, k);
        }
    }

    /// <summary>Puts the ship exactly on the rail with no easing.</summary>
    public void Snap()
    {
        lastDistance = -1f;
        LateUpdate();
    }
}
