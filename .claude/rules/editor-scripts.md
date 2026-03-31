---
paths:
  - "Assets/Scripts/Editor/**"
---

# Editor Script Rules

- Always wrap entire file in `#if UNITY_EDITOR` / `#endif`
- Use `EditorUtility.SetDirty(asset)` after modifying any ScriptableObject fields
- Call `AssetDatabase.SaveAssets()` and `AssetDatabase.Refresh()` after batch operations
- Use `AssetDatabase.CreateFolder()` with `AssetDatabase.IsValidFolder()` guard before creating directories
- Use `[MenuItem("Tools/...")]` for menu entries — keep all tools under the Tools menu
- Editor assembly is defined in `Assets/Scripts/Editor/Editor.asmdef` (Editor-only platform)
