using UnityEngine;
using System.Collections.Generic;

public enum ComboBookRarity
{
    Common,
    Rare,
    Legendary
}

[CreateAssetMenu(fileName = "NewComboBook", menuName = "Game/Combo Book")]
public class ComboBookData : ScriptableObject
{
    public string bookName;
    public ComboBookRarity rarity;
    [SerializeField] private AttackData[] slots;

    public int SlotCount => slots != null ? slots.Length : 0;

    public void InitSlots()
    {
        if (slots != null && slots.Length > 0) return;

        int count = rarity switch
        {
            ComboBookRarity.Common => 2,
            ComboBookRarity.Rare => 3,
            ComboBookRarity.Legendary => 4,
            _ => 2
        };
        slots = new AttackData[count];
    }

    public void ForceInitSlots()
    {
        int count = rarity switch
        {
            ComboBookRarity.Common => 2,
            ComboBookRarity.Rare => 3,
            ComboBookRarity.Legendary => 4,
            _ => 2
        };
        slots = new AttackData[count];
    }

    public void SetAttack(int slotIndex, AttackData attack)
    {
        if (slots == null || slotIndex < 0 || slotIndex >= slots.Length) return;
        slots[slotIndex] = attack;
    }

    public AttackData GetAttack(int slotIndex)
    {
        if (slots == null || slotIndex < 0 || slotIndex >= slots.Length) return null;
        return slots[slotIndex];
    }

    public void ClearSlot(int slotIndex)
    {
        SetAttack(slotIndex, null);
    }

    public AttackData[] GetFilledAttacks()
    {
        if (slots == null) return new AttackData[0];

        var filled = new List<AttackData>();
        foreach (var slot in slots)
        {
            if (slot != null) filled.Add(slot);
        }
        return filled.ToArray();
    }
}
