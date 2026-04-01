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
3. Post-process the full Gemini output: remove watermark, resize to target resolution, save

Gemini works better with larger images — the surrounding tiles provide style context and the model can understand the material and pixel art style. This mirrors how `generate_player_sprites.py` sends full character images rather than tiny crops. Gemini always outputs at ~1024x1024 regardless of input size, so all resizing is done in post-processing.

**Output resolution:** Wall edge sprites at 512x512, floor textures at 1024x1024 — matching the level geometry spec's SpriteShape requirements. Unity's Pixels Per Unit handles display scaling.

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
    "floor": (x1, y1, x2, y2),       # ~128x128 chunk of cracked stone floor area (bottom-right of sheet)
    "wall_edge": (x1, y1, x2, y2),   # ~128x128 chunk of horizontal wall edge strips (middle rows of sheet)
}
```

Exact pixel coordinates will be determined during implementation by inspecting the sprite sheet. The floor region should target the cracked stone ground textures visible in the bottom-right area of Ground_rocks.png. The wall edge region should target the horizontal wall edge strips (dark stone tops transitioning to lighter floor) in the middle rows.

The full Gemini output is used as the asset — no sub-tile cropping. Gemini outputs at ~1024x1024, which is resized in post-processing:
- **Floor:** Resized to 1024x1024 (seamless-tileable texture)
- **Wall edge:** Resized to 512x512 (tiles along SpriteShape splines)

---

## 4. Zone Variant Prompts

Each zone gets both color and texture modifications — not just palette swaps. The player should identify which zone they're in from walls and floor alone.

### Prompting Strategy

Prompts combine multiple Gemini editing techniques from the official guide:

- **Inpainting (semantic masking):** Target specific elements for change while preserving the rest. *"Change only the [element] to [new description]. Keep everything else exactly the same."*
- **Adding/removing elements:** Describe additions that integrate naturally. *"Add [element] to the scene. Ensure the change matches the original style, lighting, and perspective."*
- **Style transfer:** Transform the overall mood while preserving composition. *"Preserve the original composition but render it with [stylistic elements]."*
- **High-fidelity preservation:** Describe what must NOT change alongside what should. *"Keep the pixel art style, tile structure, crack patterns, and perspective unchanged."*

### Best Practices (from Gemini docs)

- **Be hyper-specific:** Instead of "make it darker," describe: "shift the stone palette to near-black obsidian with subtle blue-gray veining and faint orange-red glow in the deepest cracks."
- **Provide context and intent:** Explain purpose: "This is a floor tile for a dark fantasy catacomb level in a top-down 2D game."
- **Use semantic negative prompts:** Instead of "no bright colors," describe positively: "a muted, desaturated palette of deep earth tones."

### Prompt Templates

**Floor prompt template:**
```
Using the provided image of a dark stone pixel art floor tileset, change only
the colors and surface details to look like [zone floor description]. Keep the
pixel art style, tile structure, crack patterns, and top-down perspective
exactly the same. This is a seamless floor tile for a dark fantasy [zone name]
in a top-down 2D game. [Zone-specific additions].
```

**Wall edge prompt template:**
```
Using the provided image of dark stone pixel art wall edges, change only the
colors, surface texture, and decorative details to look like [zone wall
description]. Keep the pixel art style, edge shape, and top-down perspective
exactly the same. This is a wall edge sprite for a dark fantasy [zone name]
in a top-down 2D game. [Zone-specific additions].
```

### Zone-Specific Prompt Details

| Zone | Floor prompt details | Wall edge prompt details |
|------|---------------------|------------------------|
| **ZoneA** (Starting Ruins) | Worn gray-green flagstones with dirt in the gaps and subtle dark moss patches. Muted, desaturated palette of grays with faint green accents. Add small cracks filled with dark grime. | Gray-green broken stone masonry with uneven blocks. Add dark green moss growing in the crevices and small vine tendrils creeping along the edges. Keep the stone surface rough and weathered. |
| **ZoneB** (Catacombs) | Shift the stone palette to warm browns and bone-white. Make the surface smoother and more worn-down. Add sandy dust texture in the tile gaps and tiny bone fragment chips scattered on the surface. | Shift to warm brown worn stone. Make the surface smoother, as if eroded by centuries underground. Add small bone fragments and skull chips embedded in the crevices between stones. |
| **ZoneC** (Cursed Chapel) | Shift the stone palette to deep purple-gray and dark crimson. Add thin ritual scratch marks across the tile surface and faint crimson stains seeping between the cracks, like dried blood in the grout lines. | Shift to deep purple-gray ornate carved stone. Add thin carved ritual markings along the stone faces, with crimson stains bleeding down from the crevices. Make the stonework look more deliberately shaped, less natural. |
| **BossArena** | Shift the stone palette to near-black obsidian with subtle blue-gray veining. Add faint orange-red glow lines in the deepest cracks, like cooling magma beneath the surface. Minimal surface detail — smooth, oppressive, empty. | Shift to near-black scorched stone with a melted, fused appearance. The edges should look heat-warped, with subtle orange-red glow in the deepest gaps. Fewer individual stones visible — more monolithic and oppressive. |

---

## 5. Pipeline & Script Design

### Script: `tools/generate_level_art.py`

Mirrors `generate_player_sprites.py` patterns:

- **SDK:** `google-genai`, default model `gemini-3-pro-image-preview` (overridable via `--model`)
- **Auth:** `GEMINI_API_KEY` from environment / `.env`
- **Retry:** 3 attempts, 5s delay between retries
- **Raw output:** Saved to `Assets/Art/LevelArt/raw/` for debugging/re-processing

### Gemini API Configuration

The API supports explicit resolution and aspect ratio control via `generationConfig`:

```python
response = client.models.generate_content(
    model=MODEL,
    contents=[region_image, prompt],   # image first, then text (matches style transfer pattern)
    config=types.GenerateContentConfig(
        response_modalities=["TEXT", "IMAGE"],
        image_config=types.ImageConfig(
            image_size="1K",       # "1K", "2K", "4K" (3 Pro has no "512" option)
            aspect_ratio="1:1",
        ),
    ),
)
```

- **Both floors and wall edges:** Request `imageSize="1K"` (1024x1024), aspect ratio `"1:1"`
- **Wall edges** are resized down to 512x512 in post-processing
- **Important:** The model defaults to matching output size to input size. Since our input is a ~128x128 crop, we must specify `image_size="1K"` explicitly or Gemini may output a small image.

Prompt style follows the Gemini style transfer pattern: *"Transform the provided [subject] into [style]. Preserve the original composition but render it with [stylistic elements]."* Be hyper-specific about the desired modifications — describe materials, surface details, and color palette explicitly.

### Pipeline Steps

```
1. Load Ground_rocks.png from temp_ref/
2. Slice floor region and wall_edge region (~128x128 chunks)
3. For each zone:
   a. Check if Floor_{Zone}.png exists — skip unless --force
   b. Send floor region + zone floor prompt to Gemini (imageSize="1K")
   c. Save raw output to Assets/Art/LevelArt/raw/
   d. Post-process: remove watermark on raw 1024x1024 output
   e. Save to Assets/Art/LevelArt/{Zone}/Floor_{Zone}.png (1024x1024)
   f. Check if WallEdge_{Zone}.png exists — skip unless --force
   g. Send wall_edge region + zone wall prompt to Gemini (imageSize="1K")
   h. Save raw output to Assets/Art/LevelArt/raw/
   i. Post-process: remove watermark on raw 1024x1024, resize to 512x512
   j. Save to Assets/Art/LevelArt/{Zone}/WallEdge_{Zone}.png (512x512)
   k. Brief delay (2s) between API calls for rate limiting
