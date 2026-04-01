# Level Geometry & Visual Theming Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace tilemap-based level construction with SpriteShape polygon geometry, add NavMeshPlus enemy navigation, and generate complete zone scenes with per-zone visual theming.

**Architecture:** Rooms are closed SpriteShape polygons with EdgeCollider2D for wall collision, SpriteRenderer floors with world-space tiling Shader Graph material, and NavMeshPlus for enemy pathfinding. A RoomManager script switches CinemachineConfiner targets as the player moves between rooms. EnemyStateMachine is updated to use NavMeshAgent for pathfinding while keeping Rigidbody2D for movement.

**Tech Stack:** Unity 2022.3 LTS, URP 2D, SpriteShape (`com.unity.2d.spriteshape`), NavMeshPlus (git package), Shader Graph (Sprite Lit sub-graph), Cinemachine 2.10.3

**Spec:** `docs/superpowers/specs/2026-04-01-level-geometry-design.md`

---

## File Map

### New Files

| File | Responsibility |
|------|---------------|
| `Assets/Scripts/World/RoomManager.cs` | Switches active CinemachineConfiner when player enters a room trigger |
| `Assets/Scripts/World/LightFlicker.cs` | Flickers Point Light 2D intensity for torch effect |
| `Assets/Scripts/World/BossArenaLighting.cs` | Coroutine-driven lighting transition on boss arena entry |
| `Assets/Scripts/Editor/LevelGenerator.cs` | Generates SpriteShape rooms, corridors, floors, lights, NavMesh per scene |
| `Assets/Scripts/Editor/SpriteShapeProfileGenerator.cs` | Generates SpriteShapeProfile assets with placeholder edge/corner sprites |
| `Assets/Scripts/Editor/FloorMaterialGenerator.cs` | Generates Shader Graph material + placeholder floor textures per zone |
| (No Shader Graph file) | Floor material uses `Sprite-Lit-Default` shader with texture tiling — no custom .shadergraph needed |
| `Assets/Tests/EditMode/World/RoomManagerTests.cs` | Tests for RoomManager camera confiner switching logic |
| `Assets/Tests/PlayMode/World/LightFlickerTests.cs` | Tests for LightFlicker behavior |

### Modified Files

| File | Changes |
|------|---------|
| `Assets/Scripts/Enemies/EnemyStateMachine.cs` | Add NavMeshAgent integration (SetDestination + desiredVelocity) |
| `Assets/Scripts/Enemies/EnemyData.cs` | Add `navMeshRadius` field |
| `Assets/Scripts/Editor/SceneGenerator.cs` | Replace empty scene generation with calls to LevelGenerator |
| `Assets/Scripts/Editor/DataAssetGenerator.cs` | Add `navMeshRadius` values to enemy data creation |
| `Packages/manifest.json` | Add `com.unity.2d.spriteshape` and NavMeshPlus git URL |
| `CLAUDE.md` | Replace Tilemap with SpriteShape + NavMeshPlus in tech stack |

---

## Task 1: Install SpriteShape and NavMeshPlus packages

**Files:**
- Modify: `Packages/manifest.json`

- [ ] **Step 1: Add SpriteShape package to manifest**

In `Packages/manifest.json`, add to the `"dependencies"` object (alphabetical order):

```json
"com.unity.2d.spriteshape": "9.0.4",
```

Add after the `"com.unity.2d.sprite"` line.

- [ ] **Step 2: Add NavMeshPlus package to manifest**

In `Packages/manifest.json`, add to the `"dependencies"` object:

```json
"h8man.nav-mesh-plus": "https://github.com/h8man/NavMeshPlus.git#master",
```

Add after the `"com.unity.collab-proxy"` line.

- [ ] **Step 3: Commit**

```bash
git add Packages/manifest.json
git commit -m "deps: add SpriteShape and NavMeshPlus packages"
```

---

## Task 2: Configure sorting layers

Sorting layers control render order so floors appear behind players/enemies and walls appear in front.

**Files:**
- No script files — this is a Unity Editor configuration step

- [ ] **Step 1: Add sorting layers via script or manually**

Sorting layers cannot be reliably created from editor scripts (they live in `ProjectSettings/TagManager.asset` which is binary). Add these sorting layers manually in Unity:

**Edit > Project Settings > Tags and Layers > Sorting Layers:**

| Order | Sorting Layer |
|-------|--------------|
| 0 | Default (already exists) |
| 1 | Floor |
| 2 | Decorations |
| 3 | Walls |
| 4 | UI |

Add them in this order so Floor renders behind Default (player/enemies) and Walls renders in front.

- [ ] **Step 2: Commit**

```bash
git add ProjectSettings/TagManager.asset
git commit -m "config: add sorting layers for level geometry rendering order"
```

---

## Task 3: Create floor tiling material

**Files:**
- Create: `Assets/Scripts/Editor/FloorMaterialGenerator.cs`

- [ ] **Step 1: Write FloorMaterialGenerator**

This generator creates a placeholder floor texture (solid color per zone) and a URP Sprite-Lit material with tiling. Since Shader Graph `.shadergraph` files are binary and cannot be created from code, we use the built-in `Sprites/Default` shader with texture tiling set via `material.mainTextureScale`. This achieves world-space-like tiling without a custom Shader Graph. A proper Shader Graph can be authored in the Unity editor later for true world-space UV tiling.

```csharp
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class FloorMaterialGenerator
{
    [MenuItem("Tools/Generate Floor Materials")]
    public static void GenerateAll()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("Cannot generate floor materials during Play Mode.");
            return;
        }

        EnsureDirectory("Assets/Art");
        EnsureDirectory("Assets/Art/LevelArt");
        EnsureDirectory("Assets/Art/LevelArt/ZoneA");
        EnsureDirectory("Assets/Art/LevelArt/ZoneB");
        EnsureDirectory("Assets/Art/LevelArt/ZoneC");
        EnsureDirectory("Assets/Art/LevelArt/BossArena");
        EnsureDirectory("Assets/Materials");

        CreateFloorAssets("ZoneA", new Color(0.25f, 0.25f, 0.22f));    // Worn stone gray-green
        CreateFloorAssets("ZoneB", new Color(0.12f, 0.12f, 0.10f));    // Dark earth
        CreateFloorAssets("ZoneC", new Color(0.20f, 0.15f, 0.18f));    // Dark slate purple
        CreateFloorAssets("BossArena", new Color(0.08f, 0.08f, 0.10f)); // Near-black obsidian

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Floor materials generated!");
    }

    private static void CreateFloorAssets(string zoneName, Color baseColor)
    {
        string texturePath = $"Assets/Art/LevelArt/{zoneName}/Floor_{zoneName}.png";
        string materialPath = $"Assets/Materials/Floor_{zoneName}.mat";

        // Create placeholder floor texture (64x64 solid color)
        if (!System.IO.File.Exists(texturePath))
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = baseColor;
            texture.SetPixels(pixels);
            texture.Apply();

            var bytes = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(texturePath, bytes);
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(texturePath);

            // Set import settings for tiling
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        // Create material using Sprites/Default (URP-compatible for 2D)
        if (!System.IO.File.Exists(materialPath))
        {
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
                Debug.LogWarning($"Sprite-Lit-Default shader not found, falling back to Sprites/Default for {zoneName}");
            }

            var mat = new Material(shader);
            var floorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            if (floorSprite != null)
                mat.mainTexture = floorSprite.texture;

            // Tiling scale — controls how many times the texture repeats
            mat.mainTextureScale = new Vector2(4f, 4f);

            AssetDatabase.CreateAsset(mat, materialPath);
        }
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
git add Assets/Scripts/Editor/FloorMaterialGenerator.cs
git commit -m "feat: add FloorMaterialGenerator for per-zone floor materials"
```

