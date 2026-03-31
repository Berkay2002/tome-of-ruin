---
paths:
  - "Assets/Scripts/Enemies/**"
---

# Enemy Composition Rules

- NEVER create enemy subclasses. All enemies use the same `EnemyStateMachine` component.
- Type-specific behavior goes in `EnemyBehavior` component with `EnemyBehaviorType` enum (Melee, DashRetreat, Ranged).
- Enemy stats are data-driven via `EnemyData` ScriptableObject — add new fields there, not in MonoBehaviours.
- Armor is handled by `EnemyHealth` using `EnemyData.armorReduction` and `armorDisabledDuringStagger`.
- Attack wind-up (0.4s telegraph) is in `EnemyStateMachine.UpdateAttack()` — preserve this for player readability.
- Wire new enemy types in `DataAssetGenerator.CreateEnemyData()` and `PrefabGenerator.CreateEnemyPrefab()`.
