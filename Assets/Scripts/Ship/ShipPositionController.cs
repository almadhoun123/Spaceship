using UnityEngine;

/// <summary>
/// Moves the ship between the five fixed positions around the rail.
/// Sits on the ShipOffset object, a child of the object that follows the rail.
/// </summary>
public class ShipPositionController : MonoBehaviour
{
    [SerializeField] ShipSettings settings;

    ShipLane currentLane = ShipLane.Center;
    Vector3 velocity;

    public ShipLane CurrentLane => currentLane;
    public ShipSettings Settings => settings;

    public void SetLane(ShipLane lane) => currentLane = lane;

    /// <summary>
    /// An arrow was pressed: step one place that way from where the ship is now.
    /// Down + Up = Center, Left + Right = Center, Center + Up = Up; pushing further the same way
    /// stays put. From a side position an arrow at right angles (Left + Up) goes to that position.
    /// The Center button always goes to the center.
    /// </summary>
    public void Step(ShipLane direction)
    {
        if (direction == ShipLane.Center) { currentLane = ShipLane.Center; return; }

        Vector2Int cur = ToGrid(currentLane), dir = ToGrid(direction);
        bool sameAxis = currentLane == ShipLane.Center || (cur.x != 0) == (dir.x != 0);
        if (!sameAxis) { currentLane = direction; return; }

        var next = cur + dir;
        next.Clamp(new Vector2Int(-1, -1), new Vector2Int(1, 1));
        currentLane = FromGrid(next);
    }

    static Vector2Int ToGrid(ShipLane lane)
    {
        switch (lane)
        {
            case ShipLane.Up: return Vector2Int.up;
            case ShipLane.Down: return Vector2Int.down;
            case ShipLane.Left: return Vector2Int.left;
            case ShipLane.Right: return Vector2Int.right;
            default: return Vector2Int.zero;
        }
    }

    static ShipLane FromGrid(Vector2Int g)
    {
        if (g.y > 0) return ShipLane.Up;
        if (g.y < 0) return ShipLane.Down;
        if (g.x < 0) return ShipLane.Left;
        if (g.x > 0) return ShipLane.Right;
        return ShipLane.Center;
    }

    void OnEnable()
    {
        currentLane = ShipLane.Center;
        transform.localPosition = Vector3.zero;
        velocity = Vector3.zero;
    }

    void Update()
    {
        var target = settings.GetOffset(currentLane);
        transform.localPosition = Vector3.SmoothDamp(transform.localPosition, target, ref velocity, settings.moveSmoothTime);
    }
}
