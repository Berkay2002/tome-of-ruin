#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class PlayerSpriteSetup
{
    private const string SpritePath = "Assets/Sprites/Player";
    private const int PixelsPerUnit = 32;

    [MenuItem("Tools/Setup Player Sprites")]
    public static void SetupPlayerSprites()
    {
        ImportSprites();
        WireSpritesToAnimator();
        Debug.Log("[PlayerSpriteSetup] Done. Sprites imported and wired to PlayerSpriteAnimator.");
    }

    [MenuItem("Tools/Setup Player Sprites", true)]
    private static bool SetupPlayerSpritesValidation()
    {
        return !Application.isPlaying;
    }

    private static void ImportSprites()
    {
        string[] pngs = Directory.GetFiles(SpritePath, "*.png");
        foreach (string path in pngs)
        {
            string assetPath = path.Replace("\\", "/");
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        Debug.Log($"[PlayerSpriteSetup] Imported {pngs.Length} sprites from {SpritePath}");
    }

    private static void WireSpritesToAnimator()
    {
        // Find the Player prefab
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab Player", new[] { "Assets/Prefabs" });
        GameObject playerPrefab = null;
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == "Player")
            {
                playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                break;
            }
        }

        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerSpriteSetup] Player prefab not found in Assets/Prefabs/");
            return;
        }

        // Open prefab for editing
        string prefabPath = AssetDatabase.GetAssetPath(playerPrefab);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

        // Add PlayerSpriteAnimator if missing
        PlayerSpriteAnimator animator = prefabRoot.GetComponent<PlayerSpriteAnimator>();
        if (animator == null)
            animator = prefabRoot.AddComponent<PlayerSpriteAnimator>();

        // Wire idle sprites
        animator.idleDown = LoadSprite("player_idle_down");
        animator.idleUp = LoadSprite("player_idle_up");
        animator.idleLeft = LoadSprite("player_idle_left");
        animator.idleDownLeft = LoadSprite("player_idle_down_left");
        animator.idleUpLeft = LoadSprite("player_idle_up_left");

        // Wire moving sprites
        animator.movingDown = LoadSpriteFrames("player_moving_down", 4);
        animator.movingUp = LoadSpriteFrames("player_moving_up", 4);
        animator.movingLeft = LoadSpriteFrames("player_moving_left", 4);
        animator.movingDownLeft = LoadSpriteFrames("player_moving_down_left", 4);
        animator.movingUpLeft = LoadSpriteFrames("player_moving_up_left", 4);

        // Wire attacking sprites
        animator.attackingDown = LoadSpriteFrames("player_attacking_down", 3);
        animator.attackingUp = LoadSpriteFrames("player_attacking_up", 3);
        animator.attackingLeft = LoadSpriteFrames("player_attacking_left", 3);
        animator.attackingDownLeft = LoadSpriteFrames("player_attacking_down_left", 3);
        animator.attackingUpLeft = LoadSpriteFrames("player_attacking_up_left", 3);

        // Wire dodging sprites
        animator.dodgingDown = LoadSpriteFrames("player_dodging_down", 2);
        animator.dodgingUp = LoadSpriteFrames("player_dodging_up", 2);
        animator.dodgingLeft = LoadSpriteFrames("player_dodging_left", 2);
        animator.dodgingDownLeft = LoadSpriteFrames("player_dodging_down_left", 2);
        animator.dodgingUpLeft = LoadSpriteFrames("player_dodging_up_left", 2);

        // Wire hit sprites
        animator.hitDown = LoadSprite("player_hit_down_f1");
        animator.hitUp = LoadSprite("player_hit_up_f1");
        animator.hitLeft = LoadSprite("player_hit_left_f1");
        animator.hitDownLeft = LoadSprite("player_hit_down_left_f1");
        animator.hitUpLeft = LoadSprite("player_hit_up_left_f1");

        // Wire dead sprite
        animator.dead = LoadSprite("player_dead_down_f1");

        // Also set the SpriteRenderer's default sprite to idle down
        SpriteRenderer sr = prefabRoot.GetComponent<SpriteRenderer>();
        if (sr != null && animator.idleDown != null)
            sr.sprite = animator.idleDown;

        // Save prefab
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        Debug.Log("[PlayerSpriteSetup] Wired sprites to PlayerSpriteAnimator on Player prefab.");
    }

    private static Sprite LoadSprite(string name)
    {
        string path = $"{SpritePath}/{name}.png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning($"[PlayerSpriteSetup] Sprite not found: {path}");
        return sprite;
    }

    private static Sprite[] LoadSpriteFrames(string baseName, int frameCount)
    {
        Sprite[] frames = new Sprite[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            frames[i] = LoadSprite($"{baseName}_f{i + 1}");
        }
        return frames;
    }
}
#endif
