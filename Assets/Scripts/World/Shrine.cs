using UnityEngine;
using UnityEngine.SceneManagement;

public class Shrine : MonoBehaviour
{
    public string shrineId;
    public Transform respawnPoint;
    public GameObject activatedVFX;

    private bool _discovered;

    private void Start()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.State.discoveredShrines.Contains(shrineId))
        {
            _discovered = true;
            if (activatedVFX != null) activatedVFX.SetActive(true);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Activate();
    }

    private void Activate()
    {
        if (GameManager.Instance == null) return;

        _discovered = true;
        string sceneName = SceneManager.GetActiveScene().name;
        GameManager.Instance.State.RegisterShrine(shrineId, sceneName);

        if (activatedVFX != null) activatedVFX.SetActive(true);

        var playerHealth = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerHealth>();
        if (playerHealth != null) playerHealth.Heal(playerHealth.maxHealth);
    }
}
