# Level Art Generation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create `tools/generate_level_art.py` — a Python script that slices regions from the Free Undead Tileset sprite sheet, sends them to Gemini API for zone-specific style transformation, post-processes the output, and saves the results as game-ready level art assets.

**Architecture:** Single Python script mirroring `tools/generate_player_sprites.py` patterns. Slices ~128x128 regions from `temp_ref/Ground_rocks.png`, sends each to Gemini with zone-specific prompts using the style transfer + inpainting editing techniques, post-processes (watermark removal, resize), and saves to `Assets/Art/LevelArt/{Zone}/`. 8 total Gemini API calls (2 asset types × 4 zones).

**Tech Stack:** Python 3, `google-genai` SDK, `Pillow`, `numpy`, `python-dotenv`

**Spec:** `docs/superpowers/specs/2026-04-01-level-art-generation-design.md`

**Reference:** `tools/generate_player_sprites.py` (existing script to follow for API patterns, post-processing, CLI structure)

---

### Task 1: Script skeleton — imports, config, CLI argument parsing

This task creates the script file with all configuration constants, CLI argument parsing, and the empty `main()` entry point. No Gemini calls yet.

**Files:**
- Create: `tools/generate_level_art.py`

- [ ] **Step 1: Create the script with imports, config, and CLI**

```python
"""
Level Art Generator for Tome of Ruin
Uses Gemini API to generate zone-specific floor and wall edge sprites
from a base tileset sprite sheet (Free Undead Tileset).

Usage:
    python tools/generate_level_art.py
    python tools/generate_level_art.py --zones ZoneA ZoneB
    python tools/generate_level_art.py --dry-run
    python tools/generate_level_art.py --post-process-only

Requires:
    pip install google-genai Pillow numpy python-dotenv

Environment:
    GEMINI_API_KEY must be set
"""

import argparse
import os
import sys
import time
from pathlib import Path

from dotenv import load_dotenv
from google import genai
from google.genai import types
from PIL import Image
import numpy as np

# --- Configuration ---

MODEL = "gemini-3-pro-image-preview"
SPRITESHEET_PATH = Path("temp_ref/Ground_rocks.png")
OUTPUT_BASE = Path("Assets/Art/LevelArt")
RAW_DIR = OUTPUT_BASE / "raw"
RETRY_ATTEMPTS = 3
RETRY_DELAY = 5  # seconds
INTER_CALL_DELAY = 2  # seconds between API calls for rate limiting
WATERMARK_MARGIN = 80  # pixels from bottom-right corner to clear

FLOOR_SIZE = 1024  # final output resolution for floor textures
WALL_EDGE_SIZE = 512  # final output resolution for wall edge sprites

ZONES = ["ZoneA", "ZoneB", "ZoneC", "BossArena"]
ASSET_TYPES = ["floor", "wall_edge"]

# Sprite sheet crop regions (x1, y1, x2, y2) from Ground_rocks.png (496x592)
# Floor region: cracked stone ground textures in the bottom-right area
# Wall edge region: horizontal wall edge strips in the middle rows
REGIONS = {
    "floor": (368, 464, 496, 592),
    "wall_edge": (0, 192, 128, 320),
}


def main():
    parser = argparse.ArgumentParser(description="Generate zone-specific level art from base tileset")
    parser.add_argument("--zones", "-z", nargs="+", choices=ZONES, help="Only generate specific zones")
    parser.add_argument("--assets", "-a", nargs="+", choices=ASSET_TYPES, help="Only generate specific asset types")
    parser.add_argument("--dry-run", action="store_true", help="Print prompts without calling API")
    parser.add_argument("--post-process-only", action="store_true", help="Re-run post-processing on existing raw/ images")
    parser.add_argument("--no-post-process", action="store_true", help="Save raw Gemini output without post-processing")
    parser.add_argument("--force", action="store_true", help="Overwrite existing files (default: skip if exists)")
    parser.add_argument("--model", default=MODEL, help=f"Gemini model name (default: {MODEL})")
    args = parser.parse_args()

    zones = args.zones or ZONES
    asset_types = args.assets or ASSET_TYPES

    print(f"Level Art Generator")
    print(f"  Model: {args.model}")
    print(f"  Zones: {', '.join(zones)}")
    print(f"  Assets: {', '.join(asset_types)}")
    print(f"  Force overwrite: {args.force}")
    print()

    # TODO: pipeline steps will be added in later tasks


if __name__ == "__main__":
    main()
```

