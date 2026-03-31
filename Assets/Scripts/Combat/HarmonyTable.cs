using UnityEngine;
using System;

public enum HarmonyLevel
{
    Neutral,
    Harmonious,
    Dissonant
}

[Serializable]
public struct HarmonyEntry
{
    public AttackTag fromTag;
    public AttackTag toTag;
    public HarmonyLevel level;
}

[CreateAssetMenu(fileName = "HarmonyTable", menuName = "Game/Harmony Table")]
public class HarmonyTable : ScriptableObject
{
    public HarmonyEntry[] entries;
}
