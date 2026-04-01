# Dark Fantasy Top-Down Action Game — Design Spec

**Date**: 2026-03-31
**Engine**: Unity 2D (URP)
**Scope**: Small/focused — 3 zones, 1 boss, session-based
**Development approach**: AI-driven (minimal human interaction, data-driven architecture)

---

## 1. Core Game Loop & Player Experience

### Pitch

A small dark fantasy top-down action game with interconnected exploration and a deep combo-crafting combat system. Three zones form a connected web — explore, find keys and attacks, craft combos, unlock shortcuts, and defeat the final boss.

### Core Loop

1. **Explore** — navigate rooms, find keys in side areas to unlock the main path, discover shortcuts connecting zones
2. **Fight** — real-time combat using custom chain combos built from collected attacks
3. **Collect** — find new basic attacks, Combo Books (empty or pre-configured), and health pickups
4. **Craft combos** — drag attacks into Combo Book slots, experiment with tag harmony for optimal flow
5. **Progress** — clear Zone B and Zone C (branching from A, connected to each other) to unlock the final boss

### Player Actions

- 8-directional movement
- Execute combo chain (from equipped Combo Book)
- Dodge/roll with i-frames
- Use item (health potion)
- Interact (doors, chests, shrines, NPCs)
- Open Combo Book UI (drag/drop attacks into slots)

### World Structure

- **Zone A** (Starting Ruins) — branches to B and C
- **Zone B** (Catacombs) and **Zone C** (Cursed Chapel) — also connected to each other
- Within each zone: main path gated by keys found in side rooms, shortcuts that loop back
- **Hidden shrines** as discoverable checkpoints scattered throughout
- **Boss Arena** — accessible after clearing both B and C

### Progression

- Linear-but-branching: inspired by Dark Souls 1-2
- Keys found in optional side rooms unlock doors on the main path
- Shortcuts within and between zones create a connected web
- Clearing a zone means defeating its mini-boss or reaching its end gate — both Zone B and Zone C must be cleared to unlock the Boss Arena

### Death

- Respawn at last discovered shrine
- No penalty (no currency loss, no enemy respawn)

### Scope Boundary

- 1 playable character
- 3 zones + 1 boss fight
- 3-4 enemy types + 1 boss
- 12-15 basic attacks with tags
- ~6 Combo Books (mix of empty and pre-configured, across rarities)
- No crafting, no dialogue trees, no save persistence beyond checkpoints

---

## 2. Combat & Combo System

### Basic Attacks (12-15 total)

Each attack is a ScriptableObject with:

| Field             | Description                                          |
| ----------------- | ---------------------------------------------------- |
| Name              | Display name (e.g., "Cleaving Arc", "Lunging Thrust") |
| Tags              | 1-2 from: `sweep`, `thrust`, `overhead`, `rising`, `spinning`, `slam` |
| Damage            | Base damage value                                    |
| Speed             | Execution time: fast, medium, slow                   |
| Movement pattern  | Lunge forward, hold position, pull back              |
| Animation clip    | Reference to the attack animation                    |

### Tag Harmony System

- Each tag has a set of **complementary tags** (e.g., `rising` flows into `slam`, `thrust` flows into `sweep`)
- Adjacent attacks in a combo are scored:
  - **Harmonious**: +30% damage bonus, smooth animation blending
  - **Neutral**: normal damage, standard transition
  - **Dissonant**: -40% damage penalty, sluggish transition animation
- The harmony table is a single ScriptableObject — a lookup of tag-pair to harmony level
- A fully harmonious 3-slot combo deals ~130% damage; fully dissonant deals ~60%

### Combo Books

| Rarity    | Slots | Found                                      |
| --------- | ----- | ------------------------------------------ |
| Common    | 2     | Early game, basic combos                   |
| Rare      | 3     | Mid-game, full combo potential             |
| Legendary | 4     | Late-game, powerful chains                 |

- **Empty books**: player fills slots with collected attacks
- **Pre-configured books**: found in the world with attacks already slotted, teaching good combos
- Player can swap attacks in/out at any time outside of combat

### Combat Flow

1. Player presses attack button — executes slot 1 of equipped combo
2. Pressing again within a timing window — chains to slot 2, then 3, etc.
3. Missing the timing window resets the chain
4. Dodge cancels the current attack (with a small recovery penalty)
5. Enemies have telegraphed attacks with readable wind-ups

### Enemy Types

| Type     | Role                                             |
| -------- | ------------------------------------------------ |
| Hollow   | Basic melee, slow — teaches fundamentals         |
| Wraith   | Fast, dashes in/out — teaches dodge timing       |
| Knight   | Armored, requires combos to stagger — punishes button mashing |
| Caster   | Ranged projectiles — forces movement and closing distance |

### Boss

- Accessible after clearing both Zone B and Zone C
- Multi-phase fight with escalating attack patterns
- Specific design TBD during implementation

---

## 3. Architecture & Technical Design

### Engine & Rendering

- Unity 2D with URP (Universal Render Pipeline) for 2D lighting
- MonoBehaviour + ScriptableObject architecture (not ECS/DOTS)

### AI-Friendly Design Principles

- **ScriptableObjects for all game data** — attacks, combo books, enemy stats, harmony table, loot tables. Adding content = creating new assets, not writing code.
- **One MonoBehaviour per file** — class name matches file name, always
- **Composition over inheritance** — enemies share components but differ via data. No deep class hierarchies.
- **Simple state machines via enums** — switch statements, not frameworks
- **Prefab-based content** — each enemy, item, interactable is a self-contained prefab

