using UnityEngine;

public class KeyGate : MonoBehaviour
{
    public string requiredKeyId;
    public GameObject doorVisual;

    private bool _opened;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_opened) return;
        if (!other.CompareTag("Player")) return;

        if (GameManager.Instance != null && GameManager.Instance.State.HasKey(requiredKeyId))
        {
            Open();
        }
    }

    private void Open()
    {
        _opened = true;
        if (doorVisual != null) doorVisual.SetActive(false);
        GetComponent<Collider2D>().enabled = false;
    }

    private void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.State.HasKey(requiredKeyId))
            Open();
    }
}
