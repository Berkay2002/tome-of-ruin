using UnityEngine;

public struct AttackResult
{
    public bool executed;
    public int slotIndex;
    public AttackData attackData;
    public float rawDamage;
    public float harmonyMultiplier;
    public float totalDamage;
    public HarmonyLevel harmonyLevel;

    public static AttackResult Failed => new AttackResult { executed = false };
}

public class ComboExecutor : MonoBehaviour
{
    public float chainWindowDuration = 0.5f;
    public HarmonyTable harmonyTable;

    public int CurrentSlot { get; private set; }
    public bool IsExecuting { get; private set; }

    private ComboBookData _equippedBook;
    private float _chainTimer;
    private float[] _cachedMultipliers;

    public void EquipBook(ComboBookData book)
    {
        _equippedBook = book;
        ResetCombo();
        CacheMultipliers();
    }

    public AttackResult Attack()
    {
        if (_equippedBook == null) return AttackResult.Failed;

        var attack = _equippedBook.GetAttack(CurrentSlot);
        if (attack == null) return AttackResult.Failed;

        if (IsExecuting) return AttackResult.Failed;

        float multiplier = _cachedMultipliers != null && CurrentSlot < _cachedMultipliers.Length
            ? _cachedMultipliers[CurrentSlot]
            : 1.0f;

        HarmonyLevel level = HarmonyLevel.Neutral;
        if (CurrentSlot > 0 && harmonyTable != null)
        {
            var prevAttack = _equippedBook.GetAttack(CurrentSlot - 1);
            if (prevAttack != null)
                level = HarmonyCalculator.GetBestHarmony(harmonyTable, prevAttack, attack);
        }

        IsExecuting = true;
        _chainTimer = chainWindowDuration;

        return new AttackResult
        {
            executed = true,
            slotIndex = CurrentSlot,
            attackData = attack,
            rawDamage = attack.baseDamage,
            harmonyMultiplier = multiplier,
            totalDamage = attack.baseDamage * multiplier,
            harmonyLevel = level
        };
    }

    public void FinishCurrentAttack()
    {
        IsExecuting = false;
        CurrentSlot++;

        if (CurrentSlot >= _equippedBook.SlotCount || _equippedBook.GetAttack(CurrentSlot) == null)
        {
            ResetCombo();
        }
    }

    public void ResetCombo()
    {
        CurrentSlot = 0;
        IsExecuting = false;
        _chainTimer = 0f;
    }

    private void Update()
    {
        if (!IsExecuting && CurrentSlot > 0)
        {
            _chainTimer -= Time.deltaTime;
            if (_chainTimer <= 0f)
                ResetCombo();
        }
    }

    private void CacheMultipliers()
    {
        if (_equippedBook == null || harmonyTable == null)
        {
            _cachedMultipliers = null;
            return;
        }

        var attacks = new AttackData[_equippedBook.SlotCount];
        for (int i = 0; i < _equippedBook.SlotCount; i++)
            attacks[i] = _equippedBook.GetAttack(i);

        _cachedMultipliers = HarmonyCalculator.CalculateComboMultipliers(harmonyTable, attacks);
    }
}
