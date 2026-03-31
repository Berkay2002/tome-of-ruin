using UnityEngine;
using System;

public class PlayerPotions : MonoBehaviour
{
    public int maxPotions = 5;
    public float healAmount = 40f;
    public int startingPotions = 3;

    public int CurrentPotions { get; private set; }

    public event Action<int> OnPotionCountChanged;

    private PlayerHealth _health;

    private void Awake()
    {
        _health = GetComponent<PlayerHealth>();
        CurrentPotions = startingPotions;
    }

    public void AddPotion()
    {
        if (CurrentPotions >= maxPotions) return;
        CurrentPotions++;
        OnPotionCountChanged?.Invoke(CurrentPotions);
    }

    public void UsePotion()
    {
        if (CurrentPotions <= 0) return;
        if (_health == null) return;
        if (_health.CurrentHealth >= _health.maxHealth) return;

        CurrentPotions--;
        _health.Heal(healAmount);
        OnPotionCountChanged?.Invoke(CurrentPotions);
    }
}
