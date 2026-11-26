using TMPro;
using UnityEngine;

/// <summary>
/// Shows a ship value (health or energy) on a cockpit bar: shrinks the fill block from the
/// right and writes the number in the middle. Flashes the number when energy runs short.
/// </summary>
public class StatBarDisplay : MonoBehaviour
{
    [SerializeField] ShipStat stat;
    [SerializeField] Transform fill;
    [SerializeField] TMP_Text label;
    [SerializeField] Color flashColor = new Color(1f, 0.15f, 0.15f);
    [SerializeField, Min(0f)] float flashDuration = 0.4f;

    float leftEdge;
    float fullWidth;
    Color labelColor;
    float flashUntil;

    void Awake()
    {
        // The fill starts at full size; remember where its left edge is.
        fullWidth = fill.localScale.x;
        leftEdge = fill.localPosition.x - fullWidth * 0.5f;
        labelColor = label.color;
    }

    void OnEnable()
    {
        stat.Changed += Show;
        if (stat is ShipEnergy energy) energy.SpendFailed += Flash;
        if (stat.Max > 0f) Show(stat.Current, stat.Max);
    }

    void OnDisable()
    {
        stat.Changed -= Show;
        if (stat is ShipEnergy energy) energy.SpendFailed -= Flash;
    }

    void Update()
    {
        if (flashUntil > 0f && Time.time >= flashUntil)
        {
            flashUntil = 0f;
            label.color = labelColor;
        }
    }

    void Flash()
    {
        label.color = flashColor;
        flashUntil = Time.time + flashDuration;
    }

    void Show(float current, float max)
    {
        float f = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        var s = fill.localScale;
        s.x = Mathf.Max(0.0001f, fullWidth * f);
        fill.localScale = s;

        var p = fill.localPosition;
        p.x = leftEdge + fullWidth * f * 0.5f;
        fill.localPosition = p;
        fill.gameObject.SetActive(f > 0f);

        label.text = Mathf.FloorToInt(current).ToString();
    }
}
