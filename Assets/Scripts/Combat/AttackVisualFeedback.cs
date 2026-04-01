using System.Collections;
using UnityEngine;

public class AttackVisualFeedback : MonoBehaviour
{
    public SpriteRenderer swingArcRenderer;

    private Coroutine _swingCoroutine;

    private void Awake()
    {
        if (swingArcRenderer != null)
            swingArcRenderer.enabled = false;
    }

    public void ShowSwing(Vector2 direction, float duration, float range)
    {
        if (swingArcRenderer == null) return;

        if (_swingCoroutine != null) StopCoroutine(_swingCoroutine);
        _swingCoroutine = StartCoroutine(SwingRoutine(direction, duration, range));
    }

    private IEnumerator SwingRoutine(Vector2 direction, float duration, float range)
    {
        // Position at attack point offset
        swingArcRenderer.transform.localPosition = (Vector3)(direction.normalized * 0.5f);

        // Rotate to face attack direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        swingArcRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        // Scale to match attack range
        float scaleFactor = range / 1.2f; // Normalize against default attackRange
        swingArcRenderer.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);

        swingArcRenderer.enabled = true;

        yield return new WaitForSeconds(duration);

        swingArcRenderer.enabled = false;
        _swingCoroutine = null;
    }
}
