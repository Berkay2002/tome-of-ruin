using UnityEngine;
using TMPro;

public class ComboBookHUD : MonoBehaviour
{
    public TextMeshProUGUI bookNameText;
    public TextMeshProUGUI potionCountText;

    private PlayerInventory _inventory;
    private PlayerPotions _potions;

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _inventory = player.GetComponent<PlayerInventory>();
            _potions = player.GetComponent<PlayerPotions>();

            if (_potions != null)
                _potions.OnPotionCountChanged += UpdatePotionCount;
        }

        UpdateDisplay();
    }

    private void OnDestroy()
    {
        if (_potions != null)
            _potions.OnPotionCountChanged -= UpdatePotionCount;
    }

    private void UpdateDisplay()
    {
        if (_inventory != null && _inventory.equippedBook != null && bookNameText != null)
            bookNameText.text = _inventory.equippedBook.bookName;
        else if (bookNameText != null)
            bookNameText.text = "No Book";

        if (_potions != null && potionCountText != null)
            potionCountText.text = $"Potions: {_potions.CurrentPotions}";
    }

    private void UpdatePotionCount(int count)
    {
        if (potionCountText != null)
            potionCountText.text = $"Potions: {count}";
    }

    public void RefreshBookName()
    {
        UpdateDisplay();
    }
}
