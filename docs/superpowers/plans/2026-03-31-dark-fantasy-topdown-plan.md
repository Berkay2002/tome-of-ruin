# Dark Fantasy Top-Down Action Game — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a small dark fantasy top-down action game with interconnected zone exploration and a tag-based combo crafting combat system in Unity 2D.

**Architecture:** MonoBehaviour + ScriptableObject composition. Data-driven design — all game content (attacks, combo books, enemies, harmony rules) defined as ScriptableObjects. Simple enum-based state machines for player and enemy logic. Scene-per-zone with a persistent GameManager singleton.

**Tech Stack:** Unity 2022.3 LTS, URP 2D, C#, Cinemachine, Unity Tilemap, Unity Test Framework (NUnit)

**Spec:** `docs/superpowers/specs/2026-03-31-dark-fantasy-topdown-design.md`

---

## File Structure

```
Assets/
├── Scenes/
│   ├── MainMenu.unity
│   ├── ZoneA.unity
│   ├── ZoneB.unity
│   ├── ZoneC.unity
│   └── BossArena.unity
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs            # Singleton, DontDestroyOnLoad, tracks global state
│   │   ├── GameState.cs              # Runtime class: keys, attacks, shrines, cleared zones
│   │   ├── SceneTransition.cs        # Trigger-based scene loading with fade
│   │   └── FadeScreen.cs             # UI Image overlay for fade in/out
│   ├── Player/
│   │   ├── PlayerController.cs       # Movement, input, state machine
│   │   ├── PlayerHealth.cs           # Health tracking, damage, death, shrine respawn
│   │   ├── PlayerInventory.cs        # Collected attacks, combo books, keys
│   │   ├── PlayerPotions.cs          # Health potion inventory: collect, use, count
│   │   └── PlayerCombat.cs           # Combo execution, dodge, i-frames, movement patterns
│   ├── Combat/
│   │   ├── AttackData.cs             # ScriptableObject: attack definition
│   │   ├── AttackSpeed.cs            # Enum: fast, medium, slow
│   │   ├── MovementPattern.cs        # Enum: hold, lunge, pull back, arc
│   │   ├── ComboBookData.cs          # ScriptableObject: combo book definition
│   │   ├── HarmonyTable.cs           # ScriptableObject: tag-pair lookup
│   │   ├── HarmonyCalculator.cs      # Pure C# static class: calculates harmony score
│   │   ├── ComboExecutor.cs          # MonoBehaviour: runtime combo chain logic
│   │   ├── AttackResult.cs           # Struct: result of executing an attack
│   │   ├── AttackTag.cs              # Enum: sweep, thrust, overhead, rising, spinning, slam
│   │   ├── HarmonyLevel.cs           # Enum: neutral, harmonious, dissonant
│   │   └── ComboBookRarity.cs        # Enum: common, rare, legendary
│   ├── Enemies/
│   │   ├── EnemyData.cs              # ScriptableObject: enemy stat block
│   │   ├── EnemyStateMachine.cs      # MonoBehaviour: enum state machine, common behavior
│   │   ├── EnemyHealth.cs            # Health, stagger, death, armor support
│   │   ├── EnemyBehavior.cs          # Optional component: type-specific behavior (composition)
│   │   └── Projectile.cs             # Projectile for ranged enemies
│   ├── World/
│   │   ├── KeyGate.cs                # Door that requires a specific key to open
│   │   ├── ShortcutDoor.cs           # One-way unlock, becomes permanent passage
│   │   ├── Shrine.cs                 # Checkpoint: registers with GameManager on interact
│   │   ├── ItemPickup.cs             # Generic pickup: attacks, combo books, health potions, keys
│   │   └── ZoneGate.cs               # Boss arena gate: checks both zones cleared
│   └── UI/
│       ├── HealthBarUI.cs            # Player health bar display
│       ├── ComboBookUI.cs            # Full-screen combo book editor overlay
│       ├── ComboSlotUI.cs            # Single drag-drop slot in combo book
│       ├── AttackCardUI.cs           # Draggable attack card with tag display
│       ├── HarmonyPreviewUI.cs       # Shows harmony score before committing combo
│       └── ComboBookHUD.cs           # Equipped combo book indicator (bottom-left HUD)
├── Data/
│   ├── Attacks/                      # 12-15 AttackData .asset files
│   ├── ComboBooks/                   # ~6 ComboBookData .asset files
│   ├── Enemies/                      # 4 EnemyData .asset files
│   └── HarmonyTable.asset            # Single harmony lookup
├── Prefabs/
│   ├── Player/
│   │   └── Player.prefab
│   ├── Enemies/
│   │   ├── Hollow.prefab
│   │   ├── Wraith.prefab
│   │   ├── Knight.prefab
│   │   └── Caster.prefab
│   ├── Interactables/
│   │   ├── KeyGate.prefab
│   │   ├── ShortcutDoor.prefab
│   │   ├── Shrine.prefab
│   │   └── ItemPickup.prefab
│   └── UI/
│       ├── HealthBar.prefab
│       └── ComboBookPanel.prefab
├── Art/
│   ├── Sprites/
│   ├── Tilesets/
│   └── VFX/
├── Audio/
└── Tilemaps/
```

```
Assets/Tests/
├── EditMode/
│   ├── Combat/
│   │   ├── HarmonyCalculatorTests.cs
│   │   ├── AttackDataTests.cs
│   │   └── ComboBookDataTests.cs
│   ├── Core/
│   │   └── GameStateTests.cs
│   └── EditMode.asmdef
└── PlayMode/
    ├── Player/
    │   ├── PlayerControllerTests.cs
    │   └── PlayerCombatTests.cs
    ├── Combat/
    │   └── ComboExecutorTests.cs
    └── PlayMode.asmdef
```

---

## Task 1: Unity Project Scaffolding

**Files:**
- Create: Unity project at `Assets/` root
- Create: All directories from file structure above
- Create: `.gitignore` for Unity
- Create: Assembly definitions for tests

- [ ] **Step 1: Create Unity project**

Create a new Unity 2D URP project. If using Unity Hub CLI:

```bash
# From /repo/eorhber/other/game/
unity -createProject . -template com.unity.template.universal-2d
```

Or create via Unity Hub GUI: New Project → 2D (URP) → set path to `/repo/eorhber/other/game/`.

- [ ] **Step 2: Create `.gitignore`**

Create `.gitignore` at project root:

```gitignore
# Unity
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/

# IDE
.vs/
.vscode/
*.csproj
*.sln
*.suo
*.tmp
*.user
*.userprefs
*.pidb
*.booproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db

# OS
.DS_Store
Thumbs.db

# Artifacts
*.apk
*.aab
*.unitypackage
*.app
```

- [ ] **Step 3: Create directory structure**

```bash
cd Assets
mkdir -p Scenes
mkdir -p Scripts/{Core,Player,Combat,Enemies,World,UI}
mkdir -p Data/{Attacks,ComboBooks,Enemies}
mkdir -p Prefabs/{Player,Enemies,Interactables,UI}
mkdir -p Art/{Sprites,Tilesets,VFX}
mkdir -p Audio
mkdir -p Tilemaps
mkdir -p Tests/EditMode/Combat
mkdir -p Tests/EditMode/Core
mkdir -p Tests/PlayMode/Player
mkdir -p Tests/PlayMode/Combat
```

- [ ] **Step 4: Create assembly definition for game scripts**

Create `Assets/Scripts/GameScripts.asmdef`:

