# Visual Feedback System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add visual combat feedback — enemy hit flash, stagger tint+shake, death flash+fade, and player attack swing arc.

**Architecture:** Two new MonoBehaviours: `EnemyVisualFeedback` (on enemies, subscribes to EnemyHealth events) and `AttackVisualFeedback` (on player, called by PlayerCombat). One shared WhiteFlash material via Shader Graph. Asset sprite for slash arc already exists at project root (`slash.png`).

**Tech Stack:** Unity 2022.3 LTS, URP 2D, C#, Shader Graph

**Spec:** `docs/superpowers/specs/2026-04-01-visual-feedback-design.md`

---

## File Structure

| Action | Path | Responsibility |
|--------|------|---------------|
| Create | `Assets/Scripts/Combat/EnemyVisualFeedback.cs` | Hit flash, stagger tint+shake, death flash+fade on enemies |
| Create | `Assets/Scripts/Combat/AttackVisualFeedback.cs` | Swing arc sprite display on player attacks |
| Create | `Assets/Shaders/WhiteFlash.shadergraph` | Shader Graph: outputs white RGB, preserves sprite alpha |
| Create | `Assets/Materials/WhiteFlash.mat` | Material using WhiteFlash shader, shared by all enemies |
| Move | `slash.png` → `Assets/Sprites/Effects/SlashArc.png` | Slash arc sprite asset |
| Modify | `Assets/Scripts/Enemies/EnemyStateMachine.cs:187-198,217-234` | Remove DeathFade coroutine, remove StartCoroutine call from SetState |
| Modify | `Assets/Scripts/Enemies/BossController.cs:165-170` | Remove `enabled = false` from OnBossDeath |
| Modify | `Assets/Scripts/Player/PlayerCombat.cs:58-68` | Add ShowSwing call in TryAttack |
| Modify | `Assets/Scripts/Editor/PrefabGenerator.cs:45-88,98-138` | Add visual feedback components to prefabs, create Boss prefab |
| Create | `Assets/Tests/PlayMode/Combat/EnemyVisualFeedbackTests.cs` | PlayMode tests for hit flash, stagger, death |
| Create | `Assets/Tests/PlayMode/Combat/AttackVisualFeedbackTests.cs` | PlayMode tests for swing arc |

---

### Task 1: Move slash sprite asset to correct location

**Files:**
- Move: `slash.png` → `Assets/Sprites/Effects/SlashArc.png`

- [ ] **Step 1: Create Effects directory and move sprite**

```bash
mkdir -p Assets/Sprites/Effects
mv slash.png Assets/Sprites/Effects/SlashArc.png
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Sprites/Effects/SlashArc.png
git rm --cached slash.png 2>/dev/null; git add -u slash.png
git commit -m "asset: move slash arc sprite to Assets/Sprites/Effects/"
```

---

### Task 2: Create WhiteFlash shader and material

This task creates the shader via an editor script since Shader Graph `.shadergraph` files are complex JSON — we generate both the shader and material programmatically.

**Files:**
- Create: `Assets/Scripts/Editor/MaterialGenerator.cs`

The generator creates:
1. A simple unlit shader file (`Assets/Shaders/WhiteFlash.shader`) that outputs solid white while sampling the sprite texture alpha
2. A material using that shader (`Assets/Materials/WhiteFlash.mat`)

We use a hand-written `.shader` instead of Shader Graph because ShaderGraph JSON is not human-writable. A simple vertex/fragment shader achieves the same result and is fully compatible with URP 2D.

- [ ] **Step 1: Create MaterialGenerator.cs**

Create `Assets/Scripts/Editor/MaterialGenerator.cs`:

