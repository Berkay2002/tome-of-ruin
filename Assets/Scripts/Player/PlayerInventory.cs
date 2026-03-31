using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    public List<AttackData> attacks = new List<AttackData>();
    public List<ComboBookData> comboBooks = new List<ComboBookData>();
    public ComboBookData equippedBook;

    public event Action<AttackData> OnAttackCollected;
    public event Action<ComboBookData> OnComboBookCollected;

    public void AddAttack(AttackData attack)
    {
        if (attacks.Contains(attack)) return;
        attacks.Add(attack);
        OnAttackCollected?.Invoke(attack);

        if (GameManager.Instance != null)
            GameManager.Instance.State.DiscoverAttack(attack);
    }

    public void AddComboBook(ComboBookData book)
    {
        if (comboBooks.Contains(book)) return;
        comboBooks.Add(book);
        OnComboBookCollected?.Invoke(book);

        if (GameManager.Instance != null)
            GameManager.Instance.State.DiscoverComboBook(book);

        if (equippedBook == null)
            EquipBook(book);
    }

    public void EquipBook(ComboBookData book)
    {
        equippedBook = Instantiate(book);
        equippedBook.InitSlots();
        var executor = GetComponent<ComboExecutor>();
        if (executor != null) executor.EquipBook(equippedBook);
    }
}
