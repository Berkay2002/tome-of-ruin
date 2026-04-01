# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Tome of Ruin** — dark fantasy top-down 2D action game.
Unity 2022.3 LTS, URP 2D, C#, Cinemachine, SpriteShape, NavMeshPlus, Unity Test Framework (NUnit).

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

## Unity 2D Rendering Gotchas

- `SpriteRenderer` needs `.sprite` assigned — a `.material` alone renders a colored rectangle, not the texture
- `MeshRenderer` in 2D: set `sortingLayerName` and `sortingOrder` explicitly — defaults to Default/0 which renders on top of sprites
- Polygon triangulation (ear-clipping) requires CCW winding — detect via signed area and reverse if CW
- SpriteShapeController causes editor hangs — currently disabled, walls use EdgeCollider2D only
- Editor generator `File.Exists` guards prevent texture refresh — generators must update references on existing assets, not just skip

## Code Style

- One class/enum per file (small related enums can share a file, e.g. `AttackTag.cs`)
- `_camelCase` for private fields, `PascalCase` for public fields/properties/methods
- `[Header("Section")]` attributes to group Inspector fields
- `[RequireComponent]` attribute when a MonoBehaviour depends on another component
- `[CreateAssetMenu]` attribute on all ScriptableObjects

## First-Time Unity Setup

1. Open project folder in **Unity 2022.3 LTS** (URP template)
2. Run **Tools > Generate Data Assets** — creates all SOs (attacks, combo books, enemies, harmony table)
3. Run **Tools > Generate Materials** — creates WhiteFlash shader and material
4. Run **Tools > Generate SpriteShape Profiles** — creates per-zone wall profiles
5. Run **Tools > Generate Floor Materials** — creates per-zone floor textures and materials
6. Run **Tools > Generate Prefabs** — creates Player, 4 enemies, Boss, Projectile, 4 interactables
7. Run **Tools > Generate Levels** — creates ZoneA, ZoneB, ZoneC, BossArena with SpriteShape rooms
8. Run **Tools > Generate Scenes** — creates MainMenu
9. File > Build Settings — add all 5 scenes (MainMenu, ZoneA, ZoneB, ZoneC, BossArena)
10. Edit > Project Settings > Tags and Layers — add `"Enemy"` tag and `"Enemy"` layer
11. On Player prefab: set `PlayerCombat.enemyLayer` to the Enemy layer
12. On Player prefab: assign `HarmonyTable` asset to `ComboExecutor.harmonyTable`

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

## Gemini Image Generation

- Model: `gemini-3-pro-image-preview` — use for asset generation (has thinking process, up to 4K)
- Resolutions: 1K/2K/4K only (no 512). 2K costs same tokens as 1K — always prefer 2K+
- Must specify `image_size` explicitly when input image is small — Gemini defaults to matching input size
- Content order: `[image, prompt]` (image first) for style transfer / editing
- Watermark: Gemini star in bottom-right — fill with average border color (not transparent) for opaque tiles
- Scripts: `tools/generate_player_sprites.py` (characters), `tools/generate_level_art.py` (level textures)

## Design References

- `docs/superpowers/specs/2026-03-31-dark-fantasy-topdown-design.md`
- `docs/superpowers/plans/2026-03-31-dark-fantasy-topdown-plan.md`
