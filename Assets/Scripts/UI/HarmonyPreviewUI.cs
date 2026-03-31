using UnityEngine;
using TMPro;

public class HarmonyPreviewUI : MonoBehaviour
{
    public TextMeshProUGUI totalDamageText;
    public TextMeshProUGUI overallHarmonyText;

    public void UpdatePreview(ComboBookData book, HarmonyTable table)
    {
        if (book == null || table == null) return;

        var attacks = new AttackData[book.SlotCount];
        for (int i = 0; i < book.SlotCount; i++)
            attacks[i] = book.GetAttack(i);

        float[] multipliers = HarmonyCalculator.CalculateComboMultipliers(table, attacks);

        float totalDamage = 0f;
        int harmoniousCount = 0;
        int dissonantCount = 0;

        for (int i = 0; i < attacks.Length; i++)
        {
            if (attacks[i] == null) continue;
            totalDamage += attacks[i].baseDamage * multipliers[i];

            if (i > 0 && attacks[i - 1] != null)
            {
                var h = HarmonyCalculator.GetBestHarmony(table, attacks[i - 1], attacks[i]);
                if (h == HarmonyLevel.Harmonious) harmoniousCount++;
                else if (h == HarmonyLevel.Dissonant) dissonantCount++;
            }
        }

        if (totalDamageText != null)
            totalDamageText.text = $"Total: {totalDamage:F0}";

        if (overallHarmonyText != null)
        {
            if (dissonantCount > 0)
            {
                overallHarmonyText.text = "Flow: Disrupted";
                overallHarmonyText.color = Color.red;
            }
            else if (harmoniousCount > 0)
            {
                overallHarmonyText.text = "Flow: Harmonious";
                overallHarmonyText.color = Color.green;
            }
            else
            {
                overallHarmonyText.text = "Flow: Neutral";
                overallHarmonyText.color = Color.gray;
            }
        }
    }
}