- [ ] **Step 2: Verify the script runs with --help and --dry-run**

Run: `python tools/generate_level_art.py --help`
Expected: Help output showing all CLI options

Run: `python tools/generate_level_art.py --dry-run`
Expected: Prints config header, no errors

- [ ] **Step 3: Commit**

```bash
git add tools/generate_level_art.py
git commit -m "feat: add level art generator skeleton with CLI parsing"
```

---

### Task 2: Region slicing and post-processing functions

Add the functions that slice regions from the sprite sheet and post-process Gemini output (watermark removal + resize). These are pure functions with no API dependency.

**Files:**
- Modify: `tools/generate_level_art.py`

- [ ] **Step 1: Add the slice_region function**

Add after the `REGIONS` dict:

```python
def slice_region(spritesheet: Image.Image, region_name: str) -> Image.Image:
    """Crop a region from the sprite sheet to use as Gemini input."""
    x1, y1, x2, y2 = REGIONS[region_name]
    region = spritesheet.crop((x1, y1, x2, y2))
    print(f"  Sliced {region_name} region: ({x1},{y1})-({x2},{y2}) = {region.size[0]}x{region.size[1]}")
    return region
```

- [ ] **Step 2: Add the post_process function**

Add after `slice_region`:

```python
def post_process(image: Image.Image, target_size: int) -> Image.Image:
    """Remove Gemini watermark and resize to target resolution."""
    image = image.convert("RGBA")
    data = np.array(image)
    h, w = data.shape[:2]

    # Clear Gemini watermark in bottom-right corner
    data[h - WATERMARK_MARGIN:, w - WATERMARK_MARGIN:] = [0, 0, 0, 0]

    image = Image.fromarray(data)

    # Resize to target size if needed
    if image.size[0] != target_size or image.size[1] != target_size:
        # Step down gradually for best quality: halve repeatedly, then final LANCZOS
        while image.size[0] > target_size * 2:
            half = (image.size[0] // 2, image.size[1] // 2)
            image = image.resize(half, Image.Resampling.LANCZOS)
        image = image.resize((target_size, target_size), Image.Resampling.LANCZOS)

    return image
```

- [ ] **Step 3: Add the to_pil helper (same as player sprite script)**

Add after `post_process`:

```python
def to_pil(image) -> Image.Image:
    """Convert a Gemini Image to a PIL Image if needed."""
    if isinstance(image, Image.Image):
        return image
    import tempfile
    with tempfile.NamedTemporaryFile(suffix=".png", delete=False) as tmp:
        tmp_path = tmp.name
        image.save(tmp_path)
    pil_img = Image.open(tmp_path).copy()
    os.unlink(tmp_path)
    return pil_img
```

- [ ] **Step 4: Test slicing manually**

Run: `python -c "from PIL import Image; img=Image.open('temp_ref/Ground_rocks.png'); floor=img.crop((368,464,496,592)); floor.save('temp_ref/test_floor_crop.png'); wall=img.crop((0,192,128,320)); wall.save('temp_ref/test_wall_crop.png'); print(f'floor: {floor.size}, wall: {wall.size}')"`

Expected: `floor: (128, 128), wall: (128, 128)` — visually inspect the two test crops to verify they contain the right content (floor textures and wall edges respectively). Delete test files after inspection.

**Important:** If the crops don't contain the expected content (floor tiles vs wall edges), adjust the `REGIONS` coordinates in the script. The sprite sheet layout is:
- Bottom-right area: cracked stone floor textures
- Middle rows: horizontal wall edge strips (dark stone tops transitioning to lighter floor)

- [ ] **Step 5: Commit**

```bash
git add tools/generate_level_art.py
git commit -m "feat: add region slicing and post-processing functions"
```

---

### Task 3: Zone prompts

Add the complete prompt configuration for all 4 zones × 2 asset types.

**Files:**
- Modify: `tools/generate_level_art.py`

- [ ] **Step 1: Add the ZONE_PROMPTS dict**

Add after the `REGIONS` dict (before the function definitions):

