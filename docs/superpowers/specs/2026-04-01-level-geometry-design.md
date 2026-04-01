# Level Geometry & Visual Theming — Design Spec

**Date**: 2026-04-01
**Decision**: Replace tilemaps with SpriteShape-based polygon geometry for all level construction.
**Motivation**: Precision combat (Souls-like) demands organic room shapes and pixel-accurate collision. Research into comparable games (Hyper Light Drifter, Hades, Eldest Souls, Titan Souls, Death's Door) shows hand-crafted polygon geometry is the standard for this genre. Tilemaps create grid-locked aesthetics and corner-snagging that undermine combat feel.

---

## 1. Room Structure

Each room is a GameObject hierarchy:

```
Room_<ZoneName>_<ID>/
├── Walls          (SpriteShapeController + PolygonCollider2D)
├── Floor          (SpriteRenderer + world-space tiling material)
├── CameraConfiner (PolygonCollider2D, trigger only, no renderer)
├── Decorations/   (child SpriteRenderers — pillars, rubble, etc.)
└── NavMeshSurface (NavMeshPlus, bakes from wall colliders)
```

- **Walls**: Closed `SpriteShapeController` defining the room boundary. One `SpriteShapeProfile` per zone. Auto-generates `PolygonCollider2D` from shape control points.
- **Floor**: Single large `SpriteRenderer` with a Shader Graph material that tiles a floor texture in world space. One floor texture per zone.
- **Camera confiner**: Separate `PolygonCollider2D` (trigger only) matching or slightly inset from the wall shape, assigned to `CinemachineConfiner`.
- **Decorations**: Optional child sprites (pillars, cracks, moss) placed by the generator. No collision unless gameplay-relevant.

---

## 2. Zone Layout & Room Connections

Each zone scene contains multiple rooms connected by corridors within a single scene (no per-room loading).

- **Rooms** are individual closed SpriteShape polygons with irregular, non-rectangular shapes. The generator defines vertex lists per room.
- **Corridors** connect rooms via open SpriteShape paths (two parallel wall edges). Transition between rooms is seamless within the scene.
- **Doors/gates** are `PolygonCollider2D` objects at corridor pinch points. `KeyGate` enables/disables its collider when unlocked. `ShortcutDoor` works identically.
- **Scene transitions** (zone-to-zone) use trigger colliders at zone edges, unchanged from current approach.

### Room Count Per Zone

| Zone | Rooms | Layout |
|------|-------|--------|
| Zone A (Starting Ruins) | 4-5 | Hub room, 2 side rooms (keys), main path forward, shrine alcove |
| Zone B (Catacombs) | 4-5 | Tight corridors, branching paths, shortcut loop back to A |
| Zone C (Cursed Chapel) | 4-5 | More vertical layout, connects to B via shortcut, larger rooms |
| Boss Arena | 1 | Single large circular arena |

Room vertices, enemy spawn points, interactable positions, and door locations are all defined as data in the generator script.

---

## 3. SpriteShape Profiles & Visual Theming

One `SpriteShapeProfile` asset per zone, generated via editor script. Each profile defines 6-8 edge sprites + 4 corner sprites (inner/outer variants), except the Boss Arena which uses 4-6 edge sprites for a cleaner look. All sprites are AI-generated.

### Zone A — Starting Ruins

- **Edge sprites**: Jagged broken stone masonry, cracked bricks, uneven blocks overgrown with dark green moss and vines. Variations alternate between heavy moss and lighter cracks.
- **Corner sprites**: Chipped 90-degree turns with moss accumulation.
- **Floor**: Seamless worn stone flagstones with dirt/gravel in gaps and subtle moss patches. Desaturated grays with green accents. `Sprite-Lit-Default` material + normal map (deep cracks, raised moss).
- **Lighting**: Global Light 2D at 0.15 intensity, cool gray-blue tint. 4-6 Point Lights (warm orange-yellow, RGB ~255/180/80, intensity 1.5-3, radius 4-8) at torch positions with flicker script.

### Zone B — Catacombs

- **Edge sprites**: Interlocking bones, ribs, skulls, and vertebrae with glowing blue-green veins. Variations emphasize different bone/skull densities.
- **Corner sprites**: Clustered skulls at joints.
- **Floor**: Scattered bone shards on dark earth with faint blue-green emissive veins. Pale off-white bones on dark gray-black ground. `Sprite-Lit-Default` + strong normal map (raised bones, deep grooves).
- **Lighting**: Global Light 2D at 0.1 intensity, desaturated teal-black. Multiple Freeform Lights (blue-green, RGB ~80/255/200, intensity 0.4-0.8) following bone clusters. Sparse cold cyan Point Lights on key bones.

### Zone C — Cursed Chapel

- **Edge sprites**: Ornate cracked stone chapel walls with embedded stained-glass shards, broken arches, crimson blood-like stains, and cursed motifs (thorns, faint crosses).
- **Corner sprites**: Shattered arch details.
- **Floor**: Cracked dark slate tiles with crimson grout, scattered glass shards, faint ritual patterns. `Sprite-Lit-Default` + normal map (deep cracks, raised glass shards).
- **Lighting**: Global Light 2D at 0.2 intensity, deep purple-crimson tint. Point Lights in crimson-red (RGB ~200/0/50) and purple (RGB ~100/0/200). 1-2 Freeform Lights with soft crimson bleed across floor.

### Boss Arena

- **Edge sprites**: Smooth dark polished stone/marble with subtle engravings, metallic inlays, faint runes. Fewer variations (4-6) for a clean, precise arena look.
- **Corner sprites**: Sharp geometric turns.
- **Floor**: Dark polished marble/obsidian with subtle geometric patterns. Strong normal map (bevels, engravings). `Sprite-Lit-Default` or simple Shader Graph with specular boost for "wet stone" sheen.
- **Lighting**: Global Light 2D starts at 0.2 neutral. On arena entry (trigger + coroutine), lerps to 0.4 with red-black shift. 4-6 strong perimeter Point Lights (white-red, intensity 3-5) activate on entry. 1-2 pulsing Freeform Lights in center for boss focus.

---

## 4. Enemy Navigation

- **NavMeshPlus** package (installed via git URL: `https://github.com/h8man/NavMeshPlus.git`).
- Each room has a `NavMeshSurface` + `NavMeshCollectSources2D` component set to collect from **Physics Colliders**.
- Bake once at scene load (static rooms, no runtime rebake).
- `NavigationModifier` on wall objects marks them as non-walkable.

### NavMeshAgent Settings Per Enemy Type

| Enemy | Speed | Radius | Navigation Behavior |
|-------|-------|--------|---------------------|
| Hollow | Slow | Small | Path to player, attack in melee range |
| Wraith | Fast | Small | Path to dash range, dash through player |
| Knight | Medium | Large | Path to player, wide attack arc |
| Caster | Medium | Small | Path to max spell range, maintain distance |

The existing `EnemyStateMachine` drives when to path (Chase state) vs. stop and attack (Attack state). `NavMeshAgent` handles the pathfinding.

---

## 5. Asset Pipeline & Generation

All visual assets are AI-generated and imported via editor scripts.

### Per Zone, the Generator Creates

- 1 `SpriteShapeProfile` with 6-8 edge sprites + 4 corner sprites
- 1 floor fill texture (1024x1024, seamless) + matching normal map
- Light prefab presets (type, color, intensity, radius)

### AI Sprite Generation Prompts

Consistent formula across all zones:
- **Edge sprites**: `"top-down [zone theme] edge sprite, dark fantasy, seamless border, sprite shape, 512x512"`
- **Floor textures**: `"seamless top-down [zone theme] floor texture, dark fantasy, 1024x1024"`
- **Normal maps**: Generated from albedo textures via tooling

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
- Shader Graph — world-space tiling floor material (built into URP)

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
