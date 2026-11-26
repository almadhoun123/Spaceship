using TMPro;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Test mode controls: a slider that moves the ship anywhere along the rail,
/// play / pause, and play speed. Pause and speed work through the game's time scale,
/// so everything (ship, enemies, debris, energy) pauses and speeds up together.
/// </summary>
public class TestModePanel : MonoBehaviour
{
    [SerializeField] SplineAnimate ship;
    [Tooltip("Made unable to die while test mode is on.")]
    [SerializeField] ShipHealth shipHealth;
    [Tooltip("Rail smoothing is turned off in test mode, so the ship follows the spline exactly as authored.")]
    [SerializeField] ShipRailFollower railFollower;
    [SerializeField] UnityEngine.UI.Slider progress;
    [SerializeField] TMP_Text progressLabel;
    [SerializeField] UnityEngine.UI.Button playPauseButton;
    [SerializeField] TMP_Text playPauseLabel;
    [Tooltip("One button per entry in Speeds, in the same order.")]
    [SerializeField] UnityEngine.UI.Button[] speedButtons;
    [SerializeField] float[] speeds = { 0.25f, 0.5f, 1f, 2f, 4f };
    [SerializeField] TMP_Text speedLabel;
    [Tooltip("The rail's segments: a pin on the slider marks where each one starts.")]
    [SerializeField] RailSegments segments;

    RectTransform pinRoot;

    bool playing;
    float speed = 1f;
    float railLength;

    void Awake()
    {
        progress.minValue = 0f;
        progress.maxValue = 1f;
        progress.onValueChanged.AddListener(OnScrub);
        playPauseButton.onClick.AddListener(TogglePlay);
        for (int i = 0; i < speedButtons.Length && i < speeds.Length; i++)
        {
            float s = speeds[i];
            speedButtons[i].onClick.AddListener(() => SetSpeed(s));
        }
    }

    /// <summary>Called when test mode is chosen: starts paused at normal speed.</summary>
    public void Begin()
    {
        railLength = ship.Container != null ? ship.Container.CalculateLength() : 0f;
        if (shipHealth != null) shipHealth.CannotDie = true;
        if (railFollower != null) railFollower.Smoothing = false;
        playing = false;
        speed = 1f;
        Apply();
        BuildSegmentPins();
    }

    /// <summary>
    /// One pin per segment on the slider, at the point where that segment starts, in the
    /// segment's color and numbered. Placed in the handle's slide area so a pin sits exactly
    /// where the handle is when the ship reaches that segment.
    /// </summary>
    void BuildSegmentPins()
    {
        if (segments == null || progress.handleRect == null) return;
        if (pinRoot != null) Destroy(pinRoot.gameObject);

        var area = (RectTransform)progress.handleRect.parent;
        pinRoot = new GameObject("SegmentPins", typeof(RectTransform)).GetComponent<RectTransform>();
        pinRoot.SetParent(area, false);
        pinRoot.anchorMin = Vector2.zero;
        pinRoot.anchorMax = Vector2.one;
        pinRoot.offsetMin = pinRoot.offsetMax = Vector2.zero;
        pinRoot.SetAsFirstSibling();   // behind the handle

        for (int i = 0; i < segments.SegmentCount; i++)
        {
            float start = segments.SegmentRange(i).x;
            var color = segments.SegmentColor(i);

            var pin = new GameObject($"Pin_Segment{i + 1}", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();
            pin.SetParent(pinRoot, false);
            pin.anchorMin = pin.anchorMax = new Vector2(start, 0.5f);
            pin.pivot = new Vector2(0.5f, 0.5f);
            pin.sizeDelta = new Vector2(4f, 30f);
            var img = pin.GetComponent<UnityEngine.UI.Image>();
            img.color = color;
            img.raycastTarget = false;

            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.rectTransform.SetParent(pin, false);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            label.rectTransform.pivot = new Vector2(0.5f, 0f);
            label.rectTransform.sizeDelta = new Vector2(40f, 22f);
            label.rectTransform.anchoredPosition = new Vector2(0f, 2f);
            label.text = (i + 1).ToString();
            label.fontSize = 18f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.raycastTarget = false;
        }
    }

    void TogglePlay()
    {
        playing = !playing;
        Apply();
    }

    void SetSpeed(float s)
    {
        speed = s;
        Apply();
    }

    void Apply()
    {
        Time.timeScale = playing ? speed : 0f;
        playPauseLabel.text = playing ? "Pause" : "Play";
        speedLabel.text = $"Speed: {speed}x";
    }

    // Slider moved by the tester: put the ship at that point of the rail.
    void OnScrub(float value) => ship.NormalizedTime = Mathf.Clamp(value, 0f, 0.9999f);

    void Update()
    {
        // NormalizedTime keeps counting up across laps; show only this lap's part.
        float t = Mathf.Repeat(ship.NormalizedTime, 1f);
        progress.SetValueWithoutNotify(t);
        progressLabel.text = $"{t * 100f:0}%   {t * railLength:0} / {railLength:0} m";
    }
}
