using UnityEngine;
using System;

public class EnemyHealth : MonoBehaviour
{
    public EnemyData data;
    public float CurrentHealth { get; private set; }

    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;
    public event Action OnStagger;

    private float _staggerAccumulator;
    private bool _isStaggered;

    public void Init()
    {
        if (data != null)
            CurrentHealth = data.maxHealth;
    }

    private void Start()
    {
        Init();
    }

    public void TakeDamage(float amount, HarmonyLevel harmonyLevel)
    {
        if (CurrentHealth <= 0f) return;

        // Apply armor reduction (disabled during stagger)
        float finalDamage = amount;
        if (data != null && data.armorReduction > 0f)
        {
            if (!_isStaggered || !data.armorDisabledDuringStagger)
            {
                finalDamage = Mathf.Max(1f, amount - data.armorReduction);
            }
        }

        CurrentHealth = Mathf.Max(0f, CurrentHealth - finalDamage);
        _staggerAccumulator += finalDamage;
        OnHealthChanged?.Invoke(CurrentHealth, data != null ? data.maxHealth : 1f);

        if (CurrentHealth <= 0f)
        {
            OnDeath?.Invoke();
            return;
        }

        if (data != null && _staggerAccumulator >= data.staggerThreshold)
        {
            _staggerAccumulator = 0f;
            OnStagger?.Invoke();
        }
    }

    public void EndStagger()
    {
        _isStaggered = false;
    }

    public void BeginStagger()
    {
        _isStaggered = true;
    }
}
