using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class ComboBookDataTests
{
    [Test]
    public void ComboBook_Common_Has2Slots()
    {
        var book = ScriptableObject.CreateInstance<ComboBookData>();
        book.rarity = ComboBookRarity.Common;
        book.ForceInitSlots();
        Assert.AreEqual(2, book.SlotCount);
    }

    [Test]
    public void ComboBook_Rare_Has3Slots()
    {
        var book = ScriptableObject.CreateInstance<ComboBookData>();
        book.rarity = ComboBookRarity.Rare;
        book.ForceInitSlots();
        Assert.AreEqual(3, book.SlotCount);
    }

    [Test]
    public void ComboBook_Legendary_Has4Slots()
    {
        var book = ScriptableObject.CreateInstance<ComboBookData>();
        book.rarity = ComboBookRarity.Legendary;
        book.ForceInitSlots();
        Assert.AreEqual(4, book.SlotCount);
    }

    [Test]
    public void ComboBook_SetAttack_PlacesAttackInSlot()
    {
        var book = ScriptableObject.CreateInstance<ComboBookData>();
        book.rarity = ComboBookRarity.Rare;
        book.ForceInitSlots();

        var attack = ScriptableObject.CreateInstance<AttackData>();
        attack.attackName = "Test Slash";

        book.SetAttack(1, attack);

        Assert.AreEqual(attack, book.GetAttack(1));
        Assert.IsNull(book.GetAttack(0));
        Assert.IsNull(book.GetAttack(2));
    }

    [Test]
    public void ComboBook_SetAttack_OutOfRange_DoesNothing()
    {
        var book = ScriptableObject.CreateInstance<ComboBookData>();
        book.rarity = ComboBookRarity.Common;
        book.ForceInitSlots();

        var attack = ScriptableObject.CreateInstance<AttackData>();

        book.SetAttack(5, attack);
        book.SetAttack(-1, attack);

        Assert.IsNull(book.GetAttack(0));
        Assert.IsNull(book.GetAttack(1));
    }

    [Test]
    public void ComboBook_ClearSlot_RemovesAttack()
    {
        var book = ScriptableObject.CreateInstance<ComboBookData>();
        book.rarity = ComboBookRarity.Common;
        book.ForceInitSlots();

        var attack = ScriptableObject.CreateInstance<AttackData>();
        book.SetAttack(0, attack);
        book.ClearSlot(0);

        Assert.IsNull(book.GetAttack(0));
    }

    [Test]
    public void ComboBook_GetFilledAttacks_ReturnsOnlyNonNull()
    {
        var book = ScriptableObject.CreateInstance<ComboBookData>();
        book.rarity = ComboBookRarity.Rare;
        book.ForceInitSlots();

        var a1 = ScriptableObject.CreateInstance<AttackData>();
        var a3 = ScriptableObject.CreateInstance<AttackData>();
        book.SetAttack(0, a1);
        book.SetAttack(2, a3);

        var filled = book.GetFilledAttacks();
        Assert.AreEqual(2, filled.Length);
        Assert.AreEqual(a1, filled[0]);
        Assert.AreEqual(a3, filled[1]);
    }
}