```json
{
    "name": "GameScripts",
    "rootNamespace": "",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 5: Create assembly definitions for tests**

Create `Assets/Tests/EditMode/EditMode.asmdef`:

```json
{
    "name": "EditModeTests",
    "rootNamespace": "",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "GameScripts"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

Create `Assets/Tests/PlayMode/PlayMode.asmdef`:

```json
{
    "name": "PlayModeTests",
    "rootNamespace": "",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "GameScripts"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 6: Install required packages**

Open Unity Package Manager and install:
- **Cinemachine** (com.unity.cinemachine)
- **Universal RP** (should be pre-installed with URP template)
- **Input System** (com.unity.inputsystem) — new input system for clean input handling
- **TextMeshPro** (com.unity.textmeshpro) — for UI text

Or via `Packages/manifest.json`, ensure these are present:

```json
{
  "dependencies": {
    "com.unity.cinemachine": "2.9.7",
    "com.unity.inputsystem": "1.7.0",
    "com.unity.render-pipelines.universal": "14.0.11",
    "com.unity.textmeshpro": "3.0.6",
    "com.unity.test-framework": "1.3.9"
  }
}
```

- [ ] **Step 7: Commit**

```bash
git init
git add .gitignore Assets/Scripts/GameScripts.asmdef Assets/Tests/ Packages/manifest.json ProjectSettings/
git commit -m "chore: scaffold Unity 2D URP project with directory structure and test assemblies"
```

---

## Task 2: Attack Tag Enum & AttackData ScriptableObject

**Files:**
- Create: `Assets/Scripts/Combat/AttackTag.cs`
- Create: `Assets/Scripts/Combat/AttackData.cs`
- Test: `Assets/Tests/EditMode/Combat/AttackDataTests.cs`

- [ ] **Step 1: Write the failing test**

Create `Assets/Tests/EditMode/Combat/AttackDataTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class AttackDataTests
{
    [Test]
    public void AttackData_CanBeCreated_WithValidFields()
    {
        var attack = ScriptableObject.CreateInstance<AttackData>();
        attack.attackName = "Cleaving Arc";
        attack.primaryTag = AttackTag.Sweep;
        attack.secondaryTag = AttackTag.None;
        attack.baseDamage = 25f;
        attack.speed = AttackSpeed.Medium;
        attack.movementPattern = MovementPattern.LungeForward;

        Assert.AreEqual("Cleaving Arc", attack.attackName);
        Assert.AreEqual(AttackTag.Sweep, attack.primaryTag);
        Assert.AreEqual(AttackTag.None, attack.secondaryTag);
        Assert.AreEqual(25f, attack.baseDamage);
        Assert.AreEqual(AttackSpeed.Medium, attack.speed);
        Assert.AreEqual(MovementPattern.LungeForward, attack.movementPattern);
    }

    [Test]
    public void AttackData_HasTags_ReturnsTrueForMatchingTag()
    {
        var attack = ScriptableObject.CreateInstance<AttackData>();
        attack.primaryTag = AttackTag.Thrust;
        attack.secondaryTag = AttackTag.Rising;

        Assert.IsTrue(attack.HasTag(AttackTag.Thrust));
        Assert.IsTrue(attack.HasTag(AttackTag.Rising));
        Assert.IsFalse(attack.HasTag(AttackTag.Slam));
        Assert.IsFalse(attack.HasTag(AttackTag.None));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: Unity → Window → General → Test Runner → EditMode → Run All
Expected: FAIL — `AttackTag`, `AttackData`, `AttackSpeed`, `MovementPattern` not defined

- [ ] **Step 3: Implement AttackTag enum**

Create `Assets/Scripts/Combat/AttackTag.cs`:

```csharp
public enum AttackTag
{
    None,
    Sweep,
    Thrust,
    Overhead,
    Rising,
    Spinning,
    Slam
}

public enum AttackSpeed
{
    Fast,
    Medium,
    Slow
}

public enum MovementPattern
{
    HoldPosition,
    LungeForward,
    PullBack,
    CircleArc
}
```

- [ ] **Step 4: Implement AttackData ScriptableObject**

Create `Assets/Scripts/Combat/AttackData.cs`:

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "NewAttack", menuName = "Game/Attack Data")]
public class AttackData : ScriptableObject
{
    public string attackName;
    public AttackTag primaryTag;
    public AttackTag secondaryTag;
    public float baseDamage = 10f;
    public AttackSpeed speed = AttackSpeed.Medium;
    public MovementPattern movementPattern = MovementPattern.HoldPosition;
    public AnimationClip animationClip;

    public bool HasTag(AttackTag tag)
    {
        if (tag == AttackTag.None) return false;
        return primaryTag == tag || secondaryTag == tag;
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: Unity → Test Runner → EditMode → Run All
Expected: Both tests PASS

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Combat/AttackTag.cs Assets/Scripts/Combat/AttackData.cs Assets/Tests/EditMode/Combat/AttackDataTests.cs
git commit -m "feat: add AttackTag enum and AttackData ScriptableObject with tag query"
```

---

## Task 3: HarmonyTable & HarmonyCalculator

**Files:**
- Create: `Assets/Scripts/Combat/HarmonyTable.cs`
- Create: `Assets/Scripts/Combat/HarmonyCalculator.cs`
- Test: `Assets/Tests/EditMode/Combat/HarmonyCalculatorTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `Assets/Tests/EditMode/Combat/HarmonyCalculatorTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class HarmonyCalculatorTests
{
    private HarmonyTable _table;

    [SetUp]
    public void SetUp()
    {
        _table = ScriptableObject.CreateInstance<HarmonyTable>();
        _table.entries = new HarmonyEntry[]
        {
            new HarmonyEntry { fromTag = AttackTag.Rising, toTag = AttackTag.Slam, level = HarmonyLevel.Harmonious },
            new HarmonyEntry { fromTag = AttackTag.Thrust, toTag = AttackTag.Sweep, level = HarmonyLevel.Harmonious },
            new HarmonyEntry { fromTag = AttackTag.Sweep, toTag = AttackTag.Overhead, level = HarmonyLevel.Harmonious },
            new HarmonyEntry { fromTag = AttackTag.Overhead, toTag = AttackTag.Thrust, level = HarmonyLevel.Harmonious },
            new HarmonyEntry { fromTag = AttackTag.Slam, toTag = AttackTag.Rising, level = HarmonyLevel.Dissonant },
            new HarmonyEntry { fromTag = AttackTag.Spinning, toTag = AttackTag.Spinning, level = HarmonyLevel.Dissonant },
        };
    }

    [Test]
    public void GetHarmony_HarmoniousPair_ReturnsHarmonious()
    {
        var result = HarmonyCalculator.GetHarmony(_table, AttackTag.Rising, AttackTag.Slam);
        Assert.AreEqual(HarmonyLevel.Harmonious, result);
    }

    [Test]
    public void GetHarmony_DissonantPair_ReturnsDissonant()
    {
        var result = HarmonyCalculator.GetHarmony(_table, AttackTag.Slam, AttackTag.Rising);
        Assert.AreEqual(HarmonyLevel.Dissonant, result);
    }

    [Test]
    public void GetHarmony_UnlistedPair_ReturnsNeutral()
    {
        var result = HarmonyCalculator.GetHarmony(_table, AttackTag.Sweep, AttackTag.Slam);
        Assert.AreEqual(HarmonyLevel.Neutral, result);
    }

    [Test]
    public void GetDamageMultiplier_Harmonious_Returns1_3()
    {
        float mult = HarmonyCalculator.GetDamageMultiplier(HarmonyLevel.Harmonious);
        Assert.AreEqual(1.3f, mult, 0.001f);
    }

    [Test]
    public void GetDamageMultiplier_Neutral_Returns1()
    {
        float mult = HarmonyCalculator.GetDamageMultiplier(HarmonyLevel.Neutral);
        Assert.AreEqual(1.0f, mult, 0.001f);
    }

    [Test]
    public void GetDamageMultiplier_Dissonant_Returns0_6()
    {
        float mult = HarmonyCalculator.GetDamageMultiplier(HarmonyLevel.Dissonant);
        Assert.AreEqual(0.6f, mult, 0.001f);
    }

    [Test]
    public void CalculateComboScore_FullyHarmonious3Slot_ReturnsHighMultiplier()
    {
        var a1 = ScriptableObject.CreateInstance<AttackData>();
        a1.primaryTag = AttackTag.Rising;
        a1.baseDamage = 10f;

        var a2 = ScriptableObject.CreateInstance<AttackData>();
        a2.primaryTag = AttackTag.Slam;
        a2.baseDamage = 10f;

        var a3 = ScriptableObject.CreateInstance<AttackData>();
        a3.primaryTag = AttackTag.Rising; // Slam->Rising is dissonant, but we want Rising->Slam
        a3.baseDamage = 10f;

        // Rising->Slam = harmonious, Slam->Rising = dissonant
        var attacks = new AttackData[] { a1, a2, a3 };
        float[] multipliers = HarmonyCalculator.CalculateComboMultipliers(_table, attacks);

        // First attack has no predecessor: 1.0
        Assert.AreEqual(1.0f, multipliers[0], 0.001f);
        // Rising->Slam: harmonious = 1.3
        Assert.AreEqual(1.3f, multipliers[1], 0.001f);
        // Slam->Rising: dissonant = 0.6
        Assert.AreEqual(0.6f, multipliers[2], 0.001f);
    }

    [Test]
    public void CalculateComboScore_BestTagUsedForMultiTagAttacks()
    {
        var a1 = ScriptableObject.CreateInstance<AttackData>();
        a1.primaryTag = AttackTag.Sweep;
        a1.secondaryTag = AttackTag.Rising;

        var a2 = ScriptableObject.CreateInstance<AttackData>();
        a2.primaryTag = AttackTag.Slam;

        // Rising->Slam is harmonious, Sweep->Slam is neutral
        // Should pick the best: harmonious
        var attacks = new AttackData[] { a1, a2 };
        float[] multipliers = HarmonyCalculator.CalculateComboMultipliers(_table, attacks);

        Assert.AreEqual(1.0f, multipliers[0], 0.001f);
        Assert.AreEqual(1.3f, multipliers[1], 0.001f);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: Unity → Test Runner → EditMode → Run All
Expected: FAIL — `HarmonyTable`, `HarmonyCalculator`, `HarmonyLevel`, `HarmonyEntry` not defined

- [ ] **Step 3: Implement HarmonyTable ScriptableObject**

Create `Assets/Scripts/Combat/HarmonyTable.cs`:

```csharp
using UnityEngine;
using System;

public enum HarmonyLevel
{
    Neutral,
    Harmonious,
    Dissonant
}

[Serializable]
public struct HarmonyEntry
{
    public AttackTag fromTag;
    public AttackTag toTag;
    public HarmonyLevel level;
}

[CreateAssetMenu(fileName = "HarmonyTable", menuName = "Game/Harmony Table")]
public class HarmonyTable : ScriptableObject
{
    public HarmonyEntry[] entries;
}
```

- [ ] **Step 4: Implement HarmonyCalculator**

Create `Assets/Scripts/Combat/HarmonyCalculator.cs`:

```csharp
public static class HarmonyCalculator
{
    public static HarmonyLevel GetHarmony(HarmonyTable table, AttackTag from, AttackTag to)
    {
        if (table.entries == null) return HarmonyLevel.Neutral;

        foreach (var entry in table.entries)
        {
            if (entry.fromTag == from && entry.toTag == to)
                return entry.level;
        }

        return HarmonyLevel.Neutral;
    }

    public static float GetDamageMultiplier(HarmonyLevel level)
    {
        switch (level)
        {
            case HarmonyLevel.Harmonious: return 1.3f;
            case HarmonyLevel.Dissonant: return 0.6f;
            default: return 1.0f;
        }
    }

    public static HarmonyLevel GetBestHarmony(HarmonyTable table, AttackData from, AttackData to)
    {
        AttackTag[] fromTags = GetTags(from);
        AttackTag[] toTags = GetTags(to);

        HarmonyLevel best = HarmonyLevel.Neutral;

        foreach (var ft in fromTags)
        {
            foreach (var tt in toTags)
            {
                var h = GetHarmony(table, ft, tt);
                if (h == HarmonyLevel.Harmonious)
                    return HarmonyLevel.Harmonious;
                if (h == HarmonyLevel.Dissonant && best == HarmonyLevel.Neutral)
                    best = h;
            }
        }

        return best;
    }

    public static float[] CalculateComboMultipliers(HarmonyTable table, AttackData[] attacks)
    {
        float[] multipliers = new float[attacks.Length];
        multipliers[0] = 1.0f;

        for (int i = 1; i < attacks.Length; i++)
        {
            if (attacks[i] == null || attacks[i - 1] == null)
            {
                multipliers[i] = 1.0f;
                continue;
            }

            var harmony = GetBestHarmony(table, attacks[i - 1], attacks[i]);
            multipliers[i] = GetDamageMultiplier(harmony);
        }

        return multipliers;
    }

    private static AttackTag[] GetTags(AttackData attack)
    {
        if (attack.secondaryTag != AttackTag.None)
            return new AttackTag[] { attack.primaryTag, attack.secondaryTag };
        return new AttackTag[] { attack.primaryTag };
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: Unity → Test Runner → EditMode → Run All
Expected: All 8 tests PASS

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Combat/HarmonyTable.cs Assets/Scripts/Combat/HarmonyCalculator.cs Assets/Tests/EditMode/Combat/HarmonyCalculatorTests.cs
git commit -m "feat: add HarmonyTable and HarmonyCalculator with tag-pair harmony lookup"
```

---

## Task 4: ComboBookData ScriptableObject

**Files:**
- Create: `Assets/Scripts/Combat/ComboBookData.cs`
- Test: `Assets/Tests/EditMode/Combat/ComboBookDataTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `Assets/Tests/EditMode/Combat/ComboBookDataTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class ComboBookDataTests
{
    [Test]
    public void ComboBook_Common_Has2Slots()
    {
        var book = ScriptableObject.CreateInstance<ComboBookData>();
        book.rarity = ComboBookRarity.Common;
        book.ForceInitSlots();

        Assert.AreEqual(2, book.SlotCount);
    }

    [Test]
    public void ComboBook_Rare_Has3Slots()
    {
        var book = ScriptableObject.CreateInstance<ComboBookData>();
        book.rarity = ComboBookRarity.Rare;
        book.ForceInitSlots();

        Assert.AreEqual(3, book.SlotCount);
    }

    [Test]
    public void ComboBook_Legendary_Has4Slots()
    {
        var book = ScriptableObject.CreateInstance<ComboBookData>();
        book.rarity = ComboBookRarity.Legendary;
        book.ForceInitSlots();

        Assert.AreEqual(4, book.SlotCount);
    }

    [Test]
    public void ComboBook_SetAttack_PlacesAttackInSlot()
    {
        var book = ScriptableObject.CreateInstance<ComboBookData>();
        book.rarity = ComboBookRarity.Rare;
        book.ForceInitSlots();

        var attack = ScriptableObject.CreateInstance<AttackData>();
        attack.attackName = "Test Slash";

        book.SetAttack(1, attack);

        Assert.AreEqual(attack, book.GetAttack(1));
        Assert.IsNull(book.GetAttack(0));
        Assert.IsNull(book.GetAttack(2));
    }

    [Test]
    public void ComboBook_SetAttack_OutOfRange_DoesNothing()
    {
        var book = ScriptableObject.CreateInstance<ComboBookData>();
        book.rarity = ComboBookRarity.Common;
        book.ForceInitSlots();

        var attack = ScriptableObject.CreateInstance<AttackData>();

        book.SetAttack(5, attack);
        book.SetAttack(-1, attack);

        // Should not throw, just no-op
        Assert.IsNull(book.GetAttack(0));
        Assert.IsNull(book.GetAttack(1));
    }

    [Test]
    public void ComboBook_ClearSlot_RemovesAttack()
    {
        var book = ScriptableObject.CreateInstance<ComboBookData>();
        book.rarity = ComboBookRarity.Common;
        book.ForceInitSlots();

        var attack = ScriptableObject.CreateInstance<AttackData>();
        book.SetAttack(0, attack);
        book.ClearSlot(0);

        Assert.IsNull(book.GetAttack(0));
    }

    [Test]
    public void ComboBook_GetFilledAttacks_ReturnsOnlyNonNull()
    {
        var book = ScriptableObject.CreateInstance<ComboBookData>();
        book.rarity = ComboBookRarity.Rare;
        book.ForceInitSlots();

        var a1 = ScriptableObject.CreateInstance<AttackData>();
        var a3 = ScriptableObject.CreateInstance<AttackData>();
        book.SetAttack(0, a1);
        book.SetAttack(2, a3);

        var filled = book.GetFilledAttacks();
        Assert.AreEqual(2, filled.Length);
        Assert.AreEqual(a1, filled[0]);
        Assert.AreEqual(a3, filled[1]);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: Unity → Test Runner → EditMode → Run All
Expected: FAIL — `ComboBookData`, `ComboBookRarity` not defined

- [ ] **Step 3: Implement ComboBookData**

Create `Assets/Scripts/Combat/ComboBookData.cs`:

```csharp
using UnityEngine;
using System.Collections.Generic;

public enum ComboBookRarity
{
    Common,
    Rare,
    Legendary
}

[CreateAssetMenu(fileName = "NewComboBook", menuName = "Game/Combo Book")]
public class ComboBookData : ScriptableObject
{
    public string bookName;
    public ComboBookRarity rarity;
    [SerializeField] private AttackData[] slots;

    public int SlotCount => slots != null ? slots.Length : 0;

    public void InitSlots()
    {
        // Only initialize if slots not already configured (e.g., pre-configured books set in Inspector)
        if (slots != null && slots.Length > 0) return;

        int count = rarity switch
        {
            ComboBookRarity.Common => 2,
            ComboBookRarity.Rare => 3,
            ComboBookRarity.Legendary => 4,
            _ => 2
        };
        slots = new AttackData[count];
    }

    public void ForceInitSlots()
    {
        // Used in tests to always create fresh slots
        int count = rarity switch
        {
            ComboBookRarity.Common => 2,
            ComboBookRarity.Rare => 3,
            ComboBookRarity.Legendary => 4,
            _ => 2
        };
        slots = new AttackData[count];
    }

    public void SetAttack(int slotIndex, AttackData attack)
    {
        if (slots == null || slotIndex < 0 || slotIndex >= slots.Length) return;
        slots[slotIndex] = attack;
    }

    public AttackData GetAttack(int slotIndex)
    {
        if (slots == null || slotIndex < 0 || slotIndex >= slots.Length) return null;
        return slots[slotIndex];
    }

    public void ClearSlot(int slotIndex)
    {
        SetAttack(slotIndex, null);
    }

    public AttackData[] GetFilledAttacks()
    {
        if (slots == null) return new AttackData[0];

        var filled = new List<AttackData>();
        foreach (var slot in slots)
        {
            if (slot != null) filled.Add(slot);
        }
        return filled.ToArray();
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: Unity → Test Runner → EditMode → Run All
Expected: All 7 tests PASS

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Combat/ComboBookData.cs Assets/Tests/EditMode/Combat/ComboBookDataTests.cs
git commit -m "feat: add ComboBookData ScriptableObject with rarity-based slot counts"
```

---

## Task 5: GameState & GameManager

**Files:**
- Create: `Assets/Scripts/Core/GameState.cs`
- Create: `Assets/Scripts/Core/GameManager.cs`
- Test: `Assets/Tests/EditMode/Core/GameStateTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `Assets/Tests/EditMode/Core/GameStateTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[TestFixture]
public class GameStateTests
{
    private GameState _state;

    [SetUp]
    public void SetUp()
    {
        _state = new GameState();
    }

    [Test]
    public void NewGameState_HasEmptyCollections()
    {
        Assert.AreEqual(0, _state.collectedKeys.Count);
        Assert.AreEqual(0, _state.discoveredAttacks.Count);
        Assert.AreEqual(0, _state.discoveredComboBooks.Count);
        Assert.AreEqual(0, _state.unlockedShortcuts.Count);
        Assert.IsFalse(_state.zoneBCleared);
        Assert.IsFalse(_state.zoneCCleared);
    }

    [Test]
    public void CollectKey_AddsKeyToList()
    {
        _state.CollectKey("catacombs_key_01");

        Assert.IsTrue(_state.HasKey("catacombs_key_01"));
        Assert.IsFalse(_state.HasKey("chapel_key_01"));
    }

    [Test]
    public void CollectKey_Duplicate_DoesNotAddTwice()
    {
        _state.CollectKey("key_01");
        _state.CollectKey("key_01");

        Assert.AreEqual(1, _state.collectedKeys.Count);
    }

    [Test]
    public void UnlockShortcut_TracksShortcutId()
    {
        _state.UnlockShortcut("zoneA_to_zoneB_shortcut");

        Assert.IsTrue(_state.IsShortcutUnlocked("zoneA_to_zoneB_shortcut"));
        Assert.IsFalse(_state.IsShortcutUnlocked("other_shortcut"));
    }

    [Test]
    public void DiscoverAttack_AddsToCollection()
    {
        var attack = ScriptableObject.CreateInstance<AttackData>();
        attack.attackName = "Test Slash";

        _state.DiscoverAttack(attack);

        Assert.AreEqual(1, _state.discoveredAttacks.Count);
        Assert.IsTrue(_state.HasAttack(attack));
    }

    [Test]
    public void BossUnlocked_RequiresBothZonesCleared()
    {
        Assert.IsFalse(_state.IsBossUnlocked);

        _state.zoneBCleared = true;
        Assert.IsFalse(_state.IsBossUnlocked);

        _state.zoneCCleared = true;
        Assert.IsTrue(_state.IsBossUnlocked);
    }

    [Test]
    public void Reset_ClearsAllState()
    {
        _state.CollectKey("key_01");
        _state.zoneBCleared = true;
        _state.UnlockShortcut("shortcut_01");

        _state.Reset();

        Assert.AreEqual(0, _state.collectedKeys.Count);
        Assert.IsFalse(_state.zoneBCleared);
        Assert.AreEqual(0, _state.unlockedShortcuts.Count);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: Unity → Test Runner → EditMode → Run All
Expected: FAIL — `GameState` not defined

- [ ] **Step 3: Implement GameState**

Create `Assets/Scripts/Core/GameState.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

public class GameState
{
    public HashSet<string> collectedKeys = new HashSet<string>();
    public List<AttackData> discoveredAttacks = new List<AttackData>();
    public List<ComboBookData> discoveredComboBooks = new List<ComboBookData>();
    public HashSet<string> unlockedShortcuts = new HashSet<string>();
    public HashSet<string> discoveredShrines = new HashSet<string>();
    public bool zoneBCleared;
    public bool zoneCCleared;
    public string lastShrineId;
    public string lastShrineScene;

    public bool IsBossUnlocked => zoneBCleared && zoneCCleared;

    public void CollectKey(string keyId)
    {
        collectedKeys.Add(keyId);
    }

    public bool HasKey(string keyId)
    {
        return collectedKeys.Contains(keyId);
    }

    public void UnlockShortcut(string shortcutId)
    {
        unlockedShortcuts.Add(shortcutId);
    }

    public bool IsShortcutUnlocked(string shortcutId)
    {
        return unlockedShortcuts.Contains(shortcutId);
    }

    public void DiscoverAttack(AttackData attack)
    {
        if (!discoveredAttacks.Contains(attack))
            discoveredAttacks.Add(attack);
    }

    public bool HasAttack(AttackData attack)
    {
        return discoveredAttacks.Contains(attack);
    }

    public void DiscoverComboBook(ComboBookData book)
    {
        if (!discoveredComboBooks.Contains(book))
            discoveredComboBooks.Add(book);
    }

    public void RegisterShrine(string shrineId, string sceneName)
    {
        discoveredShrines.Add(shrineId);
        lastShrineId = shrineId;
        lastShrineScene = sceneName;
    }

    public void Reset()
    {
        collectedKeys.Clear();
        discoveredAttacks.Clear();
        discoveredComboBooks.Clear();
        unlockedShortcuts.Clear();
        discoveredShrines.Clear();
        zoneBCleared = false;
        zoneCCleared = false;
        lastShrineId = null;
        lastShrineScene = null;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: Unity → Test Runner → EditMode → Run All
Expected: All 7 tests PASS

- [ ] **Step 5: Implement GameManager singleton**

Create `Assets/Scripts/Core/GameManager.cs`:

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; } = new GameState();

    [Header("References")]
    public HarmonyTable harmonyTable;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RespawnAtLastShrine()
    {
        if (string.IsNullOrEmpty(State.lastShrineScene)) return;

        SceneManager.LoadScene(State.lastShrineScene);
    }

    public void NewGame()
    {
        State.Reset();
        SceneManager.LoadScene("ZoneA");
    }
}
```

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Core/GameState.cs Assets/Scripts/Core/GameManager.cs Assets/Tests/EditMode/Core/GameStateTests.cs
git commit -m "feat: add GameState tracking class and GameManager singleton"
```

---

## Task 6: Player Controller (Movement & State Machine)

**Files:**
- Create: `Assets/Scripts/Player/PlayerController.cs`
- Test: `Assets/Tests/PlayMode/Player/PlayerControllerTests.cs`

- [ ] **Step 1: Write the failing test**

Create `Assets/Tests/PlayMode/Player/PlayerControllerTests.cs`:

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestUtils;

public class PlayerControllerTests
{
    private GameObject _playerObj;
    private PlayerController _player;

    [SetUp]
    public void SetUp()
    {
        _playerObj = new GameObject("Player");
        _playerObj.AddComponent<Rigidbody2D>();
        _playerObj.AddComponent<BoxCollider2D>();
        _player = _playerObj.AddComponent<PlayerController>();
        _player.moveSpeed = 5f;
        _player.dodgeSpeed = 10f;
        _player.dodgeDuration = 0.3f;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_playerObj);
    }

    [Test]
    public void Player_StartsInIdleState()
    {
        Assert.AreEqual(PlayerState.Idle, _player.CurrentState);
    }

    [Test]
    public void Player_SetMoveInput_ChangesToMovingState()
    {
        _player.SetMoveInput(new Vector2(1, 0));
        _player.UpdateState();

        Assert.AreEqual(PlayerState.Moving, _player.CurrentState);
    }

    [Test]
    public void Player_ZeroMoveInput_ReturnsToIdle()
    {
        _player.SetMoveInput(new Vector2(1, 0));
        _player.UpdateState();
        _player.SetMoveInput(Vector2.zero);
        _player.UpdateState();

        Assert.AreEqual(PlayerState.Idle, _player.CurrentState);
    }

    [Test]
    public void Player_Dodge_EntersDodgingState()
    {
        _player.SetMoveInput(new Vector2(1, 0));
        _player.StartDodge();

        Assert.AreEqual(PlayerState.Dodging, _player.CurrentState);
    }

    [Test]
    public void Player_CannotDodge_WhenAlreadyDodging()
    {
        _player.SetMoveInput(new Vector2(1, 0));
        _player.StartDodge();
        bool result = _player.StartDodge();

        Assert.IsFalse(result);
    }

    [Test]
    public void Player_CannotMove_WhenDead()
    {
        _player.SetState(PlayerState.Dead);
        _player.SetMoveInput(new Vector2(1, 0));
        _player.UpdateState();

        Assert.AreEqual(PlayerState.Dead, _player.CurrentState);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: Unity → Test Runner → PlayMode → Run All
Expected: FAIL — `PlayerController`, `PlayerState` not defined

- [ ] **Step 3: Implement PlayerController**

Create `Assets/Scripts/Player/PlayerController.cs`:

```csharp
using UnityEngine;

public enum PlayerState
{
    Idle,
    Moving,
    Attacking,
    Dodging,
    Hit,
    Dead
}

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float dodgeSpeed = 10f;
    public float dodgeDuration = 0.3f;

    public PlayerState CurrentState { get; private set; } = PlayerState.Idle;

    private Vector2 _moveInput;
    private Vector2 _lastFacingDirection = Vector2.down;
    private Rigidbody2D _rb;
    private float _dodgeTimer;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
    }

    private void Update()
    {
        UpdateState();
    }

    private void FixedUpdate()
    {
        switch (CurrentState)
        {
            case PlayerState.Moving:
                _rb.velocity = _moveInput.normalized * moveSpeed;
                break;
            case PlayerState.Dodging:
                _rb.velocity = _lastFacingDirection * dodgeSpeed;
                _dodgeTimer -= Time.fixedDeltaTime;
                if (_dodgeTimer <= 0f)
                    CurrentState = PlayerState.Idle;
                break;
            case PlayerState.Idle:
            case PlayerState.Dead:
                _rb.velocity = Vector2.zero;
                break;
        }
    }

    public void SetMoveInput(Vector2 input)
    {
        _moveInput = input;
        if (input.sqrMagnitude > 0.01f)
            _lastFacingDirection = input.normalized;
    }

    public void UpdateState()
    {
        if (CurrentState == PlayerState.Dead) return;
        if (CurrentState == PlayerState.Dodging) return;
        if (CurrentState == PlayerState.Attacking) return;
        if (CurrentState == PlayerState.Hit) return;

        CurrentState = _moveInput.sqrMagnitude > 0.01f
            ? PlayerState.Moving
            : PlayerState.Idle;
    }

    public bool StartDodge()
    {
        if (CurrentState == PlayerState.Dodging) return false;
        if (CurrentState == PlayerState.Dead) return false;

        CurrentState = PlayerState.Dodging;
        _dodgeTimer = dodgeDuration;
        return true;
    }

    public void SetState(PlayerState state)
    {
        CurrentState = state;
    }

    public Vector2 FacingDirection => _lastFacingDirection;
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: Unity → Test Runner → PlayMode → Run All
Expected: All 6 tests PASS

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Player/PlayerController.cs Assets/Tests/PlayMode/Player/PlayerControllerTests.cs
git commit -m "feat: add PlayerController with 8-dir movement, dodge, and enum state machine"
```

---

## Task 7: Player Health & Death

**Files:**
- Create: `Assets/Scripts/Player/PlayerHealth.cs`

- [ ] **Step 1: Write the failing test**

Add to `Assets/Tests/PlayMode/Player/PlayerControllerTests.cs`:

```csharp
[Test]
public void PlayerHealth_TakeDamage_ReducesHealth()
{
    var health = _playerObj.AddComponent<PlayerHealth>();
    health.maxHealth = 100f;
    health.Init();

    health.TakeDamage(25f);

    Assert.AreEqual(75f, health.CurrentHealth, 0.001f);
}

[Test]
public void PlayerHealth_TakeLethalDamage_SetsDeadState()
{
    var health = _playerObj.AddComponent<PlayerHealth>();
    health.maxHealth = 100f;
    health.Init();

    health.TakeDamage(150f);

    Assert.AreEqual(0f, health.CurrentHealth, 0.001f);
    Assert.AreEqual(PlayerState.Dead, _player.CurrentState);
}

[Test]
public void PlayerHealth_CannotTakeDamage_WhenDodging()
{
    var health = _playerObj.AddComponent<PlayerHealth>();
    health.maxHealth = 100f;
    health.Init();

    _player.SetMoveInput(new Vector2(1, 0));
    _player.StartDodge();

    health.TakeDamage(50f);

    Assert.AreEqual(100f, health.CurrentHealth, 0.001f);
}

[Test]
public void PlayerHealth_Heal_ClampsToMax()
{
    var health = _playerObj.AddComponent<PlayerHealth>();
    health.maxHealth = 100f;
    health.Init();

    health.TakeDamage(20f);
    health.Heal(50f);

    Assert.AreEqual(100f, health.CurrentHealth, 0.001f);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: Unity → Test Runner → PlayMode → Run All
Expected: FAIL — `PlayerHealth` not defined

- [ ] **Step 3: Implement PlayerHealth**

Create `Assets/Scripts/Player/PlayerHealth.cs`:

```csharp
using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float CurrentHealth { get; private set; }

    public event Action<float, float> OnHealthChanged; // current, max
    public event Action OnDeath;

    private PlayerController _controller;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
    }

    private void Start()
    {
        Init();
    }

    public void Init()
    {
        _controller = GetComponent<PlayerController>();
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (_controller != null && _controller.CurrentState == PlayerState.Dodging)
            return;

        if (_controller != null && _controller.CurrentState == PlayerState.Dead)
            return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (ScreenShake.Instance != null)
            ScreenShake.Instance.Shake(0.5f);

        if (CurrentHealth <= 0f)
        {
            if (_controller != null) _controller.SetState(PlayerState.Dead);
            OnDeath?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: Unity → Test Runner → PlayMode → Run All
Expected: All 10 tests PASS

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Player/PlayerHealth.cs Assets/Tests/PlayMode/Player/PlayerControllerTests.cs
git commit -m "feat: add PlayerHealth with damage, heal, dodge i-frames, and death"
```

---

## Task 8: ComboExecutor (Runtime Combo Chain)

**Files:**
- Create: `Assets/Scripts/Combat/ComboExecutor.cs`
- Test: `Assets/Tests/PlayMode/Combat/ComboExecutorTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `Assets/Tests/PlayMode/Combat/ComboExecutorTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

public class ComboExecutorTests
{
    private GameObject _obj;
    private ComboExecutor _executor;
    private ComboBookData _book;
    private HarmonyTable _table;

    [SetUp]
    public void SetUp()
    {
        _obj = new GameObject("ComboTest");
        _executor = _obj.AddComponent<ComboExecutor>();
        _executor.chainWindowDuration = 0.5f;

        _table = ScriptableObject.CreateInstance<HarmonyTable>();
        _table.entries = new HarmonyEntry[]
        {
            new HarmonyEntry { fromTag = AttackTag.Rising, toTag = AttackTag.Slam, level = HarmonyLevel.Harmonious }
        };
        _executor.harmonyTable = _table;

        _book = ScriptableObject.CreateInstance<ComboBookData>();
        _book.rarity = ComboBookRarity.Rare;
        _book.InitSlots();

        var a1 = ScriptableObject.CreateInstance<AttackData>();
        a1.primaryTag = AttackTag.Rising;
        a1.baseDamage = 10f;

        var a2 = ScriptableObject.CreateInstance<AttackData>();
        a2.primaryTag = AttackTag.Slam;
        a2.baseDamage = 15f;

        var a3 = ScriptableObject.CreateInstance<AttackData>();
        a3.primaryTag = AttackTag.Sweep;
        a3.baseDamage = 12f;

        _book.SetAttack(0, a1);
        _book.SetAttack(1, a2);
        _book.SetAttack(2, a3);

        _executor.EquipBook(_book);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_obj);
    }

    [Test]
    public void Executor_StartsAtSlotZero()
    {
        Assert.AreEqual(0, _executor.CurrentSlot);
        Assert.IsFalse(_executor.IsExecuting);
    }

    [Test]
    public void Executor_Attack_ExecutesFirstSlot()
    {
        var result = _executor.Attack();

        Assert.IsTrue(result.executed);
        Assert.AreEqual(0, result.slotIndex);
        Assert.AreEqual(10f, result.rawDamage, 0.001f);
        Assert.AreEqual(1.0f, result.harmonyMultiplier, 0.001f);
        Assert.IsTrue(_executor.IsExecuting);
    }

    [Test]
    public void Executor_ChainAttack_AdvancesToNextSlot()
    {
        _executor.Attack();
        _executor.FinishCurrentAttack();

        var result = _executor.Attack();

        Assert.IsTrue(result.executed);
        Assert.AreEqual(1, result.slotIndex);
        Assert.AreEqual(15f, result.rawDamage, 0.001f);
        Assert.AreEqual(1.3f, result.harmonyMultiplier, 0.001f); // Rising->Slam = harmonious
    }

    [Test]
    public void Executor_AfterLastSlot_Resets()
    {
        _executor.Attack();
        _executor.FinishCurrentAttack();
        _executor.Attack();
        _executor.FinishCurrentAttack();
        _executor.Attack();
        _executor.FinishCurrentAttack();

        Assert.AreEqual(0, _executor.CurrentSlot);
        Assert.IsFalse(_executor.IsExecuting);
    }

    [Test]
    public void Executor_NoBook_ReturnsNotExecuted()
    {
        _executor.EquipBook(null);
        var result = _executor.Attack();

        Assert.IsFalse(result.executed);
    }

    [Test]
    public void Executor_ResetCombo_GoesBackToSlotZero()
    {
        _executor.Attack();
        _executor.FinishCurrentAttack();
        _executor.Attack();
        _executor.ResetCombo();

        Assert.AreEqual(0, _executor.CurrentSlot);
        Assert.IsFalse(_executor.IsExecuting);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: Unity → Test Runner → PlayMode → Run All
Expected: FAIL — `ComboExecutor`, `AttackResult` not defined

- [ ] **Step 3: Implement ComboExecutor**

Create `Assets/Scripts/Combat/ComboExecutor.cs`:

```csharp
using UnityEngine;

public struct AttackResult
{
    public bool executed;
    public int slotIndex;
    public AttackData attackData;
    public float rawDamage;
    public float harmonyMultiplier;
    public float totalDamage;
    public HarmonyLevel harmonyLevel;

    public static AttackResult Failed => new AttackResult { executed = false };
}

public class ComboExecutor : MonoBehaviour
{
    public float chainWindowDuration = 0.5f;
    public HarmonyTable harmonyTable;

    public int CurrentSlot { get; private set; }
    public bool IsExecuting { get; private set; }

    private ComboBookData _equippedBook;
    private float _chainTimer;
    private float[] _cachedMultipliers;

    public void EquipBook(ComboBookData book)
    {
        _equippedBook = book;
        ResetCombo();
        CacheMultipliers();
    }

    public AttackResult Attack()
    {
        if (_equippedBook == null) return AttackResult.Failed;

        var attack = _equippedBook.GetAttack(CurrentSlot);
        if (attack == null) return AttackResult.Failed;

        if (IsExecuting) return AttackResult.Failed;

        float multiplier = _cachedMultipliers != null && CurrentSlot < _cachedMultipliers.Length
            ? _cachedMultipliers[CurrentSlot]
            : 1.0f;

        HarmonyLevel level = HarmonyLevel.Neutral;
        if (CurrentSlot > 0 && harmonyTable != null)
        {
            var prevAttack = _equippedBook.GetAttack(CurrentSlot - 1);
            if (prevAttack != null)
                level = HarmonyCalculator.GetBestHarmony(harmonyTable, prevAttack, attack);
        }

        IsExecuting = true;
        _chainTimer = chainWindowDuration;

        return new AttackResult
        {
            executed = true,
            slotIndex = CurrentSlot,
            attackData = attack,
            rawDamage = attack.baseDamage,
            harmonyMultiplier = multiplier,
            totalDamage = attack.baseDamage * multiplier,
            harmonyLevel = level
        };
    }

    public void FinishCurrentAttack()
    {
        IsExecuting = false;
        CurrentSlot++;

        if (CurrentSlot >= _equippedBook.SlotCount || _equippedBook.GetAttack(CurrentSlot) == null)
        {
            ResetCombo();
        }
    }

    public void ResetCombo()
    {
        CurrentSlot = 0;
        IsExecuting = false;
        _chainTimer = 0f;
    }

    private void Update()
    {
        if (!IsExecuting && CurrentSlot > 0)
        {
            _chainTimer -= Time.deltaTime;
            if (_chainTimer <= 0f)
                ResetCombo();
        }
    }

    private void CacheMultipliers()
    {
        if (_equippedBook == null || harmonyTable == null)
        {
            _cachedMultipliers = null;
            return;
        }

        var attacks = new AttackData[_equippedBook.SlotCount];
        for (int i = 0; i < _equippedBook.SlotCount; i++)
            attacks[i] = _equippedBook.GetAttack(i);

        _cachedMultipliers = HarmonyCalculator.CalculateComboMultipliers(harmonyTable, attacks);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: Unity → Test Runner → PlayMode → Run All
Expected: All 6 tests PASS

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Combat/ComboExecutor.cs Assets/Tests/PlayMode/Combat/ComboExecutorTests.cs
git commit -m "feat: add ComboExecutor with chain timing, harmony multipliers, and combo reset"
```

---

## Task 9: PlayerInventory & PlayerCombat

**Files:**
- Create: `Assets/Scripts/Player/PlayerInventory.cs`
- Create: `Assets/Scripts/Player/PlayerCombat.cs`

- [ ] **Step 1: Implement PlayerInventory**

Create `Assets/Scripts/Player/PlayerInventory.cs`:

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    public List<AttackData> attacks = new List<AttackData>();
    public List<ComboBookData> comboBooks = new List<ComboBookData>();
    public ComboBookData equippedBook;

    public event Action<AttackData> OnAttackCollected;
    public event Action<ComboBookData> OnComboBookCollected;

    public void AddAttack(AttackData attack)
    {
        if (attacks.Contains(attack)) return;
        attacks.Add(attack);
        OnAttackCollected?.Invoke(attack);

        if (GameManager.Instance != null)
            GameManager.Instance.State.DiscoverAttack(attack);
    }

    public void AddComboBook(ComboBookData book)
    {
        if (comboBooks.Contains(book)) return;
        comboBooks.Add(book);
        OnComboBookCollected?.Invoke(book);

        if (GameManager.Instance != null)
            GameManager.Instance.State.DiscoverComboBook(book);

        if (equippedBook == null)
            EquipBook(book);
    }

    public void EquipBook(ComboBookData book)
    {
        // Clone the SO so runtime mutations don't corrupt the asset
        equippedBook = Instantiate(book);
        equippedBook.InitSlots(); // no-op if pre-configured, inits if empty
        var executor = GetComponent<ComboExecutor>();
        if (executor != null) executor.EquipBook(equippedBook);
    }
}
```

- [ ] **Step 2: Implement PlayerCombat**

Create `Assets/Scripts/Player/PlayerCombat.cs`:

```csharp
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(ComboExecutor))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Attack")]
    public float attackRange = 1.2f;
    public LayerMask enemyLayer;

    [Header("Dodge Cancel")]
    public float dodgeCancelRecovery = 0.1f; // brief delay after dodge-cancelling an attack

    private PlayerController _controller;
    private ComboExecutor _executor;
    private Rigidbody2D _rb;
    private float _attackAnimTimer;
    private bool _attackBuffered; // input buffering for responsive combos
    private AttackData _currentAttackData;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _executor = GetComponent<ComboExecutor>();
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (_controller.CurrentState == PlayerState.Attacking)
        {
            _attackAnimTimer -= Time.deltaTime;
            if (_attackAnimTimer <= 0f)
            {
                _executor.FinishCurrentAttack();
                _controller.SetState(PlayerState.Idle);

                // Process buffered input
                if (_attackBuffered)
                {
                    _attackBuffered = false;
                    TryAttack();
                }
            }
        }
    }

    public void TryAttack()
    {
        if (_controller.CurrentState == PlayerState.Dead) return;
        if (_controller.CurrentState == PlayerState.Dodging) return;

        // Buffer input if currently attacking — will fire when current attack finishes
        if (_controller.CurrentState == PlayerState.Attacking)
        {
            _attackBuffered = true;
            return;
        }

        var result = _executor.Attack();
        if (!result.executed) return;

        _controller.SetState(PlayerState.Attacking);
        _currentAttackData = result.attackData;
        _attackAnimTimer = GetAttackDuration(result.attackData);
        _attackBuffered = false;

        ApplyAttackMovement(result.attackData);
        DealDamage(result);
    }

    public bool TryDodgeCancel()
    {
        // Allow dodging out of an attack with a small recovery penalty
        if (_controller.CurrentState != PlayerState.Attacking) return false;

        _executor.ResetCombo();
        _attackBuffered = false;
        _controller.SetState(PlayerState.Idle);

        // Brief recovery before dodge starts
        return true;
    }

    private void ApplyAttackMovement(AttackData attack)
    {
        Vector2 facing = _controller.FacingDirection;
        switch (attack.movementPattern)
        {
            case MovementPattern.LungeForward:
                _rb.velocity = facing * 6f;
                break;
            case MovementPattern.PullBack:
                _rb.velocity = -facing * 3f;
                break;
            case MovementPattern.CircleArc:
                _rb.velocity = new Vector2(-facing.y, facing.x) * 4f;
                break;
            case MovementPattern.HoldPosition:
            default:
                _rb.velocity = Vector2.zero;
                break;
        }
    }

    private void DealDamage(AttackResult result)
    {
        Vector2 attackOrigin = (Vector2)transform.position + _controller.FacingDirection * 0.5f;
        var hits = Physics2D.OverlapCircleAll(attackOrigin, attackRange, enemyLayer);

        foreach (var hit in hits)
        {
            var enemyHealth = hit.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(result.totalDamage, result.harmonyLevel);
            }
        }

        if (hits.Length > 0 && ScreenShake.Instance != null)
            ScreenShake.Instance.Shake(0.3f);
    }

    private float GetAttackDuration(AttackData attack)
    {
        return attack.speed switch
        {
            AttackSpeed.Fast => 0.25f,
            AttackSpeed.Medium => 0.4f,
            AttackSpeed.Slow => 0.6f,
            _ => 0.4f
        };
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Player/PlayerInventory.cs Assets/Scripts/Player/PlayerCombat.cs
git commit -m "feat: add PlayerInventory and PlayerCombat with attack execution and hit detection"
```

---

## Task 10: Enemy System (Data, Health, State Machine)

**Files:**
- Create: `Assets/Scripts/Enemies/EnemyData.cs`
- Create: `Assets/Scripts/Enemies/EnemyHealth.cs`
- Create: `Assets/Scripts/Enemies/EnemyStateMachine.cs`

- [ ] **Step 1: Implement EnemyData ScriptableObject**

Create `Assets/Scripts/Enemies/EnemyData.cs`:

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public float maxHealth = 50f;
    public float moveSpeed = 2f;
    public float attackDamage = 10f;
    public float attackRange = 1f;
    public float attackCooldown = 1.5f;
    public float detectionRange = 6f;
    public float staggerThreshold = 20f;
    public float staggerDuration = 0.5f;
}
```

- [ ] **Step 2: Implement EnemyHealth**

Create `Assets/Scripts/Enemies/EnemyHealth.cs`:

```csharp
using UnityEngine;
using System;

public class EnemyHealth : MonoBehaviour
{
    public EnemyData data;
    public float CurrentHealth { get; private set; }

    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;
    public event Action OnStagger;

    private float _staggerAccumulator;

    public void Init()
    {
        if (data != null)
            CurrentHealth = data.maxHealth;
    }

    private void Start()
    {
        Init();
    }

    public void TakeDamage(float amount, HarmonyLevel harmonyLevel)
    {
        if (CurrentHealth <= 0f) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        _staggerAccumulator += amount;
        OnHealthChanged?.Invoke(CurrentHealth, data.maxHealth);

        if (CurrentHealth <= 0f)
        {
            OnDeath?.Invoke();
            return;
        }

        if (_staggerAccumulator >= data.staggerThreshold)
        {
            _staggerAccumulator = 0f;
            OnStagger?.Invoke();
        }
    }
}
```

- [ ] **Step 3: Implement EnemyStateMachine**

Create `Assets/Scripts/Enemies/EnemyStateMachine.cs`:

```csharp
using UnityEngine;

public enum EnemyState
{
    Idle,
    Patrol,
    Chase,
    Attack,
    Stagger,
    Dead
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyStateMachine : MonoBehaviour
{
    public EnemyData data;
    public EnemyState CurrentState { get; private set; } = EnemyState.Idle;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float patrolWaitTime = 1f;

    [Header("Type-Specific Behavior")]
    public EnemyBehavior behavior; // optional composition component

    private Rigidbody2D _rb;
    private EnemyHealth _health;
    private Transform _player;
    private int _patrolIndex;
    private float _patrolWaitTimer;
    private float _attackCooldownTimer;
    private float _staggerTimer;
    private float _attackWindupTimer;
    private bool _attackWindingUp;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
        _health = GetComponent<EnemyHealth>();
    }

    private void Start()
    {
        _health.OnDeath += () => SetState(EnemyState.Dead);
        _health.OnStagger += () =>
        {
            if (CurrentState != EnemyState.Dead)
            {
                SetState(EnemyState.Stagger);
                _staggerTimer = data.staggerDuration;
            }
        };

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;
    }

    private void Update()
    {
        if (_player == null) return;

        switch (CurrentState)
        {
            case EnemyState.Idle:
                UpdateIdle();
                break;
            case EnemyState.Patrol:
                UpdatePatrol();
                break;
            case EnemyState.Chase:
                UpdateChase();
                break;
            case EnemyState.Attack:
                UpdateAttack();
                break;
            case EnemyState.Stagger:
                UpdateStagger();
                break;
            case EnemyState.Dead:
                _rb.velocity = Vector2.zero;
                break;
        }
    }

    private void UpdateIdle()
    {
        float dist = Vector2.Distance(transform.position, _player.position);
        if (dist <= data.detectionRange)
        {
            SetState(EnemyState.Chase);
            return;
        }

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            _patrolWaitTimer -= Time.deltaTime;
            if (_patrolWaitTimer <= 0f)
                SetState(EnemyState.Patrol);
        }
    }

    private void UpdatePatrol()
    {
        float dist = Vector2.Distance(transform.position, _player.position);
        if (dist <= data.detectionRange)
        {
            SetState(EnemyState.Chase);
            return;
        }

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            SetState(EnemyState.Idle);
            return;
        }

        var target = patrolPoints[_patrolIndex].position;
        Vector2 dir = ((Vector2)target - (Vector2)transform.position).normalized;
        _rb.velocity = dir * data.moveSpeed;

        if (Vector2.Distance(transform.position, target) < 0.3f)
        {
            _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
            _patrolWaitTimer = patrolWaitTime;
            SetState(EnemyState.Idle);
        }
    }

    private void UpdateChase()
    {
        float dist = Vector2.Distance(transform.position, _player.position);

        if (dist > data.detectionRange * 1.5f)
        {
            SetState(EnemyState.Idle);
            return;
        }

        if (dist <= data.attackRange && _attackCooldownTimer <= 0f)
        {
            SetState(EnemyState.Attack);
            return;
        }

        Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
        _rb.velocity = dir * data.moveSpeed;

        _attackCooldownTimer -= Time.deltaTime;
    }

    private void UpdateAttack()
    {
        _rb.velocity = Vector2.zero;

        // Telegraph / wind-up phase before dealing damage
        if (!_attackWindingUp)
        {
            _attackWindingUp = true;
            _attackWindupTimer = 0.4f; // wind-up time — visible telegraph
            return;
        }

        _attackWindupTimer -= Time.deltaTime;
        if (_attackWindupTimer > 0f) return;

        // Wind-up complete — deal damage
        _attackWindingUp = false;
        _attackCooldownTimer = data.attackCooldown;
        OnAttackExecute();
        SetState(EnemyState.Chase);
    }

    private void UpdateStagger()
    {
        _rb.velocity = Vector2.zero;
        _staggerTimer -= Time.deltaTime;
        if (_staggerTimer <= 0f)
            SetState(EnemyState.Chase);
    }

    public void SetState(EnemyState state)
    {
        if (CurrentState == EnemyState.Dead) return;
        CurrentState = state;

        if (state == EnemyState.Dead)
        {
            _rb.velocity = Vector2.zero;
            GetComponent<Collider2D>().enabled = false;
        }
    }

    private void OnAttackExecute()
    {
        // Delegate to behavior component if present, otherwise default melee
        if (behavior != null)
        {
            behavior.ExecuteAttack(transform, _player, data);
            return;
        }

        float dist = Vector2.Distance(transform.position, _player.position);
        if (dist <= data.attackRange)
        {
            var playerHealth = _player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.TakeDamage(data.attackDamage);
        }
    }
}
```

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Enemies/EnemyData.cs Assets/Scripts/Enemies/EnemyHealth.cs Assets/Scripts/Enemies/EnemyStateMachine.cs
git commit -m "feat: add EnemyData SO, EnemyHealth with stagger, and EnemyStateMachine"
```

---

## Task 11: Enemy Behavior Components (Composition) & Armor System

**Files:**
- Create: `Assets/Scripts/Enemies/EnemyBehavior.cs`
- Modify: `Assets/Scripts/Enemies/EnemyData.cs` — add armor fields
- Modify: `Assets/Scripts/Enemies/EnemyHealth.cs` — add armor damage reduction
- Create: `Assets/Scripts/Enemies/Projectile.cs`

All enemy types use the same `EnemyStateMachine` component. Type-specific behavior (Wraith retreat, Caster projectiles) is handled by an optional `EnemyBehavior` composition component. Knight armor is data-driven via `EnemyData` + `EnemyHealth`.

- [ ] **Step 1: Add armor fields to EnemyData**

Modify `Assets/Scripts/Enemies/EnemyData.cs` — add after `staggerDuration`:

```csharp
    [Header("Armor")]
    public float armorReduction = 0f; // 0 = no armor, 0.5 = takes half damage
    public bool armorDisabledDuringStagger = false; // Knight: true
```

- [ ] **Step 2: Add armor logic to EnemyHealth**

Modify `Assets/Scripts/Enemies/EnemyHealth.cs` — replace `TakeDamage` and add stagger tracking:

```csharp
    private bool _isStaggered;

    public void TakeDamage(float amount, HarmonyLevel harmonyLevel)
    {
        if (CurrentHealth <= 0f) return;

        // Apply armor reduction (disabled during stagger)
        float finalAmount = amount;
        if (data.armorReduction > 0f && !(data.armorDisabledDuringStagger && _isStaggered))
        {
            finalAmount = amount * (1f - data.armorReduction);
        }

        CurrentHealth = Mathf.Max(0f, CurrentHealth - finalAmount);
        _staggerAccumulator += finalAmount;
        OnHealthChanged?.Invoke(CurrentHealth, data.maxHealth);

        if (CurrentHealth <= 0f)
        {
            OnDeath?.Invoke();
            return;
        }

        if (_staggerAccumulator >= data.staggerThreshold)
        {
            _staggerAccumulator = 0f;
            _isStaggered = true;
            OnStagger?.Invoke();
        }
    }

    public void EndStagger()
    {
        _isStaggered = false;
    }
```

Also update `EnemyStateMachine.UpdateStagger` to call `_health.EndStagger()` when stagger ends:
```csharp
    private void UpdateStagger()
    {
        _rb.velocity = Vector2.zero;
        _staggerTimer -= Time.deltaTime;
        if (_staggerTimer <= 0f)
        {
            _health.EndStagger();
            SetState(EnemyState.Chase);
        }
    }
```

- [ ] **Step 3: Implement EnemyBehavior (composition component)**

Create `Assets/Scripts/Enemies/EnemyBehavior.cs`:

```csharp
using UnityEngine;

public enum EnemyBehaviorType
{
    Melee,       // Hollow, Knight — default melee
    DashRetreat, // Wraith — attacks then dashes away
    Ranged       // Caster — fires projectile instead of melee
}

public class EnemyBehavior : MonoBehaviour
{
    public EnemyBehaviorType behaviorType;

    [Header("Dash Retreat (Wraith)")]
    public float retreatSpeed = 12f;
    public float retreatDistance = 3f;

    [Header("Ranged (Caster)")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 8f;

    private bool _isRetreating;
    private Transform _retreatFrom;

    public void ExecuteAttack(Transform self, Transform player, EnemyData data)
    {
        switch (behaviorType)
        {
            case EnemyBehaviorType.Melee:
                MeleeAttack(self, player, data);
                break;
            case EnemyBehaviorType.DashRetreat:
                MeleeAttack(self, player, data);
                _isRetreating = true;
                _retreatFrom = player;
                break;
            case EnemyBehaviorType.Ranged:
                RangedAttack(self, player, data);
                break;
        }
    }

    private void MeleeAttack(Transform self, Transform player, EnemyData data)
    {
        float dist = Vector2.Distance(self.position, player.position);
        if (dist <= data.attackRange)
        {
            var playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.TakeDamage(data.attackDamage);
        }
    }

    private void RangedAttack(Transform self, Transform player, EnemyData data)
    {
        if (projectilePrefab == null) return;

        Vector2 dir = ((Vector2)player.position - (Vector2)self.position).normalized;
        var proj = Instantiate(projectilePrefab, self.position, Quaternion.identity);
        var rb = proj.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = dir * projectileSpeed;

        var dmg = proj.GetComponent<Projectile>();
        if (dmg != null) dmg.damage = data.attackDamage;
    }

    private void LateUpdate()
    {
        if (!_isRetreating || _retreatFrom == null) return;

        float dist = Vector2.Distance(transform.position, _retreatFrom.position);
        if (dist < retreatDistance)
        {
            Vector2 away = ((Vector2)transform.position - (Vector2)_retreatFrom.position).normalized;
            GetComponent<Rigidbody2D>().velocity = away * retreatSpeed;
        }
        else
        {
            _isRetreating = false;
        }
    }
}
```

- [ ] **Step 4: Create Projectile helper**

Create `Assets/Scripts/Enemies/Projectile.cs`:

```csharp
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    public float damage = 10f;
    public float lifetime = 5f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var health = other.GetComponent<PlayerHealth>();
            if (health != null) health.TakeDamage(damage);
            Destroy(gameObject);
        }

        if (other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}
```

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Enemies/EnemyBehavior.cs Assets/Scripts/Enemies/Projectile.cs Assets/Scripts/Enemies/EnemyData.cs Assets/Scripts/Enemies/EnemyHealth.cs
git commit -m "feat: add EnemyBehavior composition component, Projectile, and armor system"
```

---

## Task 12: World Interactables (Keys, Doors, Shortcuts, Shrines)

**Files:**
- Create: `Assets/Scripts/World/KeyGate.cs`
- Create: `Assets/Scripts/World/ShortcutDoor.cs`
- Create: `Assets/Scripts/World/Shrine.cs`
- Create: `Assets/Scripts/World/ItemPickup.cs`
- Create: `Assets/Scripts/World/ZoneGate.cs`

- [ ] **Step 1: Implement KeyGate**

Create `Assets/Scripts/World/KeyGate.cs`:

```csharp
using UnityEngine;

public class KeyGate : MonoBehaviour
{
    public string requiredKeyId;
    public GameObject doorVisual;

    private bool _opened;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_opened) return;
        if (!other.CompareTag("Player")) return;

        if (GameManager.Instance != null && GameManager.Instance.State.HasKey(requiredKeyId))
        {
            Open();
        }
    }

    private void Open()
    {
        _opened = true;
        if (doorVisual != null) doorVisual.SetActive(false);
        GetComponent<Collider2D>().enabled = false;
    }

    private void Start()
    {
        // Check if already opened (re-entering scene)
        if (GameManager.Instance != null && GameManager.Instance.State.HasKey(requiredKeyId))
            Open();
    }
}
```

- [ ] **Step 2: Implement ShortcutDoor**

Create `Assets/Scripts/World/ShortcutDoor.cs`:

```csharp
using UnityEngine;

public class ShortcutDoor : MonoBehaviour
{
    public string shortcutId;
    public GameObject doorVisual;
    public bool openFromThisSide = true; // can only be opened from one side initially

    private bool _unlocked;

    private void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.State.IsShortcutUnlocked(shortcutId))
            Unlock();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_unlocked) return;
        if (!other.CompareTag("Player")) return;
        if (!openFromThisSide) return;

        Unlock();
    }

    private void Unlock()
    {
        _unlocked = true;
        if (doorVisual != null) doorVisual.SetActive(false);
        GetComponent<Collider2D>().enabled = false;

        if (GameManager.Instance != null)
            GameManager.Instance.State.UnlockShortcut(shortcutId);
    }
}
```

- [ ] **Step 3: Implement Shrine**

Create `Assets/Scripts/World/Shrine.cs`:

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

public class Shrine : MonoBehaviour
{
    public string shrineId;
    public Transform respawnPoint;
    public GameObject activatedVFX;

    private bool _discovered;

    private void Start()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.State.discoveredShrines.Contains(shrineId))
        {
            _discovered = true;
            if (activatedVFX != null) activatedVFX.SetActive(true);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Activate();
    }

    private void Activate()
    {
        if (GameManager.Instance == null) return;

        _discovered = true;
        string sceneName = SceneManager.GetActiveScene().name;
        GameManager.Instance.State.RegisterShrine(shrineId, sceneName);

        if (activatedVFX != null) activatedVFX.SetActive(true);

        // Heal player on shrine activation
        var playerHealth = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerHealth>();
        if (playerHealth != null) playerHealth.Heal(playerHealth.maxHealth);
    }
}
```

- [ ] **Step 4: Implement ItemPickup**

Create `Assets/Scripts/World/ItemPickup.cs`:

```csharp
using UnityEngine;

public enum PickupType
{
    Attack,
    ComboBook,
    HealthPotion,
    Key
}

public class ItemPickup : MonoBehaviour
{
    public PickupType pickupType;
    public AttackData attackData;
    public ComboBookData comboBookData;
    public string keyId;
    public float healAmount = 25f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        switch (pickupType)
        {
            case PickupType.Attack:
                var inv = other.GetComponent<PlayerInventory>();
                if (inv != null && attackData != null) inv.AddAttack(attackData);
                break;

            case PickupType.ComboBook:
                var inv2 = other.GetComponent<PlayerInventory>();
                if (inv2 != null && comboBookData != null) inv2.AddComboBook(comboBookData);
                break;

            case PickupType.HealthPotion:
                var potions = other.GetComponent<PlayerPotions>();
                if (potions != null) potions.AddPotion();
                break;

            case PickupType.Key:
                if (GameManager.Instance != null)
                    GameManager.Instance.State.CollectKey(keyId);
                break;
        }

        Destroy(gameObject);
    }
}
```

- [ ] **Step 5: Implement ZoneGate**

Create `Assets/Scripts/World/ZoneGate.cs`:

```csharp
using UnityEngine;

public class ZoneGate : MonoBehaviour
{
    public GameObject lockedVisual;
    public GameObject unlockedVisual;
    public string targetScene = "BossArena";

    private bool _unlocked;

    private void Start()
    {
        CheckUnlocked();
    }

    private void Update()
    {
        if (!_unlocked) CheckUnlocked();
    }

    private void CheckUnlocked()
    {
        if (GameManager.Instance == null) return;
        _unlocked = GameManager.Instance.State.IsBossUnlocked;

        if (lockedVisual != null) lockedVisual.SetActive(!_unlocked);
        if (unlockedVisual != null) unlockedVisual.SetActive(_unlocked);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_unlocked) return;
        if (!other.CompareTag("Player")) return;

        UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
    }
}
```

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/World/KeyGate.cs Assets/Scripts/World/ShortcutDoor.cs Assets/Scripts/World/Shrine.cs Assets/Scripts/World/ItemPickup.cs Assets/Scripts/World/ZoneGate.cs
git commit -m "feat: add world interactables — KeyGate, ShortcutDoor, Shrine, ItemPickup, ZoneGate"
```

---

## Task 13: Scene Transition & Fade Screen

**Files:**
- Create: `Assets/Scripts/Core/SceneTransition.cs`
- Create: `Assets/Scripts/Core/FadeScreen.cs`

- [ ] **Step 1: Implement FadeScreen**

Create `Assets/Scripts/Core/FadeScreen.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeScreen : MonoBehaviour
{
    public Image fadeImage;
    public float fadeDuration = 0.5f;

    public static FadeScreen Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeImage != null)
        {
            fadeImage.color = new Color(0, 0, 0, 0);
            fadeImage.raycastTarget = false;
        }
    }

    public Coroutine FadeOut()
    {
        return StartCoroutine(Fade(0f, 1f));
    }

    public Coroutine FadeIn()
    {
        return StartCoroutine(Fade(1f, 0f));
    }

    private IEnumerator Fade(float from, float to)
    {
        if (fadeImage == null) yield break;

        fadeImage.raycastTarget = true;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, to);
        fadeImage.raycastTarget = (to > 0.5f);
    }
}
```

- [ ] **Step 2: Implement SceneTransition**

Create `Assets/Scripts/Core/SceneTransition.cs`:

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransition : MonoBehaviour
{
    public string targetScene;
    public string entryPointId;

    private bool _transitioning;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_transitioning) return;
        if (!other.CompareTag("Player")) return;

        LoadScene(targetScene);
    }

    public void LoadScene(string sceneName)
    {
        if (_transitioning) return;
        _transitioning = true;
        StartCoroutine(TransitionCoroutine(sceneName));
    }

    private IEnumerator TransitionCoroutine(string sceneName)
    {
        if (FadeScreen.Instance != null)
            yield return FadeScreen.Instance.FadeOut();

        SceneManager.LoadScene(sceneName);

        if (FadeScreen.Instance != null)
            yield return FadeScreen.Instance.FadeIn();

        _transitioning = false;
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Core/SceneTransition.cs Assets/Scripts/Core/FadeScreen.cs
git commit -m "feat: add SceneTransition with fade-to-black and FadeScreen overlay"
```

