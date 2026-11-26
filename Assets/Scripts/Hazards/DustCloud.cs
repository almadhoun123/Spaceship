using UnityEngine;

/// <summary>
/// A yellow cloud of small rocks. Works like debris (it sits on the lanes, can be shot down and
/// shows a health bar), but flying into it does not hurt the ship: it jams the weapons for a few
/// seconds instead (see <see cref="WeaponLockout"/>). The rocks turn slowly so it looks alive.
/// </summary>
public class DustCloud : Debris
{
    [Tooltip("How fast the cloud turns (degrees per second).")]
    [SerializeField] float spinSpeed = 25f;

    Vector3 spinAxis = Vector3.up;

    void OnEnable() => spinAxis = Random.onUnitSphere;

    void Update() => transform.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.World);
}