```csharp
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class MaterialGenerator
{
    [MenuItem("Tools/Generate Materials")]
    public static void GenerateAll()
    {
        EnsureDirectory("Assets/Shaders");
        EnsureDirectory("Assets/Materials");

        CreateWhiteFlashShader();
        CreateWhiteFlashMaterial();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Materials generated!");
    }

    private static void CreateWhiteFlashShader()
    {
        string shaderPath = "Assets/Shaders/WhiteFlash.shader";
        if (File.Exists(shaderPath)) return;

        string shaderCode = @"Shader ""Custom/WhiteFlash""
{
    Properties
    {
        _MainTex (""Texture"", 2D) = ""white"" {}
    }
    SubShader
    {
        Tags { ""Queue""=""Transparent"" ""RenderType""=""Transparent"" ""RenderPipeline""=""UniversalPipeline"" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl""

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                return half4(1, 1, 1, texColor.a * IN.color.a);
            }
            ENDHLSL
        }
    }
}";
        File.WriteAllText(shaderPath, shaderCode);
        AssetDatabase.ImportAsset(shaderPath);
    }

    private static void CreateWhiteFlashMaterial()
    {
        string matPath = "Assets/Materials/WhiteFlash.mat";
        if (File.Exists(matPath)) return;

        // Wait for shader to be available
        var shader = Shader.Find("Custom/WhiteFlash");
        if (shader == null)
        {
            Debug.LogError("WhiteFlash shader not found. Run Tools > Generate Materials again after compilation.");
            return;
        }

        var mat = new Material(shader);
        AssetDatabase.CreateAsset(mat, matPath);
    }

    private static void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string folder = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/Editor/MaterialGenerator.cs
git commit -m "feat: add MaterialGenerator for WhiteFlash shader and material"
```

**Note:** The generator must be run in Unity (Tools > Generate Materials) before proceeding. The shader file is created on first run; the material may need a second run if the shader hasn't compiled yet.

---

### Task 3: Create EnemyVisualFeedback component

**Files:**
- Create: `Assets/Scripts/Combat/EnemyVisualFeedback.cs`

- [ ] **Step 1: Create EnemyVisualFeedback.cs**

Create `Assets/Scripts/Combat/EnemyVisualFeedback.cs`:

```csharp
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
        _originalMaterial = _sr.material;
        _originalColor = _sr.color;
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
        _sr.material = _originalMaterial;
        _sr.color = _originalColor;
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator HitFlashRoutine(float duration)
    {
        if (whiteFlashMaterial != null)
            _sr.material = whiteFlashMaterial;

        yield return new WaitForSeconds(duration);

        if (!_isDead)
            _sr.material = _originalMaterial;

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
            _sr.material = whiteFlashMaterial;

        yield return new WaitForSeconds(deathFlashDuration);

        _sr.material = _originalMaterial;

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
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/Combat/EnemyVisualFeedback.cs
git commit -m "feat: add EnemyVisualFeedback component — hit flash, stagger, death"
```

---

### Task 4: Create AttackVisualFeedback component

**Files:**
- Create: `Assets/Scripts/Combat/AttackVisualFeedback.cs`

- [ ] **Step 1: Create AttackVisualFeedback.cs**

Create `Assets/Scripts/Combat/AttackVisualFeedback.cs`:

```csharp
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
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/Combat/AttackVisualFeedback.cs
git commit -m "feat: add AttackVisualFeedback component — swing arc display"
```

---

### Task 5: Remove DeathFade from EnemyStateMachine

**Files:**
- Modify: `Assets/Scripts/Enemies/EnemyStateMachine.cs:187-198,217-234`

- [ ] **Step 1: Remove StartCoroutine(DeathFade()) from SetState**

In `Assets/Scripts/Enemies/EnemyStateMachine.cs`, replace the `SetState` method (lines 187-198):

```csharp
    public void SetState(EnemyState state)
    {
        if (CurrentState == EnemyState.Dead) return;
        CurrentState = state;

        if (state == EnemyState.Dead)
        {
            _rb.velocity = Vector2.zero;
            GetComponent<Collider2D>().enabled = false;
            StartCoroutine(DeathFade());
        }
    }
```

Replace with:

```csharp
    public void SetState(EnemyState state)
    {
        if (CurrentState == EnemyState.Dead) return;
        CurrentState = state;

        if (state == EnemyState.Dead)
        {
            _rb.velocity = Vector2.zero;
            GetComponent<Collider2D>().enabled = false;
        }
    }
```

- [ ] **Step 2: Remove the DeathFade coroutine**

Delete the entire `DeathFade()` method (lines 217-234):

```csharp
    private IEnumerator DeathFade()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        float duration = 0.5f;
        float elapsed = 0f;
        Color original = sr.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            sr.color = new Color(original.r, original.g, original.b, 1f - (elapsed / duration));
            yield return null;
        }

        Destroy(gameObject);
    }
```