---

## Task 14: UI — Health Bar

**Files:**
- Create: `Assets/Scripts/UI/HealthBarUI.cs`

- [ ] **Step 1: Implement HealthBarUI**

Create `Assets/Scripts/UI/HealthBarUI.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public Image fillImage;

    private PlayerHealth _playerHealth;

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerHealth = player.GetComponent<PlayerHealth>();
            if (_playerHealth != null)
                _playerHealth.OnHealthChanged += UpdateBar;
        }
    }

    private void OnDestroy()
    {
        if (_playerHealth != null)
            _playerHealth.OnHealthChanged -= UpdateBar;
    }

    private void UpdateBar(float current, float max)
    {
        if (fillImage != null && max > 0f)
            fillImage.fillAmount = current / max;
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/UI/HealthBarUI.cs
git commit -m "feat: add HealthBarUI that binds to PlayerHealth events"
```

---

## Task 15: UI — Combo Book Editor

**Files:**
- Create: `Assets/Scripts/UI/ComboBookUI.cs`
- Create: `Assets/Scripts/UI/ComboSlotUI.cs`
- Create: `Assets/Scripts/UI/AttackCardUI.cs`
- Create: `Assets/Scripts/UI/HarmonyPreviewUI.cs`

- [ ] **Step 1: Implement AttackCardUI**

