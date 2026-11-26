using System.Collections.Generic;
using UnityEngine;

/// <summary>Pool of enemy rays, so shots are reused instead of created and destroyed.</summary>
public class EnemyProjectilePool : MonoBehaviour
{
    [SerializeField] EnemyProjectile prefab;
    [SerializeField, Min(1)] int prewarm = 20;

    readonly List<EnemyProjectile> pool = new List<EnemyProjectile>();

    void Awake()
    {
        for (int i = 0; i < prewarm; i++) Create();
    }

    EnemyProjectile Create()
    {
        var p = Instantiate(prefab, transform);
        p.name = $"EnemyRay_{pool.Count:00}";
        p.gameObject.SetActive(false);
        pool.Add(p);
        return p;
    }

    public void Fire(Vector3 position, Vector3 direction, float speed, float lifetime, float damagePercent)
    {
        var p = pool.Find(x => !x.gameObject.activeSelf) ?? Create();
        p.Launch(position, direction, speed, lifetime, damagePercent);
    }

    public void DespawnAll()
    {
        foreach (var p in pool) p.Despawn();
    }
}
