using UnityEngine;

public enum BossPhase
{
    Phase1,
    Phase2,
    Phase3
}

[RequireComponent(typeof(Rigidbody2D))]
public class BossController : MonoBehaviour
{
    public EnemyData data;
    public float phase2HealthPercent = 0.6f;
    public float phase3HealthPercent = 0.3f;

    [Header("Attack Patterns")]
    public float sweepRadius = 3f;
    public float chargeSpeed = 8f;
    public float chargeDuration = 0.5f;
    public GameObject projectilePrefab;

    public BossPhase CurrentPhase { get; private set; } = BossPhase.Phase1;

    private EnemyHealth _health;
    private Rigidbody2D _rb;
    private Transform _player;
    private float _attackTimer;
    private float _actionTimer;
    private bool _isCharging;

    private void Awake()
    {
        _health = GetComponent<EnemyHealth>();
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
    }

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;

        if (_health != null)
        {
            _health.OnDeath += OnBossDeath;
            _health.OnHealthChanged += CheckPhaseTransition;
        }
    }

    private void Update()
    {
        if (_player == null) return;
        if (_health != null && _health.CurrentHealth <= 0) return;

        _attackTimer -= Time.deltaTime;

        if (_isCharging)
        {
            _actionTimer -= Time.deltaTime;
            if (_actionTimer <= 0f)
            {
                _isCharging = false;
                _rb.velocity = Vector2.zero;
            }
            return;
        }

        // Move toward player
        Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
        float dist = Vector2.Distance(transform.position, _player.position);

        if (dist > 2f)
        {
            _rb.velocity = dir * (data != null ? data.moveSpeed : 2f);
        }
        else
        {
            _rb.velocity = Vector2.zero;
        }

        if (_attackTimer <= 0f)
        {
            ExecutePhaseAttack(dir, dist);
            _attackTimer = GetAttackCooldown();
        }
    }

    private void ExecutePhaseAttack(Vector2 dirToPlayer, float dist)
    {
        switch (CurrentPhase)
        {
            case BossPhase.Phase1:
                MeleeSwipe();
                break;
            case BossPhase.Phase2:
                if (dist > 3f)
                    ChargeAttack(dirToPlayer);
                else
                    MeleeSwipe();
                break;
            case BossPhase.Phase3:
                int choice = Random.Range(0, 3);
                if (choice == 0) MeleeSwipe();
                else if (choice == 1) ChargeAttack(dirToPlayer);
                else FireProjectile(dirToPlayer);
                break;
        }
    }

    private void MeleeSwipe()
    {
        if (_player == null) return;
        float dist = Vector2.Distance(transform.position, _player.position);
        if (dist <= sweepRadius)
        {
            var playerHealth = _player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.TakeDamage(data != null ? data.attackDamage : 15f);
        }
    }

    private void ChargeAttack(Vector2 direction)
    {
        _isCharging = true;
        _actionTimer = chargeDuration;
        _rb.velocity = direction * chargeSpeed;
    }

    private void FireProjectile(Vector2 direction)
    {
        if (projectilePrefab == null) return;
        var proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        var rb = proj.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = direction * 6f;
        var dmg = proj.GetComponent<Projectile>();
        if (dmg != null) dmg.damage = data != null ? data.attackDamage * 0.8f : 12f;
    }

    private void CheckPhaseTransition(float current, float max)
    {
        float percent = current / max;
        if (percent <= phase3HealthPercent && CurrentPhase != BossPhase.Phase3)
        {
            CurrentPhase = BossPhase.Phase3;
        }
        else if (percent <= phase2HealthPercent && CurrentPhase == BossPhase.Phase1)
        {
            CurrentPhase = BossPhase.Phase2;
        }
    }

    private float GetAttackCooldown()
    {
        return CurrentPhase switch
        {
            BossPhase.Phase1 => 2.5f,
            BossPhase.Phase2 => 1.8f,
            BossPhase.Phase3 => 1.2f,
            _ => 2.0f
        };
    }

    private void OnBossDeath()
    {
        _rb.velocity = Vector2.zero;
        // Death visual + destroy handled by EnemyVisualFeedback
    }
}
