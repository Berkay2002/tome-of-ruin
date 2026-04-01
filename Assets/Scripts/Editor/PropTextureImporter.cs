#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Configures imported prop and detail textures as pixel-art sprites.
/// Run after copying prop PNGs into Assets/Art/LevelArt/Props/.
/// </summary>
public static class PropTextureImporter
{
    [MenuItem("Tools/Import Prop Textures")]
    public static void ImportAll()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("PropTextureImporter: Cannot run in Play Mode.");
            return;
        }

        int count = 0;

        // Props — 32ppu so a 32x32 sprite = 1 world unit (visible at room scale)
        count += ImportDirectory("Assets/Art/LevelArt/Props", 32);

        // Details — smaller ppu for floor scatter
        count += ImportDirectory("Assets/Art/LevelArt/Details", 32);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"PropTextureImporter: Configured {count} texture(s) as pixel-art sprites.");
    }

    private static int ImportDirectory(string basePath, int pixelsPerUnit)
    {
        if (!Directory.Exists(basePath)) return 0;

        int count = 0;
        var files = Directory.GetFiles(basePath, "*.png", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            string assetPath = file.Replace("\\", "/");
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) continue;

            bool changed = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }
            if (importer.spritePixelsPerUnit != pixelsPerUnit)
            {
                importer.spritePixelsPerUnit = pixelsPerUnit;
                changed = true;
            }
            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                changed = true;
            }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
                count++;
            }
        }

        return count;
    }
}
#endif