```python
# Zone-specific prompts for Gemini image editing
# Each zone has separate floor and wall_edge prompts
# Prompts use Gemini's inpainting + style transfer + adding elements patterns
ZONE_PROMPTS = {
    "ZoneA": {
        "floor": (
            "Using the provided image of a dark stone pixel art floor tileset, "
            "change only the colors and surface details to look like ancient, "
            "weathered ruins. Worn gray-green flagstones with dirt in the gaps "
            "and subtle dark moss patches. A muted, desaturated palette of grays "
            "with faint green accents. Add small cracks filled with dark grime. "
            "Keep the pixel art style, tile structure, crack patterns, and "
            "top-down perspective exactly the same. This is a seamless floor "
            "tile for a dark fantasy starting ruins area in a top-down 2D game."
        ),
        "wall_edge": (
            "Using the provided image of dark stone pixel art wall edges, "
            "change only the colors, surface texture, and decorative details "
            "to look like ancient overgrown ruins. Gray-green broken stone "
            "masonry with uneven blocks. Add dark green moss growing in the "
            "crevices and small vine tendrils creeping along the edges. Keep "
            "the stone surface rough and weathered. Keep the pixel art style, "
            "edge shape, and top-down perspective exactly the same. This is a "
            "wall edge sprite for a dark fantasy starting ruins area in a "
            "top-down 2D game."
        ),
    },
    "ZoneB": {
        "floor": (
            "Using the provided image of a dark stone pixel art floor tileset, "
            "change only the colors and surface details to look like underground "
            "catacombs. Shift the stone palette to warm browns and bone-white. "
            "Make the surface smoother and more worn-down. Add sandy dust texture "
            "in the tile gaps and tiny bone fragment chips scattered on the surface. "
            "Keep the pixel art style, tile structure, crack patterns, and "
            "top-down perspective exactly the same. This is a seamless floor "
            "tile for a dark fantasy catacomb level in a top-down 2D game."
        ),
        "wall_edge": (
            "Using the provided image of dark stone pixel art wall edges, "
            "change only the colors, surface texture, and decorative details "
            "to look like underground catacombs. Shift to warm brown worn stone. "
            "Make the surface smoother, as if eroded by centuries underground. "
            "Add small bone fragments and skull chips embedded in the crevices "
            "between stones. Keep the pixel art style, edge shape, and top-down "
            "perspective exactly the same. This is a wall edge sprite for a "
            "dark fantasy catacomb level in a top-down 2D game."
        ),
    },
    "ZoneC": {
        "floor": (
            "Using the provided image of a dark stone pixel art floor tileset, "
            "change only the colors and surface details to look like a cursed "
            "chapel. Shift the stone palette to deep purple-gray and dark crimson. "
            "Add thin ritual scratch marks across the tile surface and faint "
            "crimson stains seeping between the cracks, like dried blood in the "
            "grout lines. Keep the pixel art style, tile structure, crack patterns, "
            "and top-down perspective exactly the same. This is a seamless floor "
            "tile for a dark fantasy cursed chapel in a top-down 2D game."
        ),
        "wall_edge": (
            "Using the provided image of dark stone pixel art wall edges, "
            "change only the colors, surface texture, and decorative details "
            "to look like a cursed chapel. Shift to deep purple-gray ornate "
            "carved stone. Add thin carved ritual markings along the stone faces, "
            "with crimson stains bleeding down from the crevices. Make the "
            "stonework look more deliberately shaped, less natural. Keep the "
            "pixel art style, edge shape, and top-down perspective exactly the "
            "same. This is a wall edge sprite for a dark fantasy cursed chapel "
            "in a top-down 2D game."
        ),
    },
    "BossArena": {
        "floor": (
            "Using the provided image of a dark stone pixel art floor tileset, "
            "change only the colors and surface details to look like a scorched "
            "boss arena. Shift the stone palette to near-black obsidian with "
            "subtle blue-gray veining. Add faint orange-red glow lines in the "
            "deepest cracks, like cooling magma beneath the surface. Minimal "
            "surface detail — smooth, oppressive, empty. Keep the pixel art style, "
            "tile structure, crack patterns, and top-down perspective exactly the "
            "same. This is a seamless floor tile for a dark fantasy boss arena "
            "in a top-down 2D game."
        ),
        "wall_edge": (
            "Using the provided image of dark stone pixel art wall edges, "
            "change only the colors, surface texture, and decorative details "
            "to look like a scorched boss arena. Shift to near-black scorched "
            "stone with a melted, fused appearance. The edges should look "
            "heat-warped, with subtle orange-red glow in the deepest gaps. "
            "Fewer individual stones visible — more monolithic and oppressive. "
            "Keep the pixel art style, edge shape, and top-down perspective "
            "exactly the same. This is a wall edge sprite for a dark fantasy "
            "boss arena in a top-down 2D game."
        ),
    },
}
```