Create `Assets/Scripts/UI/AttackCardUI.cs`:

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class AttackCardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI tagText;
    public TextMeshProUGUI damageText;

    public AttackData AttackData { get; private set; }

    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Vector2 _originalPosition;
    private Transform _originalParent;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Setup(AttackData data)
    {
        AttackData = data;
        if (nameText != null) nameText.text = data.attackName;
        if (tagText != null)
        {
            string tags = data.primaryTag.ToString();
            if (data.secondaryTag != AttackTag.None)
                tags += " / " + data.secondaryTag.ToString();
            tagText.text = tags;
        }
        if (damageText != null) damageText.text = $"DMG: {data.baseDamage}";
    }

    private Canvas _rootCanvas;

    public void OnBeginDrag(PointerEventData eventData)
    {
        _originalPosition = _rectTransform.anchoredPosition;
        _originalParent = transform.parent;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.alpha = 0.7f;
        _rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        transform.SetParent(transform.root);
    }

    public void OnDrag(PointerEventData eventData)
    {
        float scaleFactor = _rootCanvas != null ? _rootCanvas.scaleFactor : 1f;
        _rectTransform.anchoredPosition += eventData.delta / scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha = 1f;
        transform.SetParent(_originalParent);
        _rectTransform.anchoredPosition = _originalPosition;
    }
}
```

- [ ] **Step 2: Implement ComboSlotUI**

Create `Assets/Scripts/UI/ComboSlotUI.cs`:

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System;

public class ComboSlotUI : MonoBehaviour, IDropHandler
{
    public int slotIndex;
    public Image slotBackground;
    public TextMeshProUGUI attackNameText;
    public TextMeshProUGUI harmonyText;

    public event Action<int, AttackData> OnAttackDropped;

    private AttackData _currentAttack;

    public void SetAttack(AttackData attack)
    {
        _currentAttack = attack;
        if (attackNameText != null)
            attackNameText.text = attack != null ? attack.attackName : "Empty";
    }

    public void SetHarmonyDisplay(HarmonyLevel level)
    {
        if (harmonyText == null) return;

        switch (level)
        {
            case HarmonyLevel.Harmonious:
                harmonyText.text = "Harmonious";
                harmonyText.color = Color.green;
                break;
            case HarmonyLevel.Dissonant:
                harmonyText.text = "Dissonant";
                harmonyText.color = Color.red;
                break;
            default:
                harmonyText.text = "Neutral";
                harmonyText.color = Color.gray;
                break;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        var card = eventData.pointerDrag?.GetComponent<AttackCardUI>();
        if (card == null || card.AttackData == null) return;

        SetAttack(card.AttackData);
        OnAttackDropped?.Invoke(slotIndex, card.AttackData);
    }
}
```

