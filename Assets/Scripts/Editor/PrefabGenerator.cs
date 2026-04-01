#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class PrefabGenerator
{
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

    private static void AssignPlaceholderSprite(GameObject go, string spriteName)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/Placeholders/{spriteName}.png");
        if (sprite != null)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = sprite;
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

    private static void CreatePlayerPrefab()
    {
        var go = new GameObject("Player");
        go.tag = "Player";
        go.AddComponent<SpriteRenderer>();
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        var playerCol = go.AddComponent<BoxCollider2D>();
        playerCol.size = new Vector2(1f, 1f);

        var controller = go.AddComponent<PlayerController>();
        controller.moveSpeed = 5f;
        controller.dodgeSpeed = 10f;
        controller.dodgeDuration = 0.3f;

        var health = go.AddComponent<PlayerHealth>();
        health.maxHealth = 100f;

        var inventory = go.AddComponent<PlayerInventory>();
        var startingBook = AssetDatabase.LoadAssetAtPath<ComboBookData>("Assets/Data/ComboBooks/TatteredManual.asset");
        if (startingBook != null)
            inventory.startingBook = startingBook;

        var combat = go.AddComponent<PlayerCombat>();
        combat.attackRange = 1.2f;
        combat.enemyLayer = LayerMask.GetMask("Enemy");

        var executor = go.AddComponent<ComboExecutor>();
        executor.chainWindowDuration = 0.5f;
        var harmonyTable = AssetDatabase.LoadAssetAtPath<HarmonyTable>("Assets/Data/HarmonyTable.asset");
        if (harmonyTable != null)
            executor.harmonyTable = harmonyTable;

        var potions = go.AddComponent<PlayerPotions>();
        potions.maxPotions = 5;
        potions.healAmount = 40f;

        go.AddComponent<PlayerInput>();

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

        // Prefer real sprite, fall back to placeholder
        var playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Player/player_idle_down.png");
        if (playerSprite != null)
            go.GetComponent<SpriteRenderer>().sprite = playerSprite;
        else
            AssignPlaceholderSprite(go, "Player");

        PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Player/Player.prefab");
        Object.DestroyImmediate(go);
    }

    private static void CreateEnemyPrefabs()
    {
        CreateEnemyPrefab("Hollow", null);
        CreateEnemyPrefab("Wraith", EnemyBehaviorType.DashRetreat);
        CreateEnemyPrefab("Knight", null);
        CreateEnemyPrefab("Caster", EnemyBehaviorType.Ranged);
    }

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

        var agent = go.AddComponent<UnityEngine.AI.NavMeshAgent>();
        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.radius = 0.7f;
        agent.height = 0.1f;
        agent.baseOffset = 0f;

        AssignPlaceholderSprite(go, "Caster"); // Reuse Caster placeholder until Boss sprite exists
        PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Enemies/Boss.prefab");
        Object.DestroyImmediate(go);
    }

    private static void CreateEnemyPrefab(string name, EnemyBehaviorType? behaviorType)
    {
        var go = new GameObject(name);
        // Tag must exist in project
        try { go.tag = "Enemy"; }
        catch { Debug.LogWarning($"Tag 'Enemy' not found. Add it in Edit > Project Settings > Tags and Layers."); }

        // Layer must exist for combat hit detection
        int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
        if (enemyLayerIndex >= 0)
            go.layer = enemyLayerIndex;
        else
            Debug.LogWarning("Layer 'Enemy' not found. Add it in Edit > Project Settings > Tags and Layers.");

        go.AddComponent<SpriteRenderer>();
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);

        var health = go.AddComponent<EnemyHealth>();
        var sm = go.AddComponent<EnemyStateMachine>();

        // Assign EnemyData SO
        var enemyData = AssetDatabase.LoadAssetAtPath<EnemyData>($"Assets/Data/Enemies/{name}.asset");
        if (enemyData != null)
        {
            sm.data = enemyData;
            health.data = enemyData;
        }
        else
            Debug.LogWarning($"EnemyData not found for {name}. Run Tools > Generate Data Assets first.");

        if (behaviorType.HasValue)
        {
            var behavior = go.AddComponent<EnemyBehavior>();
            behavior.behaviorType = behaviorType.Value;
            sm.behavior = behavior;
        }

        var agent = go.AddComponent<UnityEngine.AI.NavMeshAgent>();
        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.radius = 0.4f;
        agent.height = 0.1f;
        agent.baseOffset = 0f;

        AssignPlaceholderSprite(go, name);

        // Visual feedback
        var vfx = go.AddComponent<EnemyVisualFeedback>();
        var whiteFlashMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/WhiteFlash.mat");
        if (whiteFlashMat != null)
            vfx.whiteFlashMaterial = whiteFlashMat;
        else
            Debug.LogWarning("WhiteFlash material not found. Run Tools > Generate Materials first.");

        PrefabUtility.SaveAsPrefabAsset(go, $"Assets/Prefabs/Enemies/{name}.prefab");
        Object.DestroyImmediate(go);
    }

    private static void CreateProjectilePrefab()
    {
        var go = new GameObject("Projectile");
        go.AddComponent<SpriteRenderer>();
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        go.AddComponent<Projectile>();

        AssignPlaceholderSprite(go, "Projectile");
        PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Enemies/Projectile.prefab");
        Object.DestroyImmediate(go);
    }

    private static void CreateInteractablePrefabs()
    {
        CreateInteractable<KeyGate>("KeyGate");
        CreateInteractable<ShortcutDoor>("ShortcutDoor");
        CreateInteractable<Shrine>("Shrine");
        CreateInteractable<ItemPickup>("ItemPickup");
    }

    private static void CreateInteractable<T>(string name) where T : MonoBehaviour
    {
        var go = new GameObject(name);
        go.AddComponent<SpriteRenderer>();
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        go.AddComponent<T>();

        AssignPlaceholderSprite(go, name);
        PrefabUtility.SaveAsPrefabAsset(go, $"Assets/Prefabs/Interactables/{name}.prefab");
        Object.DestroyImmediate(go);
    }
}
#endif
