# Level Art Generation — Design Spec

**Date**: 2026-04-01
**Decision**: Generate zone-specific level art by slicing regions from the Free Undead Tileset pack and using Gemini API to create color/texture variants per zone.
**Motivation**: The tileset pack provides consistent, high-quality pixel art as a base. Gemini handles zone differentiation (color shifts, texture tweaks) — simpler and more reliable than generating art from scratch. Phased approach validates style in-game before scaling to more assets.

---

## 1. Approach — Hybrid: Tileset Base + Gemini Variants

**Base assets:** Free Undead Tileset Top Down Pixel Art pack, already extracted to `temp_ref/`:
- `Ground_rocks.png` (496x592) — wall edges from every angle + floor textures
- `Objects.png` (768x704) — decoration props (future phases)
- `Details.png` (576x176) — scatter details (future phases)

**Method (Sheet-section transformation):**
1. Slice a ~128x128 region from Ground_rocks.png containing the target tile type (floor or wall edge) with surrounding context
2. Send the region to Gemini with a zone-specific prompt describing the desired color/texture modifications
3. Post-process the output: remove watermark, crop to a single 16x16 tile, save

Gemini works better with larger images — the surrounding tiles provide style context and the model can understand the material and pixel art style. This mirrors how `generate_player_sprites.py` sends full character images rather than tiny crops.

**Output resolution:** Native 16x16 pixels. Unity handles display scaling via Pixels Per Unit.

---

## 2. Phase 1 Scope — Minimal Viable Art

Phase 1 produces **1 floor texture + 1 wall edge sprite per zone** (4 zones = 8 Gemini calls, 8 output files).

Future phases (not in scope for this spec):
- **Phase 2:** 4 corner pieces per zone for complete SpriteShape wall rendering
- **Phase 3:** 3-5 decoration props per zone sliced from Objects.png + Details.png

---

## 3. Sprite Sheet Regions

Two regions are sliced from `Ground_rocks.png` as input to Gemini:

```python
REGIONS = {
    "floor": (x1, y1, x2, y2),       # ~128x128 chunk of cracked stone floor area (bottom-right)
    "wall_edge": (x1, y1, x2, y2),   # ~128x128 chunk of horizontal wall edge strips (middle rows)
}
```

Exact pixel coordinates will be determined during implementation by inspecting the sprite sheet. The regions should contain multiple tile variations to give Gemini enough context.

After Gemini transforms the region, a single representative 16x16 tile is cropped from the result:
- **Floor:** A seamless-tileable cracked stone tile
- **Wall edge:** A horizontal strip that tiles along SpriteShape splines

---

## 4. Zone Variant Prompts

Each zone gets both color and texture modifications — not just palette swaps. The player should identify which zone they're in from walls and floor alone.

Prompt template: *"Modify this dark stone pixel art tileset to look like [zone description]. Keep the same pixel art style, same tile size, same perspective. Only change the colors, textures, and surface details."*

| Zone | Color Shift | Texture/Detail Tweaks |
|------|------------|----------------------|
| **ZoneA** (Starting Ruins) | Gray-green, muted | Closest to base pack. Add moss patches, small cracks. Default ruin look. |
| **ZoneB** (Catacombs) | Warm brown/bone | Smoother, worn-down stone. Bone fragments embedded in walls, sandy dust on floors. Underground feel. |
| **ZoneC** (Cursed Chapel) | Deep purple/crimson | More ornate — carved stone edges, ritual markings/scratches on floors. Corruption visible in architecture. |
| **BossArena** | Near-black/obsidian | Scorched, melted-looking edges. Floor has faint glowing cracks. Minimal detail — oppressive and empty. |

---

## 5. Pipeline & Script Design

### Script: `tools/generate_level_art.py`

Mirrors `generate_player_sprites.py` patterns:

- **SDK:** `google-genai`, model `gemini-3-pro-image-preview`
- **Auth:** `GEMINI_API_KEY` from environment / `.env`
- **Retry:** 3 attempts, 5s delay between retries
- **Raw output:** Saved to `Assets/Art/LevelArt/raw/` for debugging/re-processing

### Pipeline Steps

```
1. Load Ground_rocks.png from temp_ref/
2. Slice floor region and wall_edge region (128x128 chunks)
3. For each zone:
   a. Send floor region + zone floor prompt to Gemini
   b. Post-process: remove watermark, crop target 16x16 tile
   c. Save to Assets/Art/LevelArt/{Zone}/Floor_{Zone}.png
   d. Send wall_edge region + zone wall prompt to Gemini
   e. Post-process: remove watermark, crop target 16x16 tile
   f. Save to Assets/Art/LevelArt/{Zone}/WallEdge_{Zone}.png
4. Print summary
```

### Post-Processing

Key difference from player sprites: **no background removal.** These are opaque tiles, not transparent characters.

Post-processing steps:
1. **Watermark removal:** Clear Gemini star in bottom-right corner (same `WATERMARK_MARGIN` approach as player sprites)
2. **Crop:** Extract a single 16x16 tile from the transformed region. Crop coordinates target the center or a known-good position within the region.
3. **Resize:** If Gemini output is larger than expected, downscale using nearest-neighbor resampling to preserve pixel art crispness.

### CLI Interface

```
python tools/generate_level_art.py

Options:
  --zones ZONE [ZONE ...]    Only generate specific zones (e.g., ZoneA BossArena)
  --assets ASSET [ASSET ...]  Only generate specific asset types (e.g., floor wall_edge)
  --dry-run                   Print prompts without calling API
  --post-process-only         Re-run post-processing on existing raw/ images
  --no-post-process           Save raw Gemini output without post-processing
```

No positional argument needed — the base tileset path is hardcoded to `temp_ref/Ground_rocks.png` (relative to project root).

---

## 6. Output Structure

```
Assets/Art/LevelArt/
├── raw/                           # Raw Gemini outputs for debugging
│   ├── floor_ZoneA_raw.png
│   ├── wall_edge_ZoneA_raw.png
│   └── ...
├── ZoneA/
│   ├── Floor_ZoneA.png            (16x16, replaces procedural placeholder)
│   └── WallEdge_ZoneA.png         (16x16, replaces procedural placeholder)
├── ZoneB/
│   ├── Floor_ZoneB.png
│   └── WallEdge_ZoneB.png
├── ZoneC/
│   ├── Floor_ZoneC.png
│   └── WallEdge_ZoneC.png
└── BossArena/
    ├── Floor_BossArena.png
    └── WallEdge_BossArena.png
```

Output filenames match existing placeholders so existing `.meta` files and material references remain valid.

---

## 7. Unity Integration

After running the Python script:
1. Re-run **Tools > Generate SpriteShape Profiles** — picks up new wall edge sprites
2. Re-run **Tools > Generate Floor Materials** — picks up new floor textures

The existing C# generators (`FloorMaterialGenerator.cs`, `SpriteShapeProfileGenerator.cs`) currently create procedural placeholders at the same paths. The Python script's output overwrites them. Running the C# generators again after the Python script would overwrite the real art — avoid that. Future improvement: make the C# generators detect existing real art and skip procedural generation.
