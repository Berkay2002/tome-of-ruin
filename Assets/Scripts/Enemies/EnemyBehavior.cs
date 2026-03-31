using UnityEngine;

public enum EnemyBehaviorType
{
    Melee,
    DashRetreat,
    Ranged
}

public class EnemyBehavior : MonoBehaviour
{
    public EnemyBehaviorType behaviorType;

    [Header("Dash Retreat (Wraith)")]
    public float retreatSpeed = 12f;
    public float retreatDistance = 3f;

    [Header("Ranged (Caster)")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 8f;

    private bool _isRetreating;
    private Transform _retreatFrom;

    public void ExecuteAttack(Transform self, Transform player, EnemyData data)
    {
        switch (behaviorType)
        {
            case EnemyBehaviorType.Melee:
                MeleeAttack(self, player, data);
                break;
            case EnemyBehaviorType.DashRetreat:
                MeleeAttack(self, player, data);
                _isRetreating = true;
                _retreatFrom = player;
                break;
            case EnemyBehaviorType.Ranged:
                RangedAttack(self, player, data);
                break;
        }
    }

    public void UpdateBehavior(Transform self, Transform target, EnemyData data, Rigidbody2D rb)
    {
        // Called by EnemyStateMachine if needed for custom chase/patrol behavior
    }

    private void MeleeAttack(Transform self, Transform player, EnemyData data)
    {
        float dist = Vector2.Distance(self.position, player.position);
        if (dist <= data.attackRange)
        {
            var playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.TakeDamage(data.attackDamage);
        }
    }

    private void RangedAttack(Transform self, Transform player, EnemyData data)
    {
        if (projectilePrefab == null) return;

        Vector2 dir = ((Vector2)player.position - (Vector2)self.position).normalized;
        var proj = Instantiate(projectilePrefab, self.position, Quaternion.identity);
        var rb = proj.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = dir * projectileSpeed;

        var dmg = proj.GetComponent<Projectile>();
        if (dmg != null) dmg.damage = data.attackDamage;
    }

    private void LateUpdate()
    {
        if (!_isRetreating || _retreatFrom == null) return;

        float dist = Vector2.Distance(transform.position, _retreatFrom.position);
        if (dist < retreatDistance)
        {
            Vector2 away = ((Vector2)transform.position - (Vector2)_retreatFrom.position).normalized;
            GetComponent<Rigidbody2D>().velocity = away * retreatSpeed;
        }
        else
        {
            _isRetreating = false;
        }
    }
}