- [ ] **Step 3: Implement HarmonyPreviewUI**

Create `Assets/Scripts/UI/HarmonyPreviewUI.cs`:

```csharp
using UnityEngine;
using TMPro;

public class HarmonyPreviewUI : MonoBehaviour
{
    public TextMeshProUGUI totalDamageText;
    public TextMeshProUGUI overallHarmonyText;

    public void UpdatePreview(ComboBookData book, HarmonyTable table)
    {
        if (book == null || table == null) return;

        var attacks = new AttackData[book.SlotCount];
        for (int i = 0; i < book.SlotCount; i++)
            attacks[i] = book.GetAttack(i);

        float[] multipliers = HarmonyCalculator.CalculateComboMultipliers(table, attacks);

        float totalDamage = 0f;
        int harmoniousCount = 0;
        int dissonantCount = 0;

        for (int i = 0; i < attacks.Length; i++)
        {
            if (attacks[i] == null) continue;
            totalDamage += attacks[i].baseDamage * multipliers[i];

            if (i > 0 && attacks[i - 1] != null)
            {
                var h = HarmonyCalculator.GetBestHarmony(table, attacks[i - 1], attacks[i]);
                if (h == HarmonyLevel.Harmonious) harmoniousCount++;
                else if (h == HarmonyLevel.Dissonant) dissonantCount++;
            }
        }

        if (totalDamageText != null)
            totalDamageText.text = $"Total: {totalDamage:F0}";

        if (overallHarmonyText != null)
        {
            if (dissonantCount > 0)
            {
                overallHarmonyText.text = "Flow: Disrupted";
                overallHarmonyText.color = Color.red;
            }
            else if (harmoniousCount > 0)
            {
                overallHarmonyText.text = "Flow: Harmonious";
                overallHarmonyText.color = Color.green;
            }
            else
            {
                overallHarmonyText.text = "Flow: Neutral";
                overallHarmonyText.color = Color.gray;
            }
        }
    }
}
```

