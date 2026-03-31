using UnityEngine;
using System.Collections.Generic;

public class ComboBookUI : MonoBehaviour
{
    public GameObject panel;
    public Transform slotContainer;
    public Transform attackListContainer;
    public ComboSlotUI slotPrefab;
    public AttackCardUI cardPrefab;
    public HarmonyPreviewUI harmonyPreview;

    private PlayerInventory _inventory;
    private ComboBookData _activeBook;
    private List<ComboSlotUI> _slots = new List<ComboSlotUI>();
    private List<AttackCardUI> _cards = new List<AttackCardUI>();
    private bool _isOpen;

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _inventory = player.GetComponent<PlayerInventory>();

        if (panel != null) panel.SetActive(false);
    }

    public void Toggle()
    {
        if (_isOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (_inventory == null) return;
        _activeBook = _inventory.equippedBook;
        if (_activeBook == null) return;

        _isOpen = true;
        if (panel != null) panel.SetActive(true);
        Time.timeScale = 0f;

        BuildSlots();
        BuildAttackList();
        UpdateHarmonyPreview();
    }

    public void Close()
    {
        _isOpen = false;
        if (panel != null) panel.SetActive(false);
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (_isOpen) Time.timeScale = 1f;
    }

    private void BuildSlots()
    {
        foreach (var slot in _slots) Destroy(slot.gameObject);
        _slots.Clear();

        for (int i = 0; i < _activeBook.SlotCount; i++)
        {
            var slot = Instantiate(slotPrefab, slotContainer);
            slot.slotIndex = i;
            slot.SetAttack(_activeBook.GetAttack(i));
            slot.OnAttackDropped += OnSlotChanged;
            _slots.Add(slot);
        }
    }

    private void BuildAttackList()
    {
        foreach (var card in _cards) Destroy(card.gameObject);
        _cards.Clear();

        foreach (var attack in _inventory.attacks)
        {
            var card = Instantiate(cardPrefab, attackListContainer);
            card.Setup(attack);
            _cards.Add(card);
        }
    }

    private void OnSlotChanged(int slotIndex, AttackData attack)
    {
        _activeBook.SetAttack(slotIndex, attack);
        UpdateHarmonyPreview();

        var executor = _inventory.GetComponent<ComboExecutor>();
        if (executor != null) executor.EquipBook(_activeBook);
    }

    private void UpdateHarmonyPreview()
    {
        if (harmonyPreview == null || GameManager.Instance == null) return;
        harmonyPreview.UpdatePreview(_activeBook, GameManager.Instance.harmonyTable);

        for (int i = 0; i < _slots.Count; i++)
        {
            if (i == 0)
            {
                _slots[i].SetHarmonyDisplay(HarmonyLevel.Neutral);
                continue;
            }

            var prev = _activeBook.GetAttack(i - 1);
            var curr = _activeBook.GetAttack(i);
            if (prev != null && curr != null)
            {
                var h = HarmonyCalculator.GetBestHarmony(
                    GameManager.Instance.harmonyTable, prev, curr);
                _slots[i].SetHarmonyDisplay(h);
            }
            else
            {
                _slots[i].SetHarmonyDisplay(HarmonyLevel.Neutral);
            }
        }
    }
}
