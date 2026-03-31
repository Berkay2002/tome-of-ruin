using UnityEngine;

public class ShortcutDoor : MonoBehaviour
{
    public string shortcutId;
    public GameObject doorVisual;
    public bool openFromThisSide = true;

    private bool _unlocked;

    private void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.State.IsShortcutUnlocked(shortcutId))
            Unlock();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_unlocked) return;
        if (!other.CompareTag("Player")) return;
        if (!openFromThisSide) return;

        Unlock();
    }

    private void Unlock()
    {
        _unlocked = true;
        if (doorVisual != null) doorVisual.SetActive(false);
        GetComponent<Collider2D>().enabled = false;

        if (GameManager.Instance != null)
            GameManager.Instance.State.UnlockShortcut(shortcutId);
    }
}