### Project Structure

```
Assets/
├── Scenes/             # ZoneA, ZoneB, ZoneC, BossArena, MainMenu
├── Scripts/
│   ├── Player/         # PlayerController, PlayerHealth, PlayerInventory
│   ├── Combat/         # ComboExecutor, HarmonyCalculator, AttackSlot
│   ├── Enemies/        # EnemyBase, EnemyStateMachine, per-type behavior scripts
│   ├── World/          # DoorController, KeyGate, ShortcutDoor, Shrine
│   ├── UI/             # ComboBookUI, HealthBar, InventoryPanel
│   └── Core/           # GameManager, SceneTransition, CheckpointManager
├── Data/
│   ├── Attacks/        # ScriptableObjects — one per attack
│   ├── ComboBooks/     # ScriptableObjects — one per book
│   ├── Enemies/        # ScriptableObjects — enemy stat blocks
│   └── HarmonyTable.asset  # Tag-pair compatibility lookup
├── Prefabs/
│   ├── Player/
│   ├── Enemies/
│   ├── Interactables/  # Doors, chests, shrines, pickups
│   └── UI/
├── Art/
│   ├── Sprites/        # AI-generated character/enemy/item sprites
│   ├── LevelArt/       # Per-zone: edge sprites, corner sprites, floor textures, normal maps
│   └── VFX/            # Slash effects, dodge trail, combo particles
├── Audio/              # SFX and ambient
```

### State Machines

**Player states**: `Idle`, `Moving`, `Attacking`, `Dodging`, `Hit`, `Dead`
**Enemy states**: `Idle`, `Patrol`, `Chase`, `Attack`, `Stagger`, `Dead`

Implemented as enum + switch — no state machine frameworks.

### Scene Management

- Each zone is its own Unity scene
- `GameManager` singleton (`DontDestroyOnLoad`) tracks:
  - Current checkpoint (last shrine)
  - Collected keys
  - Discovered attacks
  - Unlocked shortcuts
  - Cleared zones (B done, C done)
- Scene transitions via trigger colliders at zone connections — fade to black, load next scene, place player at entry point

### Level Geometry

- SpriteShape-based polygon rooms with EdgeCollider2D for wall collision
- Floor visuals via SpriteRenderer with world-space tiling material
- One SpriteShapeProfile per zone for wall theming
- NavMeshPlus for enemy pathfinding (baked from wall colliders)
- See `docs/superpowers/specs/2026-04-01-level-geometry-design.md` for full details

### Camera

- Cinemachine 2D with confiner — follows player, bounded per room
- Slight damping for weighty, atmospheric feel

### Lighting (URP 2D)

- Global light dimmed low for dark atmosphere
- Point lights on torches, shrines, and VFX
- Player carries a subtle ambient light radius

---

## 4. Art Direction & Polish

### Visual Style

- Dark fantasy with desaturated palette — deep purples, greys, muted greens, accented with warm firelight
- 3/4 top-down perspective (Link to the Past / Souls-like)
- AI-generated sprites with consistent style prompts for cohesion
- Wall edge sprites: 512x512, floor textures: 1024x1024, character/boss sprites: larger as needed

### Zone Visual Identity

| Zone                  | Aesthetic                                              |
| --------------------- | ------------------------------------------------------ |
| Zone A (Starting Ruins) | Crumbling stone, overgrown moss, dim torch light     |
| Zone B (Catacombs)    | Bone walls, blue-green phosphorescence, tight corridors |
| Zone C (Cursed Chapel) | Stained glass shards, crimson accents, fallen pillars |
| Boss Arena            | Open circular room, dramatic lighting shift on entry   |

### Lighting & Atmosphere

- Low global ambient — the world is dark by default
- Point lights on torches, braziers, shrines (warm orange/yellow)
- Zone-specific accent lights (green for catacombs, red for chapel)
- Player emits a subtle light radius — creates intimacy, limits visibility
- Boss arena: dynamic lighting change on entry (ambient drops, spotlights on boss)

### Animation

- **Player**: idle, walk (8-dir), attack (per-attack-type), dodge roll, hit stagger, death
- **Enemies**: idle, patrol walk, attack telegraph, attack, stagger, death
- **Combo flow feedback**: smooth blending for harmonious chains, visible hitch/stutter for dissonant ones
- Screen shake on heavy hits and boss attacks

### VFX

- Slash trails on attacks (color varies by attack tag)
- Dodge roll: brief afterimage/shadow trail
- Harmony feedback: subtle glow on harmonious hits, sparks/friction on dissonant ones
- Shrine discovery: light burst + particle bloom
- Enemy death: dissolve/fade with particle burst

### Audio

- Ambient loops per zone (wind, dripping, distant chanting)
- Combat SFX: sword swings, impacts, dodge whoosh
- Combo harmony: satisfying chain sound for flow, clunky metal-on-metal for dissonant
- Boss entrance: musical sting
- Shrine activation: chime/bell

### UI

- **Minimal HUD**: health bar (top-left), equipped combo book indicator (bottom-left)
- **Combo Book screen**: full-screen overlay, drag-and-drop slots, tag labels visible, harmony preview before committing
- **No minimap** — exploration relies on player memory and spatial awareness (Souls-like)
- Damage numbers optional (off by default for atmosphere)
