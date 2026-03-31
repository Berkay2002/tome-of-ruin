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
        CreateProjectilePrefab();
        CreateInteractablePrefabs();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("All prefabs generated!");
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
        go.AddComponent<BoxCollider2D>();

        var controller = go.AddComponent<PlayerController>();
        controller.moveSpeed = 5f;
        controller.dodgeSpeed = 10f;
        controller.dodgeDuration = 0.3f;

        var health = go.AddComponent<PlayerHealth>();
        health.maxHealth = 100f;

        go.AddComponent<PlayerInventory>();

        var combat = go.AddComponent<PlayerCombat>();
        combat.attackRange = 1.2f;

        var executor = go.AddComponent<ComboExecutor>();
        executor.chainWindowDuration = 0.5f;

        var potions = go.AddComponent<PlayerPotions>();
        potions.maxPotions = 5;
        potions.healAmount = 40f;

        go.AddComponent<PlayerInput>();

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

    private static void CreateEnemyPrefab(string name, EnemyBehaviorType? behaviorType)
    {
        var go = new GameObject(name);
        // Tag must exist in project — user may need to add "Enemy" tag manually
        try { go.tag = "Enemy"; }
        catch { Debug.LogWarning($"Tag 'Enemy' not found. Add it in Edit > Project Settings > Tags and Layers."); }

        go.AddComponent<SpriteRenderer>();
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        go.AddComponent<BoxCollider2D>();

        go.AddComponent<EnemyHealth>();
        var sm = go.AddComponent<EnemyStateMachine>();

        if (behaviorType.HasValue)
        {
            var behavior = go.AddComponent<EnemyBehavior>();
            behavior.behaviorType = behaviorType.Value;
            sm.behavior = behavior;
        }

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

        PrefabUtility.SaveAsPrefabAsset(go, $"Assets/Prefabs/Interactables/{name}.prefab");
        Object.DestroyImmediate(go);
    }
}
#endif
