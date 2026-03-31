---
paths:
  - "Assets/Scripts/Combat/**"
  - "Assets/Scripts/Enemies/EnemyData.cs"
---

# ScriptableObject Rules

- Always add `[CreateAssetMenu(fileName = "NewX", menuName = "Game/X")]` attribute
- Use public fields (not properties) for Inspector-editable data
- Group fields with `[Header("Section")]` when there are 4+ fields
- One SO class per file, filename matches class name
- When adding new SO types, also add creation logic to `DataAssetGenerator.cs`
- Runtime cloning: use `Instantiate(so)` to clone SOs before mutating at runtime (see `PlayerInventory.EquipBook`)