4. Print summary
```

### Post-Processing

Key difference from player sprites: **no background removal.** These are opaque tiles, not transparent characters.

Post-processing steps (same order as `generate_player_sprites.py`):
1. **Watermark removal:** Clear Gemini star in bottom-right corner (same `WATERMARK_MARGIN` approach as player sprites — 80px corner clear on raw output)
2. **Resize (fallback):** If Gemini returns an unexpected size, downscale to target resolution using gradual step-down with LANCZOS resampling (matching the player sprite script's approach). This is a safety net — the `imageSize` config should produce the correct size directly.

### CLI Interface

```
python tools/generate_level_art.py

Options:
  --zones ZONE [ZONE ...]    Only generate specific zones (e.g., ZoneA BossArena)
  --assets ASSET [ASSET ...]  Only generate specific asset types (e.g., floor wall_edge)
  --dry-run                   Print prompts without calling API
  --post-process-only         Re-run post-processing on existing raw/ images
  --no-post-process           Save raw Gemini output without post-processing
  --force                     Overwrite existing files (default: skip if exists)
  --model MODEL               Gemini model name (default: gemini-3-pro-image-preview)
```

**Dependencies:** `google-genai`, `Pillow`, `numpy`, `python-dotenv` (same as `generate_player_sprites.py`)

No positional argument needed — the base tileset path is hardcoded to `temp_ref/Ground_rocks.png` (relative to project root).

---

## 6. Output Structure

```
Assets/Art/LevelArt/
├── raw/                           # Raw Gemini outputs (~1024x1024) for debugging
│   ├── floor_ZoneA_raw.png
│   ├── wall_edge_ZoneA_raw.png
│   └── ...
├── ZoneA/
│   ├── Floor_ZoneA.png            (1024x1024, replaces procedural placeholder)
│   └── WallEdge_ZoneA.png         (512x512, replaces procedural placeholder)
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

The existing C# generators (`FloorMaterialGenerator.cs`, `SpriteShapeProfileGenerator.cs`) already have `File.Exists` guards — they skip texture creation when the file already exists. As long as the Python script's output filenames match exactly (`Floor_{Zone}.png`, `WallEdge_{Zone}.png`), re-running the C# generators will not overwrite the real art.
