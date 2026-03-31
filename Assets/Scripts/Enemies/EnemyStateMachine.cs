using System.Collections;
using UnityEngine;

public enum EnemyState
{
    Idle,
    Patrol,
    Chase,
    Attack,
    Stagger,
    Dead
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyStateMachine : MonoBehaviour
{
    public EnemyData data;
    public EnemyState CurrentState { get; private set; } = EnemyState.Idle;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float patrolWaitTime = 1f;

    [Header("Type-Specific Behavior")]
    public EnemyBehavior behavior;

    private Rigidbody2D _rb;
    private EnemyHealth _health;
    private Transform _player;
    private int _patrolIndex;
    private float _patrolWaitTimer;
    private float _attackCooldownTimer;
    private float _staggerTimer;
    private float _attackWindupTimer;
    private bool _attackWindingUp;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
        _health = GetComponent<EnemyHealth>();
    }

    private void Start()
    {
        _health.OnDeath += () => SetState(EnemyState.Dead);
        _health.OnStagger += () =>
        {
            if (CurrentState != EnemyState.Dead)
            {
                SetState(EnemyState.Stagger);
                _staggerTimer = data.staggerDuration;
                _health.BeginStagger();
            }
        };

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;
    }

    private void Update()
    {
        if (_player == null || data == null) return;

        switch (CurrentState)
        {
            case EnemyState.Idle:
                UpdateIdle();
                break;
            case EnemyState.Patrol:
                UpdatePatrol();
                break;
            case EnemyState.Chase:
                UpdateChase();
                break;
            case EnemyState.Attack:
                UpdateAttack();
                break;
            case EnemyState.Stagger:
                UpdateStagger();
                break;
            case EnemyState.Dead:
                _rb.velocity = Vector2.zero;
                break;
        }
    }

    private void UpdateIdle()
    {
        float dist = Vector2.Distance(transform.position, _player.position);
        if (dist <= data.detectionRange)
        {
            SetState(EnemyState.Chase);
            return;
        }

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            _patrolWaitTimer -= Time.deltaTime;
            if (_patrolWaitTimer <= 0f)
                SetState(EnemyState.Patrol);
        }
    }

    private void UpdatePatrol()
    {
        float dist = Vector2.Distance(transform.position, _player.position);
        if (dist <= data.detectionRange)
        {
            SetState(EnemyState.Chase);
            return;
        }

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            SetState(EnemyState.Idle);
            return;
        }

        var target = patrolPoints[_patrolIndex].position;
        Vector2 dir = ((Vector2)target - (Vector2)transform.position).normalized;
        _rb.velocity = dir * data.moveSpeed;

        if (Vector2.Distance(transform.position, target) < 0.3f)
        {
            _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
            _patrolWaitTimer = patrolWaitTime;
            SetState(EnemyState.Idle);
        }
    }

    private void UpdateChase()
    {
        float dist = Vector2.Distance(transform.position, _player.position);

        if (dist > data.detectionRange * 1.5f)
        {
            SetState(EnemyState.Idle);
            return;
        }

        if (dist <= data.attackRange && _attackCooldownTimer <= 0f)
        {
            SetState(EnemyState.Attack);
            return;
        }

        Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
        _rb.velocity = dir * data.moveSpeed;

        _attackCooldownTimer -= Time.deltaTime;
    }

    private void UpdateAttack()
    {
        _rb.velocity = Vector2.zero;

        if (!_attackWindingUp)
        {
            _attackWindingUp = true;
            _attackWindupTimer = 0.4f;
            return;
        }

        _attackWindupTimer -= Time.deltaTime;
        if (_attackWindupTimer > 0f) return;

        _attackWindingUp = false;
        _attackCooldownTimer = data.attackCooldown;
        OnAttackExecute();
        SetState(EnemyState.Chase);
    }

    private void UpdateStagger()
    {
        _rb.velocity = Vector2.zero;
        _staggerTimer -= Time.deltaTime;
        if (_staggerTimer <= 0f)
        {
            _health.EndStagger();
            SetState(EnemyState.Chase);
        }
    }

    public void SetState(EnemyState state)
    {
        if (CurrentState == EnemyState.Dead) return;
        CurrentState = state;

        if (state == EnemyState.Dead)
        {
            _rb.velocity = Vector2.zero;
            GetComponent<Collider2D>().enabled = false;
            StartCoroutine(DeathFade());
        }
    }

    private void OnAttackExecute()
    {
        if (behavior != null)
        {
            behavior.ExecuteAttack(transform, _player, data);
            return;
        }

        float dist = Vector2.Distance(transform.position, _player.position);
        if (dist <= data.attackRange)
        {
            var playerHealth = _player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.TakeDamage(data.attackDamage);
        }
    }

    private IEnumerator DeathFade()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        float duration = 0.5f;
        float elapsed = 0f;
        Color original = sr.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            sr.color = new Color(original.r, original.g, original.b, 1f - (elapsed / duration));
            yield return null;
        }

        Destroy(gameObject);
    }
}