- [ ] **Step 4: Implement ComboBookUI**

Create `Assets/Scripts/UI/ComboBookUI.cs`:

```csharp
using UnityEngine;
using System.Collections.Generic;

public class ComboBookUI : MonoBehaviour
{
    public GameObject panel;
    public Transform slotContainer;
    public Transform attackListContainer;
    public ComboSlotUI slotPrefab;
    public AttackCardUI cardPrefab;
    public HarmonyPreviewUI harmonyPreview;

    private PlayerInventory _inventory;
    private ComboBookData _activeBook;
    private List<ComboSlotUI> _slots = new List<ComboSlotUI>();
    private List<AttackCardUI> _cards = new List<AttackCardUI>();
    private bool _isOpen;

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _inventory = player.GetComponent<PlayerInventory>();

        if (panel != null) panel.SetActive(false);
    }

    public void Toggle()
    {
        if (_isOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (_inventory == null) return;
        _activeBook = _inventory.equippedBook;
        if (_activeBook == null) return;

        _isOpen = true;
        if (panel != null) panel.SetActive(true);
        Time.timeScale = 0f; // Pause game while editing combos

        BuildSlots();
        BuildAttackList();
        UpdateHarmonyPreview();
    }

    public void Close()
    {
        _isOpen = false;
        if (panel != null) panel.SetActive(false);
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        // Safety: ensure timeScale is restored if UI is destroyed while open
        if (_isOpen) Time.timeScale = 1f;
    }

    private void BuildSlots()
    {
        foreach (var slot in _slots) Destroy(slot.gameObject);
        _slots.Clear();

        for (int i = 0; i < _activeBook.SlotCount; i++)
        {
            var slot = Instantiate(slotPrefab, slotContainer);
            slot.slotIndex = i;
            slot.SetAttack(_activeBook.GetAttack(i));
            slot.OnAttackDropped += OnSlotChanged;
            _slots.Add(slot);
        }
    }

    private void BuildAttackList()
    {
        foreach (var card in _cards) Destroy(card.gameObject);
        _cards.Clear();

        foreach (var attack in _inventory.attacks)
        {
            var card = Instantiate(cardPrefab, attackListContainer);
            card.Setup(attack);
            _cards.Add(card);
        }
    }

    private void OnSlotChanged(int slotIndex, AttackData attack)
    {
        _activeBook.SetAttack(slotIndex, attack);
        UpdateHarmonyPreview();

        // Re-cache multipliers in executor
        var executor = _inventory.GetComponent<ComboExecutor>();
        if (executor != null) executor.EquipBook(_activeBook);
    }

    private void UpdateHarmonyPreview()
    {
        if (harmonyPreview == null || GameManager.Instance == null) return;
        harmonyPreview.UpdatePreview(_activeBook, GameManager.Instance.harmonyTable);

        // Update per-slot harmony display
        for (int i = 0; i < _slots.Count; i++)
        {
            if (i == 0)
            {
                _slots[i].SetHarmonyDisplay(HarmonyLevel.Neutral);
                continue;
            }

            var prev = _activeBook.GetAttack(i - 1);
            var curr = _activeBook.GetAttack(i);
            if (prev != null && curr != null)
            {
                var h = HarmonyCalculator.GetBestHarmony(
                    GameManager.Instance.harmonyTable, prev, curr);
                _slots[i].SetHarmonyDisplay(h);
            }
            else
            {
                _slots[i].SetHarmonyDisplay(HarmonyLevel.Neutral);
            }
        }
    }
}
```

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/UI/AttackCardUI.cs Assets/Scripts/UI/ComboSlotUI.cs Assets/Scripts/UI/HarmonyPreviewUI.cs Assets/Scripts/UI/ComboBookUI.cs
git commit -m "feat: add Combo Book UI with drag-drop slots, attack cards, and harmony preview"
```

---

## Task 16: Input Wiring

**Files:**
- Create: `Assets/Scripts/Player/PlayerInput.cs`

- [ ] **Step 1: Implement PlayerInput**

Create `Assets/Scripts/Player/PlayerInput.cs`:

```csharp
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerCombat))]
public class PlayerInput : MonoBehaviour
{
    private PlayerController _controller;
    private PlayerCombat _combat;
    private PlayerPotions _potions;
    private ComboBookUI _comboBookUI;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _combat = GetComponent<PlayerCombat>();
        _potions = GetComponent<PlayerPotions>();
    }

    private void Start()
    {
        _comboBookUI = FindObjectOfType<ComboBookUI>();
    }

    private void Update()
    {
        // Movement
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        _controller.SetMoveInput(new Vector2(h, v));

        // Attack
        if (Input.GetButtonDown("Fire1"))
        {
            _combat.TryAttack();
        }

        // Dodge (with dodge-cancel support)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _combat.TryDodgeCancel(); // cancel current attack if attacking
            _controller.StartDodge();
        }

        // Use health potion
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (_potions != null) _potions.UsePotion();
        }

        // Combo Book UI toggle
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (_comboBookUI != null) _comboBookUI.Toggle();
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/Player/PlayerInput.cs
git commit -m "feat: add PlayerInput wiring keyboard to controller, combat, dodge, and UI"
```

---

## Task 17: PlayerPotions & ComboBookHUD

**Files:**
- Create: `Assets/Scripts/Player/PlayerPotions.cs`
- Create: `Assets/Scripts/UI/ComboBookHUD.cs`

- [ ] **Step 1: Implement PlayerPotions**

Create `Assets/Scripts/Player/PlayerPotions.cs`:

```csharp
using UnityEngine;
using System;

