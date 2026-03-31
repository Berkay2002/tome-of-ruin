using UnityEngine;

[CreateAssetMenu(fileName = "NewAttack", menuName = "Game/Attack Data")]
public class AttackData : ScriptableObject
{
    public string attackName;
    public AttackTag primaryTag;
    public AttackTag secondaryTag;
    public float baseDamage = 10f;
    public AttackSpeed speed = AttackSpeed.Medium;
    public MovementPattern movementPattern = MovementPattern.HoldPosition;
    public AnimationClip animationClip;

    public bool HasTag(AttackTag tag)
    {
        if (tag == AttackTag.None) return false;
        return primaryTag == tag || secondaryTag == tag;
    }
}
