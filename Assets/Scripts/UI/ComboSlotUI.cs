using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System;

public class ComboSlotUI : MonoBehaviour, IDropHandler
{
    public int slotIndex;
    public Image slotBackground;
    public TextMeshProUGUI attackNameText;
    public TextMeshProUGUI harmonyText;

    public event Action<int, AttackData> OnAttackDropped;

    private AttackData _currentAttack;

    public void SetAttack(AttackData attack)
    {
        _currentAttack = attack;
        if (attackNameText != null)
            attackNameText.text = attack != null ? attack.attackName : "Empty";
    }

    public void SetHarmonyDisplay(HarmonyLevel level)
    {
        if (harmonyText == null) return;

        switch (level)
        {
            case HarmonyLevel.Harmonious:
                harmonyText.text = "Harmonious";
                harmonyText.color = Color.green;
                break;
            case HarmonyLevel.Dissonant:
                harmonyText.text = "Dissonant";
                harmonyText.color = Color.red;
                break;
            default:
                harmonyText.text = "Neutral";
                harmonyText.color = Color.gray;
                break;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        var card = eventData.pointerDrag?.GetComponent<AttackCardUI>();
        if (card == null || card.AttackData == null) return;

        SetAttack(card.AttackData);
        OnAttackDropped?.Invoke(slotIndex, card.AttackData);
    }
}
