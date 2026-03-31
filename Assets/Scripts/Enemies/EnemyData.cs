using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public float maxHealth = 50f;
    public float moveSpeed = 2f;
    public float attackDamage = 10f;
    public float attackRange = 1f;
    public float attackCooldown = 1.5f;
    public float detectionRange = 6f;
    public float staggerThreshold = 20f;
    public float staggerDuration = 0.5f;
    public float armorReduction = 0f;
    public bool armorDisabledDuringStagger = true;
}
