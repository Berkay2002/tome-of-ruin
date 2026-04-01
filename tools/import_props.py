"""
Import prop sprites from the Free Undead Tileset into the Unity project.

Copies Objects_separately PNGs into Assets/Art/LevelArt/Props/{category}/
Maps shadow variants to zones:
  shadow1 → ZoneA, shadow2 → ZoneB, shadow3 → ZoneC
  BossArena gets a programmatic dark tint of shadow3.

Also copies Details.png as a spritesheet for floor detail overlays.

Usage:
    python tools/import_props.py [--source PATH] [--dry-run]
"""

import argparse
import os
import re
import shutil
from pathlib import Path

from PIL import Image, ImageEnhance
import numpy as np

# --- Configuration ---

DEFAULT_SOURCE = Path(os.path.expanduser(
    "~/Downloads/Free-Undead-Tileset-Top-Down-Pixel-Art/PNG"
))
OUTPUT_BASE = Path("Assets/Art/LevelArt/Props")
DETAILS_OUTPUT = Path("Assets/Art/LevelArt/Details")

# Shadow variant → zone mapping
SHADOW_TO_ZONE = {
    "shadow1": "ZoneA",
    "shadow2": "ZoneB",
    "shadow3": "ZoneC",
}

# Normalize messy filenames from the tileset
# Maps category prefix from filename → clean category name
CATEGORY_NORMALIZE = {
    "Bones": "Bones",
    "Broken_ tree": "BrokenTree",
    "Broken_tree": "BrokenTree",
    "Crystal": "Crystal",
    "Dead_arm": "DeadArm",
    "Dead_tree": "DeadTree",
    "Grave": "Grave",
    "Lich": "Lich",
    "Pile_sculls": "PileOfSkulls",
    "Plant_": "Plant",
    "Plant": "Plant",
    "Rock": "Rock",
    "Ruin": "Ruin",
    "Scull_door": "SkullDoor",
    "Thorn_palnt": "ThornPlant",
    "Thorn_plant": "ThornPlant",
    "Tree": "Tree",
}

# BossArena tint: darken + shift toward blue-black (obsidian look)
BOSS_BRIGHTNESS = 0.35
BOSS_SATURATION = 0.5
BOSS_TINT = np.array([10, 10, 20, 0], dtype=np.int16)  # subtle blue shift


def parse_filename(filename: str):
    """Parse 'Category_shadowN_num.png' into (category, shadow_variant, number).

    Returns None if the filename doesn't match the expected pattern.
    """
    stem = Path(filename).stem
    # Match patterns like: Rock_shadow1_3, Lich_shadow2, Broken_ tree_shadow3_1
    match = re.match(r'^(.+?)_(shadow\d+)(?:_(.+))?$', stem)
    if not match:
        return None
    raw_category = match.group(1)
    shadow = match.group(2)
    number = match.group(3) or "1"

    category = CATEGORY_NORMALIZE.get(raw_category)
    if category is None:
        # Try without trailing underscore/space
        for key, val in CATEGORY_NORMALIZE.items():
            if raw_category.strip().rstrip('_') == key.strip().rstrip('_'):
                category = val
                break
    if category is None:
        category = raw_category.replace(' ', '').replace('_', '')

    return category, shadow, number


def tint_for_boss(image: Image.Image) -> Image.Image:
    """Create a dark obsidian-tinted variant for BossArena."""
    img = image.convert("RGBA")

    # Darken
    enhancer = ImageEnhance.Brightness(img)
    img = enhancer.enhance(BOSS_BRIGHTNESS)

    # Desaturate
    enhancer = ImageEnhance.Color(img)
    img = enhancer.enhance(BOSS_SATURATION)

    # Apply blue-black tint shift
    data = np.array(img, dtype=np.int16)
    # Only tint non-transparent pixels
    alpha = data[:, :, 3]
    mask = alpha > 0
    data[mask] = np.clip(data[mask] + BOSS_TINT, 0, 255)

    return Image.fromarray(data.astype(np.uint8))


def main():
    parser = argparse.ArgumentParser(description="Import prop sprites into Unity project")
    parser.add_argument(
        "--source", type=Path, default=DEFAULT_SOURCE,
        help=f"Path to PNG/ folder from tileset (default: {DEFAULT_SOURCE})"
    )
    parser.add_argument("--dry-run", action="store_true", help="Print actions without copying")
    args = parser.parse_args()

    source_objects = args.source / "Objects_separately"
    source_details = args.source / "Details.png"

    if not source_objects.exists():
        print(f"ERROR: Objects_separately not found at {source_objects}")
        return 1

    # --- Import Object Props ---
    print(f"Source: {source_objects}")
    print(f"Output: {OUTPUT_BASE}\n")

    copied = 0
    skipped = 0
    boss_generated = 0

    for filename in sorted(os.listdir(source_objects)):
        if not filename.endswith('.png'):
            continue

        parsed = parse_filename(filename)
        if parsed is None:
            print(f"  [SKIP] {filename} (unrecognized pattern)")
            skipped += 1
            continue

        category, shadow, number = parsed
        zone = SHADOW_TO_ZONE.get(shadow)
        if zone is None:
            print(f"  [SKIP] {filename} (unknown shadow variant: {shadow})")
            skipped += 1
            continue

        # Clean output name: Category_ZoneName_Number.png
        out_name = f"{category}_{zone}_{number}.png"
        out_dir = OUTPUT_BASE / category
        out_path = out_dir / out_name

        if args.dry_run:
            print(f"  [DRY] {filename} -> {out_path}")
        else:
            out_dir.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source_objects / filename, out_path)
            copied += 1

        # Generate BossArena tinted variant from shadow3
        if shadow == "shadow3":
            boss_name = f"{category}_BossArena_{number}.png"
            boss_path = out_dir / boss_name

            if args.dry_run:
                print(f"  [DRY] {filename} -> {boss_path} (tinted)")
            else:
                img = Image.open(source_objects / filename)
                tinted = tint_for_boss(img)
                tinted.save(boss_path)
                boss_generated += 1

    # --- Import Details spritesheet ---
    if source_details.exists():
        details_dir = DETAILS_OUTPUT
        details_out = details_dir / "Details.png"
        if args.dry_run:
            print(f"\n  [DRY] Details.png -> {details_out}")
        else:
            details_dir.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source_details, details_out)
            print(f"\nDetails.png -> {details_out}")
    else:
        print(f"\nWARNING: Details.png not found at {source_details}")

    # --- Summary ---
    print(f"\n=== Summary ===")
    print(f"Copied:         {copied}")
    print(f"Boss tinted:    {boss_generated}")
    print(f"Skipped:        {skipped}")
    print(f"Output:         {OUTPUT_BASE}")

    return 0


if __name__ == "__main__":
    exit(main())