---

## Task 4: Create SpriteShapeProfile generator

**Files:**
- Create: `Assets/Scripts/Editor/SpriteShapeProfileGenerator.cs`

- [ ] **Step 1: Write SpriteShapeProfileGenerator**

Creates one `SpriteShapeProfile` per zone with placeholder edge sprites (solid-colored rectangles). Real AI-generated sprites replace these later.

```csharp
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.U2D;

public static class SpriteShapeProfileGenerator
{
    [MenuItem("Tools/Generate SpriteShape Profiles")]
    public static void GenerateAll()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("Cannot generate profiles during Play Mode.");
            return;
        }

        EnsureDirectory("Assets/Art");
        EnsureDirectory("Assets/Art/LevelArt");
        EnsureDirectory("Assets/Art/LevelArt/ZoneA");
        EnsureDirectory("Assets/Art/LevelArt/ZoneB");
        EnsureDirectory("Assets/Art/LevelArt/ZoneC");
        EnsureDirectory("Assets/Art/LevelArt/BossArena");
        EnsureDirectory("Assets/Data");

        CreateProfile("ZoneA", new Color(0.4f, 0.4f, 0.35f));      // Gray stone
        CreateProfile("ZoneB", new Color(0.7f, 0.7f, 0.6f));       // Pale bone
        CreateProfile("ZoneC", new Color(0.35f, 0.25f, 0.3f));     // Dark chapel stone
        CreateProfile("BossArena", new Color(0.15f, 0.15f, 0.18f)); // Dark polished stone

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("SpriteShape profiles generated!");
    }

    private static void CreateProfile(string zoneName, Color wallColor)
    {
        string profilePath = $"Assets/Data/SpriteShapeProfile_{zoneName}.asset";
        if (System.IO.File.Exists(profilePath)) return;

        // Create placeholder edge sprite (32x8 solid color rectangle)
        string spritePath = $"Assets/Art/LevelArt/{zoneName}/WallEdge_{zoneName}.png";
        if (!System.IO.File.Exists(spritePath))
        {
            var texture = new Texture2D(32, 8, TextureFormat.RGBA32, false);
            var pixels = new Color[32 * 8];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = wallColor;
            texture.SetPixels(pixels);
            texture.Apply();

            var bytes = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(spritePath, bytes);
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(spritePath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(spritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        var edgeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

        // Create SpriteShapeProfile
        var profile = ScriptableObject.CreateInstance<SpriteShapeProfile>();
        profile.fillPixelsPerUnit = 100;
        profile.fillOffset = 0f;

        // Set up angle ranges with the edge sprite
        // SpriteShapeProfile has a fixed set of angle ranges
        // We assign our edge sprite to all ranges for uniform walls
        var angleRanges = profile.angleRanges;
        for (int i = 0; i < angleRanges.Count; i++)
        {
            var range = angleRanges[i];
            range.sprites = new System.Collections.Generic.List<Sprite> { edgeSprite };
            angleRanges[i] = range;
        }

        AssetDatabase.CreateAsset(profile, profilePath);
        EditorUtility.SetDirty(profile);
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
git add Assets/Scripts/Editor/SpriteShapeProfileGenerator.cs
git commit -m "feat: add SpriteShapeProfileGenerator for per-zone wall profiles"
```

---

## Task 5: Create RoomManager for camera confiner switching

**Files:**
- Create: `Assets/Scripts/World/RoomManager.cs`
- Create: `Assets/Tests/EditMode/World/RoomManagerTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using NUnit.Framework;
using UnityEngine;

public class RoomManagerTests
{
    [Test]
    public void SetActiveRoom_UpdatesCurrentRoomIndex()
    {
        var go = new GameObject("RoomManager");
        var roomManager = go.AddComponent<RoomManager>();

        // Create two dummy confiner colliders
        var room0 = new GameObject("Room0");
        var collider0 = room0.AddComponent<PolygonCollider2D>();
        var room1 = new GameObject("Room1");
        var collider1 = room1.AddComponent<PolygonCollider2D>();

        roomManager.roomConfiners = new PolygonCollider2D[] { collider0, collider1 };
        roomManager.SetActiveRoom(1);

        Assert.AreEqual(1, roomManager.CurrentRoomIndex);

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(room0);
        Object.DestroyImmediate(room1);
    }

    [Test]
    public void SetActiveRoom_InvalidIndex_DoesNotChange()
    {
        var go = new GameObject("RoomManager");
        var roomManager = go.AddComponent<RoomManager>();

        var room0 = new GameObject("Room0");
        var collider0 = room0.AddComponent<PolygonCollider2D>();
        roomManager.roomConfiners = new PolygonCollider2D[] { collider0 };
        roomManager.SetActiveRoom(0);

        roomManager.SetActiveRoom(5); // Out of range
        Assert.AreEqual(0, roomManager.CurrentRoomIndex);

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(room0);
    }
}
```

- [ ] **Step 2: Write RoomManager implementation**

```csharp
using UnityEngine;
using Cinemachine;

public class RoomManager : MonoBehaviour
{
    [Header("Room Confiners")]
    public PolygonCollider2D[] roomConfiners;

    [Header("Camera")]
    public CinemachineConfiner confiner;

    public int CurrentRoomIndex { get; private set; } = -1;

    public void SetActiveRoom(int roomIndex)
    {
        if (roomIndex < 0 || roomIndex >= roomConfiners.Length) return;

        CurrentRoomIndex = roomIndex;

        if (confiner != null)
        {
            confiner.m_BoundingShape2D = roomConfiners[roomIndex];
            confiner.InvalidatePathCache();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Find which room this trigger belongs to by checking parent name
        // RoomTrigger GameObjects are children of Room_* objects
        for (int i = 0; i < roomConfiners.Length; i++)
        {
            if (roomConfiners[i] != null &&
                roomConfiners[i].transform.parent == transform.parent)
            {
                SetActiveRoom(i);
                return;
            }
        }
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/World/RoomManager.cs Assets/Tests/EditMode/World/RoomManagerTests.cs
git commit -m "feat: add RoomManager for camera confiner switching between rooms"
```