Also remove `using System.Collections;` from line 1 if no other coroutines remain in the file (they don't).

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Enemies/EnemyStateMachine.cs
git commit -m "refactor: remove DeathFade from EnemyStateMachine — now in EnemyVisualFeedback"
```

---

### Task 6: Update BossController.OnBossDeath

**Files:**
- Modify: `Assets/Scripts/Enemies/BossController.cs:165-170`

- [ ] **Step 1: Remove enabled = false from OnBossDeath**

In `Assets/Scripts/Enemies/BossController.cs`, replace the `OnBossDeath` method (lines 165-170):

```csharp
    private void OnBossDeath()
    {
        _rb.velocity = Vector2.zero;
        // Could trigger victory screen, for now just disable
        enabled = false;
    }
```

Replace with:

```csharp
    private void OnBossDeath()
    {
        _rb.velocity = Vector2.zero;
        // Death visual + destroy handled by EnemyVisualFeedback
    }
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/Enemies/BossController.cs
git commit -m "refactor: BossController death delegates visual+destroy to EnemyVisualFeedback"
```

---

### Task 7: Add ShowSwing call to PlayerCombat

**Files:**
- Modify: `Assets/Scripts/Player/PlayerCombat.cs:58-68`

- [ ] **Step 1: Add ShowSwing call in TryAttack**

In `Assets/Scripts/Player/PlayerCombat.cs`, the current `TryAttack` method lines 58-68 are:

```csharp
        var result = _executor.Attack();
        if (!result.executed) return;

        _controller.SetState(PlayerState.Attacking);
        _currentAttackData = result.attackData;
        _attackAnimTimer = GetAttackDuration(result.attackData);
        _attackBuffered = false;

        ApplyAttackMovement(result.attackData);
        DealDamage(result);
    }
```

Replace with:

```csharp
        var result = _executor.Attack();
        if (!result.executed) return;

        _controller.SetState(PlayerState.Attacking);
        _currentAttackData = result.attackData;
        _attackAnimTimer = GetAttackDuration(result.attackData);
        _attackBuffered = false;

        ApplyAttackMovement(result.attackData);
        DealDamage(result);

        var swingVfx = GetComponent<AttackVisualFeedback>();
        if (swingVfx != null)
            swingVfx.ShowSwing(_controller.FacingDirection, _attackAnimTimer, attackRange);
    }
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/Player/PlayerCombat.cs
git commit -m "feat: PlayerCombat triggers swing arc visual on attack"
```

---

### Task 8: Update PrefabGenerator — enemies, boss, and player

**Files:**
- Modify: `Assets/Scripts/Editor/PrefabGenerator.cs:8-23,45-88,98-138`

- [ ] **Step 1: Add Boss prefab creation to GenerateAll**

In `Assets/Scripts/Editor/PrefabGenerator.cs`, replace the `GenerateAll` method (lines 8-23):

```csharp
    [MenuItem("Tools/Generate Prefabs")]
    public static void GenerateAll()
    {
        EnsureDirectory("Assets/Prefabs/Player");
        EnsureDirectory("Assets/Prefabs/Enemies");
        EnsureDirectory("Assets/Prefabs/Interactables");
        EnsureDirectory("Assets/Prefabs/UI");

        CreatePlayerPrefab();
        CreateEnemyPrefabs();
        CreateProjectilePrefab();
        CreateInteractablePrefabs();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("All prefabs generated!");
    }
```

Replace with:

```csharp
    [MenuItem("Tools/Generate Prefabs")]
    public static void GenerateAll()
    {
        EnsureDirectory("Assets/Prefabs/Player");
        EnsureDirectory("Assets/Prefabs/Enemies");
        EnsureDirectory("Assets/Prefabs/Interactables");
        EnsureDirectory("Assets/Prefabs/UI");

        CreatePlayerPrefab();
        CreateEnemyPrefabs();
        CreateBossPrefab();
        CreateProjectilePrefab();
        CreateInteractablePrefabs();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("All prefabs generated!");
    }
```

- [ ] **Step 2: Add EnemyVisualFeedback to CreateEnemyPrefab**

In `CreateEnemyPrefab` (line 98), after `AssignPlaceholderSprite(go, name);` (line 136) and before the `PrefabUtility.SaveAsPrefabAsset` call (line 137), add:

```csharp
        // Visual feedback
        var vfx = go.AddComponent<EnemyVisualFeedback>();
        var whiteFlashMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/WhiteFlash.mat");
        if (whiteFlashMat != null)
            vfx.whiteFlashMaterial = whiteFlashMat;
        else
            Debug.LogWarning("WhiteFlash material not found. Run Tools > Generate Materials first.");
