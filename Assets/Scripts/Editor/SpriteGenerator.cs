#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class SpriteGenerator
{
    [MenuItem("Tools/Generate Placeholder Sprites")]
    public static void GenerateAll()
    {
        EnsureDirectory("Assets/Sprites");
        EnsureDirectory("Assets/Sprites/Placeholders");

        // Player - blue
        CreateSquareSprite("Player", 32, new Color(0.2f, 0.4f, 0.9f));
        // Enemies - red/orange variants
        CreateSquareSprite("Hollow", 32, new Color(0.7f, 0.2f, 0.2f));
        CreateSquareSprite("Wraith", 32, new Color(0.5f, 0.2f, 0.6f));
        CreateSquareSprite("Knight", 32, new Color(0.8f, 0.4f, 0.1f));
        CreateSquareSprite("Caster", 32, new Color(0.9f, 0.1f, 0.5f));
        // Projectile - yellow circle
        CreateCircleSprite("Projectile", 16, new Color(1f, 0.9f, 0.2f));
        // Interactables
        CreateSquareSprite("KeyGate", 32, new Color(0.6f, 0.6f, 0.1f));
        CreateSquareSprite("ShortcutDoor", 32, new Color(0.4f, 0.3f, 0.2f));
        CreateSquareSprite("Shrine", 32, new Color(0.3f, 0.8f, 0.4f));
        CreateSquareSprite("ItemPickup", 16, new Color(1f, 1f, 0.5f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        AssignSpritesToPrefabs();

        Debug.Log("Placeholder sprites generated and assigned to prefabs!");
    }

    private static void CreateSquareSprite(string name, int size, Color color)
    {
        var tex = new Texture2D(size, size);
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Border: darker shade
                bool isBorder = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                pixels[y * size + x] = isBorder ? color * 0.5f : color;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;

        SaveSprite(name, tex, size);
    }

    private static void CreateCircleSprite(string name, int size, Color color)
    {
        var tex = new Texture2D(size, size);
        var pixels = new Color[size * size];
        float center = size / 2f;
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                if (dist < radius - 1)
                    pixels[y * size + x] = color;
                else if (dist < radius)
                    pixels[y * size + x] = color * 0.5f;
                else
                    pixels[y * size + x] = Color.clear;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;

        SaveSprite(name, tex, size);
    }

    private static void SaveSprite(string name, Texture2D tex, int size)
    {
        string path = $"Assets/Sprites/Placeholders/{name}.png";
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = size;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    private static void AssignSpritesToPrefabs()
    {
        AssignSprite("Assets/Prefabs/Player/Player.prefab", "Player");
        AssignSprite("Assets/Prefabs/Enemies/Hollow.prefab", "Hollow");
        AssignSprite("Assets/Prefabs/Enemies/Wraith.prefab", "Wraith");
        AssignSprite("Assets/Prefabs/Enemies/Knight.prefab", "Knight");
        AssignSprite("Assets/Prefabs/Enemies/Caster.prefab", "Caster");
        AssignSprite("Assets/Prefabs/Enemies/Projectile.prefab", "Projectile");
        AssignSprite("Assets/Prefabs/Interactables/KeyGate.prefab", "KeyGate");
        AssignSprite("Assets/Prefabs/Interactables/ShortcutDoor.prefab", "ShortcutDoor");
        AssignSprite("Assets/Prefabs/Interactables/Shrine.prefab", "Shrine");
        AssignSprite("Assets/Prefabs/Interactables/ItemPickup.prefab", "ItemPickup");
    }

    private static void AssignSprite(string prefabPath, string spriteName)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return;

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/Placeholders/{spriteName}.png");
        if (sprite == null) return;

        var sr = prefab.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        sr.sprite = sprite;
        EditorUtility.SetDirty(prefab);
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
