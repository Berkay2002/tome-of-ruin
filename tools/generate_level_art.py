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
SPRITESHEET_PATH = Path("temp_ref/Ground_rocks.png")
OUTPUT_BASE = Path("Assets/Art/LevelArt")
RAW_DIR = OUTPUT_BASE / "raw"
RETRY_ATTEMPTS = 3
RETRY_DELAY = 5  # seconds
INTER_CALL_DELAY = 2  # seconds between API calls for rate limiting
WATERMARK_MARGIN = 80  # pixels from bottom-right corner to fill for Gemini watermark
FLOOR_SIZE = 1024   # final output resolution for floor tiles
WALL_EDGE_SIZE = 512  # final output resolution for wall edge sprites
ZONES = ["ZoneA", "ZoneB", "ZoneC", "BossArena"]
ASSET_TYPES = ["floor", "wall_edge"]

# Pixel regions to slice from the spritesheet (left, top, right, bottom)
REGIONS = {
    "floor": (368, 464, 496, 592),
    "wall_edge": (0, 192, 128, 320),
}

# --- Zone Prompts ---

ZONE_PROMPTS = {
    "ZoneA": {
        "floor": (
            "Using the provided image of a dark stone pixel art floor tileset, change only the colors and surface "
            "details to look like ancient, weathered ruins. Worn gray-green flagstones with dirt in the gaps and "
            "subtle dark moss patches. A muted, desaturated palette of grays with faint green accents. Add small "
            "cracks filled with dark grime. Keep the pixel art style, tile structure, crack patterns, and top-down "
            "perspective exactly the same. This is a seamless floor tile for a dark fantasy starting ruins area in "
            "a top-down 2D game."
        ),
        "wall_edge": (
            "Using the provided image of dark stone pixel art wall edges, change only the colors, surface texture, "
            "and decorative details to look like ancient overgrown ruins. Gray-green broken stone masonry with "
            "uneven blocks. Add dark green moss growing in the crevices and small vine tendrils creeping along the "
            "edges. Keep the stone surface rough and weathered. Keep the pixel art style, edge shape, and top-down "
            "perspective exactly the same. This is a wall edge sprite for a dark fantasy starting ruins area in a "
            "top-down 2D game."
        ),
    },
    "ZoneB": {
        "floor": (
            "Using the provided image of a dark stone pixel art floor tileset, change only the colors and surface "
            "details to look like underground catacombs. Shift the stone palette to warm browns and bone-white. "
            "Make the surface smoother and more worn-down. Add sandy dust texture in the tile gaps and tiny bone "
            "fragment chips scattered on the surface. Keep the pixel art style, tile structure, crack patterns, "
            "and top-down perspective exactly the same. This is a seamless floor tile for a dark fantasy catacomb "
            "level in a top-down 2D game."
        ),
        "wall_edge": (
            "Using the provided image of dark stone pixel art wall edges, change only the colors, surface texture, "
            "and decorative details to look like underground catacombs. Shift to warm brown worn stone. Make the "
            "surface smoother, as if eroded by centuries underground. Add small bone fragments and skull chips "
            "embedded in the crevices between stones. Keep the pixel art style, edge shape, and top-down "
            "perspective exactly the same. This is a wall edge sprite for a dark fantasy catacomb level in a "
            "top-down 2D game."
        ),
    },
    "ZoneC": {
        "floor": (
            "Using the provided image of a dark stone pixel art floor tileset, change only the colors and surface "
            "details to look like a cursed chapel. Shift the stone palette to deep purple-gray and dark crimson. "
            "Add thin ritual scratch marks across the tile surface and faint crimson stains seeping between the "
            "cracks, like dried blood in the grout lines. Keep the pixel art style, tile structure, crack patterns, "
            "and top-down perspective exactly the same. This is a seamless floor tile for a dark fantasy cursed "
            "chapel in a top-down 2D game."
        ),
        "wall_edge": (
            "Using the provided image of dark stone pixel art wall edges, change only the colors, surface texture, "
            "and decorative details to look like a cursed chapel. Shift to deep purple-gray ornate carved stone. "
            "Add thin carved ritual markings along the stone faces, with crimson stains bleeding down from the "
            "crevices. Make the stonework look more deliberately shaped, less natural. Keep the pixel art style, "
            "edge shape, and top-down perspective exactly the same. This is a wall edge sprite for a dark fantasy "
            "cursed chapel in a top-down 2D game."
        ),
    },
    "BossArena": {
        "floor": (
            "Using the provided image of a dark stone pixel art floor tileset, change only the colors and surface "
            "details to look like a scorched boss arena. Shift the stone palette to near-black obsidian with subtle "
            "blue-gray veining. Add faint orange-red glow lines in the deepest cracks, like cooling magma beneath "
            "the surface. Minimal surface detail — smooth, oppressive, empty. Keep the pixel art style, tile "
            "structure, crack patterns, and top-down perspective exactly the same. This is a seamless floor tile "
            "for a dark fantasy boss arena in a top-down 2D game."
        ),
        "wall_edge": (
            "Using the provided image of dark stone pixel art wall edges, change only the colors, surface texture, "
            "and decorative details to look like a scorched boss arena. Shift to near-black scorched stone with a "
            "melted, fused appearance. The edges should look heat-warped, with subtle orange-red glow in the "
            "deepest gaps. Fewer individual stones visible — more monolithic and oppressive. Keep the pixel art "
            "style, edge shape, and top-down perspective exactly the same. This is a wall edge sprite for a dark "
            "fantasy boss arena in a top-down 2D game."
        ),
    },
}


