using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[TestFixture]
public class GameStateTests
{
    private GameState _state;

    [SetUp]
    public void SetUp()
    {
        _state = new GameState();
    }

    [Test]
    public void NewGameState_HasEmptyCollections()
    {
        Assert.AreEqual(0, _state.collectedKeys.Count);
        Assert.AreEqual(0, _state.discoveredAttacks.Count);
        Assert.AreEqual(0, _state.discoveredComboBooks.Count);
        Assert.AreEqual(0, _state.unlockedShortcuts.Count);
        Assert.IsFalse(_state.zoneBCleared);
        Assert.IsFalse(_state.zoneCCleared);
    }

    [Test]
    public void CollectKey_AddsKeyToList()
    {
        _state.CollectKey("catacombs_key_01");
        Assert.IsTrue(_state.HasKey("catacombs_key_01"));
        Assert.IsFalse(_state.HasKey("chapel_key_01"));
    }

    [Test]
    public void CollectKey_Duplicate_DoesNotAddTwice()
    {
        _state.CollectKey("key_01");
        _state.CollectKey("key_01");
        Assert.AreEqual(1, _state.collectedKeys.Count);
    }

    [Test]
    public void UnlockShortcut_TracksShortcutId()
    {
        _state.UnlockShortcut("zoneA_to_zoneB_shortcut");
        Assert.IsTrue(_state.IsShortcutUnlocked("zoneA_to_zoneB_shortcut"));
        Assert.IsFalse(_state.IsShortcutUnlocked("other_shortcut"));
    }

    [Test]
    public void DiscoverAttack_AddsToCollection()
    {
        var attack = ScriptableObject.CreateInstance<AttackData>();
        attack.attackName = "Test Slash";
        _state.DiscoverAttack(attack);
        Assert.AreEqual(1, _state.discoveredAttacks.Count);
        Assert.IsTrue(_state.HasAttack(attack));
    }

    [Test]
    public void BossUnlocked_RequiresBothZonesCleared()
    {
        Assert.IsFalse(_state.IsBossUnlocked);
        _state.zoneBCleared = true;
        Assert.IsFalse(_state.IsBossUnlocked);
        _state.zoneCCleared = true;
        Assert.IsTrue(_state.IsBossUnlocked);
    }

    [Test]
    public void Reset_ClearsAllState()
    {
        _state.CollectKey("key_01");
        _state.zoneBCleared = true;
        _state.UnlockShortcut("shortcut_01");
        _state.Reset();
        Assert.AreEqual(0, _state.collectedKeys.Count);
        Assert.IsFalse(_state.zoneBCleared);
        Assert.AreEqual(0, _state.unlockedShortcuts.Count);
    }
}
