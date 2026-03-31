using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public Image fillImage;

    private PlayerHealth _playerHealth;

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerHealth = player.GetComponent<PlayerHealth>();
            if (_playerHealth != null)
                _playerHealth.OnHealthChanged += UpdateBar;
        }
    }

    private void OnDestroy()
    {
        if (_playerHealth != null)
            _playerHealth.OnHealthChanged -= UpdateBar;
    }

    private void UpdateBar(float current, float max)
    {
        if (fillImage != null && max > 0f)
            fillImage.fillAmount = current / max;
    }
}
