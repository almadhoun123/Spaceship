using UnityEngine;

/// <summary>
/// The shared weapon in the middle of the console. Player 1 arms the left pad, Player 2 arms
/// the right pad. While both are armed, the centre button lights up and pressing it fires a
/// blast that destroys enemies and debris near the ship. Costs a lot of shared energy.
/// </summary>
public class SharedWeapon : MonoBehaviour
{
    [SerializeField] SharedWeaponSettings settings;
    [Tooltip("Player 1's arming pad (left of the circle).")]
    [SerializeField] CockpitButton leftPad;
    [Tooltip("Player 2's arming pad (right of the circle).")]
    [SerializeField] CockpitButton rightPad;
    [Tooltip("The circle: fires when both pads are armed.")]
    [SerializeField] CockpitButton fireButton;
    [SerializeField] ShipEnergy energy;
    [Tooltip("Blast centre: the ship's hull.")]
    [SerializeField] Transform ship;
    [SerializeField] EnemySpawner enemies;
    [Tooltip("Sphere that grows out from the ship when it fires.")]
    [SerializeField] Renderer blastEffect;
    [Tooltip("When jammed (yellow rock cloud), the blast won't fire.")]
    [SerializeField] WeaponLockout lockout;

    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    float leftArmedUntil = -1f;
    float rightArmedUntil = -1f;
    float nextFireTime;
    float blastTime = -1f;
    Renderer fireRenderer;
    MaterialPropertyBlock fireBlock;
    MaterialPropertyBlock blastBlock;

    public bool LeftArmed => settings.requireHold ? leftPad.IsHeld : Time.time < leftArmedUntil;
    public bool RightArmed => settings.requireHold ? rightPad.IsHeld : Time.time < rightArmedUntil;
    public bool Ready => LeftArmed && RightArmed && Time.time >= nextFireTime;

    /// <summary>Raised when the blast goes off.</summary>
    public event System.Action Fired;

    void Awake()
    {
        fireRenderer = fireButton.GetComponent<Renderer>();
        fireBlock = new MaterialPropertyBlock();
        blastBlock = new MaterialPropertyBlock();
    }

    void OnEnable()
    {
        leftPad.Pressed.AddListener(ArmLeft);
        rightPad.Pressed.AddListener(ArmRight);
        fireButton.Pressed.AddListener(Fire);
    }

    void OnDisable()
    {
        leftPad.Pressed.RemoveListener(ArmLeft);
        rightPad.Pressed.RemoveListener(ArmRight);
        fireButton.Pressed.RemoveListener(Fire);
    }

    void Start() => blastEffect.gameObject.SetActive(false);

    void ArmLeft() => leftArmedUntil = Time.time + settings.armDuration;
    void ArmRight() => rightArmedUntil = Time.time + settings.armDuration;

    void Update()
    {
        // Armed pads stay pushed down.
        leftPad.Latched = LeftArmed;
        rightPad.Latched = RightArmed;

        // Light up the circle when it can fire.
        if (Ready)
        {
            fireBlock.SetColor(BaseColor, settings.readyColor);
            fireRenderer.SetPropertyBlock(fireBlock);
        }
        else fireRenderer.SetPropertyBlock(null);

        UpdateBlastEffect();
    }

    /// <summary>The circle was pressed: fires only if both players have armed their pad.</summary>
    public void Fire()
    {
        if (!Ready) return;
        if (lockout != null && lockout.Active) return;   // jammed by a yellow rock cloud
        if (!energy.TrySpend(settings.energyCost)) return;
        nextFireTime = Time.time + settings.cooldown;

        var centre = ship.position;
        float r2 = settings.radius * settings.radius;
        foreach (var enemy in FindObjectsByType<EnemyBase>())
            if ((enemy.transform.position - centre).sqrMagnitude <= r2)
                enemy.TakeDamage(settings.damage, false);   // no kamikaze blast back at the ship
        foreach (var debris in FindObjectsByType<Debris>())
            if ((debris.transform.position - centre).sqrMagnitude <= r2)
                debris.TakeDamage(settings.damage);
        if (settings.clearEnemyRays && enemies != null) enemies.Projectiles.DespawnAll();

        // Used up: both players arm again for the next one.
        leftArmedUntil = rightArmedUntil = -1f;
        blastTime = 0f;
        blastEffect.gameObject.SetActive(true);
        Fired?.Invoke();
    }

    void UpdateBlastEffect()
    {
        if (blastTime < 0f) return;
        blastTime += Time.deltaTime;
        float t = blastTime / settings.blastDuration;
        if (t >= 1f)
        {
            blastTime = -1f;
            blastEffect.gameObject.SetActive(false);
            return;
        }
        var tr = blastEffect.transform;
        tr.position = ship.position;
        tr.localScale = Vector3.one * (settings.radius * 2f * Mathf.Sqrt(t));
        var c = settings.blastColor;
        c.a *= 1f - t;
        blastBlock.SetColor(BaseColor, c);
        blastEffect.SetPropertyBlock(blastBlock);
    }
}
