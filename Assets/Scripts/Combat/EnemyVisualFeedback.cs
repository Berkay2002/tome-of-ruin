using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyVisualFeedback : MonoBehaviour
{
    [Header("Hit Flash")]
    public float flashDuration = 0.1f;
    public Material whiteFlashMaterial;

    [Header("Stagger")]
    public Color staggerTintColor = new Color(1f, 0.9f, 0.4f);
    public float shakeIntensity = 0.05f;

    [Header("Death")]
    public float deathFlashDuration = 0.15f;
    public float deathFadeDuration = 0.5f;

    private SpriteRenderer _sr;
    private EnemyHealth _health;
    private Material _originalMaterial;
    private Color _originalColor;
    private Vector3 _originalLocalPos;
    private bool _isDead;
    private Coroutine _flashCoroutine;
    private Coroutine _staggerCoroutine;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _health = GetComponent<EnemyHealth>();
        _originalMaterial = _sr.sharedMaterial;
        _originalColor = _sr.color;
        _originalLocalPos = transform.localPosition;
    }

    private void Start()
    {
        _health.OnHealthChanged += OnHealthChanged;
        _health.OnStagger += OnStagger;
        _health.OnDeath += OnDeath;
    }

    private void OnDestroy()
    {
        if (_health != null)
        {
            _health.OnHealthChanged -= OnHealthChanged;
            _health.OnStagger -= OnStagger;
            _health.OnDeath -= OnDeath;
        }
    }

    private void OnHealthChanged(float currentHealth, float maxHealth)
    {
        if (_isDead) return;
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(HitFlashRoutine(flashDuration));
    }

    private void OnStagger()
    {
        if (_isDead) return;
        if (_staggerCoroutine != null) StopCoroutine(_staggerCoroutine);
        float duration = _health.data != null ? _health.data.staggerDuration : 0.5f;
        _staggerCoroutine = StartCoroutine(StaggerRoutine(duration));
    }

    private void OnDeath()
    {
        _isDead = true;
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        if (_staggerCoroutine != null) StopCoroutine(_staggerCoroutine);
        _sr.sharedMaterial = _originalMaterial;
        _sr.color = _originalColor;
        transform.localPosition = _originalLocalPos;
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator HitFlashRoutine(float duration)
    {
        if (whiteFlashMaterial != null)
            _sr.sharedMaterial = whiteFlashMaterial;

        yield return new WaitForSeconds(duration);

        if (!_isDead)
            _sr.sharedMaterial = _originalMaterial;

        _flashCoroutine = null;
    }

    private IEnumerator StaggerRoutine(float duration)
    {
        _originalLocalPos = transform.localPosition;
        _sr.color = staggerTintColor;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float offsetX = Mathf.Sin(elapsed * 30f * Mathf.PI) * shakeIntensity;
            float offsetY = Mathf.Cos(elapsed * 25f * Mathf.PI) * shakeIntensity * 0.5f;
            transform.localPosition = _originalLocalPos + new Vector3(offsetX, offsetY, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = _originalLocalPos;
        _sr.color = _originalColor;
        _staggerCoroutine = null;
    }

    private IEnumerator DeathRoutine()
    {
        // Death flash
        if (whiteFlashMaterial != null)
            _sr.sharedMaterial = whiteFlashMaterial;

        yield return new WaitForSeconds(deathFlashDuration);

        _sr.sharedMaterial = _originalMaterial;

        // Death fade
        float elapsed = 0f;
        while (elapsed < deathFadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / deathFadeDuration);
            _sr.color = new Color(_originalColor.r, _originalColor.g, _originalColor.b, alpha);
            yield return null;
        }

        Destroy(gameObject);
    }
}