```

- [ ] **Step 3: Add CreateBossPrefab method**

Add the following method after `CreateEnemyPrefabs()` (after line 96):

```csharp
    private static void CreateBossPrefab()
    {
        var go = new GameObject("Boss");
        try { go.tag = "Enemy"; }
        catch { Debug.LogWarning("Tag 'Enemy' not found. Add it in Edit > Project Settings > Tags and Layers."); }

        int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
        if (enemyLayerIndex >= 0)
            go.layer = enemyLayerIndex;

        go.AddComponent<SpriteRenderer>();
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.5f, 1.5f);

        var health = go.AddComponent<EnemyHealth>();
        var boss = go.AddComponent<BossController>();

        var enemyData = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/Enemies/Boss.asset");
        if (enemyData != null)
        {
            boss.data = enemyData;
            health.data = enemyData;
        }

        var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Projectile.prefab");
        if (projectilePrefab != null)
            boss.projectilePrefab = projectilePrefab;

        // Visual feedback
        var vfx = go.AddComponent<EnemyVisualFeedback>();
        var whiteFlashMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/WhiteFlash.mat");
        if (whiteFlashMat != null)
            vfx.whiteFlashMaterial = whiteFlashMat;

        AssignPlaceholderSprite(go, "Caster"); // Reuse Caster placeholder until Boss sprite exists
        PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Enemies/Boss.prefab");
        Object.DestroyImmediate(go);
    }
```

- [ ] **Step 4: Update CreatePlayerPrefab to add AttackVisualFeedback + SwingArc child**

In `CreatePlayerPrefab` (line 45), after `go.AddComponent<PlayerInput>();` (line 83) and before `AssignPlaceholderSprite(go, "Player");` (line 85), add:

```csharp
        // Swing arc child for attack visual
        var swingArcObj = new GameObject("SwingArc");
        swingArcObj.transform.SetParent(go.transform);
        swingArcObj.transform.localPosition = Vector3.zero;
        var swingArcSr = swingArcObj.AddComponent<SpriteRenderer>();
        swingArcSr.sortingOrder = 10;
        var slashSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Effects/SlashArc.png");
        if (slashSprite != null)
            swingArcSr.sprite = slashSprite;
        else
            Debug.LogWarning("SlashArc sprite not found at Assets/Sprites/Effects/SlashArc.png");
        swingArcSr.enabled = false;

        var attackVfx = go.AddComponent<AttackVisualFeedback>();
        attackVfx.swingArcRenderer = swingArcSr;
```

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Editor/PrefabGenerator.cs
git commit -m "feat: PrefabGenerator adds visual feedback components + Boss prefab"
```

---

### Task 9: PlayMode tests for EnemyVisualFeedback

**Files:**
- Create: `Assets/Tests/PlayMode/Combat/EnemyVisualFeedbackTests.cs`

- [ ] **Step 1: Create test file**

Create `Assets/Tests/PlayMode/Combat/EnemyVisualFeedbackTests.cs`:

```csharp
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
        _health.TakeDamage(10f, HarmonyLevel.None);

        // Material should be flash material immediately
        Assert.AreEqual(_flashMat, _sr.material);

        // Wait for flash to end
        yield return new WaitForSeconds(0.15f);

        // Material should be restored
        Assert.AreNotEqual(_flashMat, _sr.material);
    }

    [UnityTest]
    public IEnumerator HitFlash_RapidHits_RestartTimer()
    {
        _health.TakeDamage(5f, HarmonyLevel.None);
        yield return new WaitForSeconds(0.05f);

        // Hit again mid-flash
        _health.TakeDamage(5f, HarmonyLevel.None);

        // Should still be flashing
        Assert.AreEqual(_flashMat, _sr.material);

        // Wait for second flash to end
        yield return new WaitForSeconds(0.15f);

        Assert.AreNotEqual(_flashMat, _sr.material);
    }

    [UnityTest]
    public IEnumerator Stagger_AppliesTint_ThenRestores()
    {
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
        _health.TakeDamage(150f, HarmonyLevel.None);

        // Should be flashing white
        Assert.AreEqual(_flashMat, _sr.material);

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
    public void Awake_CachesOriginalMaterial()
    {
        // Original material should be cached (not the flash material)
        Assert.AreNotEqual(_flashMat, _sr.material);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Tests/PlayMode/Combat/EnemyVisualFeedbackTests.cs
git commit -m "test: add PlayMode tests for EnemyVisualFeedback"
```

---

### Task 10: PlayMode tests for AttackVisualFeedback

**Files:**
- Create: `Assets/Tests/PlayMode/Combat/AttackVisualFeedbackTests.cs`

- [ ] **Step 1: Create test file**

