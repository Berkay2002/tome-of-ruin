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

## Editor Setup Tools

Unity is not installed on the dev server. Three editor scripts auto-generate assets:
- **Tools > Generate Data Assets** — creates all SOs (attacks, combo books, enemies, harmony table)
- **Tools > Generate Prefabs** — creates Player, 4 enemies, Projectile, 4 interactables
- **Tools > Generate Scenes** — creates ZoneA, ZoneB, ZoneC, BossArena, MainMenu

## Testing

- Tests use Unity Test Framework (NUnit) via EditMode and PlayMode assembly definitions
- EditMode tests: `Assets/Tests/EditMode/` — pure logic (HarmonyCalculator, ComboBookData, GameState, AttackData)
- PlayMode tests: `Assets/Tests/PlayMode/` — MonoBehaviour tests (PlayerController, PlayerHealth, ComboExecutor)
- Use `ScriptableObject.CreateInstance<T>()` to create test SOs, `Object.DestroyImmediate()` in TearDown

## Design References

- @docs/superpowers/specs/2026-03-31-dark-fantasy-topdown-design.md
- @docs/superpowers/plans/2026-03-31-dark-fantasy-topdown-plan.md
