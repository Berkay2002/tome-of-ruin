using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class HarmonyCalculatorTests
{
    private HarmonyTable _table;

    [SetUp]
    public void SetUp()
    {
        _table = ScriptableObject.CreateInstance<HarmonyTable>();
        _table.entries = new HarmonyEntry[]
        {
            new HarmonyEntry { fromTag = AttackTag.Rising, toTag = AttackTag.Slam, level = HarmonyLevel.Harmonious },
            new HarmonyEntry { fromTag = AttackTag.Thrust, toTag = AttackTag.Sweep, level = HarmonyLevel.Harmonious },
            new HarmonyEntry { fromTag = AttackTag.Sweep, toTag = AttackTag.Overhead, level = HarmonyLevel.Harmonious },
            new HarmonyEntry { fromTag = AttackTag.Overhead, toTag = AttackTag.Thrust, level = HarmonyLevel.Harmonious },
            new HarmonyEntry { fromTag = AttackTag.Slam, toTag = AttackTag.Rising, level = HarmonyLevel.Dissonant },
            new HarmonyEntry { fromTag = AttackTag.Spinning, toTag = AttackTag.Spinning, level = HarmonyLevel.Dissonant },
        };
    }

    [Test]
    public void GetHarmony_HarmoniousPair_ReturnsHarmonious()
    {
        var result = HarmonyCalculator.GetHarmony(_table, AttackTag.Rising, AttackTag.Slam);
        Assert.AreEqual(HarmonyLevel.Harmonious, result);
    }

    [Test]
    public void GetHarmony_DissonantPair_ReturnsDissonant()
    {
        var result = HarmonyCalculator.GetHarmony(_table, AttackTag.Slam, AttackTag.Rising);
        Assert.AreEqual(HarmonyLevel.Dissonant, result);
    }

    [Test]
    public void GetHarmony_UnlistedPair_ReturnsNeutral()
    {
        var result = HarmonyCalculator.GetHarmony(_table, AttackTag.Sweep, AttackTag.Slam);
        Assert.AreEqual(HarmonyLevel.Neutral, result);
    }

    [Test]
    public void GetDamageMultiplier_Harmonious_Returns1_3()
    {
        float mult = HarmonyCalculator.GetDamageMultiplier(HarmonyLevel.Harmonious);
        Assert.AreEqual(1.3f, mult, 0.001f);
    }

    [Test]
    public void GetDamageMultiplier_Neutral_Returns1()
    {
        float mult = HarmonyCalculator.GetDamageMultiplier(HarmonyLevel.Neutral);
        Assert.AreEqual(1.0f, mult, 0.001f);
    }

    [Test]
    public void GetDamageMultiplier_Dissonant_Returns0_6()
    {
        float mult = HarmonyCalculator.GetDamageMultiplier(HarmonyLevel.Dissonant);
        Assert.AreEqual(0.6f, mult, 0.001f);
    }

    [Test]
    public void CalculateComboScore_FullyHarmonious3Slot_ReturnsHighMultiplier()
    {
        var a1 = ScriptableObject.CreateInstance<AttackData>();
        a1.primaryTag = AttackTag.Rising;
        a1.baseDamage = 10f;

        var a2 = ScriptableObject.CreateInstance<AttackData>();
        a2.primaryTag = AttackTag.Slam;
        a2.baseDamage = 10f;

        var a3 = ScriptableObject.CreateInstance<AttackData>();
        a3.primaryTag = AttackTag.Rising;
        a3.baseDamage = 10f;

        var attacks = new AttackData[] { a1, a2, a3 };
        float[] multipliers = HarmonyCalculator.CalculateComboMultipliers(_table, attacks);

        Assert.AreEqual(1.0f, multipliers[0], 0.001f);
        Assert.AreEqual(1.3f, multipliers[1], 0.001f);
        Assert.AreEqual(0.6f, multipliers[2], 0.001f);
    }

    [Test]
    public void CalculateComboScore_BestTagUsedForMultiTagAttacks()
    {
        var a1 = ScriptableObject.CreateInstance<AttackData>();
        a1.primaryTag = AttackTag.Sweep;
        a1.secondaryTag = AttackTag.Rising;

        var a2 = ScriptableObject.CreateInstance<AttackData>();
        a2.primaryTag = AttackTag.Slam;

        var attacks = new AttackData[] { a1, a2 };
        float[] multipliers = HarmonyCalculator.CalculateComboMultipliers(_table, attacks);

        Assert.AreEqual(1.0f, multipliers[0], 0.001f);
        Assert.AreEqual(1.3f, multipliers[1], 0.001f);
    }
}
