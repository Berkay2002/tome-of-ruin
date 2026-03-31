public static class HarmonyCalculator
{
    public static HarmonyLevel GetHarmony(HarmonyTable table, AttackTag from, AttackTag to)
    {
        if (table.entries == null) return HarmonyLevel.Neutral;

        foreach (var entry in table.entries)
        {
            if (entry.fromTag == from && entry.toTag == to)
                return entry.level;
        }

        return HarmonyLevel.Neutral;
    }

    public static float GetDamageMultiplier(HarmonyLevel level)
    {
        switch (level)
        {
            case HarmonyLevel.Harmonious: return 1.3f;
            case HarmonyLevel.Dissonant: return 0.6f;
            default: return 1.0f;
        }
    }

    public static HarmonyLevel GetBestHarmony(HarmonyTable table, AttackData from, AttackData to)
    {
        AttackTag[] fromTags = GetTags(from);
        AttackTag[] toTags = GetTags(to);

        HarmonyLevel best = HarmonyLevel.Neutral;

        foreach (var ft in fromTags)
        {
            foreach (var tt in toTags)
            {
                var h = GetHarmony(table, ft, tt);
                if (h == HarmonyLevel.Harmonious)
                    return HarmonyLevel.Harmonious;
                if (h == HarmonyLevel.Dissonant && best == HarmonyLevel.Neutral)
                    best = h;
            }
        }

        return best;
    }

    public static float[] CalculateComboMultipliers(HarmonyTable table, AttackData[] attacks)
    {
        float[] multipliers = new float[attacks.Length];
        multipliers[0] = 1.0f;

        for (int i = 1; i < attacks.Length; i++)
        {
            if (attacks[i] == null || attacks[i - 1] == null)
            {
                multipliers[i] = 1.0f;
                continue;
            }

            var harmony = GetBestHarmony(table, attacks[i - 1], attacks[i]);
            multipliers[i] = GetDamageMultiplier(harmony);
        }

        return multipliers;
    }

    private static AttackTag[] GetTags(AttackData attack)
    {
        if (attack.secondaryTag != AttackTag.None)
            return new AttackTag[] { attack.primaryTag, attack.secondaryTag };
        return new AttackTag[] { attack.primaryTag };
    }
}
