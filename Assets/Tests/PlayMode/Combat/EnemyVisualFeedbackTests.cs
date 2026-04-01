using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class EnemyVisualFeedbackTests
{
    private GameObject _enemyObj;
    private EnemyHealth _health;
    private EnemyVisualFeedback _vfx;
    private SpriteRenderer _sr;
    private Material _flashMat;
    private Material _originalMat;
    private EnemyData _data;

    [SetUp]
    public void SetUp()
    {
        _enemyObj = new GameObject("TestEnemy");
        _sr = _enemyObj.AddComponent<SpriteRenderer>();
        _enemyObj.AddComponent<Rigidbody2D>();
        _enemyObj.AddComponent<BoxCollider2D>();
        _health = _enemyObj.AddComponent<EnemyHealth>();

        _data = ScriptableObject.CreateInstance<EnemyData>();
        _data.maxHealth = 100f;
        _data.staggerThreshold = 20f;
        _data.staggerDuration = 0.5f;
        _health.data = _data;

        // Create a simple flash material for testing
        _flashMat = new Material(Shader.Find("Sprites/Default"));
        _flashMat.name = "TestFlash";

        _vfx = _enemyObj.AddComponent<EnemyVisualFeedback>();
        _vfx.whiteFlashMaterial = _flashMat;
        _vfx.flashDuration = 0.1f;
        _vfx.deathFlashDuration = 0.15f;
        _vfx.deathFadeDuration = 0.5f;

        _originalMat = _sr.material;

        _health.Init();
    }

    [TearDown]
    public void TearDown()
    {
        if (_enemyObj != null)
            Object.DestroyImmediate(_enemyObj);
        if (_data != null)
            Object.DestroyImmediate(_data);
        if (_flashMat != null)
            Object.DestroyImmediate(_flashMat);
    }

    [UnityTest]
    public IEnumerator HitFlash_SwapsMaterial_ThenRestores()
    {
        // Wait one frame so Start() runs and event subscriptions are active
        yield return null;

        _health.TakeDamage(10f, HarmonyLevel.None);

        // Material should be flash material immediately
        Assert.AreEqual(_flashMat, _sr.sharedMaterial);

        // Wait for flash to end
        yield return new WaitForSeconds(0.15f);

        // Material should be restored
        Assert.AreNotEqual(_flashMat, _sr.sharedMaterial);
    }

    [UnityTest]
    public IEnumerator HitFlash_RapidHits_RestartTimer()
    {
        yield return null; // Let Start() run

        _health.TakeDamage(5f, HarmonyLevel.None);
        yield return new WaitForSeconds(0.05f);

        // Hit again mid-flash
        _health.TakeDamage(5f, HarmonyLevel.None);

        // Should still be flashing
        Assert.AreEqual(_flashMat, _sr.sharedMaterial);

        // Wait for second flash to end
        yield return new WaitForSeconds(0.15f);

        Assert.AreNotEqual(_flashMat, _sr.sharedMaterial);
    }

    [UnityTest]
    public IEnumerator Stagger_AppliesTint_ThenRestores()
    {
        yield return null; // Let Start() run

        Color originalColor = _sr.color;

        // Deal enough damage to trigger stagger
        _health.TakeDamage(25f, HarmonyLevel.None);

        // Wait a frame for stagger to start
        yield return null;
        yield return null;

        // Color should be stagger tint
        Assert.AreEqual(_vfx.staggerTintColor, _sr.color);

        // Wait for stagger to end
        yield return new WaitForSeconds(0.6f);

        // Color should be restored
        Assert.AreEqual(originalColor.r, _sr.color.r, 0.01f);
        Assert.AreEqual(originalColor.g, _sr.color.g, 0.01f);
        Assert.AreEqual(originalColor.b, _sr.color.b, 0.01f);
    }

    [UnityTest]
    public IEnumerator Death_FlashesThenFades_ThenDestroysObject()
    {
        yield return null; // Let Start() run

        _health.TakeDamage(150f, HarmonyLevel.None);

        // Should be flashing white
        Assert.AreEqual(_flashMat, _sr.sharedMaterial);

        // Wait past death flash
        yield return new WaitForSeconds(0.2f);

        // Should be fading (alpha < 1)
        Assert.Less(_sr.color.a, 1f);

        // Wait for full death sequence
        yield return new WaitForSeconds(0.6f);

        // Object should be destroyed
        Assert.IsTrue(_enemyObj == null);
    }

    [Test]
    public void Setup_ComponentsWiredCorrectly()
    {
        // Verify the test setup produces a valid enemy with all required components
        Assert.IsNotNull(_vfx);
        Assert.IsNotNull(_health);
        Assert.IsNotNull(_sr);
        Assert.AreEqual(100f, _health.CurrentHealth, 0.01f);
        Assert.AreEqual(_flashMat, _vfx.whiteFlashMaterial);
    }
}