---

## Task 6: Create LightFlicker and BossArenaLighting scripts

**Files:**
- Create: `Assets/Scripts/World/LightFlicker.cs`
- Create: `Assets/Scripts/World/BossArenaLighting.cs`
- Create: `Assets/Tests/PlayMode/World/LightFlickerTests.cs`

- [ ] **Step 1: Write the LightFlicker failing test**

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Rendering.Universal;

public class LightFlickerTests
{
    [UnityTest]
    public IEnumerator LightFlicker_ChangesIntensityOverTime()
    {
        var go = new GameObject("TestLight");
        var light = go.AddComponent<Light2D>();
        light.intensity = 2f;

        var flicker = go.AddComponent<LightFlicker>();
        flicker.baseIntensity = 2f;
        flicker.flickerAmount = 0.5f;
        flicker.flickerSpeed = 10f;

        yield return new WaitForSeconds(0.2f);

        // Intensity should have changed from the base value due to flickering
        Assert.AreNotEqual(2f, light.intensity, 0.01f);

        Object.DestroyImmediate(go);
    }
}
```

- [ ] **Step 2: Write LightFlicker implementation**

```csharp
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class LightFlicker : MonoBehaviour
{
    [Header("Flicker Settings")]
    public float baseIntensity = 2f;
    public float flickerAmount = 0.3f;
    public float flickerSpeed = 5f;

    private Light2D _light;
    private float _seed;

    private void Awake()
    {
        _light = GetComponent<Light2D>();
        _seed = Random.Range(0f, 100f);
    }

    private void Update()
    {
        float noise = Mathf.PerlinNoise(_seed, Time.time * flickerSpeed);
        _light.intensity = baseIntensity + (noise - 0.5f) * 2f * flickerAmount;
    }
}
```

- [ ] **Step 3: Write BossArenaLighting implementation**

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BossArenaLighting : MonoBehaviour
{
    [Header("Global Light")]
    public Light2D globalLight;
    public float startIntensity = 0.2f;
    public float endIntensity = 0.4f;
    public Color endColor = new Color(0.3f, 0.05f, 0.05f);

    [Header("Perimeter Lights")]
    public Light2D[] perimeterLights;
    public float perimeterTargetIntensity = 4f;

    [Header("Transition")]
    public float transitionDuration = 2f;

    private bool _triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;

        _triggered = true;
        StartCoroutine(TransitionLighting());
    }

    private IEnumerator TransitionLighting()
    {
        Color startColor = globalLight.color;
        float elapsed = 0f;

        // Enable perimeter lights (start at 0 intensity)
        foreach (var light in perimeterLights)
        {
            if (light != null)
            {
                light.enabled = true;
                light.intensity = 0f;
            }
        }

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            globalLight.intensity = Mathf.Lerp(startIntensity, endIntensity, smooth);
            globalLight.color = Color.Lerp(startColor, endColor, smooth);

            foreach (var light in perimeterLights)
            {
                if (light != null)
                    light.intensity = Mathf.Lerp(0f, perimeterTargetIntensity, smooth);
            }

            yield return null;
        }

        // Ensure final values
        globalLight.intensity = endIntensity;
        globalLight.color = endColor;
        foreach (var light in perimeterLights)
        {
            if (light != null)
                light.intensity = perimeterTargetIntensity;
        }
    }
}
```

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/World/LightFlicker.cs Assets/Scripts/World/BossArenaLighting.cs Assets/Tests/PlayMode/World/LightFlickerTests.cs
git commit -m "feat: add LightFlicker and BossArenaLighting for zone atmosphere"
```

---

## Task 7: Add NavMeshAgent integration to EnemyStateMachine

**Files:**
- Modify: `Assets/Scripts/Enemies/EnemyStateMachine.cs`
- Modify: `Assets/Scripts/Enemies/EnemyData.cs`
- Modify: `Assets/Scripts/Editor/DataAssetGenerator.cs`

- [ ] **Step 1: Add navMeshRadius field to EnemyData**

In `Assets/Scripts/Enemies/EnemyData.cs`, add after the `armorDisabledDuringStagger` field (line 16):

```csharp
    [Header("Navigation")]
    public float navMeshRadius = 0.4f;
```

- [ ] **Step 2: Add navMeshRadius values to DataAssetGenerator**

In `Assets/Scripts/Editor/DataAssetGenerator.cs`, find each `CreateEnemyData` call and add `navMeshRadius` after the other fields. The values per enemy type:

- Hollow: `navMeshRadius = 0.4f`
- Wraith: `navMeshRadius = 0.3f`
- Knight: `navMeshRadius = 0.6f`
- Caster: `navMeshRadius = 0.3f`
- Boss: `navMeshRadius = 0.7f`

Find each enemy data creation block and add the field assignment. For example, after `enemy.armorDisabledDuringStagger = true;` (or false), add `enemy.navMeshRadius = 0.4f;` with the appropriate value.

- [ ] **Step 3: Update EnemyStateMachine to use NavMeshAgent**

Replace the contents of `Assets/Scripts/Enemies/EnemyStateMachine.cs` with:

```csharp
using UnityEngine;
using UnityEngine.AI;

public enum EnemyState
{
    Idle,
    Patrol,
    Chase,
    Attack,
    Stagger,
    Dead
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyStateMachine : MonoBehaviour
{
    public EnemyData data;
    public EnemyState CurrentState { get; private set; } = EnemyState.Idle;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float patrolWaitTime = 1f;

    [Header("Type-Specific Behavior")]
    public EnemyBehavior behavior;

    private Rigidbody2D _rb;
    private EnemyHealth _health;
    private NavMeshAgent _agent;
    private Transform _player;
    private int _patrolIndex;
    private float _patrolWaitTimer;
    private float _attackCooldownTimer;
    private float _staggerTimer;
    private float _attackWindupTimer;
    private bool _attackWindingUp;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;

        _health = GetComponent<EnemyHealth>();

        // Set up NavMeshAgent for 2D pathfinding
        _agent = GetComponent<NavMeshAgent>();
        if (_agent != null)
        {
            _agent.updatePosition = false;
            _agent.updateRotation = false;
            _agent.updateUpAxis = false;
        }
    }

