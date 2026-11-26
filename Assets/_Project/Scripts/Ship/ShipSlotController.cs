using UnityEngine;

namespace Spaceship
{
    /// <summary>
    /// Moves the ship body between the five predefined rail positions (GDD 2.4).
    /// Put this on a child of the RailFollower so offsets are local to the rail.
    /// Wire the up/down/left/right/center buttons to the public methods.
    /// </summary>
    public class ShipSlotController : MonoBehaviour
    {
        [SerializeField] float horizontalOffset = 6f;
        [SerializeField] float verticalOffset = 4f;
        [SerializeField] float moveSmoothTime = 0.25f;
        [SerializeField] float maxBankAngle = 20f;

        public RailSlot CurrentSlot { get; private set; } = RailSlot.Center;

        Vector3 velocity;

        public void GoCenter() => SetSlot(RailSlot.Center);
        public void GoUp() => SetSlot(RailSlot.Up);
        public void GoDown() => SetSlot(RailSlot.Down);
        public void GoLeft() => SetSlot(RailSlot.Left);
        public void GoRight() => SetSlot(RailSlot.Right);

        public void SetSlot(RailSlot slot) => CurrentSlot = slot;

        Vector3 TargetOffset => CurrentSlot switch
        {
            RailSlot.Up => new Vector3(0, verticalOffset, 0),
            RailSlot.Down => new Vector3(0, -verticalOffset, 0),
            RailSlot.Left => new Vector3(-horizontalOffset, 0, 0),
            RailSlot.Right => new Vector3(horizontalOffset, 0, 0),
            _ => Vector3.zero,
        };

        void Update()
        {
            transform.localPosition = Vector3.SmoothDamp(transform.localPosition, TargetOffset, ref velocity, moveSmoothTime);
            // Bank into sideways movement for feel.
            float bank = Mathf.Clamp(-velocity.x * 2f, -maxBankAngle, maxBankAngle);
            transform.localRotation = Quaternion.Euler(0, 0, bank);
        }
    }
}