- [ ] **Step 2: Verify prompts with dry-run**

Add a temporary print in `main()` to verify prompts load correctly:

```python
    # Temporary verification — remove after confirming
    for zone in zones:
        for asset in asset_types:
            prompt = ZONE_PROMPTS[zone][asset]
            print(f"  {zone}/{asset}: {len(prompt)} chars")
            if args.dry_run:
                print(f"    Prompt: {prompt[:100]}...")
    print()
```

Run: `python tools/generate_level_art.py --dry-run`
Expected: All 8 prompts print their length and first 100 chars, no KeyErrors

- [ ] **Step 3: Commit**

```bash
git add tools/generate_level_art.py
git commit -m "feat: add zone-specific prompts for floor and wall edge generation"
```

---

### Task 4: Gemini API call function

Add the `generate_image` function that sends a region + prompt to Gemini with the correct config.

**Files:**
- Modify: `tools/generate_level_art.py`

- [ ] **Step 1: Add the generate_image function**

Add after the `to_pil` function:

```python
def generate_image(client, region_image: Image.Image, prompt: str, model: str) -> Image.Image | None:
    """Send a region image + text prompt to Gemini and return the generated PIL image."""
    for attempt in range(RETRY_ATTEMPTS):
        try:
            response = client.models.generate_content(
                model=model,
                contents=[region_image, prompt],
                config=types.GenerateContentConfig(
                    response_modalities=["TEXT", "IMAGE"],
                    image_config=types.ImageConfig(
                        image_size="1K",
                        aspect_ratio="1:1",
                    ),
                ),
            )
            for part in response.parts:
                if part.inline_data is not None:
                    return to_pil(part.as_image())
            print(f"  WARNING: No image in response. Text: {response.text}")
            return None
        except Exception as e:
            print(f"  Attempt {attempt + 1}/{RETRY_ATTEMPTS} failed: {e}")
            if attempt < RETRY_ATTEMPTS - 1:
                print(f"  Retrying in {RETRY_DELAY}s...")
                time.sleep(RETRY_DELAY)
    return None
```

- [ ] **Step 2: Commit**

```bash
git add tools/generate_level_art.py
git commit -m "feat: add Gemini API call function with retry and 1K config"
```

---

### Task 5: Main pipeline — generation loop and post-process-only mode

Wire everything together in `main()`: load the sprite sheet, slice regions, iterate zones × asset types, call Gemini, post-process, save.

**Files:**
- Modify: `tools/generate_level_art.py`

- [ ] **Step 1: Replace the main() function body (after arg parsing)**

Replace the placeholder content in `main()` (everything after `print()` at the end of the config summary) with the full pipeline:

