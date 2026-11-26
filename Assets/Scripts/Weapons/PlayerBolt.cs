using UnityEngine;

/// <summary>
/// A fast projectile fired by a player's laser or machine gun: a short glowing streak that flies
/// from the player's muzzle, damages the first enemy or debris it touches and disappears.
/// If aim assist picked a target, it curves toward it so a fair shot still lands as things move.
/// It also disappears after flying its maximum distance. Pooled by <see cref="PlayerWeapons"/>.
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class PlayerBolt : MonoBehaviour
{
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    MeshRenderer meshRenderer;
    MaterialPropertyBlock block;

    Vector3 position;
    Vector3 direction;
    float speed;
    float maxDistance;
    float distanceLeft;
    float damage;
    float hitRadius;
    float length;
    float width;
    LayerMask mask;
    Transform target;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        block = new MaterialPropertyBlock();
    }

    /// <param name="homingTarget">What aim assist picked, or null to fly straight.</param>
    public void Fire(Vector3 from, Vector3 aimPoint, Transform homingTarget, PlayerBoltSpec spec,
                     Material material, Color color, LayerMask targets)
    {
        position = from;
        direction = (aimPoint - from).sqrMagnitude > 0.0001f ? (aimPoint - from).normalized : Vector3.forward;
        target = homingTarget;
        speed = spec.speed;
        maxDistance = spec.maxDistance;
        distanceLeft = maxDistance;
        damage = spec.damage;
        hitRadius = spec.hitRadius;
        length = spec.length;
        width = spec.width;
        mask = targets;

        meshRenderer.sharedMaterial = material;
        block.SetColor(BaseColor, color);
        meshRenderer.SetPropertyBlock(block);
        gameObject.SetActive(true);
        Place();
    }

    void Update()
    {
        float step = speed * Time.deltaTime;
        if (step <= 0f) return;   // paused

        // Curve toward the aim-assisted target while it is still around.
        if (target != null)
        {
            if (!target.gameObject.activeInHierarchy) target = null;
            else
            {
                var to = target.position - position;
                if (Vector3.Dot(to, direction) > 0f) direction = to.normalized;
                else target = null;   // already passed it
            }
        }

        // Sweep the path covered this frame, so a fast bolt never skips through something.
        Physics.SyncTransforms();   // enemies and debris move by transform
        float travel = Mathf.Min(step, distanceLeft);
        if (Physics.SphereCast(position, hitRadius, direction, out var hit, travel, mask, QueryTriggerInteraction.Ignore))
        {
            var enemy = hit.collider.GetComponentInParent<EnemyBase>();
            if (enemy != null) enemy.TakeDamage(damage);
            else
            {
                var debris = hit.collider.GetComponentInParent<Debris>();
                if (debris != null) debris.TakeDamage(damage);
            }
            gameObject.SetActive(false);
            return;
        }

        position += direction * travel;
        distanceLeft -= travel;
        if (distanceLeft <= 0f)
        {
            gameObject.SetActive(false);
            return;
        }
        Place();
    }

    void Place()
    {
        // The streak trails behind the bolt's tip, never further back than the muzzle.
        float travelled = maxDistance - distanceLeft;
        float l = Mathf.Clamp(travelled, 0.05f, length);
        transform.SetPositionAndRotation(position - direction * (l * 0.5f), Quaternion.LookRotation(direction));
        transform.localScale = new Vector3(width, width, l);
    }
}