    private void Start()
    {
        _health.OnDeath += () => SetState(EnemyState.Dead);
        _health.OnStagger += () =>
        {
            if (CurrentState != EnemyState.Dead)
            {
                SetState(EnemyState.Stagger);
                _staggerTimer = data.staggerDuration;
                _health.BeginStagger();
            }
        };

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;

        // Configure NavMeshAgent from data
        if (_agent != null && data != null)
        {
            _agent.speed = data.moveSpeed;
            _agent.radius = data.navMeshRadius;
        }
    }

    private void Update()
    {
        if (_player == null || data == null) return;

        // Keep NavMeshAgent position synced with Rigidbody2D position
        if (_agent != null)
            _agent.nextPosition = transform.position;

        switch (CurrentState)
        {
            case EnemyState.Idle:
                UpdateIdle();
                break;
            case EnemyState.Patrol:
                UpdatePatrol();
                break;
            case EnemyState.Chase:
                UpdateChase();
                break;
            case EnemyState.Attack:
                UpdateAttack();
                break;
            case EnemyState.Stagger:
                UpdateStagger();
                break;
            case EnemyState.Dead:
                _rb.velocity = Vector2.zero;
                break;
        }
    }

    private void UpdateIdle()
    {
        float dist = Vector2.Distance(transform.position, _player.position);
        if (dist <= data.detectionRange)
        {
            SetState(EnemyState.Chase);
            return;
        }

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            _patrolWaitTimer -= Time.deltaTime;
            if (_patrolWaitTimer <= 0f)
                SetState(EnemyState.Patrol);
        }
    }

    private void UpdatePatrol()
    {
        float dist = Vector2.Distance(transform.position, _player.position);
        if (dist <= data.detectionRange)
        {
            SetState(EnemyState.Chase);
            return;
        }

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            SetState(EnemyState.Idle);
            return;
        }

        var target = patrolPoints[_patrolIndex].position;

        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.SetDestination(target);
            Vector2 dir = ((Vector2)_agent.desiredVelocity).normalized;
            _rb.velocity = dir * data.moveSpeed;
        }
        else
        {
            // Fallback: direct movement when no NavMesh available
            Vector2 dir = ((Vector2)target - (Vector2)transform.position).normalized;
            _rb.velocity = dir * data.moveSpeed;
        }

        if (Vector2.Distance(transform.position, target) < 0.3f)
        {
            _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
            _patrolWaitTimer = patrolWaitTime;
            SetState(EnemyState.Idle);
        }
    }

    private void UpdateChase()
    {
        float dist = Vector2.Distance(transform.position, _player.position);

        if (dist > data.detectionRange * 1.5f)
        {
            SetState(EnemyState.Idle);
            return;
        }

        if (dist <= data.attackRange && _attackCooldownTimer <= 0f)
        {
            SetState(EnemyState.Attack);
            return;
        }

        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.SetDestination(_player.position);
            Vector2 dir = ((Vector2)_agent.desiredVelocity).normalized;
            _rb.velocity = dir * data.moveSpeed;
        }
        else
        {
            // Fallback: direct movement when no NavMesh available
            Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
            _rb.velocity = dir * data.moveSpeed;
        }

        _attackCooldownTimer -= Time.deltaTime;
    }

    private void UpdateAttack()
    {
        _rb.velocity = Vector2.zero;

        if (!_attackWindingUp)
        {
            _attackWindingUp = true;
            _attackWindupTimer = 0.4f;
            return;
        }

        _attackWindupTimer -= Time.deltaTime;
        if (_attackWindupTimer > 0f) return;

        _attackWindingUp = false;
        _attackCooldownTimer = data.attackCooldown;
        OnAttackExecute();
        SetState(EnemyState.Chase);
    }

    private void UpdateStagger()
    {
        _rb.velocity = Vector2.zero;
        _staggerTimer -= Time.deltaTime;
        if (_staggerTimer <= 0f)
        {
            _health.EndStagger();
            SetState(EnemyState.Chase);
        }
    }

    public void SetState(EnemyState state)
    {
        if (CurrentState == EnemyState.Dead) return;
        CurrentState = state;

        if (state == EnemyState.Dead)
        {
            _rb.velocity = Vector2.zero;
            GetComponent<Collider2D>().enabled = false;
            if (_agent != null) _agent.enabled = false;
        }
    }

    private void OnAttackExecute()
    {
        if (behavior != null)
        {
            behavior.ExecuteAttack(transform, _player, data);
            return;
        }

        float dist = Vector2.Distance(transform.position, _player.position);
        if (dist <= data.attackRange)
        {
            var playerHealth = _player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.TakeDamage(data.attackDamage);
        }
    }
}
```

Key changes from the original:
- Added `using UnityEngine.AI;`
- Added `private NavMeshAgent _agent;` field
- In `Awake()`: gets NavMeshAgent, sets `updatePosition = false`, `updateRotation = false`, `updateUpAxis = false`
- In `Start()`: configures agent speed/radius from data
- In `Update()`: syncs `_agent.nextPosition = transform.position` each frame
- In `UpdatePatrol()` and `UpdateChase()`: uses `_agent.SetDestination()` + `_agent.desiredVelocity` for direction, falls back to direct movement if no NavMesh
- In `SetState(Dead)`: disables NavMeshAgent

- [ ] **Step 4: Update PrefabGenerator to add NavMeshAgent to enemies**

In `Assets/Scripts/Editor/PrefabGenerator.cs`, in the `CreateEnemyPrefab` method, add after `sm.behavior = behavior;` (inside the behaviorType block, around line 196) or at the end before `AssignPlaceholderSprite`:

Add this block before `AssignPlaceholderSprite(go, name);`:

```csharp
        // NavMeshAgent for pathfinding (configured at runtime by EnemyStateMachine)
        var agent = go.AddComponent<UnityEngine.AI.NavMeshAgent>();
        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.radius = 0.4f;
        agent.height = 0.1f;
        agent.baseOffset = 0f;
```

Also add `NavMeshAgent` to the Boss prefab in `CreateBossPrefab`, before `AssignPlaceholderSprite`:

```csharp
        // NavMeshAgent for pathfinding
        var agent = go.AddComponent<UnityEngine.AI.NavMeshAgent>();
        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.radius = 0.7f;
        agent.height = 0.1f;
        agent.baseOffset = 0f;
```

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Enemies/EnemyStateMachine.cs Assets/Scripts/Enemies/EnemyData.cs Assets/Scripts/Editor/DataAssetGenerator.cs Assets/Scripts/Editor/PrefabGenerator.cs
git commit -m "feat: integrate NavMeshAgent into EnemyStateMachine with Rigidbody2D fallback"
```