Create `Assets/Tests/PlayMode/Combat/AttackVisualFeedbackTests.cs`:

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class AttackVisualFeedbackTests
{
    private GameObject _playerObj;
    private AttackVisualFeedback _vfx;
    private SpriteRenderer _swingArcSr;

    [SetUp]
    public void SetUp()
    {
        _playerObj = new GameObject("TestPlayer");

        var swingArcObj = new GameObject("SwingArc");
        swingArcObj.transform.SetParent(_playerObj.transform);
        swingArcObj.transform.localPosition = Vector3.zero;
        _swingArcSr = swingArcObj.AddComponent<SpriteRenderer>();
        _swingArcSr.enabled = false;

        _vfx = _playerObj.AddComponent<AttackVisualFeedback>();
        _vfx.swingArcRenderer = _swingArcSr;
    }

    [TearDown]
    public void TearDown()
    {
        if (_playerObj != null)
            Object.DestroyImmediate(_playerObj);
    }

    [Test]
    public void SwingArc_StartsDisabled()
    {
        Assert.IsFalse(_swingArcSr.enabled);
    }

    [UnityTest]
    public IEnumerator ShowSwing_EnablesArc_ThenDisablesAfterDuration()
    {
        _vfx.ShowSwing(Vector2.right, 0.2f, 1.2f);

        // Arc should be enabled
        yield return null;
        Assert.IsTrue(_swingArcSr.enabled);

        // Wait for swing to end
        yield return new WaitForSeconds(0.25f);

        Assert.IsFalse(_swingArcSr.enabled);
    }

    [UnityTest]
    public IEnumerator ShowSwing_RotatesToFaceDirection()
    {
        _vfx.ShowSwing(Vector2.up, 0.3f, 1.2f);
        yield return null;

        // Up direction should be ~90 degrees
        float expectedAngle = 90f;
        float actualAngle = _swingArcSr.transform.localRotation.eulerAngles.z;
        Assert.AreEqual(expectedAngle, actualAngle, 1f);
    }

    [UnityTest]
    public IEnumerator ShowSwing_ScalesWithRange()
    {
        float range = 2.4f;
        float expectedScale = range / 1.2f; // 2.0
        _vfx.ShowSwing(Vector2.right, 0.3f, range);
        yield return null;

        Assert.AreEqual(expectedScale, _swingArcSr.transform.localScale.x, 0.01f);
    }

    [UnityTest]
    public IEnumerator ShowSwing_NewSwingCancelsPrevious()
    {
        _vfx.ShowSwing(Vector2.right, 0.5f, 1.2f);
        yield return new WaitForSeconds(0.1f);

        // Start new swing in different direction
        _vfx.ShowSwing(Vector2.left, 0.2f, 1.2f);
        yield return null;

        // Should be rotated for left direction (~180 degrees)
        float actualAngle = _swingArcSr.transform.localRotation.eulerAngles.z;
        Assert.AreEqual(180f, actualAngle, 1f);

        // Wait for second swing to end
        yield return new WaitForSeconds(0.25f);
        Assert.IsFalse(_swingArcSr.enabled);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Tests/PlayMode/Combat/AttackVisualFeedbackTests.cs
git commit -m "test: add PlayMode tests for AttackVisualFeedback"
```

---

### Task 11: Generate .meta files and verify

**Files:**
- All new files need `.meta` files for Unity

- [ ] **Step 1: Run verification skill**

Run the `verify` skill to check all C# scripts compile and have no issues.

- [ ] **Step 2: Commit any generated .meta files**

```bash
git add Assets/Scripts/Combat/EnemyVisualFeedback.cs.meta
git add Assets/Scripts/Combat/AttackVisualFeedback.cs.meta
git add Assets/Scripts/Editor/MaterialGenerator.cs.meta
git add Assets/Tests/PlayMode/Combat/EnemyVisualFeedbackTests.cs.meta
git add Assets/Tests/PlayMode/Combat/AttackVisualFeedbackTests.cs.meta
git add Assets/Sprites/Effects/SlashArc.png.meta
git add Assets/Sprites/Effects.meta
git commit -m "chore: add .meta files for visual feedback assets"
```

- [ ] **Step 3: Run generators in Unity**

In Unity Editor:
1. **Tools > Generate Materials** — creates WhiteFlash shader + material
2. **Tools > Generate Prefabs** — regenerates all prefabs with visual feedback components

- [ ] **Step 4: Final commit of generated assets**

```bash
git add Assets/Shaders/ Assets/Materials/ Assets/Prefabs/
git commit -m "asset: generated WhiteFlash shader/material and updated prefabs"
```
