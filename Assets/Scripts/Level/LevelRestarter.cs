using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Restarts the level from the beginning when the ship's health reaches zero.
/// Reloading the scene resets everything: the rail, health, energy, debris and enemies.
/// </summary>
public class LevelRestarter : MonoBehaviour
{
    [SerializeField] ShipHealth health;
    [SerializeField] ShipSettings settings;

    bool restarting;

    void OnEnable() => health.Died += OnDied;
    void OnDisable() => health.Died -= OnDied;

    void OnDied()
    {
        if (restarting) return;
        restarting = true;
        StartCoroutine(RestartAfterDelay());
    }

    IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSecondsRealtime(settings.restartDelay);
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex >= 0 ? scene.buildIndex : 0);
    }
}