---

## Task 8: Create LevelGenerator for SpriteShape rooms

**Files:**
- Create: `Assets/Scripts/Editor/LevelGenerator.cs`

This is the largest task. The LevelGenerator creates complete zone scenes with SpriteShape rooms, corridors, floors, lighting, NavMesh, enemies, and interactables.

- [ ] **Step 1: Write LevelGenerator**

```csharp
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.U2D;
using UnityEngine.Rendering.Universal;
using Cinemachine;

public static class LevelGenerator
{
    [MenuItem("Tools/Generate Levels")]
    public static void GenerateAll()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("Cannot generate levels during Play Mode.");
            return;
        }

        EnsureDirectory("Assets/Scenes");

        GenerateZoneA();
        GenerateZoneB();
        GenerateZoneC();
        GenerateBossArena();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("All levels generated with SpriteShape rooms, lighting, and NavMesh!");
    }

    // --- Zone Definitions ---

    private static void GenerateZoneA()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var sceneRoot = SetupSceneInfrastructure("ZoneA", new Color(0.05f, 0.05f, 0.1f), 0.15f, new Color(0.6f, 0.7f, 0.9f));

        // Hub room (center)
        var hubRoom = CreateRoom("Room_StartingRuins_Hub", "ZoneA",
            new Vector2[] {
                new(-8, -6), new(-8, 6), new(-2, 8), new(8, 6),
                new(10, 2), new(10, -2), new(8, -6), new(-2, -8)
            },
            Vector2.zero);

        // Side room A (key)
        var sideA = CreateRoom("Room_StartingRuins_SideA", "ZoneA",
            new Vector2[] {
                new(-5, -4), new(-5, 4), new(5, 4), new(5, -4)
            },
            new Vector2(-22, 8));

        // Side room B (key)
        var sideB = CreateRoom("Room_StartingRuins_SideB", "ZoneA",
            new Vector2[] {
                new(-4, -5), new(-4, 5), new(4, 5), new(6, 3), new(6, -3), new(4, -5)
            },
            new Vector2(22, -8));

        // Shrine alcove
        var shrine = CreateRoom("Room_StartingRuins_Shrine", "ZoneA",
            new Vector2[] {
                new(-3, -3), new(-3, 3), new(3, 3), new(3, -3)
            },
            new Vector2(0, -20));

        // Exit corridor to Zone B (left)
        var exitB = CreateRoom("Room_StartingRuins_ExitB", "ZoneA",
            new Vector2[] {
                new(-4, -3), new(-4, 3), new(4, 3), new(4, -3)
            },
            new Vector2(-22, -10));

        // Spawn enemies
        SpawnPrefab("Assets/Prefabs/Enemies/Hollow.prefab", new Vector3(5, 3, 0));
        SpawnPrefab("Assets/Prefabs/Enemies/Hollow.prefab", new Vector3(-4, -3, 0));
        SpawnPrefab("Assets/Prefabs/Enemies/Wraith.prefab", new Vector3(7, -2, 0));
        SpawnPrefab("Assets/Prefabs/Enemies/Knight.prefab", new Vector3(-20, 8, 0));

        // Spawn interactables
        SpawnPrefab("Assets/Prefabs/Interactables/Shrine.prefab", new Vector3(0, -20, 0));
        SpawnPrefab("Assets/Prefabs/Interactables/ItemPickup.prefab", new Vector3(-22, 10, 0));
        SpawnPrefab("Assets/Prefabs/Interactables/ItemPickup.prefab", new Vector3(24, -8, 0));

        // Torches (Point Lights)
        CreateTorchLight(new Vector3(-6, 0, 0), 2f, 0.3f);
        CreateTorchLight(new Vector3(6, 0, 0), 2f, 0.3f);
        CreateTorchLight(new Vector3(0, 5, 0), 1.8f, 0.25f);
        CreateTorchLight(new Vector3(-20, 9, 0), 1.5f, 0.2f);

        // Scene transitions
        CreateSceneTransition("SceneTransition_ToZoneB", new Vector3(-26, -10, 0), "ZoneB");
        CreateSceneTransition("SceneTransition_ToZoneC", new Vector3(26, 8, 0), "ZoneC");

        // Player + camera
        SetupPlayerAndCamera(Vector3.zero);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/ZoneA.unity");
    }

    private static void GenerateZoneB()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        SetupSceneInfrastructure("ZoneB", new Color(0.02f, 0.05f, 0.05f), 0.1f, new Color(0.3f, 0.8f, 0.7f));

        // Entry room
        CreateRoom("Room_Catacombs_Entry", "ZoneB",
            new Vector2[] {
                new(-5, -4), new(-5, 4), new(5, 5), new(7, 2), new(7, -2), new(5, -4)
            },
            Vector2.zero);

        // Branching corridor
        CreateRoom("Room_Catacombs_Branch", "ZoneB",
            new Vector2[] {
                new(-6, -3), new(-6, 3), new(6, 3), new(6, -3)
            },
            new Vector2(16, 0));

        // Dead end with loot
        CreateRoom("Room_Catacombs_DeadEnd", "ZoneB",
            new Vector2[] {
                new(-3, -3), new(-3, 3), new(3, 4), new(4, 2), new(4, -2), new(3, -3)
            },
            new Vector2(16, 12));

        // Mini-boss room
        CreateRoom("Room_Catacombs_MiniBoss", "ZoneB",
            new Vector2[] {
                new(-7, -5), new(-7, 5), new(7, 5), new(7, -5)
            },
            new Vector2(30, 0));

        // Shortcut alcove (back to A)
        CreateRoom("Room_Catacombs_Shortcut", "ZoneB",
            new Vector2[] {
                new(-3, -3), new(-3, 3), new(3, 3), new(3, -3)
            },
            new Vector2(-10, -12));

        // Enemies
        SpawnPrefab("Assets/Prefabs/Enemies/Wraith.prefab", new Vector3(3, 2, 0));
        SpawnPrefab("Assets/Prefabs/Enemies/Knight.prefab", new Vector3(18, 1, 0));
        SpawnPrefab("Assets/Prefabs/Enemies/Caster.prefab", new Vector3(16, 13, 0));
        SpawnPrefab("Assets/Prefabs/Enemies/Hollow.prefab", new Vector3(28, 3, 0));

        // Phosphorescent glow lights
        CreateFreeformLight(new Vector3(5, 0, 0), new Color(0.3f, 1f, 0.8f), 0.6f);
        CreateFreeformLight(new Vector3(20, 2, 0), new Color(0.3f, 1f, 0.8f), 0.5f);
        CreateFreeformLight(new Vector3(16, 14, 0), new Color(0.3f, 1f, 0.8f), 0.4f);

        // Interactables
        SpawnPrefab("Assets/Prefabs/Interactables/Shrine.prefab", new Vector3(-2, 0, 0));
        SpawnPrefab("Assets/Prefabs/Interactables/ShortcutDoor.prefab", new Vector3(-10, -12, 0));

        // Scene transitions
        CreateSceneTransition("SceneTransition_ToZoneA", new Vector3(-8, 0, 0), "ZoneA");
        CreateSceneTransition("SceneTransition_ToZoneC", new Vector3(37, 0, 0), "ZoneC");

        SetupPlayerAndCamera(new Vector3(-3, 0, 0));

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/ZoneB.unity");
    }

    private static void GenerateZoneC()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        SetupSceneInfrastructure("ZoneC", new Color(0.08f, 0.03f, 0.05f), 0.2f, new Color(0.6f, 0.2f, 0.3f));

        // Entry room
        CreateRoom("Room_CursedChapel_Entry", "ZoneC",
            new Vector2[] {
                new(-6, -5), new(-6, 5), new(6, 5), new(6, -5)
            },
            Vector2.zero);

        // Main hall
        CreateRoom("Room_CursedChapel_Hall", "ZoneC",
            new Vector2[] {
                new(-8, -6), new(-8, 6), new(8, 6), new(8, -6)
            },
            new Vector2(0, 16));

        // Mini-boss room
        CreateRoom("Room_CursedChapel_MiniBoss", "ZoneC",
            new Vector2[] {
                new(-7, -6), new(-7, 6), new(7, 6), new(7, -6)
            },
            new Vector2(0, 32));

        // Side chapel
        CreateRoom("Room_CursedChapel_Side", "ZoneC",
            new Vector2[] {
                new(-4, -4), new(-4, 4), new(4, 4), new(4, -4)
            },
            new Vector2(16, 16));

        // Shortcut to B
        CreateRoom("Room_CursedChapel_Shortcut", "ZoneC",
            new Vector2[] {
                new(-3, -3), new(-3, 3), new(3, 3), new(3, -3)
            },
            new Vector2(-16, 16));

        // Enemies
        SpawnPrefab("Assets/Prefabs/Enemies/Knight.prefab", new Vector3(3, 17, 0));
        SpawnPrefab("Assets/Prefabs/Enemies/Caster.prefab", new Vector3(-5, 18, 0));
        SpawnPrefab("Assets/Prefabs/Enemies/Wraith.prefab", new Vector3(2, 30, 0));
        SpawnPrefab("Assets/Prefabs/Enemies/Caster.prefab", new Vector3(17, 17, 0));

        // Crimson lights
        CreatePointLight(new Vector3(0, 16, 0), new Color(0.8f, 0.1f, 0.15f), 2.5f, 6f);
        CreatePointLight(new Vector3(0, 32, 0), new Color(0.8f, 0.1f, 0.15f), 3f, 8f);
        CreateFreeformLight(new Vector3(0, 20, 0), new Color(0.6f, 0.1f, 0.2f), 0.4f);

        // Interactables
        SpawnPrefab("Assets/Prefabs/Interactables/Shrine.prefab", new Vector3(0, 0, 0));
        SpawnPrefab("Assets/Prefabs/Interactables/ShortcutDoor.prefab", new Vector3(-16, 16, 0));

        // Scene transitions
        CreateSceneTransition("SceneTransition_ToZoneA", new Vector3(0, -8, 0), "ZoneA");
        CreateSceneTransition("SceneTransition_ToZoneB", new Vector3(-19, 16, 0), "ZoneB");
        CreateSceneTransition("SceneTransition_ToBossArena", new Vector3(0, 40, 0), "BossArena");

        SetupPlayerAndCamera(new Vector3(0, -3, 0));

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/ZoneC.unity");
    }

    private static void GenerateBossArena()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        SetupSceneInfrastructure("BossArena", new Color(0.03f, 0.02f, 0.03f), 0.2f, new Color(0.7f, 0.7f, 0.7f));

        // Circular arena (12-point polygon approximation)
        float radius = 12f;
        int points = 12;
        var vertices = new Vector2[points];
        for (int i = 0; i < points; i++)
        {
            float angle = i * (360f / points) * Mathf.Deg2Rad;
            vertices[i] = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
        }
        CreateRoom("Room_BossArena", "BossArena", vertices, Vector2.zero);

        // Boss spawn point
        var bossSpawn = new GameObject("BossSpawn");
        bossSpawn.transform.position = new Vector3(0, 4, 0);

        // Perimeter lights (for BossArenaLighting to control)
        var perimeterLights = new Light2D[6];
        for (int i = 0; i < 6; i++)
        {
            float angle = i * (360f / 6f) * Mathf.Deg2Rad;
            var pos = new Vector3(Mathf.Cos(angle) * 10f, Mathf.Sin(angle) * 10f, 0);
            var lightObj = new GameObject($"PerimeterLight_{i}");
            lightObj.transform.position = pos;
            var light = lightObj.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = new Color(1f, 0.3f, 0.2f);
            light.intensity = 0f; // Starts off, activated by BossArenaLighting
            light.pointLightOuterRadius = 8f;
            light.enabled = false;
            perimeterLights[i] = light;
        }

        // Boss arena lighting trigger
        var lightingTrigger = new GameObject("BossArenaLightingTrigger");
        lightingTrigger.transform.position = new Vector3(0, -8, 0);
        var triggerCol = lightingTrigger.AddComponent<BoxCollider2D>();
        triggerCol.isTrigger = true;
        triggerCol.size = new Vector2(6, 2);
        var bossLighting = lightingTrigger.AddComponent<BossArenaLighting>();

        // Find the global light to assign
        var globalLightObj = GameObject.Find("GlobalLight2D");
        if (globalLightObj != null)
            bossLighting.globalLight = globalLightObj.GetComponent<Light2D>();
        bossLighting.perimeterLights = perimeterLights;
        bossLighting.startIntensity = 0.2f;
        bossLighting.endIntensity = 0.4f;
        bossLighting.endColor = new Color(0.3f, 0.05f, 0.05f);
        bossLighting.transitionDuration = 2f;

        // Player + camera
        SetupPlayerAndCamera(new Vector3(0, -10, 0));

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/BossArena.unity");
    }

    // --- Room Creation ---

    private static GameObject CreateRoom(string name, string zoneName, Vector2[] vertices, Vector2 position)
    {
        var roomObj = new GameObject(name);
        roomObj.transform.position = new Vector3(position.x, position.y, 0);

        // Walls — SpriteShapeController with EdgeCollider2D
        var wallsObj = new GameObject("Walls");
        wallsObj.transform.SetParent(roomObj.transform, false);

        var spriteShapeController = wallsObj.AddComponent<SpriteShapeController>();
        var spline = spriteShapeController.spline;
        spline.Clear();

        for (int i = 0; i < vertices.Length; i++)
        {
            spline.InsertPointAt(i, new Vector3(vertices[i].x, vertices[i].y, 0));
            spline.SetTangentMode(i, ShapeTangentMode.Linear);
        }

        spriteShapeController.splineDetail = 4;
        spriteShapeController.autoUpdateCollider = true;

        // Load and assign zone profile
        var profile = AssetDatabase.LoadAssetAtPath<SpriteShapeProfile>($"Assets/Data/SpriteShapeProfile_{zoneName}.asset");
        if (profile != null)
            spriteShapeController.spriteShape = profile;

        // EdgeCollider2D is auto-generated by SpriteShapeController
        var edgeCollider = wallsObj.GetComponent<EdgeCollider2D>();
        if (edgeCollider == null)
            edgeCollider = wallsObj.AddComponent<EdgeCollider2D>();

        // Floor — large SpriteRenderer with tiling material
        var floorObj = new GameObject("Floor");
        floorObj.transform.SetParent(roomObj.transform, false);
        var floorRenderer = floorObj.AddComponent<SpriteRenderer>();
        floorRenderer.sortingLayerName = "Default";
        floorRenderer.sortingOrder = -10; // Behind everything
        floorRenderer.drawMode = SpriteDrawMode.Tiled;

        // Size the floor to cover the room bounds
        var bounds = CalculateBounds(vertices);
        floorRenderer.size = bounds;
        floorObj.transform.localScale = Vector3.one;

        var floorMat = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/Floor_{zoneName}.mat");
        if (floorMat != null)
            floorRenderer.material = floorMat;

        // Camera confiner — PolygonCollider2D matching room shape
        var confinerObj = new GameObject("CameraConfiner");
        confinerObj.transform.SetParent(roomObj.transform, false);
        var confinerCollider = confinerObj.AddComponent<PolygonCollider2D>();
        confinerCollider.isTrigger = true;
        confinerCollider.points = vertices;

        // Room trigger — detects player entry for camera switching
        var triggerObj = new GameObject("RoomTrigger");
        triggerObj.transform.SetParent(roomObj.transform, false);
        var roomTriggerCol = triggerObj.AddComponent<BoxCollider2D>();
        roomTriggerCol.isTrigger = true;
        roomTriggerCol.size = bounds;

        return roomObj;
    }

    private static Vector2 CalculateBounds(Vector2[] vertices)
    {
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (var v in vertices)
        {
            if (v.x < minX) minX = v.x;
            if (v.x > maxX) maxX = v.x;
            if (v.y < minY) minY = v.y;
            if (v.y > maxY) maxY = v.y;
        }
        return new Vector2(maxX - minX, maxY - minY);
    }

    // --- Scene Infrastructure ---

    private static GameObject SetupSceneInfrastructure(string zoneName, Color bgColor, float globalLightIntensity, Color globalLightColor)
    {
        // Camera
        var cameraObj = new GameObject("Main Camera");
        cameraObj.tag = "MainCamera";
        var cam = cameraObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = bgColor;
        cameraObj.transform.position = new Vector3(0, 0, -10);
        cameraObj.AddComponent<CinemachineBrain>();

        // Global Light 2D
        var globalLightObj = new GameObject("GlobalLight2D");
        var globalLight = globalLightObj.AddComponent<Light2D>();
        globalLight.lightType = Light2D.LightType.Global;
        globalLight.intensity = globalLightIntensity;
        globalLight.color = globalLightColor;

        return globalLightObj;
    }

    private static void SetupPlayerAndCamera(Vector3 playerPos)
    {
        // Player
        var player = SpawnPrefab("Assets/Prefabs/Player/Player.prefab", playerPos);

        // Virtual camera
        var vcamObj = new GameObject("CM vcam1");
        var vcam = vcamObj.AddComponent<CinemachineVirtualCamera>();
        vcam.m_Lens.OrthographicSize = 5f;
        vcam.m_Lens.NearClipPlane = 0.1f;
        vcam.m_Lens.FarClipPlane = 100f;
        vcam.Follow = player.transform;

        var body = vcam.AddCinemachineComponent<CinemachineFramingTransposer>();
        body.m_LookaheadTime = 0f;
        body.m_DeadZoneWidth = 0.1f;
        body.m_DeadZoneHeight = 0.1f;
        body.m_SoftZoneWidth = 0.5f;
        body.m_SoftZoneHeight = 0.5f;
        body.m_CameraDistance = 10f;

        // Confiner for room bounds
        var confiner = vcamObj.AddComponent<CinemachineConfiner>();

        vcamObj.AddComponent<CinemachineImpulseListener>();

        // Screen shake on player
        var shakeObj = new GameObject("ScreenShake");
        shakeObj.transform.SetParent(player.transform);
        shakeObj.AddComponent<CinemachineImpulseSource>();
        shakeObj.AddComponent<ScreenShake>();
    }

    // --- Lighting Helpers ---

    private static void CreateTorchLight(Vector3 position, float intensity, float flickerAmount)
    {
        var lightObj = new GameObject("TorchLight");
        lightObj.transform.position = position;

        var light = lightObj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = new Color(1f, 0.7f, 0.3f); // Warm orange
        light.intensity = intensity;
        light.pointLightOuterRadius = 6f;
        light.pointLightInnerRadius = 1f;

        var flicker = lightObj.AddComponent<LightFlicker>();
        flicker.baseIntensity = intensity;
        flicker.flickerAmount = flickerAmount;
        flicker.flickerSpeed = 5f;
    }

    private static void CreatePointLight(Vector3 position, Color color, float intensity, float radius)
    {
        var lightObj = new GameObject("PointLight");
        lightObj.transform.position = position;

        var light = lightObj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.pointLightOuterRadius = radius;
        light.pointLightInnerRadius = 1f;
    }

    private static void CreateFreeformLight(Vector3 position, Color color, float intensity)
    {
        var lightObj = new GameObject("FreeformLight");
        lightObj.transform.position = position;

        var light = lightObj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Freeform;
        light.color = color;
        light.intensity = intensity;
    }

    // --- Scene Transitions ---

    private static void CreateSceneTransition(string name, Vector3 position, string targetScene)
    {
        var transObj = new GameObject(name);
        transObj.transform.position = position;
        var col = transObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2, 2);

        var transition = transObj.AddComponent<SceneTransition>();
        transition.targetScene = targetScene;
    }

    // --- Utility ---

    private static GameObject SpawnPrefab(string prefabPath, Vector3 position)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"Prefab not found: {prefabPath}. Run Tools > Generate Prefabs first.");
            var placeholder = new GameObject(System.IO.Path.GetFileNameWithoutExtension(prefabPath));
            placeholder.transform.position = position;
            return placeholder;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = position;
        return instance;
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
git add Assets/Scripts/Editor/LevelGenerator.cs
git commit -m "feat: add LevelGenerator with SpriteShape rooms, lighting, and zone layouts"
```

