#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.U2D;
using System.IO;

public static class SpriteShapeProfileGenerator
{
    private static readonly (string name, Color color)[] Zones = new[]
    {
        ("ZoneA",     new Color(0.4f,  0.4f,  0.35f)),
        ("ZoneB",     new Color(0.7f,  0.7f,  0.6f)),
        ("ZoneC",     new Color(0.35f, 0.25f, 0.3f)),
        ("BossArena", new Color(0.15f, 0.15f, 0.18f)),
    };

    [MenuItem("Tools/Generate SpriteShape Profiles")]
    public static void GenerateAll()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("SpriteShapeProfileGenerator: Cannot run in Play Mode.");
            return;
        }

        EnsureDirectory("Assets/Art");
        EnsureDirectory("Assets/Art/LevelArt");
        EnsureDirectory("Assets/Data");

        foreach (var zone in Zones)
        {
            string zoneDir = $"Assets/Art/LevelArt/{zone.name}";
            EnsureDirectory(zoneDir);

            Sprite edgeSprite = CreateWallEdgeSprite(zone.name, zoneDir, zone.color);
            CreateSpriteShapeProfile(zone.name, edgeSprite);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("SpriteShapeProfileGenerator: Done — per-zone SpriteShapeProfiles created.");
    }

    private static Sprite CreateWallEdgeSprite(string zoneName, string zoneDir, Color color)
    {
        string texPath = $"{zoneDir}/WallEdge_{zoneName}.png";

        if (!File.Exists(texPath))
        {
            var tex = new Texture2D(32, 8, TextureFormat.RGBA32, false);
            var pixels = new Color[32 * 8];
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
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
    }

    private static void CreateSpriteShapeProfile(string zoneName, Sprite edgeSprite)
    {
        string profilePath = $"Assets/Data/SpriteShapeProfile_{zoneName}.asset";
        if (File.Exists(profilePath))
            return;

        var profile = ScriptableObject.CreateInstance<SpriteShapeProfile>();
        profile.fillPixelsPerUnit = 100;
        profile.fillOffset = 0f;

        // Attempt to assign the edge sprite to existing angle ranges.
        // The SpriteShapeProfile ships with a default set of angle ranges; we
        // iterate them and assign our wall-edge sprite to each one.  If the API
        // changes or the list is empty we fall through gracefully and log a note.
        var ranges = profile.angleRanges;
        if (ranges != null && ranges.Count > 0 && edgeSprite != null)
        {
            for (int i = 0; i < ranges.Count; i++)
            {
                var range = ranges[i];
                // Sprites list on an AngleRange is a plain List<Sprite>
                range.sprites.Clear();
                range.sprites.Add(edgeSprite);
                ranges[i] = range;
            }
            Debug.Log($"SpriteShapeProfileGenerator: Assigned edge sprite to {ranges.Count} angle range(s) on {profilePath}.");
        }
        else
        {
            Debug.Log($"SpriteShapeProfileGenerator: No angle ranges found on new profile for {zoneName}. " +
                      "Assign the WallEdge sprite to angle ranges manually in the Inspector.");
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
