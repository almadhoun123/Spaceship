using UnityEngine;

/// <summary>An enemy ray. Flies in a straight line and damages the ship if it hits the hull.</summary>
[RequireComponent(typeof(Collider))]
public class EnemyProjectile : MonoBehaviour
{
    Vector3 velocity;
    float lifeLeft;

    public float DamagePercent { get; private set; }

    public void Launch(Vector3 position, Vector3 direction, float speed, float lifetime, float damagePercent)
    {
        transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
        velocity = direction * speed;
        lifeLeft = lifetime;
        DamagePercent = damagePercent;
        gameObject.SetActive(true);
    }

    void Update()
    {
        transform.position += velocity * Time.deltaTime;
        lifeLeft -= Time.deltaTime;
        if (lifeLeft <= 0f) Despawn();
    }

    public void Despawn() => gameObject.SetActive(false);
}