---

## Task 9: Update SceneGenerator to delegate to LevelGenerator

**Files:**
- Modify: `Assets/Scripts/Editor/SceneGenerator.cs`

- [ ] **Step 1: Update SceneGenerator**

Replace the zone scene creation in `SceneGenerator.GenerateAll()` to delegate to `LevelGenerator` for zone scenes, keeping MainMenu generation in SceneGenerator since it has no level geometry.

In `Assets/Scripts/Editor/SceneGenerator.cs`, replace the body of `GenerateAll()` (lines 12-30) with:

```csharp
    [MenuItem("Tools/Generate Scenes")]
    public static void GenerateAll()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("Cannot generate scenes during Play Mode. Stop Play Mode first.");
            return;
        }

        EnsureDirectory("Assets/Scenes");

        // Zone scenes are now generated by LevelGenerator
        LevelGenerator.GenerateAll();

        // MainMenu is not a gameplay scene — stays here
        CreateMainMenu();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("All scenes generated!");
    }
```

Remove the methods `CreateZoneScene`, `CreateBossArena`, `SetupCamera`, `SetupVirtualCamera`, and `SpawnPrefab` from SceneGenerator since they are now handled by LevelGenerator. Keep `CreateMainMenu` and `EnsureDirectory`.

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/Editor/SceneGenerator.cs
git commit -m "refactor: delegate zone scene generation to LevelGenerator"
```

---

## Task 10: Update CLAUDE.md and original design spec

**Files:**
- Modify: `CLAUDE.md`
- Modify: `docs/superpowers/specs/2026-03-31-dark-fantasy-topdown-design.md`

- [ ] **Step 1: Update CLAUDE.md tech stack**

In `CLAUDE.md`, line 8, replace:

```
Unity 2022.3 LTS, URP 2D, C#, Cinemachine, Tilemap, Unity Test Framework (NUnit).
```

with:

```
Unity 2022.3 LTS, URP 2D, C#, Cinemachine, SpriteShape, NavMeshPlus, Unity Test Framework (NUnit).
```

- [ ] **Step 2: Update CLAUDE.md First-Time Setup**

In `CLAUDE.md`, after step 3 (`Generate Prefabs`), add:

```
4. Run **Tools > Generate SpriteShape Profiles** — creates per-zone wall profiles
5. Run **Tools > Generate Floor Materials** — creates per-zone floor textures and materials
6. Run **Tools > Generate Levels** — creates ZoneA, ZoneB, ZoneC, BossArena with SpriteShape rooms
7. Run **Tools > Generate Scenes** — creates MainMenu
```

Renumber the remaining steps (Build Settings, Tags and Layers, Player prefab config) accordingly.

- [ ] **Step 3: Update original design spec Tilemap section**

In `docs/superpowers/specs/2026-03-31-dark-fantasy-topdown-design.md`, replace the Tilemap subsection (lines 189-192):

```markdown
### Tilemap

