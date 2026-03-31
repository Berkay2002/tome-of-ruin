# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Tome of Ruin** — dark fantasy top-down 2D action game.
Unity 2022.3 LTS, URP 2D, C#, Cinemachine, Tilemap, Unity Test Framework (NUnit).

## Architecture

- **MonoBehaviour + ScriptableObject composition.** No ECS/DOTS — explicitly rejected.
- **Enemies use composition, not inheritance.** Single `EnemyStateMachine` + optional `EnemyBehavior` component. Never create enemy subclasses.
- **Data-driven design.** All game content (attacks, combo books, enemies, harmony rules) as ScriptableObjects.
- **Enum-based state machines** for player (`PlayerState`) and enemies (`EnemyState`).
- **Scene-per-zone** with persistent `GameManager` singleton (`DontDestroyOnLoad`).

## Unity 2022.3 LTS API Constraints

- Use `Rigidbody2D.velocity` — `linearVelocity` does NOT exist in 2022.3
- Use `FindObjectOfType<T>()` — `FindFirstObjectByType<T>()` does NOT exist in 2022.3
- Use `Rigidbody2D.freezeRotation` — not `constraints` enum

## Code Style

- One class/enum per file (small related enums can share a file, e.g. `AttackTag.cs`)
- `_camelCase` for private fields, `PascalCase` for public fields/properties/methods
- `[Header("Section")]` attributes to group Inspector fields
- `[RequireComponent]` attribute when a MonoBehaviour depends on another component
- `[CreateAssetMenu]` attribute on all ScriptableObjects

## First-Time Unity Setup

1. Open project folder in **Unity 2022.3 LTS** (URP template)
2. Run **Tools > Generate Data Assets** — creates all SOs (attacks, combo books, enemies, harmony table)
3. Run **Tools > Generate Prefabs** — creates Player, 4 enemies, Projectile, 4 interactables
4. Run **Tools > Generate Scenes** — creates ZoneA, ZoneB, ZoneC, BossArena, MainMenu
5. File > Build Settings — add all 5 scenes (MainMenu, ZoneA, ZoneB, ZoneC, BossArena)
6. Edit > Project Settings > Tags and Layers — add `"Enemy"` tag and `"Enemy"` layer
7. On Player prefab: set `PlayerCombat.enemyLayer` to the Enemy layer
8. On Player prefab: assign `HarmonyTable` asset to `ComboExecutor.harmonyTable`

## Testing

- Tests use Unity Test Framework (NUnit) via EditMode and PlayMode assembly definitions
- EditMode tests: `Assets/Tests/EditMode/` — pure logic (HarmonyCalculator, ComboBookData, GameState, AttackData)
- PlayMode tests: `Assets/Tests/PlayMode/` — MonoBehaviour tests (PlayerController, PlayerHealth, ComboExecutor)
- Use `ScriptableObject.CreateInstance<T>()` to create test SOs, `Object.DestroyImmediate()` in TearDown

## Design Vision

- **Dark Souls 1-2 inspired** progression — linear but branching, interconnected zones, shortcuts, keys in side rooms
- **Small/focused scope** — prototype-first mentality, don't over-engineer
- **AI-driven development** — minimize human interaction, maximize what agents can generate reliably
- **AI-generated art** — sprites and assets are AI-generated, 3/4 top-down perspective, dark desaturated palette

## Design References

- `docs/superpowers/specs/2026-03-31-dark-fantasy-topdown-design.md`
- `docs/superpowers/plans/2026-03-31-dark-fantasy-topdown-plan.md`
