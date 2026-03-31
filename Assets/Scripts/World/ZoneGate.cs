using UnityEngine;

public class ZoneGate : MonoBehaviour
{
    public GameObject lockedVisual;
    public GameObject unlockedVisual;
    public string targetScene = "BossArena";

    private bool _unlocked;

    private void Start()
    {
        CheckUnlocked();
    }

    private void Update()
    {
        if (!_unlocked) CheckUnlocked();
    }

    private void CheckUnlocked()
    {
        if (GameManager.Instance == null) return;
        _unlocked = GameManager.Instance.State.IsBossUnlocked;

        if (lockedVisual != null) lockedVisual.SetActive(!_unlocked);
        if (unlockedVisual != null) unlockedVisual.SetActive(_unlocked);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_unlocked) return;
        if (!other.CompareTag("Player")) return;

        UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
    }
}
