using NUnit.Framework;
using UnityEngine;

public class ComboExecutorTests
{
    private GameObject _obj;
    private ComboExecutor _executor;
    private ComboBookData _book;
    private HarmonyTable _table;

    [SetUp]
    public void SetUp()
    {
        _obj = new GameObject("ComboTest");
        _executor = _obj.AddComponent<ComboExecutor>();
        _executor.chainWindowDuration = 0.5f;

        _table = ScriptableObject.CreateInstance<HarmonyTable>();
        _table.entries = new HarmonyEntry[]
        {
            new HarmonyEntry { fromTag = AttackTag.Rising, toTag = AttackTag.Slam, level = HarmonyLevel.Harmonious }
        };
        _executor.harmonyTable = _table;

        _book = ScriptableObject.CreateInstance<ComboBookData>();
        _book.rarity = ComboBookRarity.Rare;
        _book.InitSlots();

        var a1 = ScriptableObject.CreateInstance<AttackData>();
        a1.primaryTag = AttackTag.Rising;
        a1.baseDamage = 10f;

        var a2 = ScriptableObject.CreateInstance<AttackData>();
        a2.primaryTag = AttackTag.Slam;
        a2.baseDamage = 15f;

        var a3 = ScriptableObject.CreateInstance<AttackData>();
        a3.primaryTag = AttackTag.Sweep;
        a3.baseDamage = 12f;

        _book.SetAttack(0, a1);
        _book.SetAttack(1, a2);
        _book.SetAttack(2, a3);

        _executor.EquipBook(_book);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_obj);
    }

    [Test]
    public void Executor_StartsAtSlotZero()
    {
        Assert.AreEqual(0, _executor.CurrentSlot);
        Assert.IsFalse(_executor.IsExecuting);
    }

    [Test]
    public void Executor_Attack_ExecutesFirstSlot()
    {
        var result = _executor.Attack();

        Assert.IsTrue(result.executed);
        Assert.AreEqual(0, result.slotIndex);
        Assert.AreEqual(10f, result.rawDamage, 0.001f);
        Assert.AreEqual(1.0f, result.harmonyMultiplier, 0.001f);
        Assert.IsTrue(_executor.IsExecuting);
    }

    [Test]
    public void Executor_ChainAttack_AdvancesToNextSlot()
    {
        _executor.Attack();
        _executor.FinishCurrentAttack();

        var result = _executor.Attack();

        Assert.IsTrue(result.executed);
        Assert.AreEqual(1, result.slotIndex);
        Assert.AreEqual(15f, result.rawDamage, 0.001f);
        Assert.AreEqual(1.3f, result.harmonyMultiplier, 0.001f);
    }

    [Test]
    public void Executor_AfterLastSlot_Resets()
    {
        _executor.Attack();
        _executor.FinishCurrentAttack();
        _executor.Attack();
        _executor.FinishCurrentAttack();
        _executor.Attack();
        _executor.FinishCurrentAttack();

        Assert.AreEqual(0, _executor.CurrentSlot);
        Assert.IsFalse(_executor.IsExecuting);
    }

    [Test]
    public void Executor_NoBook_ReturnsNotExecuted()
    {
        _executor.EquipBook(null);
        var result = _executor.Attack();

        Assert.IsFalse(result.executed);
    }

    [Test]
    public void Executor_ResetCombo_GoesBackToSlotZero()
    {
        _executor.Attack();
        _executor.FinishCurrentAttack();
        _executor.Attack();
        _executor.ResetCombo();

        Assert.AreEqual(0, _executor.CurrentSlot);
        Assert.IsFalse(_executor.IsExecuting);
    }
}
