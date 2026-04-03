"""
Level Art Generator for Tome of Ruin
Uses Gemini API to generate per-zone floor and wall edge tiles from a tileset sprite sheet.

Usage:
    python tools/generate_level_art.py [--zones ZoneA ZoneB ...] [--assets floor wall_edge] [--dry-run]

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
SPRITESHEET_PATHS = {
    "ground": Path("temp_ref/Ground_rocks.png"),
    "coast": Path("temp_ref/Water_coasts.png"),
}
# Legacy alias for backward compat
SPRITESHEET_PATH = SPRITESHEET_PATHS["ground"]
OUTPUT_BASE = Path("Assets/Art/LevelArt")
RAW_DIR = OUTPUT_BASE / "raw"
RETRY_ATTEMPTS = 3
RETRY_DELAY = 5  # seconds
INTER_CALL_DELAY = 2  # seconds between API calls for rate limiting
WATERMARK_MARGIN = 80  # pixels from bottom-right corner to fill for Gemini watermark
FLOOR_SIZE = 2048   # final output resolution for floor tiles (2K)
WALL_EDGE_SIZE = 512  # final output resolution for wall edge sprites
DEPTH_SIZE = 512    # final output resolution for depth feature sprites
ZONES = ["ZoneA", "ZoneB", "ZoneC", "BossArena"]
ASSET_TYPES = ["floor", "wall_edge", "boulder", "cave", "rock_column", "stairs"]

# Pixel regions to slice from spritesheets: (sheet_key, left, top, right, bottom)
REGIONS = {
    "floor": ("ground", 368, 464, 496, 592),
    "wall_edge": ("ground", 0, 192, 128, 320),
    "boulder": ("coast", 64, 128, 128, 192),         # 64x64 single boulder with drop shadow
    "cave": ("ground", 0, 0, 96, 112),               # 96x112 oval cave/pit entrance
    "rock_column": ("ground", 432, 48, 464, 112),    # 32x64 stacked stone column/pillar
    "stairs": ("coast", 768, 128, 832, 192),          # 64x64 single stone stair pile
}

# --- Zone Prompts ---

ZONE_PROMPTS = {
    "ZoneA": {
        "floor": (
            "Generate a seamless tileable top-down dark fantasy floor texture in pixel art style. "
            "Ancient weathered ruins: irregular gray-green flagstones of varying sizes and shapes, with dark dirt "
            "and grime filling the gaps between stones. Subtle dark moss patches growing in random corners. "
            "Small cracks of different lengths and directions across the stones. Each stone should look slightly "
            "different — vary the shade, size, wear pattern, and crack density so no two stones are identical. "
            "Muted desaturated palette of grays with faint green accents. The texture must tile seamlessly in "
            "all directions with no visible seam lines. Avoid any obvious repeating grid pattern."
        ),
        "wall_edge": (
            "Using the provided image of dark stone pixel art wall edges, change only the colors, surface texture, "
            "and decorative details to look like ancient overgrown ruins. Gray-green broken stone masonry with "
            "uneven blocks. Add dark green moss growing in the crevices and small vine tendrils creeping along the "
            "edges. Keep the stone surface rough and weathered. Keep the pixel art style, edge shape, and top-down "
            "perspective exactly the same. This is a wall edge sprite for a dark fantasy starting ruins area in a "
            "top-down 2D game."
        ),
        "boulder": (
            "Using the provided image of a top-down boulder with shadow, change only the colors and surface "
            "texture to look like a moss-covered ancient ruin stone. Gray-green weathered rock with dark green "
            "moss patches and subtle lichen. Keep the exact same shape, shadow, and top-down perspective. "
            "The transparent background must remain transparent. This is a decorative boulder for a dark fantasy "
            "ruins zone in a top-down 2D game."
        ),
        "cave": (
            "Using the provided image of an oval cave opening seen from top-down, change only the colors and "
            "surface texture to look like an ancient ruin pit or cave entrance. Gray-green weathered stone rim "
            "with moss growing along the edges. The dark interior should stay dark. Keep the exact same oval shape, "
            "shadow, and top-down perspective. The transparent background must remain transparent. "
            "This is a cave/pit entrance for a dark fantasy ruins zone in a top-down 2D game."
        ),
        "rock_column": (
            "Using the provided image of a stacked stone column seen from top-down, change only the colors and "
            "surface texture to look like a crumbling ancient ruin pillar. Gray-green weathered stone with moss "
            "growing in the cracks between blocks. Keep the exact same shape, stacking, and top-down perspective. "
            "The transparent background must remain transparent. This is a decorative pillar for a dark fantasy "
            "ruins zone in a top-down 2D game."
        ),
        "stairs": (
            "Using the provided image of stepped stone piles seen from top-down, change only the colors and "
            "surface texture to look like crumbling ancient ruin steps. Gray-green weathered stone blocks, "
            "irregular and broken, with moss growing between the cracks. Keep the exact same shape, stacking, "
            "shadow, and top-down perspective. The transparent background must remain transparent. "
            "This is a stairs sprite for a dark fantasy ruins zone in a top-down 2D game."
        ),
    },
    "ZoneB": {
        "floor": (
            "Generate a seamless tileable top-down dark fantasy floor texture in pixel art style. "
            "Underground catacombs: smooth worn-down stone in warm browns and bone-white, eroded by centuries. "
            "Sandy dust fills the gaps between stones. Tiny bone fragment chips and teeth scattered randomly "
            "across the surface — some half-buried in dust, others loose. Occasional larger skull fragment or "
            "rib bone shard embedded in the stone. Each stone varies in size, shade, and wear. "
            "The texture must tile seamlessly in all directions with no visible seam lines. "
            "Avoid any obvious repeating grid pattern."
        ),
        "wall_edge": (
            "Using the provided image of dark stone pixel art wall edges, change only the colors, surface texture, "
            "and decorative details to look like underground catacombs. Shift to warm brown worn stone. Make the "
            "surface smoother, as if eroded by centuries underground. Add small bone fragments and skull chips "
            "embedded in the crevices between stones. Keep the pixel art style, edge shape, and top-down "
            "perspective exactly the same. This is a wall edge sprite for a dark fantasy catacomb level in a "
            "top-down 2D game."
        ),
        "boulder": (
            "Using the provided image of a top-down boulder with shadow, change only the colors and surface "
            "texture to look like a catacomb rock formation. Warm brown smooth stone, eroded and rounded by "
            "centuries underground. Bone-white mineral deposits on the surface. Keep the exact same shape, shadow, "
            "and top-down perspective. The transparent background must remain transparent. This is a decorative "
            "boulder for a dark fantasy catacomb in a top-down 2D game."
        ),
        "cave": (
            "Using the provided image of an oval cave opening seen from top-down, change only the colors and "
            "surface texture to look like a catacomb pit or ossuary entrance. Warm brown eroded stone rim with "
            "bone fragments embedded along the edges. The dark interior should stay dark. Keep the exact same "
            "oval shape, shadow, and top-down perspective. The transparent background must remain transparent. "
            "This is a cave/pit entrance for a dark fantasy catacomb in a top-down 2D game."
        ),
        "rock_column": (
            "Using the provided image of a stacked stone column seen from top-down, change only the colors and "
            "surface texture to look like a worn catacomb pillar. Warm brown smooth stone, eroded by centuries. "
            "Bone-white mineral deposits in the cracks. Keep the exact same shape, stacking, and top-down "
            "perspective. The transparent background must remain transparent. This is a decorative pillar for "
            "a dark fantasy catacomb in a top-down 2D game."
        ),
        "stairs": (
            "Using the provided image of stepped stone piles seen from top-down, change only the colors and "
            "surface texture to look like worn catacomb steps. Warm brown stone, smoothed by centuries of foot "
            "traffic, with sandy dust accumulated in the corners. Keep the exact same shape, stacking, shadow, "
            "and top-down perspective. The transparent background must remain transparent. "
            "This is a stairs sprite for a dark fantasy catacomb in a top-down 2D game."
        ),
    },
    "ZoneC": {
        "floor": (
            "Generate a seamless tileable top-down dark fantasy floor texture in pixel art style. "
            "Cursed chapel: cracked dark slate tiles in deep purple-gray with dark crimson grout lines that look "
            "like dried blood seeping between the stones. Thin ritual scratch marks etched randomly across some "
            "tiles — pentagrams, runes, claw marks. Faint crimson stains splattered unevenly. Scattered glass "
            "shards from broken stained glass catch faint light. Each tile varies in shade and damage level. "
            "The texture must tile seamlessly in all directions with no visible seam lines. "
            "Avoid any obvious repeating grid pattern."
        ),
        "wall_edge": (
            "Using the provided image of dark stone pixel art wall edges, change only the colors, surface texture, "
            "and decorative details to look like a cursed chapel. Shift to deep purple-gray ornate carved stone. "
            "Add thin carved ritual markings along the stone faces, with crimson stains bleeding down from the "
            "crevices. Make the stonework look more deliberately shaped, less natural. Keep the pixel art style, "
            "edge shape, and top-down perspective exactly the same. This is a wall edge sprite for a dark fantasy "
            "cursed chapel in a top-down 2D game."
        ),
        "boulder": (
            "Using the provided image of a top-down boulder with shadow, change only the colors and surface "
            "texture to look like a cursed chapel rubble stone. Deep purple-gray carved stone, deliberately shaped "
            "but now broken. Faint crimson stains and ritual scratch marks on the surface. Keep the exact same "
            "shape, shadow, and top-down perspective. The transparent background must remain transparent. "
            "This is a decorative boulder for a dark fantasy cursed chapel in a top-down 2D game."
        ),
        "cave": (
            "Using the provided image of an oval cave opening seen from top-down, change only the colors and "
            "surface texture to look like a cursed chapel pit or ritual well. Deep purple-gray ornate stone rim "
            "with crimson stains dripping into the darkness. Faint ritual scratch marks along the edge. Keep the "
            "exact same oval shape, shadow, and top-down perspective. The transparent background must remain "
            "transparent. This is a cave/pit entrance for a dark fantasy cursed chapel in a top-down 2D game."
        ),
        "rock_column": (
            "Using the provided image of a stacked stone column seen from top-down, change only the colors and "
            "surface texture to look like a cursed chapel pillar. Deep purple-gray ornate carved stone with "
            "ritual markings and crimson stains between blocks. Keep the exact same shape, stacking, and top-down "
            "perspective. The transparent background must remain transparent. This is a decorative pillar for "
            "a dark fantasy cursed chapel in a top-down 2D game."
        ),
        "stairs": (
            "Using the provided image of stepped stone piles seen from top-down, change only the colors and "
            "surface texture to look like cursed chapel steps. Deep purple-gray ornate carved stone blocks "
            "with crimson stains seeping between steps. Keep the exact same shape, stacking, shadow, and "
            "top-down perspective. The transparent background must remain transparent. "
            "This is a stairs sprite for a dark fantasy cursed chapel in a top-down 2D game."
        ),
    },
    "BossArena": {
        "floor": (
            "Generate a seamless tileable top-down dark fantasy floor texture in pixel art style. "
            "Boss arena: dark polished obsidian stone, near-black with subtle blue-gray veining running through "
            "it like marble. Faint orange-red glow lines in the deepest cracks, like cooling magma beneath the "
            "surface. The stone is smoother and more uniform than natural rock — deliberately shaped, oppressive. "
            "Minimal surface detail. Occasional hairline fracture. Very dark, almost black overall. "
            "The texture must tile seamlessly in all directions with no visible seam lines. "
            "Avoid any obvious repeating grid pattern."
        ),
        "wall_edge": (
            "Using the provided image of dark stone pixel art wall edges, change only the colors, surface texture, "
            "and decorative details to look like a scorched boss arena. Shift to near-black scorched stone with a "
            "melted, fused appearance. The edges should look heat-warped, with subtle orange-red glow in the "
            "deepest gaps. Fewer individual stones visible — more monolithic and oppressive. Keep the pixel art "
            "style, edge shape, and top-down perspective exactly the same. This is a wall edge sprite for a dark "
            "fantasy boss arena in a top-down 2D game."
        ),
        "boulder": (
            "Using the provided image of a top-down boulder with shadow, change only the colors and surface "
            "texture to look like scorched obsidian. Near-black fused stone with subtle blue-gray veining and "
            "faint orange-red glow in the deepest cracks. Smooth, melted appearance. Keep the exact same shape, "
            "shadow, and top-down perspective. The transparent background must remain transparent. "
            "This is a decorative boulder for a dark fantasy boss arena in a top-down 2D game."
        ),
        "cave": (
            "Using the provided image of an oval cave opening seen from top-down, change only the colors and "
            "surface texture to look like a scorched magma vent. Near-black obsidian rim with faint orange-red "
            "glow seeping from the interior. Melted, fused appearance. Keep the exact same oval shape, shadow, "
            "and top-down perspective. The transparent background must remain transparent. "
            "This is a cave/pit entrance for a dark fantasy boss arena in a top-down 2D game."
        ),
        "rock_column": (
            "Using the provided image of a stacked stone column seen from top-down, change only the colors and "
            "surface texture to look like a scorched obsidian pillar. Near-black fused stone with faint orange-red "
            "glow in the cracks between blocks. Melted, monolithic appearance. Keep the exact same shape, stacking, "
            "and top-down perspective. The transparent background must remain transparent. This is a decorative "
            "pillar for a dark fantasy boss arena in a top-down 2D game."
        ),
        "stairs": (
            "Using the provided image of stepped stone piles seen from top-down, change only the colors and "
            "surface texture to look like scorched obsidian steps. Near-black fused stone blocks with a melted "
            "appearance. Faint orange-red glow seeping between the steps. Keep the exact same shape, stacking, "
            "shadow, and top-down perspective. The transparent background must remain transparent. "
            "This is a stairs sprite for a dark fantasy boss arena in a top-down 2D game."
        ),
    },
}


def slice_region(sheet: Image.Image, region: tuple) -> Image.Image:
    """Slice a rectangular region from a sprite sheet. region = (left, top, right, bottom)."""
    left, top, right, bottom = region
    return sheet.crop((left, top, right, bottom))


def remove_white_background(data: np.ndarray, threshold: int = 220) -> np.ndarray:
    """Make white/near-white pixels transparent, with feathered edges."""
    white_mask = (data[:,:,0] > threshold) & (data[:,:,1] > threshold) & (data[:,:,2] > threshold)
    data[white_mask, 3] = 0

    # Feather near-white edges for smoother transition
    near_white = (
        (data[:,:,0] > threshold - 30) & (data[:,:,1] > threshold - 30) &
        (data[:,:,2] > threshold - 30) & ~white_mask
    )
    if near_white.any():
        brightness = (
            data[near_white, 0].astype(float) + data[near_white, 1].astype(float) +
            data[near_white, 2].astype(float)
        ) / 3
        alpha_factor = 1.0 - (brightness - (threshold - 30)) / 30.0
        alpha_factor = np.clip(alpha_factor, 0, 1)
        data[near_white, 3] = (alpha_factor * 255).astype(np.uint8)

    return data


def post_process(image: Image.Image, target_size: int, remove_bg: bool = False) -> Image.Image:
    """Fill Gemini watermark region with average border color, then resize."""
    image = image.convert("RGBA")
    data = np.array(image)
    h, w = data.shape[:2]

    # Fill watermark region with average color of surrounding border (NOT transparent)
    if min(h, w) < WATERMARK_MARGIN * 2:
        print(f"  WARNING: Image too small ({w}x{h}) for watermark removal — skipping fill")
    else:
        border_top = data[h - WATERMARK_MARGIN - 1, w - WATERMARK_MARGIN:]
        border_left = data[h - WATERMARK_MARGIN:, w - WATERMARK_MARGIN - 1]
        avg_color = np.concatenate([border_top, border_left]).mean(axis=0).astype(np.uint8)
        data[h - WATERMARK_MARGIN:, w - WATERMARK_MARGIN:] = avg_color

    # Remove white background for wall edge sprites
    if remove_bg:
        data = remove_white_background(data)

    image = Image.fromarray(data)

    # Resize to target size (step down gradually for best quality)
    if image.size[0] != target_size or image.size[1] != target_size:
        while image.size[0] > target_size * 2:
            half = (image.size[0] // 2, image.size[1] // 2)
            image = image.resize(half, Image.Resampling.LANCZOS)
        image = image.resize((target_size, target_size), Image.Resampling.LANCZOS)

    return image


def to_pil(image) -> Image.Image:
    """Convert a Gemini Image to a PIL Image if needed."""
    if isinstance(image, Image.Image):
        return image
    # Gemini SDK Image -> PIL via temp file
    import tempfile
    with tempfile.NamedTemporaryFile(suffix=".png", delete=False) as tmp:
        tmp_path = tmp.name
        image.save(tmp_path)
    pil_img = Image.open(tmp_path).copy()
    os.unlink(tmp_path)
    return pil_img


def generate_image(client, region_image: Image.Image, prompt: str, model: str) -> Image.Image | None:
    """Send an image + text prompt to Gemini and return the generated PIL image."""
    for attempt in range(RETRY_ATTEMPTS):
        try:
            response = client.models.generate_content(
                model=model,
                contents=[region_image, prompt],  # image first, then text
                config=types.GenerateContentConfig(
                    response_modalities=["TEXT", "IMAGE"],
                    image_config=types.ImageConfig(
                        image_size="2K",
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


def target_size_for(asset_type: str) -> int:
    """Return the final output pixel size for an asset type."""
    if asset_type == "floor":
        return FLOOR_SIZE
    if asset_type == "wall_edge":
        return WALL_EDGE_SIZE
    return DEPTH_SIZE


# Asset type -> filename prefix mapping
ASSET_FILENAMES = {
    "floor": "Floor",
    "wall_edge": "WallEdge",
    "boulder": "Boulder",
    "cave": "Cave",
    "rock_column": "RockColumn",
    "stairs": "Stairs",
}


def output_filename(asset_type: str, zone: str) -> str:
    """Return the final output filename for a zone/asset combo."""
    prefix = ASSET_FILENAMES.get(asset_type, asset_type.title())
    return f"{prefix}_{zone}.png"


def raw_filename(asset_type: str, zone: str) -> str:
    """Return the raw (pre-post-process) filename for a zone/asset combo."""
    return f"{asset_type}_{zone}_raw.png"


def main():
    parser = argparse.ArgumentParser(
        description="Generate level art tiles for each zone using Gemini API"
    )
    parser.add_argument(
        "--zones", nargs="+", default=ZONES,
        choices=ZONES, metavar="ZONE",
        help=f"Zones to generate (default: all). Choices: {', '.join(ZONES)}"
    )
    parser.add_argument(
        "--assets", nargs="+", default=ASSET_TYPES,
        choices=ASSET_TYPES, metavar="ASSET",
        help=f"Asset types to generate (default: all). Choices: {', '.join(ASSET_TYPES)}"
    )
    parser.add_argument(
        "--dry-run", action="store_true",
        help="Print prompts and file paths without calling the API"
    )
    parser.add_argument(
        "--post-process-only", action="store_true",
        help="Re-run post-processing on existing raw images only (no API calls)"
    )
    parser.add_argument(
        "--no-post-process", action="store_true",
        help="Save raw API output without post-processing"
    )
    parser.add_argument(
        "--force", action="store_true",
        help="Overwrite existing output files (default: skip if output exists)"
    )
    parser.add_argument(
        "--model", default=MODEL,
        help=f"Gemini model to use (default: {MODEL})"
    )
    args = parser.parse_args()

    # --- Post-process-only mode ---
    if args.post_process_only:
        if not RAW_DIR.exists():
            print(f"ERROR: Raw directory not found: {RAW_DIR}")
            sys.exit(1)
        pngs = sorted(RAW_DIR.glob("*.png"))
        print(f"Post-processing {len(pngs)} raw images from {RAW_DIR}")
        processed = 0
        for png_path in pngs:
            stem = png_path.stem  # e.g. floor_ZoneA_raw or wall_edge_ZoneA_raw
            # Parse: {asset_type}_{zone}_raw
            # asset_type may contain underscores (wall_edge, cliff_edge)
            parts = stem.rsplit("_raw", 1)[0]  # strip _raw suffix
            asset_type = None
            zone = None
            for at in sorted(ASSET_FILENAMES.keys(), key=len, reverse=True):
                if parts.startswith(at + "_"):
                    asset_type = at
                    zone = parts[len(at) + 1:]
                    break
            if asset_type is None or zone is None:
                print(f"  [SKIP] {png_path.name} (unrecognized naming pattern)")
                continue
            out_name = output_filename(asset_type, zone)
            target_size = target_size_for(asset_type)

            if zone not in ZONES:
                print(f"  [SKIP] {png_path.name} (zone '{zone}' not in ZONES list)")
                continue

            zone_dir = OUTPUT_BASE / zone
            zone_dir.mkdir(parents=True, exist_ok=True)
            out_path = zone_dir / out_name

            img = Image.open(png_path)
            is_floor = stem.startswith("floor_")
            result = post_process(img, target_size, remove_bg=not is_floor)
            result.save(out_path)
            print(f"  [PP] {png_path.name} -> {out_path}")
            processed += 1

        print(f"\nDone. Post-processed {processed} image(s).")
        return

    # --- Normal generation mode ---
    load_dotenv()

    api_key = os.environ.get("GEMINI_API_KEY")
    if not api_key and not args.dry_run:
        print("ERROR: GEMINI_API_KEY environment variable not set")
        sys.exit(1)

    # Load sprite sheets
    sheets = {}
    for key, path in SPRITESHEET_PATHS.items():
        if not path.exists():
            # Only error if we actually need this sheet
            needed = any(REGIONS[a][0] == key for a in args.assets if a in REGIONS)
            if needed:
                print(f"ERROR: Spritesheet not found: {path}")
                sys.exit(1)
            continue
        sheets[key] = Image.open(path)
        print(f"Loaded spritesheet: {path} ({sheets[key].size[0]}x{sheets[key].size[1]})")

    # Slice regions up front
    region_images = {}
    for asset_type, region_def in REGIONS.items():
        sheet_key = region_def[0]
        region = region_def[1:]
        if sheet_key not in sheets:
            continue
        sliced = slice_region(sheets[sheet_key], region)
        region_images[asset_type] = sliced
        print(f"  Sliced region '{asset_type}' from {sheet_key}: {region} -> {sliced.size[0]}x{sliced.size[1]}")

    # Create output directories
    RAW_DIR.mkdir(parents=True, exist_ok=True)
    for zone in args.zones:
        (OUTPUT_BASE / zone).mkdir(parents=True, exist_ok=True)

    # Initialize Gemini client
    client = None
    if not args.dry_run:
        client = genai.Client(api_key=api_key)

    # --- Main pipeline loop ---
    total_generated = 0
    total_skipped = 0
    total_failed = 0
    first_call = True

    print(f"\n=== Generating level art: {len(args.zones)} zone(s) x {len(args.assets)} asset type(s) ===\n")

    for zone in args.zones:
        for asset_type in args.assets:
            zone_dir = OUTPUT_BASE / zone
            out_filename = output_filename(asset_type, zone)
            out_path = zone_dir / out_filename
            raw_path = RAW_DIR / raw_filename(asset_type, zone)

            prompt = ZONE_PROMPTS[zone][asset_type]
            target_size = target_size_for(asset_type)

            print(f"[{zone} / {asset_type}]")
            print(f"  Output: {out_path}")

            # Skip if output already exists and --force not set
            if out_path.exists() and not args.force:
                print(f"  [SKIP] Already exists (use --force to overwrite)")
                total_skipped += 1
                continue

            if args.dry_run:
                print(f"  [DRY-RUN] Would generate with model: {args.model}")
                print(f"  Prompt: {prompt[:120]}...")
                print(f"  Raw -> {raw_path}")
                print(f"  Final -> {out_path} ({target_size}x{target_size})")
                continue

            # Rate limiting: delay between API calls (skip before first call)
            if not first_call:
                time.sleep(INTER_CALL_DELAY)
            first_call = False

            region_img = region_images[asset_type]
            result = generate_image(client, region_img, prompt, args.model)

            if result is None:
                print(f"  FAILED after {RETRY_ATTEMPTS} attempts")
                total_failed += 1
                continue

            # Save raw output
            result.save(raw_path)
            print(f"  Raw saved: {raw_path}")

            # Post-process and save final
            if args.no_post_process:
                result.save(out_path)
            else:
                processed = post_process(result, target_size, remove_bg=(asset_type != "floor"))
                processed.save(out_path)

            print(f"  Final saved: {out_path} ({target_size}x{target_size})")
            total_generated += 1

    # --- Summary ---
    print(f"\n=== Summary ===")
    print(f"Generated: {total_generated}")
    print(f"Skipped:   {total_skipped}")
    print(f"Failed:    {total_failed}")
    print(f"Output base: {OUTPUT_BASE}")


if __name__ == "__main__":
    main()
