#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class FloorMaterialGenerator
{
    private static readonly (string name, Color color)[] Zones = new[]
    {
        ("ZoneA",     new Color(0.25f, 0.25f, 0.22f)),
        ("ZoneB",     new Color(0.12f, 0.12f, 0.10f)),
        ("ZoneC",     new Color(0.20f, 0.15f, 0.18f)),
        ("BossArena", new Color(0.08f, 0.08f, 0.10f)),
    };

    [MenuItem("Tools/Generate Floor Materials")]
    public static void GenerateAll()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("FloorMaterialGenerator: Cannot run in Play Mode.");
            return;
        }

        EnsureDirectory("Assets/Art");
        EnsureDirectory("Assets/Art/LevelArt");
        EnsureDirectory("Assets/Materials");

        foreach (var zone in Zones)
        {
            string zoneDir = $"Assets/Art/LevelArt/{zone.name}";
            EnsureDirectory(zoneDir);

            CreateFloorTexture(zone.name, zoneDir, zone.color);
            CreateFloorMaterial(zone.name, zoneDir);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Floor materials generated!");
    }

    private static void CreateFloorTexture(string zoneName, string zoneDir, Color color)
    {
        string texPath = $"{zoneDir}/Floor_{zoneName}.png";
        if (File.Exists(texPath)) return;

        var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        var pixels = new Color[64 * 64];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();

        File.WriteAllBytes(texPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(texPath);

        var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }
    }

    private static void CreateFloorMaterial(string zoneName, string zoneDir)
    {
        string matPath = $"Assets/Materials/Floor_{zoneName}.mat";
        if (File.Exists(matPath)) return;

        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogError($"FloorMaterialGenerator: Could not find a suitable shader for {zoneName}.");
            return;
        }

        var mat = new Material(shader);
        mat.mainTextureScale = new Vector2(4f, 4f);

        string texPath = $"{zoneDir}/Floor_{zoneName}.png";
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex != null)
            mat.mainTexture = tex;

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