public class PlayerPotions : MonoBehaviour
{
    public int maxPotions = 5;
    public float healAmount = 40f;

    public int CurrentPotions { get; private set; }

    public event Action<int> OnPotionCountChanged;

    private PlayerHealth _health;

    private void Awake()
    {
        _health = GetComponent<PlayerHealth>();
    }

    public void AddPotion()
    {
        if (CurrentPotions >= maxPotions) return;
        CurrentPotions++;
        OnPotionCountChanged?.Invoke(CurrentPotions);
    }

    public void UsePotion()
    {
        if (CurrentPotions <= 0) return;
        if (_health == null) return;
        if (_health.CurrentHealth >= _health.maxHealth) return;

        CurrentPotions--;
        _health.Heal(healAmount);
        OnPotionCountChanged?.Invoke(CurrentPotions);
    }
}
```

- [ ] **Step 2: Implement ComboBookHUD**

Create `Assets/Scripts/UI/ComboBookHUD.cs`:

```csharp
using UnityEngine;
using TMPro;

public class ComboBookHUD : MonoBehaviour
{
    public TextMeshProUGUI bookNameText;
    public TextMeshProUGUI potionCountText;

    private PlayerInventory _inventory;
    private PlayerPotions _potions;

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _inventory = player.GetComponent<PlayerInventory>();
            _potions = player.GetComponent<PlayerPotions>();

            if (_potions != null)
                _potions.OnPotionCountChanged += UpdatePotionCount;
        }

        UpdateDisplay();
    }

    private void OnDestroy()
    {
        if (_potions != null)
            _potions.OnPotionCountChanged -= UpdatePotionCount;
    }

    private void UpdateDisplay()
    {
        if (_inventory != null && _inventory.equippedBook != null && bookNameText != null)
            bookNameText.text = _inventory.equippedBook.bookName;
        else if (bookNameText != null)
            bookNameText.text = "No Book";

        if (_potions != null && potionCountText != null)
            potionCountText.text = $"Potions: {_potions.CurrentPotions}";
    }

    private void UpdatePotionCount(int count)
    {
        if (potionCountText != null)
            potionCountText.text = $"Potions: {count}";
    }

    // Call this when the player equips a new book
    public void RefreshBookName()
    {
        UpdateDisplay();
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Player/PlayerPotions.cs Assets/Scripts/UI/ComboBookHUD.cs
git commit -m "feat: add PlayerPotions inventory system and ComboBookHUD indicator"
```

---

## Task 18: Create ScriptableObject Data Assets

**Files:**
- Create: 12 AttackData assets in `Assets/Data/Attacks/`
- Create: 6 ComboBookData assets in `Assets/Data/ComboBooks/`
- Create: 4 EnemyData assets in `Assets/Data/Enemies/`
- Create: 1 HarmonyTable asset in `Assets/Data/`

This task is done **in the Unity Editor** via right-click → Create → Game → [type]. Below are the values to enter for each asset.

- [ ] **Step 1: Create HarmonyTable asset**

Right-click `Assets/Data/` → Create → Game → Harmony Table. Name: `HarmonyTable`.

Set entries:

| From | To | Level |
|---|---|---|
| Rising | Slam | Harmonious |
| Slam | Sweep | Harmonious |
| Sweep | Overhead | Harmonious |
| Overhead | Thrust | Harmonious |
| Thrust | Rising | Harmonious |
| Spinning | Sweep | Harmonious |
| Sweep | Spinning | Harmonious |
| Slam | Rising | Dissonant |
| Rising | Rising | Dissonant |
| Slam | Slam | Dissonant |
| Overhead | Overhead | Dissonant |
| Spinning | Spinning | Dissonant |

- [ ] **Step 2: Create 12 AttackData assets**

Right-click `Assets/Data/Attacks/` → Create → Game → Attack Data for each:

| Name | Primary Tag | Secondary | Damage | Speed | Movement |
|---|---|---|---|---|---|
| Quick Slash | Sweep | None | 8 | Fast | HoldPosition |
| Lunging Thrust | Thrust | None | 12 | Medium | LungeForward |
| Overhead Chop | Overhead | None | 15 | Slow | HoldPosition |
| Rising Cut | Rising | None | 10 | Medium | HoldPosition |
| Ground Slam | Slam | None | 18 | Slow | HoldPosition |
| Spinning Slash | Spinning | None | 14 | Medium | CircleArc |
| Viper Strike | Thrust | Rising | 11 | Fast | LungeForward |
| Reaping Arc | Sweep | Slam | 16 | Slow | CircleArc |
| Skyward Rend | Rising | Overhead | 13 | Medium | HoldPosition |
| Whirlwind | Spinning | Sweep | 12 | Medium | CircleArc |
| Executioner | Overhead | Slam | 20 | Slow | LungeForward |
| Shadow Step | Thrust | Spinning | 9 | Fast | PullBack |

- [ ] **Step 3: Create 6 ComboBookData assets**

| Name | Rarity | Pre-configured? | Attacks |
|---|---|---|---|
| Tattered Manual | Common | Yes | Quick Slash, Lunging Thrust |
| Blank Tome | Common | No | Empty |
| Knight's Codex | Rare | Yes | Rising Cut, Ground Slam, Quick Slash |
| Wanderer's Journal | Rare | No | Empty |
| Forgotten Manuscript | Rare | No | Empty |
| Legendary Swordmaster's Opus | Legendary | Yes | Rising Cut, Ground Slam, Sweep (Quick Slash), Overhead Chop |

- [ ] **Step 4: Create 4 EnemyData assets**

| Name | HP | Speed | Damage | Range | Cooldown | Detection | Stagger Threshold | Armor | Armor Off When Staggered |
|---|---|---|---|---|---|---|---|---|---|
| Hollow | 30 | 1.5 | 8 | 1.0 | 2.0 | 5 | 15 | 0 | No |
| Wraith | 20 | 4.0 | 12 | 1.2 | 1.0 | 7 | 10 | 0 | No |
| Knight | 80 | 1.0 | 15 | 1.5 | 2.5 | 5 | 40 | 0.5 | Yes |
| Caster | 25 | 1.5 | 10 | 6.0 | 2.0 | 8 | 12 | 0 | No |

**Enemy prefab setup (composition-based):**
- **Hollow**: `EnemyStateMachine` + `EnemyHealth` + EnemyData(Hollow). No `EnemyBehavior` needed (default melee in state machine).
- **Wraith**: `EnemyStateMachine` + `EnemyHealth` + `EnemyBehavior(DashRetreat)` + EnemyData(Wraith).
- **Knight**: `EnemyStateMachine` + `EnemyHealth` + EnemyData(Knight, armor=0.5, armorDisabledDuringStagger=true). No `EnemyBehavior` needed.
- **Caster**: `EnemyStateMachine` + `EnemyHealth` + `EnemyBehavior(Ranged, projectilePrefab)` + EnemyData(Caster).

- [ ] **Step 5: Commit**

```bash
git add Assets/Data/
git commit -m "feat: create all ScriptableObject data assets — attacks, combo books, enemies, harmony table"
```

---

## Task 19: Player Prefab Assembly

This task is done **in the Unity Editor**.

- [ ] **Step 1: Create Player GameObject**

1. Create empty GameObject named "Player"
2. Set Tag to "Player"
3. Add components:
   - `SpriteRenderer` (assign placeholder sprite)
   - `Rigidbody2D` (gravity scale = 0, freeze rotation Z)
   - `BoxCollider2D` (size to sprite)
   - `PlayerController`
   - `PlayerHealth` (maxHealth = 100)
   - `PlayerInventory`
   - `PlayerCombat` (attackRange = 1.2, enemyLayer = Enemy)
   - `ComboExecutor` (chainWindowDuration = 0.5, assign HarmonyTable)
   - `PlayerPotions` (maxPotions = 5, healAmount = 40)
   - `PlayerInput`
4. Add child `Light2D` (Point Light, range 4, intensity 0.5, warm white)
5. Save as prefab to `Assets/Prefabs/Player/Player.prefab`

- [ ] **Step 2: Create Enemy Prefabs**

For each enemy type (Hollow, Wraith, Knight, Caster):
1. Create empty GameObject named after type
2. Set Tag to "Enemy", Layer to "Enemy"
3. Add components:
   - `SpriteRenderer` (placeholder sprite)
   - `Rigidbody2D` (gravity scale = 0, freeze rotation Z)
   - `BoxCollider2D`
   - `EnemyHealth` (assign matching EnemyData)
   - `EnemyStateMachine` (assign matching EnemyData)
   - `EnemyBehavior` (only for Wraith: DashRetreat, Caster: Ranged with projectile prefab)
4. Assign matching `EnemyData` asset to the `data` field
5. For Caster: create a Projectile prefab (Sprite + Rigidbody2D + CircleCollider2D (trigger) + `Projectile` script)
6. Save each as prefab to `Assets/Prefabs/Enemies/`

- [ ] **Step 3: Create Interactable Prefabs**

Create prefabs for: KeyGate, ShortcutDoor, Shrine, ItemPickup
- Each needs: SpriteRenderer, BoxCollider2D (trigger), respective script
- Shrine: add a child particle system (deactivated by default) for activatedVFX
- Save to `Assets/Prefabs/Interactables/`

- [ ] **Step 4: Commit**

```bash
git add Assets/Prefabs/
git commit -m "feat: assemble Player, Enemy, and Interactable prefabs"
```

---

## Task 20: Zone A Scene Setup

This task is done **in the Unity Editor** using Tilemap.

- [ ] **Step 1: Create ZoneA scene**

1. File → New Scene → save as `Assets/Scenes/ZoneA.unity`
2. Add Tilemap GameObject (Grid → Tilemap) with layers: Ground, Walls, Decoration
3. Configure Tilemap Collider 2D + Composite Collider 2D on Walls layer
4. Paint a starting area layout:
   - Central spawn room
   - Main corridor branching north (to Zone B connector) and east (to Zone C connector)
   - 2 side rooms off the main corridor (for key + attack pickups)
   - 1 shortcut loop connecting a side room back to spawn
5. Add Global Light 2D (intensity 0.15, dark ambient)
6. Add Point Light 2D on torches (intensity 1, range 3, warm orange)

- [ ] **Step 2: Place interactables**

1. Place Player prefab at spawn point
2. Place 2 Shrine prefabs (one near spawn, one deeper)
3. Place 2 KeyGate prefabs (blocking main path, with unique keyIds)
4. Place 2 ItemPickup prefabs (keys matching the gates, in side rooms)
5. Place 2 ItemPickup prefabs (attacks: Quick Slash, Rising Cut)
6. Place 1 ItemPickup prefab (Tattered Manual combo book)
7. Place 1 ShortcutDoor prefab
8. Place 3-5 Hollow enemies along corridors

- [ ] **Step 3: Add SceneTransition triggers**

1. Place SceneTransition trigger at north edge (targetScene = "ZoneB")
2. Place SceneTransition trigger at east edge (targetScene = "ZoneC")

- [ ] **Step 4: Add Cinemachine**

1. Add Cinemachine 2D Camera (follow Player)
2. Add CinemachineConfiner2D with collider bounds matching the zone extents
3. Set damping to (0.2, 0.2, 0)

- [ ] **Step 5: Add UI Canvas**

1. Create Canvas (Screen Space - Overlay)
2. Add HealthBarUI with fill image (top-left)
3. Add ComboBookHUD (bottom-left) — book name text + potion count text
4. Add ComboBookUI panel (hidden by default)
5. Add FadeScreen overlay image (full screen, black, alpha 0)

- [ ] **Step 6: Add GameManager**

1. Create empty GameObject "GameManager"
2. Add GameManager component, assign HarmonyTable
3. This only needs to exist in the first loaded scene (Zone A or MainMenu)

- [ ] **Step 7: Commit**

```bash
git add Assets/Scenes/ZoneA.unity
git commit -m "feat: build Zone A scene with tilemap, interactables, enemies, and UI"
```

---

## Task 21: Zone B & Zone C Scene Setup

- [ ] **Step 1: Create ZoneB scene**

Same process as Zone A but with Catacombs aesthetic:
- Tighter corridors, bone-themed tileset
- Blue-green point lights
- Place: 2 shrines, 2 key gates, matching key pickups, 2-3 attack pickups
- Enemies: mix of Hollow + Wraith
- ShortcutDoor back to Zone A
- SceneTransition to Zone C (east) and Zone A (south)
- Place a zone-clear trigger at the end (sets `GameManager.Instance.State.zoneBCleared = true`)

- [ ] **Step 2: Create ZoneC scene**

Same process with Cursed Chapel aesthetic:
- Wider rooms with fallen pillars, crimson point lights
- Place: 2 shrines, 2 key gates, matching key pickups, 2-3 attack pickups, 1 Legendary combo book
- Enemies: mix of Knight + Caster
- ShortcutDoor back to Zone A
- SceneTransition to Zone B (west) and Zone A (south)
- Place a zone-clear trigger at the end (sets `GameManager.Instance.State.zoneCCleared = true`)
- Place ZoneGate leading to BossArena

- [ ] **Step 3: Create zone-clear trigger script**

Create `Assets/Scripts/World/ZoneClearTrigger.cs`:

```csharp
using UnityEngine;

public class ZoneClearTrigger : MonoBehaviour
{
    public enum Zone { B, C }
    public Zone zone;

    private bool _triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;

        _triggered = true;

        if (GameManager.Instance == null) return;

        switch (zone)
        {
            case Zone.B:
                GameManager.Instance.State.zoneBCleared = true;
                break;
            case Zone.C:
                GameManager.Instance.State.zoneCCleared = true;
                break;
        }
    }
}
```

- [ ] **Step 4: Commit**

```bash
git add Assets/Scenes/ZoneB.unity Assets/Scenes/ZoneC.unity Assets/Scripts/World/ZoneClearTrigger.cs
git commit -m "feat: build Zone B (Catacombs) and Zone C (Cursed Chapel) scenes"
```

---

## Task 22: Boss Arena & Boss Fight

**Files:**
- Create: `Assets/Scripts/Enemies/BossController.cs`
- Create: `Assets/Scenes/BossArena.unity`

- [ ] **Step 1: Implement BossController**

Create `Assets/Scripts/Enemies/BossController.cs`:

```csharp
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(Rigidbody2D))]
public class BossController : MonoBehaviour
{
    public enum BossPhase { One, Two }

