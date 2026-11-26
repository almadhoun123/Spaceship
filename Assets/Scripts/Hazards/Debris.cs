using UnityEngine;

/// <summary>
/// A piece of space debris. Can be shot down (it has health), and damages the ship on contact,
/// then disappears.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Debris : MonoBehaviour, IHasHealth
{
    float maxHealth = 1f;
    float health = 1f;

    public float HealthFraction => maxHealth > 0f ? health / maxHealth : 1f;

    public void ResetHealth(float max)
    {
        maxHealth = Mathf.Max(0.01f, max);
        health = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0f) Hit();
    }

    /// <summary>Removes the debris (shot down, or hit by the ship).</summary>
    public void Hit() => gameObject.SetActive(false);
}
