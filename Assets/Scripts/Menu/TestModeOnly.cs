using UnityEngine;

/// <summary>
/// Shows this object's renderers only in Test Mode (such as the temporary grid tunnel).
/// Hidden on the start menu and in normal Play. In a scene with no start menu it stays visible.
/// </summary>
public class TestModeOnly : MonoBehaviour
{
    Renderer[] renderers;

    void Awake() => renderers = GetComponentsInChildren<Renderer>(true);

    void OnEnable()
    {
        GameStartMenu.ModeChanged += Apply;
        bool hasMenu = FindAnyObjectByType<GameStartMenu>(FindObjectsInactive.Include) != null;
        SetVisible(!hasMenu || GameStartMenu.CurrentMode == GameMode.Test);
    }

    void OnDisable() => GameStartMenu.ModeChanged -= Apply;

    void Apply(GameMode mode) => SetVisible(mode == GameMode.Test);

    void SetVisible(bool visible)
    {
        foreach (var r in renderers) r.enabled = visible;
    }
}
