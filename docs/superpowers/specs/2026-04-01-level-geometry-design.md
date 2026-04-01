# Level Geometry & Visual Theming — Design Spec

**Date**: 2026-04-01
**Decision**: Replace tilemaps with SpriteShape-based polygon geometry for all level construction.
**Motivation**: Precision combat (Souls-like) demands organic room shapes and pixel-accurate collision. Research into comparable games (Hyper Light Drifter, Hades, Eldest Souls, Titan Souls, Death's Door) shows hand-crafted polygon geometry is the standard for this genre. Tilemaps create grid-locked aesthetics and corner-snagging that undermine combat feel.

---

## 1. Room Structure

Each room is a GameObject hierarchy:

```
Room_<ZoneName>_<ID>/
├── Walls          (SpriteShapeController + EdgeCollider2D)
├── Floor          (SpriteRenderer + world-space tiling Shader Graph material)
├── CameraConfiner (PolygonCollider2D, trigger only, no renderer)
├── Decorations/   (child SpriteRenderers — pillars get BoxCollider2D, all others visual only)
└── RoomTrigger    (BoxCollider2D trigger — switches active CinemachineConfiner on player enter)
```

- **Walls**: Closed `SpriteShapeController` defining the room boundary. One `SpriteShapeProfile` per zone. SpriteShape generates an `EdgeCollider2D` along the spline path (this is its built-in behavior). The generator script reads spline points and also creates a separate `PolygonCollider2D` from those same points on the NavMesh-related objects for navigation baking (see Section 4).
- **Floor**: Single large `SpriteRenderer` with a custom Shader Graph material (based on `Sprite Lit` sub-graph) that tiles a floor texture in world space. One floor texture + normal map per zone.
- **Camera confiner**: Separate `PolygonCollider2D` (trigger only, on its own layer) matching or slightly inset from the wall shape, assigned to `CinemachineConfiner`. Each room has its own confiner.
- **Camera switching**: Each room has a `RoomTrigger` (`BoxCollider2D` trigger covering the room area). When the player enters, a `RoomManager` script sets the active `CinemachineConfiner` target to that room's confiner collider.
- **Decorations**: Optional child sprites (pillars, cracks, moss) placed by the generator. Pillars and gameplay-blocking obstacles get a `BoxCollider2D`. All other decorations are visual only (no collider).
- **Player navigation**: The player uses `Rigidbody2D` + direct input, colliding with wall `EdgeCollider2D` via physics. No NavMesh needed for the player.

### Sorting Layers

| Sorting Layer | Order | Contents |
|---------------|-------|----------|
| Floor | 0 | Floor SpriteRenderer |
| Decorations | 1 | Non-blocking decoration sprites |
| Default | 2 | Player, enemies, interactables (Y-sorted within this layer) |
| Walls | 3 | SpriteShapeRenderer (wall edges) |
| UI | 4 | HUD elements |

---

## 2. Zone Layout & Room Connections

Each zone scene contains multiple rooms connected by corridors within a single scene (no per-room loading). One `NavMeshSurface` per scene covers all rooms and corridors, allowing enemies to pathfind across the entire zone.

- **Rooms** are individual closed SpriteShape polygons with irregular, non-rectangular shapes. The generator defines vertex lists per room.
- **Corridors** are separate GameObjects, each with two open (non-closed) `SpriteShapeController` components forming the left and right walls. Corridor wall endpoints overlap with room wall endpoints at shared vertices to create seamless geometry. Each corridor has its own floor sprite.
- **Doors/gates** are `PolygonCollider2D` objects at corridor pinch points. `KeyGate` enables/disables its collider when unlocked. `ShortcutDoor` works identically.
- **Scene transitions** (zone-to-zone) use trigger colliders at zone edges, unchanged from current approach.

### Room Count Per Zone

| Zone | Rooms | Layout |
|------|-------|--------|
| Zone A (Starting Ruins) | 5 | Hub room, 2 side rooms (keys), exit corridor to B, exit corridor to C, shrine alcove |
| Zone B (Catacombs) | 5 | Entry from A, tight corridors, branching paths, mini-boss room, shortcut door to A, scene transition to C |
| Zone C (Cursed Chapel) | 5 | Entry from A, larger rooms, mini-boss room, shortcut door to B, scene transition to Boss Arena |
| Boss Arena | 1 | Single large circular arena |

### Inter-Zone Connections

Zone A branches to both Zone B and Zone C via two separate exit corridors with scene transition triggers. Zone B and Zone C connect to each other via a `ShortcutDoor` that uses a scene transition trigger (loads the other zone and places the player at the corresponding shortcut entry point). Both Zone B and Zone C must be cleared (mini-boss defeated or end gate reached) to unlock the Boss Arena scene transition in Zone C.

Room vertices, enemy spawn points, interactable positions, and door locations are all defined as data in the generator script.

---

## 3. SpriteShape Profiles & Visual Theming

One `SpriteShapeProfile` asset per zone, generated via editor script. Each profile defines 6-8 edge sprites + 4 corner sprites (inner/outer variants), except the Boss Arena which uses 4-6 edge sprites for a cleaner look. All sprites are AI-generated.

All floor materials use a custom **Shader Graph** material based on the `Sprite Lit` sub-graph with world-space UV tiling. This ensures correct URP 2D lighting response (Global Light 2D, Point Lights, etc.) while tiling the floor texture without stretch distortion at any size. Normal maps are assigned to the same material for lighting depth.

### Zone A — Starting Ruins

- **Edge sprites**: Jagged broken stone masonry, cracked bricks, uneven blocks overgrown with dark green moss and vines. Variations alternate between heavy moss and lighter cracks.
- **Corner sprites**: Chipped 90-degree turns with moss accumulation.
- **Floor**: Seamless worn stone flagstones with dirt/gravel in gaps and subtle moss patches. Desaturated grays with green accents. World-space tiling Shader Graph material + normal map (deep cracks, raised moss).
- **Lighting**: Global Light 2D at 0.15 intensity, cool gray-blue tint. 4-6 Point Lights (warm orange-yellow, RGB ~255/180/80, intensity 1.5-3, radius 4-8) at torch positions with flicker script.

### Zone B — Catacombs

- **Edge sprites**: Interlocking bones, ribs, skulls, and vertebrae with glowing blue-green veins. Variations emphasize different bone/skull densities.
- **Corner sprites**: Clustered skulls at joints.
- **Floor**: Scattered bone shards on dark earth with faint blue-green emissive veins. Pale off-white bones on dark gray-black ground. World-space tiling Shader Graph material + strong normal map (raised bones, deep grooves).
- **Lighting**: Global Light 2D at 0.1 intensity, desaturated teal-black. Multiple Freeform Lights (blue-green, RGB ~80/255/200, intensity 0.4-0.8) following bone clusters. Sparse cold cyan Point Lights on key bones.

### Zone C — Cursed Chapel

- **Edge sprites**: Ornate cracked stone chapel walls with embedded stained-glass shards, broken arches, crimson blood-like stains, and cursed motifs (thorns, faint crosses).
- **Corner sprites**: Shattered arch details.
- **Floor**: Cracked dark slate tiles with crimson grout, scattered glass shards, faint ritual patterns. World-space tiling Shader Graph material + normal map (deep cracks, raised glass shards).
- **Lighting**: Global Light 2D at 0.2 intensity, deep purple-crimson tint. Point Lights in crimson-red (RGB ~200/0/50) and purple (RGB ~100/0/200). 1-2 Freeform Lights with soft crimson bleed across floor.

### Boss Arena

- **Edge sprites**: Smooth dark polished stone/marble with subtle engravings, metallic inlays, faint runes. Fewer variations (4-6) for a clean, precise arena look.
- **Corner sprites**: Sharp geometric turns.
- **Floor**: Dark polished marble/obsidian with subtle geometric patterns. World-space tiling Shader Graph material with specular boost for "wet stone" sheen + strong normal map (bevels, engravings).
- **Lighting**: Global Light 2D starts at 0.2 neutral. On arena entry (trigger + coroutine), lerps to 0.4 with red-black shift. 4-6 strong perimeter Point Lights (white-red, intensity 3-5) activate on entry. 1-2 pulsing Freeform Lights in center for boss focus.

---

## 4. Enemy Navigation

- **NavMeshPlus** package (installed via git URL: `https://github.com/h8man/NavMeshPlus.git`).
- One `NavMeshSurface` + `NavMeshCollectSources2D` per scene (not per room), placed on a root-level GameObject. Collects from **Physics Colliders** — wall `EdgeCollider2D` components are detected as non-walkable obstacles.
- `NavigationModifier` on wall objects explicitly marks them as non-walkable.
- Bake once at scene load (static rooms, no runtime rebake).
- NavMeshAgent agent type settings: radius 0.3, height 0.1 (effectively 2D).

### NavMeshAgent + Rigidbody2D Integration

`NavMeshAgent` and `Rigidbody2D` conflict when both try to control position. The solution:

- `NavMeshAgent.updatePosition = false` and `NavMeshAgent.updateRotation = false` on all enemies.
- `EnemyStateMachine` uses `NavMeshAgent.SetDestination()` to compute paths, then reads `NavMeshAgent.desiredVelocity` to get the direction.
- Movement is still applied via `Rigidbody2D.velocity` (preserving physics interactions like knockback).
- In **Chase** state: `_rb.velocity = _agent.desiredVelocity.normalized * data.moveSpeed`
- In **Patrol** state: `_agent.SetDestination(patrolPoints[_patrolIndex].position)` then same velocity pattern.
- In **Attack/Stagger/Dead** states: `_rb.velocity = Vector2.zero` (unchanged).
- Each frame, sync NavMeshAgent's internal position: `_agent.nextPosition = transform.position`.

This keeps the existing `Rigidbody2D`-driven movement pattern while gaining NavMesh pathfinding around walls.

### NavMeshAgent Settings Per Enemy Type

| Enemy | Speed | Radius | Navigation Behavior |
|-------|-------|--------|---------------------|
| Hollow | Slow | Small | Path to player, attack in melee range |
| Wraith | Fast | Small | Path to dash range, dash through player |
| Knight | Medium | Large | Path to player, wide attack arc |
| Caster | Medium | Small | Path to max spell range, maintain distance |

Actual numeric values for speed/radius are defined in `EnemyData` ScriptableObjects, not hardcoded.

---

## 5. Asset Pipeline & Generation

All visual assets are AI-generated and imported via editor scripts.

### Per Zone, the Generator Creates

- 1 `SpriteShapeProfile` with 6-8 edge sprites + 4 corner sprites
- 1 floor texture (1024x1024, seamless) + matching normal map
- 1 Shader Graph material (Sprite Lit sub-graph, world-space tiling)
- Light prefab presets (type, color, intensity, radius)

### AI Sprite Generation Prompts

Consistent formula across all zones:
- **Edge sprites**: `"top-down [zone theme] edge sprite, dark fantasy, seamless border, sprite shape, 512x512"`
- **Floor textures**: `"seamless top-down [zone theme] floor texture, dark fantasy, 1024x1024"`
- **Normal maps**: Generated from albedo textures via tooling

Note: The original design spec's 16x16/32x32 tile resolution targets no longer apply — SpriteShape edge sprites use 512x512, floor textures use 1024x1024. The original spec should be updated to reflect this.

### Import Settings (Automated via AssetPostprocessor)

- Texture type: Sprite
- Sprite mode: Multiple (edges) or Single (floors)
- Pixels Per Unit: 100 (consistent across all assets)
- Wrap mode: Repeat (floors), Clamp (edges)

---

## 6. Tech Stack Changes

### Added

- `com.unity.2d.spriteshape` — SpriteShape package (via Package Manager)
- NavMeshPlus — 2D NavMesh support (via git URL)
- Shader Graph — world-space tiling floor material (built into URP, using `Sprite Lit` sub-graph)

### Removed

- `Tilemap` from tech stack — no longer used for level geometry
- `TilemapCollider2D` / `CompositeCollider2D` approach for wall collision
- `Assets/Tilemaps/` directory from project structure
- `Assets/Art/Tilesets/` directory from project structure

### Updated Project Structure

```
Assets/
├── Art/
│   ├── Sprites/          # Characters, enemies, items
│   ├── LevelArt/         # Per-zone: edge sprites, corner sprites, floor textures, normal maps
│   └── VFX/              # Slash effects, dodge trail, combo particles
```

`Assets/Art/Tilesets/` and `Assets/Tilemaps/` are removed entirely.

### Required Updates to Other Files

- **CLAUDE.md**: Replace `Tilemap` with `SpriteShape, NavMeshPlus` in tech stack. Update project structure. Add SpriteShape/NavMeshPlus to First-Time Unity Setup.
- **Original design spec** (`2026-03-31-dark-fantasy-topdown-design.md`): Update Section 3 (Tilemap subsection) to reflect SpriteShape approach. Update art resolution targets in Section 4. Update project structure to remove `Tilesets/` and `Tilemaps/` directories.