```python
    # Post-process only mode: apply post-processing to all PNGs in raw/
    if args.post_process_only:
        if not RAW_DIR.exists():
            print(f"ERROR: Raw directory not found: {RAW_DIR}")
            sys.exit(1)
        pngs = sorted(RAW_DIR.glob("*.png"))
        print(f"Post-processing {len(pngs)} images from {RAW_DIR}")
        for png_path in pngs:
            img = Image.open(png_path)
            # Determine target size from filename
            if "floor_" in png_path.name:
                target_size = FLOOR_SIZE
            else:
                target_size = WALL_EDGE_SIZE
            # Determine zone from filename (e.g., floor_ZoneA_raw.png -> ZoneA)
            zone = png_path.stem.split("_")[1]
            if png_path.stem.startswith("floor_"):
                out_name = f"Floor_{zone}.png"
            else:
                out_name = f"WallEdge_{zone}.png"
            out_path = OUTPUT_BASE / zone / out_name
            processed = post_process(img, target_size)
            processed.save(out_path)
            print(f"  [PP] {png_path.name} -> {out_path}")
        print("Done.")
        return

    # Load sprite sheet
    if not SPRITESHEET_PATH.exists():
        print(f"ERROR: Sprite sheet not found: {SPRITESHEET_PATH}")
        print("Expected: temp_ref/Ground_rocks.png (extract from Free Undead Tileset pack)")
        sys.exit(1)

    spritesheet = Image.open(SPRITESHEET_PATH)
    print(f"Loaded sprite sheet: {SPRITESHEET_PATH} ({spritesheet.size[0]}x{spritesheet.size[1]})")

    # Slice regions
    print("\n=== Slicing regions ===")
    regions = {}
    for asset_type in asset_types:
        regions[asset_type] = slice_region(spritesheet, asset_type)

    # Load .env and initialize client
    load_dotenv()
    client = None
    if not args.dry_run:
        api_key = os.environ.get("GEMINI_API_KEY")
        if not api_key:
            print("ERROR: GEMINI_API_KEY environment variable not set")
            sys.exit(1)
        client = genai.Client(api_key=api_key)

    # Ensure output directories exist
    RAW_DIR.mkdir(parents=True, exist_ok=True)
    for zone in zones:
        (OUTPUT_BASE / zone).mkdir(parents=True, exist_ok=True)

    # --- Generate assets ---
    print("\n=== Generating level art ===")
    total_generated = 0
    total_skipped = 0
    total_failed = 0

    for zone in zones:
        print(f"\n--- {zone} ---")
        for asset_type in asset_types:
            # Determine output path and target size
            if asset_type == "floor":
                out_name = f"Floor_{zone}.png"
                target_size = FLOOR_SIZE
                raw_name = f"floor_{zone}_raw.png"
            else:
                out_name = f"WallEdge_{zone}.png"
                target_size = WALL_EDGE_SIZE
                raw_name = f"wall_edge_{zone}_raw.png"

            out_path = OUTPUT_BASE / zone / out_name
            raw_path = RAW_DIR / raw_name

            # Skip if exists and not forcing
            if out_path.exists() and not args.force:
                print(f"  [SKIP] {out_name} (exists, use --force to overwrite)")
                total_skipped += 1
                continue

            prompt = ZONE_PROMPTS[zone][asset_type]
            print(f"  [GEN] {out_name}")

            if args.dry_run:
                print(f"    Prompt ({len(prompt)} chars): {prompt[:120]}...")
                continue

            result = generate_image(client, regions[asset_type], prompt, args.model)
            if result:
                # Save raw output
                result.save(raw_path)
                print(f"    Raw saved: {raw_path}")

                # Post-process and save
                if args.no_post_process:
                    result.save(out_path)
                else:
                    processed = post_process(result, target_size)
                    processed.save(out_path)
                total_generated += 1
                print(f"    Saved: {out_path} ({target_size}x{target_size})")
            else:
                total_failed += 1
                print(f"    FAILED")

            # Rate limiting between API calls
            if not args.dry_run:
                time.sleep(INTER_CALL_DELAY)

    # --- Summary ---
    print(f"\n=== Summary ===")
    print(f"Generated: {total_generated}")
    print(f"Skipped: {total_skipped}")
    print(f"Failed: {total_failed}")
    print(f"Output: {OUTPUT_BASE}")
```

- [ ] **Step 2: Remove the temporary verification print from Task 3**

Remove the temporary prompt verification loop that was added in Task 3 Step 2 (the `for zone in zones: for asset in asset_types: ...` block). The main pipeline now handles `--dry-run` output directly.

- [ ] **Step 3: Test with --dry-run**

Run: `python tools/generate_level_art.py --dry-run`

Expected output (approximately):
```
Level Art Generator
  Model: gemini-3-pro-image-preview
  Zones: ZoneA, ZoneB, ZoneC, BossArena
  Assets: floor, wall_edge
  Force overwrite: False

Loaded sprite sheet: temp_ref/Ground_rocks.png (496x592)

=== Slicing regions ===
  Sliced floor region: (368,464)-(496,592) = 128x128
  Sliced wall_edge region: (0,192)-(128,320) = 128x128

=== Generating level art ===

--- ZoneA ---
  [SKIP] Floor_ZoneA.png (exists, use --force to overwrite)
  [SKIP] WallEdge_ZoneA.png (exists, use --force to overwrite)
...
```

Run: `python tools/generate_level_art.py --dry-run --force`

Expected: All 8 assets show `[GEN]` with prompt preview instead of `[SKIP]`

