using UnityEngine;

/// <summary>
/// Start menu shown before the game runs: Play starts the game, Test Mode opens the test panel.
/// The game is frozen (time scale 0) while the menu is up.
/// After a level restart the last chosen mode starts again straight away, without the menu.
/// </summary>
public class GameStartMenu : MonoBehaviour
{
    [SerializeField] GameObject menuPanel;
    [SerializeField] UnityEngine.UI.Button playButton;
    [SerializeField] UnityEngine.UI.Button testButton;
    [SerializeField] TestModePanel testPanel;

    // Kept across scene reloads so a restart after dying skips the menu.
    static GameMode lastMode = GameMode.None;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => lastMode = GameMode.None;

    public static GameMode CurrentMode => lastMode;

    /// <summary>Raised when Play or Test Mode starts, with the new mode.</summary>
    public static event System.Action<GameMode> ModeChanged;

    void Awake()
    {
        playButton.onClick.AddListener(StartPlay);
        testButton.onClick.AddListener(StartTest);
        testPanel.gameObject.SetActive(false);

        switch (lastMode)
        {
            case GameMode.Play: StartPlay(); break;
            case GameMode.Test: StartTest(); break;
            default:
                menuPanel.SetActive(true);
                Time.timeScale = 0f;
                break;
        }
    }

    public void StartPlay()
    {
        lastMode = GameMode.Play;
        menuPanel.SetActive(false);
        testPanel.gameObject.SetActive(false);
        Time.timeScale = 1f;
        ModeChanged?.Invoke(lastMode);
    }

    public void StartTest()
    {
        lastMode = GameMode.Test;
        menuPanel.SetActive(false);
        testPanel.gameObject.SetActive(true);
        testPanel.Begin();
        ModeChanged?.Invoke(lastMode);
    }
}