    [Header("Phase 1")]
    public float p1MoveSpeed = 2f;
    public float p1AttackDamage = 15f;
    public float p1AttackCooldown = 2f;
    public float p1ChargeSpeed = 8f;

    [Header("Phase 2 (below 50% HP)")]
    public float p2MoveSpeed = 3.5f;
    public float p2AttackDamage = 20f;
    public float p2AttackCooldown = 1.2f;
    public GameObject p2ProjectilePrefab;
    public float p2ProjectileSpeed = 6f;

    [Header("Detection")]
    public float attackRange = 1.5f;
    public float chargeRange = 5f;

    public BossPhase CurrentPhase { get; private set; } = BossPhase.One;

    private EnemyHealth _health;
    private Rigidbody2D _rb;
    private Transform _player;
    private float _attackTimer;
    private bool _isCharging;
    private bool _isDead;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
        _health = GetComponent<EnemyHealth>();
    }

    private void Start()
    {
        _health.OnDeath += OnBossDeath;
        _health.OnHealthChanged += CheckPhaseTransition;

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;
    }

    private void Update()
    {
        if (_isDead || _player == null) return;

        float dist = Vector2.Distance(transform.position, _player.position);
        _attackTimer -= Time.deltaTime;

        float speed = CurrentPhase == BossPhase.One ? p1MoveSpeed : p2MoveSpeed;
        float cooldown = CurrentPhase == BossPhase.One ? p1AttackCooldown : p2AttackCooldown;

        if (_isCharging) return;

        if (dist <= attackRange && _attackTimer <= 0f)
        {
            MeleeAttack();
            _attackTimer = cooldown;
        }
        else if (CurrentPhase == BossPhase.Two && dist > chargeRange && _attackTimer <= 0f)
        {
            StartCoroutine(ChargeAttack());
            _attackTimer = cooldown * 1.5f;
        }
        else
        {
            Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
            _rb.velocity = dir * speed;
        }
    }

    private void MeleeAttack()
    {
        float damage = CurrentPhase == BossPhase.One ? p1AttackDamage : p2AttackDamage;
        float dist = Vector2.Distance(transform.position, _player.position);

        if (dist <= attackRange)
        {
            var playerHealth = _player.GetComponent<PlayerHealth>();
            if (playerHealth != null) playerHealth.TakeDamage(damage);
        }
    }

    private IEnumerator ChargeAttack()
    {
        _isCharging = true;
        _rb.velocity = Vector2.zero;

        // Telegraph: pause before charging
        yield return new WaitForSeconds(0.5f);

        Vector2 chargeDir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
        float chargeTime = 0.4f;
        float elapsed = 0f;

        while (elapsed < chargeTime)
        {
            _rb.velocity = chargeDir * p1ChargeSpeed;
            elapsed += Time.deltaTime;

            float dist = Vector2.Distance(transform.position, _player.position);
            if (dist <= attackRange)
            {
                var playerHealth = _player.GetComponent<PlayerHealth>();
                if (playerHealth != null) playerHealth.TakeDamage(p2AttackDamage * 1.5f);
                break;
            }

            yield return null;
        }

        _rb.velocity = Vector2.zero;
        _isCharging = false;
    }

    private void CheckPhaseTransition(float current, float max)
    {
        if (current <= max * 0.5f && CurrentPhase == BossPhase.One)
        {
            CurrentPhase = BossPhase.Two;
            // Phase transition visual/audio cue would go here
        }
    }

    private void OnBossDeath()
    {
        _isDead = true;
        _rb.velocity = Vector2.zero;
        // Victory state — could trigger end screen or cutscene
    }
}
```

- [ ] **Step 2: Build BossArena scene**

1. File → New Scene → save as `Assets/Scenes/BossArena.unity`
2. Tilemap: large circular open room, minimal obstacles
3. Global Light 2D very dim (intensity 0.1)
4. Place Player prefab at entrance
5. Place Boss prefab at center (with EnemyHealth + BossController, assign EnemyData with high HP like 200)
6. Add dramatic point lights that intensify when boss fight starts
7. Add Cinemachine camera following player, confiner to arena bounds
8. Add UI Canvas (health bar, combo book)

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Enemies/BossController.cs Assets/Scenes/BossArena.unity
git commit -m "feat: add BossController with two-phase fight and Boss Arena scene"
```

---

## Task 23: Main Menu Scene

**Files:**
- Create: `Assets/Scripts/UI/MainMenuUI.cs`
- Create: `Assets/Scenes/MainMenu.unity`

- [ ] **Step 1: Implement MainMenuUI**

Create `Assets/Scripts/UI/MainMenuUI.cs`:

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("ZoneA");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
```

- [ ] **Step 2: Build MainMenu scene**

1. File → New Scene → save as `Assets/Scenes/MainMenu.unity`
2. Create Canvas with:
   - Title text: game name (TextMeshPro, large, centered)
   - "Start" button → calls `MainMenuUI.StartGame()`
   - "Quit" button → calls `MainMenuUI.QuitGame()`
3. Dark background with atmospheric lighting
4. Add GameManager object with GameManager component (so it persists into gameplay)

- [ ] **Step 3: Set build order**

File → Build Settings → add scenes in order:
1. MainMenu
2. ZoneA
3. ZoneB
4. ZoneC
5. BossArena

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/UI/MainMenuUI.cs Assets/Scenes/MainMenu.unity
git commit -m "feat: add MainMenu scene with start/quit and build settings"
```

---

## Task 24: Polish — VFX, Screen Shake, Death Effects

**Files:**
- Create: `Assets/Scripts/Core/ScreenShake.cs`

- [ ] **Step 1: Implement ScreenShake**

Create `Assets/Scripts/Core/ScreenShake.cs`:

```csharp
using UnityEngine;
using Cinemachine;

public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance { get; private set; }

    private CinemachineImpulseSource _impulse;

    private void Awake()
    {
        Instance = this;
        _impulse = GetComponent<CinemachineImpulseSource>();
    }

    public void Shake(float force = 1f)
    {
        if (_impulse != null)
            _impulse.GenerateImpulse(force);
    }
}
```

- [ ] **Step 2: Add CinemachineImpulseSource to camera**

On the Cinemachine camera object in each scene:
1. Add `CinemachineImpulseSource` component
2. Add `CinemachineImpulseListener` to the virtual camera
3. Add `ScreenShake` component

- [ ] **Step 3: Wire screen shake into combat**

Update `PlayerCombat.DealDamage` to call:
```csharp
if (hits.Length > 0 && ScreenShake.Instance != null)
    ScreenShake.Instance.Shake(0.3f);
```

Update `PlayerHealth.TakeDamage` to call:
```csharp
if (ScreenShake.Instance != null)
    ScreenShake.Instance.Shake(0.5f);
```

- [ ] **Step 4: Add enemy death effect**

Update `EnemyStateMachine.SetState` death case:
```csharp
if (state == EnemyState.Dead)
{
    _rb.velocity = Vector2.zero;
    GetComponent<Collider2D>().enabled = false;
    StartCoroutine(DeathFade());
}
```

Add to `EnemyStateMachine`:
```csharp
private System.Collections.IEnumerator DeathFade()
{
    var sr = GetComponent<SpriteRenderer>();
    if (sr == null) yield break;

    float duration = 0.5f;
    float elapsed = 0f;
    Color original = sr.color;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        sr.color = new Color(original.r, original.g, original.b, 1f - (elapsed / duration));
        yield return null;
    }

    Destroy(gameObject);
}
```

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Core/ScreenShake.cs Assets/Scripts/Player/PlayerCombat.cs Assets/Scripts/Player/PlayerHealth.cs Assets/Scripts/Enemies/EnemyStateMachine.cs
git commit -m "feat: add screen shake, enemy death fade, and combat feedback polish"
```

---

## Task 25: Final Integration Test & Playtest

- [ ] **Step 1: Run all EditMode tests**

Unity → Test Runner → EditMode → Run All
Expected: All tests PASS (AttackData, HarmonyCalculator, ComboBookData, GameState)

- [ ] **Step 2: Run all PlayMode tests**

Unity → Test Runner → PlayMode → Run All
Expected: All tests PASS (PlayerController, PlayerHealth, ComboExecutor)

- [ ] **Step 3: Manual playtest checklist**

Play from MainMenu scene and verify:

- [ ] Player spawns in Zone A and can move 8 directions
- [ ] Player can dodge roll with Space
- [ ] Player can attack with left click, combo chains execute in sequence
- [ ] Combo Book opens with Tab, attacks can be dragged into slots
- [ ] Harmony preview updates correctly when placing attacks
- [ ] Health potions can be collected and used with Q key
- [ ] Combo Book HUD shows equipped book name and potion count
- [ ] Dodge cancels current attack (with brief recovery)
- [ ] Attack movement patterns work (lunge forward on thrust attacks, etc.)
- [ ] Keys are collected and gates open
- [ ] Shortcuts unlock and persist when re-entering a zone
- [ ] Shrines activate and become respawn points
- [ ] Enemies patrol, chase, attack, and stagger
- [ ] Player death respawns at last shrine
- [ ] Scene transitions work between all zones (A↔B, A↔C, B↔C)
- [ ] Zone B clear trigger works
- [ ] Zone C clear trigger works
- [ ] Boss gate opens after both zones cleared
- [ ] Boss fight has two phases (phase 2 at 50% HP)
- [ ] Boss death triggers (game complete state)

- [ ] **Step 4: Final commit**

```bash
git add -A
git commit -m "feat: complete dark fantasy top-down action game — all systems integrated"
```