- Unity Tilemap with layers: ground, walls, decoration
- Collision via Tilemap Collider 2D + Composite Collider 2D
- One tileset palette per zone
```

with:

```markdown
### Level Geometry

- SpriteShape-based polygon rooms with EdgeCollider2D for wall collision
- Floor visuals via SpriteRenderer with world-space tiling material
- One SpriteShapeProfile per zone for wall theming
- NavMeshPlus for enemy pathfinding (baked from wall colliders)
- See `docs/superpowers/specs/2026-04-01-level-geometry-design.md` for full details
```

- [ ] **Step 4: Update original design spec project structure**

In `docs/superpowers/specs/2026-03-31-dark-fantasy-topdown-design.md`, in the Project Structure section, replace:

```
├── Art/
│   ├── Sprites/        # AI-generated character/enemy/item sprites
│   ├── Tilesets/       # AI-generated tileset per zone
│   └── VFX/            # Slash effects, dodge trail, combo particles
```

with:

```
├── Art/
│   ├── Sprites/        # AI-generated character/enemy/item sprites
│   ├── LevelArt/       # Per-zone: edge sprites, corner sprites, floor textures, normal maps
│   └── VFX/            # Slash effects, dodge trail, combo particles
```

And remove:

```
└── Tilemaps/           # Tilemap palettes per zone
```

- [ ] **Step 5: Update original design spec art resolution**

In `docs/superpowers/specs/2026-03-31-dark-fantasy-topdown-design.md`, in the Visual Style subsection, replace:

```
- Target resolution: 16x16 or 32x32 pixel grid for tiles, larger for characters/bosses
```

with:

```
- Wall edge sprites: 512x512, floor textures: 1024x1024, character/boss sprites: larger as needed
```

- [ ] **Step 6: Commit**

```bash
git add CLAUDE.md docs/superpowers/specs/2026-03-31-dark-fantasy-topdown-design.md
git commit -m "docs: update tech stack and design spec to reflect SpriteShape approach"
```

---

## Task 11: Regenerate prefabs and data assets

This task runs the updated generators to incorporate NavMeshAgent on enemy prefabs and navMeshRadius on enemy data.

**Files:**
- No new files — runs existing generators

- [ ] **Step 1: Regenerate data assets**

Run `Tools > Generate Data Assets` in Unity to regenerate enemy data with `navMeshRadius` field values.

- [ ] **Step 2: Regenerate prefabs**

Run `Tools > Generate Prefabs` in Unity to regenerate enemy prefabs with `NavMeshAgent` component.

- [ ] **Step 3: Generate floor materials**

Run `Tools > Generate Floor Materials` in Unity.

- [ ] **Step 4: Generate SpriteShape profiles**

Run `Tools > Generate SpriteShape Profiles` in Unity.

- [ ] **Step 5: Generate levels**

Run `Tools > Generate Levels` in Unity.

- [ ] **Step 6: Generate scenes (MainMenu)**

Run `Tools > Generate Scenes` in Unity.

- [ ] **Step 7: Commit all generated assets**

```bash
git add Assets/Data/ Assets/Prefabs/ Assets/Art/LevelArt/ Assets/Materials/ Assets/Scenes/
git commit -m "asset: regenerate all assets with SpriteShape levels and NavMeshAgent enemies"
```
