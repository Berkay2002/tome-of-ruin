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

    public virtual void ExecuteAttack(Transform self, Transform target, EnemyData data)
    {
        // Stub — will be fully implemented in Task 11
    }

    public virtual void UpdateBehavior(Transform self, Transform target, EnemyData data, Rigidbody2D rb)
    {
        // Stub — will be fully implemented in Task 11
    }
}
