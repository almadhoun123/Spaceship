using UnityEngine;

/// <summary>
/// Small health bar floating above an enemy or a piece of debris. Hidden at full health;
/// appears once it has been damaged. Always faces the camera and keeps the same size
/// whatever the scale of the object it belongs to.
/// </summary>
public class WorldHealthBar : MonoBehaviour
{
    [Tooltip("Used to find the top of the object.")]
    [SerializeField] Renderer target;
    [SerializeField] Transform background;
    [SerializeField] Transform fill;
    [Tooltip("Bar size in metres.")]
    [SerializeField] Vector2 size = new Vector2(4f, 0.45f);
    [Tooltip("Gap between the top of the object and the bar (m).")]
    [SerializeField] float gap = 0.8f;
    [Tooltip("Border around the fill (m).")]
    [SerializeField] float border = 0.08f;

    IHasHealth source;
    Transform cam;
    bool visible = true;

    void Awake() => source = GetComponentInParent<IHasHealth>();

    void LateUpdate()
    {
        float f = source != null ? source.HealthFraction : 1f;
        SetVisible(f > 0f && f < 1f);
        if (!visible) return;

        if (cam == null)
        {
            var main = Camera.main;
            if (main == null) return;
            cam = main.transform;
        }

        // Sit above the object (in screen terms), face the camera, ignore the parent's scale.
        var b = target.bounds;
        transform.SetPositionAndRotation(b.center + cam.up * (b.extents.y + gap), cam.rotation);
        float parentScale = transform.parent != null ? transform.parent.lossyScale.x : 1f;
        transform.localScale = Vector3.one / Mathf.Max(parentScale, 0.0001f);

        background.localPosition = Vector3.zero;
        background.localScale = new Vector3(size.x, size.y, 0.02f);

        float inner = size.x - border * 2f;
        fill.localScale = new Vector3(inner * f, size.y - border * 2f, 0.02f);
        fill.localPosition = new Vector3(-inner * 0.5f + inner * f * 0.5f, 0f, -0.02f); // -Z is toward the camera
    }

    void SetVisible(bool show)
    {
        if (show == visible) return;
        visible = show;
        background.gameObject.SetActive(show);
        fill.gameObject.SetActive(show);
    }
}
