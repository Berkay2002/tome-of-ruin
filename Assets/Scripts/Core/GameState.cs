using System.Collections.Generic;
using UnityEngine;

public class GameState
{
    public HashSet<string> collectedKeys = new HashSet<string>();
    public List<AttackData> discoveredAttacks = new List<AttackData>();
    public List<ComboBookData> discoveredComboBooks = new List<ComboBookData>();
    public HashSet<string> unlockedShortcuts = new HashSet<string>();
    public HashSet<string> discoveredShrines = new HashSet<string>();
    public bool zoneBCleared;
    public bool zoneCCleared;
    public string lastShrineId;
    public string lastShrineScene;

    public bool IsBossUnlocked => zoneBCleared && zoneCCleared;

    public void CollectKey(string keyId)
    {
        collectedKeys.Add(keyId);
    }

    public bool HasKey(string keyId)
    {
        return collectedKeys.Contains(keyId);
    }

    public void UnlockShortcut(string shortcutId)
    {
        unlockedShortcuts.Add(shortcutId);
    }

    public bool IsShortcutUnlocked(string shortcutId)
    {
        return unlockedShortcuts.Contains(shortcutId);
    }

    public void DiscoverAttack(AttackData attack)
    {
        if (!discoveredAttacks.Contains(attack))
            discoveredAttacks.Add(attack);
    }

    public bool HasAttack(AttackData attack)
    {
        return discoveredAttacks.Contains(attack);
    }

    public void DiscoverComboBook(ComboBookData book)
    {
        if (!discoveredComboBooks.Contains(book))
            discoveredComboBooks.Add(book);
    }

    public void RegisterShrine(string shrineId, string sceneName)
    {
        discoveredShrines.Add(shrineId);
        lastShrineId = shrineId;
        lastShrineScene = sceneName;
    }

    public void Reset()
    {
        collectedKeys.Clear();
        discoveredAttacks.Clear();
        discoveredComboBooks.Clear();
        unlockedShortcuts.Clear();
        discoveredShrines.Clear();
        zoneBCleared = false;
        zoneCCleared = false;
        lastShrineId = null;
        lastShrineScene = null;
    }
}
