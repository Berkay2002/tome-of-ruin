using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(ComboExecutor))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Attack")]
    public float attackRange = 1.2f;
    public LayerMask enemyLayer;

    [Header("Dodge Cancel")]
    public float dodgeCancelRecovery = 0.1f;

    private PlayerController _controller;
    private ComboExecutor _executor;
    private Rigidbody2D _rb;
    private float _attackAnimTimer;
    private bool _attackBuffered;
    private AttackData _currentAttackData;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _executor = GetComponent<ComboExecutor>();
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (_controller.CurrentState == PlayerState.Attacking)
        {
            _attackAnimTimer -= Time.deltaTime;
            if (_attackAnimTimer <= 0f)
            {
                _executor.FinishCurrentAttack();
                _controller.SetState(PlayerState.Idle);

                if (_attackBuffered)
                {
                    _attackBuffered = false;
                    TryAttack();
                }
            }
        }
    }

    public void TryAttack()
    {
        if (_controller.CurrentState == PlayerState.Dead) return;
        if (_controller.CurrentState == PlayerState.Dodging) return;

        if (_controller.CurrentState == PlayerState.Attacking)
        {
            _attackBuffered = true;
            return;
        }

        var result = _executor.Attack();
        if (!result.executed) return;

        _controller.SetState(PlayerState.Attacking);
        _currentAttackData = result.attackData;
        _attackAnimTimer = GetAttackDuration(result.attackData);
        _attackBuffered = false;

        ApplyAttackMovement(result.attackData);
        DealDamage(result);
    }

    public bool TryDodgeCancel()
    {
        if (_controller.CurrentState != PlayerState.Attacking) return false;

        _executor.ResetCombo();
        _attackBuffered = false;
        _controller.SetState(PlayerState.Idle);

        return true;
    }

    private void ApplyAttackMovement(AttackData attack)
    {
        Vector2 facing = _controller.FacingDirection;
        switch (attack.movementPattern)
        {
            case MovementPattern.LungeForward:
                _rb.velocity = facing * 6f;
                break;
            case MovementPattern.PullBack:
                _rb.velocity = -facing * 3f;
                break;
            case MovementPattern.CircleArc:
                _rb.velocity = new Vector2(-facing.y, facing.x) * 4f;
                break;
            case MovementPattern.HoldPosition:
            default:
                _rb.velocity = Vector2.zero;
                break;
        }
    }

    private void DealDamage(AttackResult result)
    {
        Vector2 attackOrigin = (Vector2)transform.position + _controller.FacingDirection * 0.5f;
        var hits = Physics2D.OverlapCircleAll(attackOrigin, attackRange, enemyLayer);

        foreach (var hit in hits)
        {
            var enemyHealth = hit.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(result.totalDamage, result.harmonyLevel);
            }
        }

        if (hits.Length > 0 && ScreenShake.Instance != null)
            ScreenShake.Instance.Shake(0.3f);
    }

    private float GetAttackDuration(AttackData attack)
    {
        return attack.speed switch
        {
            AttackSpeed.Fast => 0.25f,
            AttackSpeed.Medium => 0.4f,
            AttackSpeed.Slow => 0.6f,
            _ => 0.4f
        };
    }
}
