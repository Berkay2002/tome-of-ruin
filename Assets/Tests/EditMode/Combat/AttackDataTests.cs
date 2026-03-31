using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class AttackDataTests
{
    [Test]
    public void AttackData_CanBeCreated_WithValidFields()
    {
        var attack = ScriptableObject.CreateInstance<AttackData>();
        attack.attackName = "Cleaving Arc";
        attack.primaryTag = AttackTag.Sweep;
        attack.secondaryTag = AttackTag.None;
        attack.baseDamage = 25f;
        attack.speed = AttackSpeed.Medium;
        attack.movementPattern = MovementPattern.LungeForward;

        Assert.AreEqual("Cleaving Arc", attack.attackName);
        Assert.AreEqual(AttackTag.Sweep, attack.primaryTag);
        Assert.AreEqual(AttackTag.None, attack.secondaryTag);
        Assert.AreEqual(25f, attack.baseDamage);
        Assert.AreEqual(AttackSpeed.Medium, attack.speed);
        Assert.AreEqual(MovementPattern.LungeForward, attack.movementPattern);
    }

    [Test]
    public void AttackData_HasTags_ReturnsTrueForMatchingTag()
    {
        var attack = ScriptableObject.CreateInstance<AttackData>();
        attack.primaryTag = AttackTag.Thrust;
        attack.secondaryTag = AttackTag.Rising;

        Assert.IsTrue(attack.HasTag(AttackTag.Thrust));
        Assert.IsTrue(attack.HasTag(AttackTag.Rising));
        Assert.IsFalse(attack.HasTag(AttackTag.Slam));
        Assert.IsFalse(attack.HasTag(AttackTag.None));
    }
}
