using TMPro;
using UnityEngine;

/// <summary>
/// Jams every weapon (both players' weapons and the shared weapon) for a few seconds, and shows
/// "Weapons temporarily disabled" with a countdown in the middle of the window meanwhile.
/// Moving between positions still works. Another jam while jammed restarts the countdown.
/// </summary>
public class WeaponLockout : MonoBehaviour
{
    [Tooltip("Message in the middle of the cockpit window.")]
    [SerializeField] TMP_Text message;
    [SerializeField] string text = "Weapons temporarily disabled";

    float until = -1f;
    int shownSeconds = -1;

    /// <summary>True while the weapons are jammed.</summary>
    public bool Active => Time.time < until;
    public float SecondsLeft => Mathf.Max(0f, until - Time.time);

    void Start() => message.gameObject.SetActive(false);

    /// <summary>Jams the weapons for this many seconds from now.</summary>
    public void Trigger(float seconds)
    {
        until = Mathf.Max(until, Time.time + seconds);
        shownSeconds = -1;
        message.gameObject.SetActive(true);
        UpdateMessage();
    }

    void Update()
    {
        if (!message.gameObject.activeSelf) return;
        if (!Active)
        {
            message.gameObject.SetActive(false);
            return;
        }
        UpdateMessage();
    }

    void UpdateMessage()
    {
        int s = Mathf.CeilToInt(SecondsLeft);
        if (s == shownSeconds) return;
        shownSeconds = s;
        message.text = $"{text}\n<size=80%>{s}</size>";
    }
}
