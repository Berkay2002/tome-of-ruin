using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float CurrentHealth { get; private set; }

    public event Action<float, float> OnHealthChanged; // current, max
    public event Action OnDeath;

    private PlayerController _controller;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
    }

    private void Start()
    {
        Init();
    }

    public void Init()
    {
        _controller = GetComponent<PlayerController>();
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (_controller != null && _controller.CurrentState == PlayerState.Dodging)
            return;

        if (_controller != null && _controller.CurrentState == PlayerState.Dead)
            return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0f)
        {
            if (_controller != null) _controller.SetState(PlayerState.Dead);
            OnDeath?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}
