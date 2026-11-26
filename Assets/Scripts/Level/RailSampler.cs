using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Gives positions and facings along the rail by real distance in metres, so the ship moves at an
/// even speed whatever the knot spacing or handle lengths, and faces a smoothed direction that
/// irons out kinks at the knots. The ship, debris and enemies all use it, so their lanes line up.
/// Rebuilds itself when the spline is edited, also during Play Mode.
/// </summary>
[RequireComponent(typeof(SplineContainer))]
public class RailSampler : MonoBehaviour
{
    [SerializeField] RailSettings settings;

    SplineContainer container;
    float[] lengths;   // distance from the start at each sample
    float[] ts;        // spline parameter at each sample
    bool dirty = true;

    public RailSettings Settings => settings;
    public SplineContainer Container => container != null ? container : container = GetComponent<SplineContainer>();

    /// <summary>Length of the rail in metres.</summary>
    public float Length { get { EnsureBuilt(); return lengths[lengths.Length - 1]; } }

    void OnEnable() { Spline.Changed += OnSplineChanged; dirty = true; }
    void OnDisable() => Spline.Changed -= OnSplineChanged;

    void OnSplineChanged(Spline spline, int knot, SplineModification mod)
    {
        if (Container != null && Container.Spline == spline) dirty = true;
    }

    void EnsureBuilt()
    {
        if (!dirty && lengths != null) return;
        dirty = false;

        // Walk the spline in fine, even parameter steps and add up the distance.
        var c = Container;
        float rough = Mathf.Max(1f, c.CalculateLength());
        int fine = Mathf.Clamp(Mathf.CeilToInt(rough * settings.samplesPerMetre * 4f), 64, 200000);
        int n = Mathf.Clamp(Mathf.CeilToInt(rough * settings.samplesPerMetre), 16, 50000);

        var fineLen = new float[fine + 1];
        Vector3 prev = c.EvaluatePosition(0f);
        for (int i = 1; i <= fine; i++)
        {
            Vector3 p = c.EvaluatePosition(i / (float)fine);
            fineLen[i] = fineLen[i - 1] + Vector3.Distance(prev, p);
            prev = p;
        }

        // Resample so that the table rows are evenly spaced in distance.
        float total = fineLen[fine];
        lengths = new float[n + 1];
        ts = new float[n + 1];
        int j = 0;
        for (int i = 0; i <= n; i++)
        {
            float d = total * i / n;
            while (j < fine - 1 && fineLen[j + 1] < d) j++;
            float seg = fineLen[j + 1] - fineLen[j];
            float f = seg > 1e-6f ? (d - fineLen[j]) / seg : 0f;
            lengths[i] = d;
            ts[i] = (j + Mathf.Clamp01(f)) / fine;
        }
    }

    /// <summary>The spline parameter (0..1) at a distance along the rail.</summary>
    public float DistanceToT(float distance)
    {
        EnsureBuilt();
        int n = lengths.Length - 1;
        float total = lengths[n];
        if (total <= 0f) return 0f;
        float x = Mathf.Clamp(distance, 0f, total) / total * n;
        int i = Mathf.Min(Mathf.FloorToInt(x), n - 1);
        return Mathf.Lerp(ts[i], ts[i + 1], x - i);
    }

    /// <summary>World position on the rail at a distance (m).</summary>
    public Vector3 Position(float distance) => Container.EvaluatePosition(DistanceToT(distance));

    /// <summary>
    /// World position and smoothed facing at a distance along the rail (m). The facing points along a
    /// line from a little behind to a little ahead, and (by default) keeps the horizon level.
    /// </summary>
    public void Sample(float distance, out Vector3 position, out Quaternion rotation)
    {
        float total = Length;
        distance = Mathf.Clamp(distance, 0f, total);
        position = Position(distance);

        float look = settings.lookDistance;
        Vector3 behind = Position(distance - look);
        Vector3 ahead = Position(distance + look);
        Vector3 forward = ahead - behind;
        if (forward.sqrMagnitude < 1e-6f)
            forward = (Vector3)math.normalizesafe(Container.EvaluateTangent(DistanceToT(distance)), new float3(0, 0, 1));
        forward.Normalize();

        Vector3 up;
        if (settings.levelHorizon)
        {
            up = Vector3.up;
        }
        else
        {
            // Average the knots' up direction over the same window so tilt changes are gentle.
            up = (Vector3)Container.EvaluateUpVector(DistanceToT(distance - look))
               + (Vector3)Container.EvaluateUpVector(DistanceToT(distance))
               + (Vector3)Container.EvaluateUpVector(DistanceToT(distance + look));
        }
        // Straight up or down: borrow a sideways reference so the facing stays defined.
        if (Vector3.Cross(forward, up).sqrMagnitude < 1e-4f) up = Vector3.Cross(forward, Vector3.right);

        rotation = Quaternion.LookRotation(forward, up);
    }
}