def slice_region(sheet: Image.Image, region: tuple) -> Image.Image:
    """Slice a rectangular region from a sprite sheet. region = (left, top, right, bottom)."""
    left, top, right, bottom = region
    return sheet.crop((left, top, right, bottom))


def post_process(image: Image.Image, target_size: int) -> Image.Image:
    """Fill Gemini watermark region with average border color, then resize."""
    image = image.convert("RGBA")
    data = np.array(image)
    h, w = data.shape[:2]

    # Fill watermark region with average color of surrounding border (NOT transparent)
    border_top = data[h - WATERMARK_MARGIN - 1, w - WATERMARK_MARGIN:]
    border_left = data[h - WATERMARK_MARGIN:, w - WATERMARK_MARGIN - 1]
    avg_color = np.concatenate([border_top, border_left]).mean(axis=0).astype(np.uint8)
    data[h - WATERMARK_MARGIN:, w - WATERMARK_MARGIN:] = avg_color
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


def target_size_for(asset_type: str) -> int:
    """Return the final output pixel size for an asset type."""
    return FLOOR_SIZE if asset_type == "floor" else WALL_EDGE_SIZE


def output_filename(asset_type: str, zone: str) -> str:
    """Return the final output filename for a zone/asset combo."""
    if asset_type == "floor":
        return f"Floor_{zone}.png"
    return f"WallEdge_{zone}.png"


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
            if stem.startswith("floor_"):
                zone = stem.split("_")[1]  # floor_ZoneA_raw -> ZoneA
                out_name = f"Floor_{zone}.png"
                target_size = FLOOR_SIZE
            elif stem.startswith("wall_edge_"):
                zone = stem.split("_")[2]  # wall_edge_ZoneA_raw -> ZoneA
                out_name = f"WallEdge_{zone}.png"
                target_size = WALL_EDGE_SIZE
            else:
                print(f"  [SKIP] {png_path.name} (unrecognized naming pattern)")
                continue

            zone_dir = OUTPUT_BASE / zone
            zone_dir.mkdir(parents=True, exist_ok=True)
            out_path = zone_dir / out_name

            img = Image.open(png_path)
            result = post_process(img, target_size)
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

    # Load sprite sheet
    sheet_path = SPRITESHEET_PATH
    if not sheet_path.exists():
        print(f"ERROR: Spritesheet not found: {sheet_path}")
        sys.exit(1)

    sheet = Image.open(sheet_path)
    print(f"Loaded spritesheet: {sheet_path} ({sheet.size[0]}x{sheet.size[1]})")

    # Slice regions up front
    region_images = {}
    for asset_type, region in REGIONS.items():
        sliced = slice_region(sheet, region)
        region_images[asset_type] = sliced
        print(f"  Sliced region '{asset_type}': {region} -> {sliced.size[0]}x{sliced.size[1]}")

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
                total_generated += 1
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
                processed = post_process(result, target_size)
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