Run: `python tools/generate_level_art.py --dry-run --force --zones ZoneA --assets floor`

Expected: Only `Floor_ZoneA.png` shows `[GEN]`

- [ ] **Step 4: Commit**

```bash
git add tools/generate_level_art.py
git commit -m "feat: wire up main generation pipeline with skip/force/dry-run"
```

---

### Task 6: End-to-end test with Gemini API

Run the script for real against the Gemini API for a single zone to validate the full pipeline. Then run the remaining zones.

**Files:**
- No code changes — this is a manual validation task

- [ ] **Step 1: Test single zone generation**

Run: `python tools/generate_level_art.py --force --zones ZoneA`

Expected:
- Two Gemini API calls (floor + wall edge)
- Raw images saved to `Assets/Art/LevelArt/raw/floor_ZoneA_raw.png` and `Assets/Art/LevelArt/raw/wall_edge_ZoneA_raw.png`
- Processed images saved to `Assets/Art/LevelArt/ZoneA/Floor_ZoneA.png` (1024x1024) and `Assets/Art/LevelArt/ZoneA/WallEdge_ZoneA.png` (512x512)

Verify:
```bash
python -c "from PIL import Image; img=Image.open('Assets/Art/LevelArt/ZoneA/Floor_ZoneA.png'); print(f'Floor: {img.size} {img.mode}'); img=Image.open('Assets/Art/LevelArt/ZoneA/WallEdge_ZoneA.png'); print(f'WallEdge: {img.size} {img.mode}')"
```

Expected: `Floor: (1024, 1024) RGBA` and `WallEdge: (512, 512) RGBA`

Visually inspect both images — do they look like zone-appropriate dark fantasy pixel art tileset variants?

- [ ] **Step 2: If ZoneA looks good, generate remaining zones**

Run: `python tools/generate_level_art.py --force --zones ZoneB ZoneC BossArena`

Expected: 6 more Gemini API calls, all zones populated.

Visually inspect all 8 output images. Each zone should have a distinct visual identity:
- ZoneA: gray-green, mossy
- ZoneB: warm brown, bone fragments
- ZoneC: purple-crimson, ritual markings
- BossArena: near-black, glowing cracks

- [ ] **Step 3: If any outputs look wrong, iterate on prompts**

If a zone's output doesn't match expectations, adjust the prompt in `ZONE_PROMPTS` and re-run:

```bash
python tools/generate_level_art.py --force --zones <problem_zone> --assets <problem_asset>
```

Repeat until all 8 assets look acceptable.

- [ ] **Step 4: Verify skip-if-exists behavior**

Run: `python tools/generate_level_art.py`

Expected: All 8 assets show `[SKIP]` (no API calls made)

Run: `python tools/generate_level_art.py --force --zones ZoneA --assets floor`

Expected: Only Floor_ZoneA is regenerated

- [ ] **Step 5: Commit the script and generated assets**

```bash
git add tools/generate_level_art.py
git add Assets/Art/LevelArt/
git commit -m "feat: generate level art — floor textures and wall edges for all 4 zones"
```

---

### Task 7: Post-process-only mode verification

Verify that the `--post-process-only` flag works correctly, allowing re-processing of raw images with adjusted parameters.

**Files:**
- No code changes — validation only

- [ ] **Step 1: Test post-process-only mode**

Run: `python tools/generate_level_art.py --post-process-only`

Expected: All raw images in `Assets/Art/LevelArt/raw/` are re-processed and saved to their zone directories. Output matches the previously generated files.

- [ ] **Step 2: Verify by comparing file sizes**

The re-processed files should be similar in size to the originals (may differ slightly due to PNG compression non-determinism, but dimensions must match).

```bash
python -c "
from PIL import Image
import os
for zone in ['ZoneA','ZoneB','ZoneC','BossArena']:
    base = f'Assets/Art/LevelArt/{zone}'
    for f in sorted(os.listdir(base)):
        if f.endswith('.png'):
            img = Image.open(f'{base}/{f}')
            print(f'{base}/{f}: {img.size} {img.mode}')
"
```

Expected: All floors 1024x1024, all wall edges 512x512, all RGBA.

- [ ] **Step 3: Commit if any changes**

If post-process-only produced updated files:
```bash
git add Assets/Art/LevelArt/
git commit -m "asset: re-processed level art with updated post-processing"
```
