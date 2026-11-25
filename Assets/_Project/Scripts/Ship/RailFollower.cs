using UnityEngine;
using UnityEngine.Splines;

namespace Spaceship
{
    /// <summary>Moves the ship along a predetermined spline route (GDD 2.1).</summary>
    public class RailFollower : MonoBehaviour
    {
        [SerializeField] SplineContainer rail;
        [Tooltip("Forward speed in meters per second.")]
        [SerializeField] float speed = 20f;
        [SerializeField] bool playOnStart = true;

        public float Progress01 { get; private set; }
        public bool IsMoving { get; set; }
        public bool ReachedEnd => Progress01 >= 1f;

        public event System.Action ReachedEndOfRail;

        float length;

        void Start()
        {
            if (rail == null) { Debug.LogWarning("RailFollower has no rail assigned.", this); enabled = false; return; }
            length = Mathf.Max(0.01f, rail.CalculateLength());
            IsMoving = playOnStart;
            Snap();
        }

        void Update()
        {
            if (!IsMoving || ReachedEnd) return;
            Progress01 = Mathf.Min(1f, Progress01 + speed * Time.deltaTime / length);
            Snap();
            if (ReachedEnd) ReachedEndOfRail?.Invoke();
        }

        void Snap()
        {
            rail.Evaluate(Progress01, out var pos, out var tangent, out var up);
            transform.position = pos;
            if (((Vector3)tangent).sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation((Vector3)tangent, (Vector3)up);
        }
    }
}
