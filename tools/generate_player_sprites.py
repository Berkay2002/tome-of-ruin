"""
Player Sprite Generator for Tome of Ruin
Uses Gemini API (Nano Banana Pro) to generate all player animation sprites
from a single base idle image.

Usage:
    python tools/generate_player_sprites.py <base_idle_image_path>

Requires:
    pip install google-genai Pillow numpy

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
OUTPUT_DIR = Path("Assets/Sprites/Player")
RETRY_ATTEMPTS = 3
RETRY_DELAY = 5  # seconds
SPRITE_SIZE = 64  # final output resolution
BG_THRESHOLD = 30  # pixels with R,G,B all below this are treated as background

# 5 unique directions (left mirrors to right, down-left mirrors to down-right, etc.)
DIRECTIONS = {
    "down": "Reverse this character so he faces toward the viewer. Keep the 45 degree top-down angle, same size, same style, same colors. Only change the direction he faces.",
    "up": None,  # This IS the base image, no transform needed
    "left": "Turn this character so he faces to the left. Keep the 45 degree top-down angle, same size, same style, same colors. Only change the direction he faces.",
    "down_left": "Turn this character so he faces diagonally toward the lower left. Keep the 45 degree top-down angle, same size, same style, same colors. Only change the direction he faces.",
    "up_left": "Turn this character so he faces diagonally toward the upper left. Keep the 45 degree top-down angle, same size, same style, same colors. Only change the direction he faces.",
}

# State prompts - applied to each direction's idle base
STATES = {
    "idle": {
        "frames": 1,
        "prompts": [None],  # Idle IS the direction base, no extra generation
    },
    "moving": {
        "frames": 4,
        "prompts": [
            "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. Walking frame 1: left leg steps forward with knee bent, right leg pushes off behind, torso leans slightly forward, sword arm swings back naturally, cape drifts gently backward mid-stride. Black background.",
            "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. Walking frame 2: both legs pass through center under the body, left foot just lifting off the ground, body upright and tall, arms at sides, cape hangs straight. Black background.",
            "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. Walking frame 3: right leg steps forward with knee bent, left leg pushes off behind, torso leans slightly forward, sword arm swings forward naturally, cape drifts gently backward mid-stride. Black background.",
            "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. Walking frame 4: both legs pass through center under the body, right foot just lifting off the ground, body upright and tall, arms at sides, cape hangs straight. Black background.",
        ],
    },
    "attacking": {
        "frames": 3,
        "direction_aware": True,
        "prompts": {
            "down": [
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing toward the viewer. Wind-up: both hands grip the sword raised above his right shoulder, weight on back foot, torso twisted slightly right, cape pulled taut behind him. Black background.",
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing toward the viewer. Mid-swing: the sword is sweeping diagonally downward from upper-right to lower-left across his body, arms fully extended, torso rotating into the slash, cape whipping upward from the force. Black background.",
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing toward the viewer. Follow-through: the sword has completed the arc and rests low on his left side, body weight shifted forward onto front foot, arms relaxed after the swing, cape floating back down. Black background.",
            ],
            "up": [
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing away from the viewer. Wind-up: both hands grip the sword raised above his right shoulder, weight on back foot, torso twisted, cape hangs toward us. Black background.",
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing away from the viewer. Mid-swing: the sword sweeps forward away from us, arms extended, torso rotating into the slash, cape flares back toward us from the force. Black background.",
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing away from the viewer. Follow-through: the sword rests low after completing the arc, body weight forward, arms relaxed, cape settling back down toward us. Black background.",
            ],
            "left": [
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing left. Wind-up: both hands grip the sword pulled back to his right side behind him, weight on right foot, torso coiled rightward, cape pulled right. Black background.",
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing left. Mid-swing: the sword sweeps from right to left in a horizontal arc, arms fully extended leftward, torso uncoiling into the slash, cape whipping to the right. Black background.",
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing left. Follow-through: the sword has completed the arc and rests extended to his left, body weight on left foot, arms relaxed, cape settling to the right. Black background.",
            ],
            "down_left": [
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing diagonally toward the lower left. Wind-up: both hands grip the sword pulled back above his right shoulder, weight on back foot, torso coiled. Black background.",
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing diagonally toward the lower left. Mid-swing: the sword sweeps diagonally toward the lower left, arms extended, torso rotating into the slash, cape flaring to the upper right. Black background.",
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing diagonally toward the lower left. Follow-through: the sword rests low to the lower left after the arc, body weight shifted forward, cape settling. Black background.",
            ],
            "up_left": [
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing diagonally toward the upper left. Wind-up: both hands grip the sword pulled back above his right shoulder, weight on back foot, torso coiled. Black background.",
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing diagonally toward the upper left. Mid-swing: the sword sweeps diagonally toward the upper left, arms extended, torso rotating into the slash, cape flaring to the lower right. Black background.",
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing diagonally toward the upper left. Follow-through: the sword rests extended toward the upper left after the arc, body weight forward, cape settling. Black background.",
            ],
        },
    },
    "dodging": {
        "frames": 2,
        "direction_aware": True,
        "prompts": {
            "down": [
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing toward the viewer. Dodge start: knees bent, body crouching low, sword and arms tucked tight against his chest, head ducked, cape compressed behind him, about to spring forward. Black background.",
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing toward the viewer. Mid-dodge roll: body airborne and tumbling forward toward us, curled into a ball, sword tucked under one arm, cape streaming behind him. Black background.",
            ],
            "up": [
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing away from the viewer. Dodge start: knees bent, body crouching low, sword and arms tucked tight, head ducked, cape compressed, about to spring forward away from us. Black background.",
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing away from the viewer. Mid-dodge roll: body airborne and tumbling forward away from us, curled into a ball, sword tucked under one arm, cape streaming toward us. Black background.",
            ],
            "left": [
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing left. Dodge start: knees bent, body crouching low, sword and arms tucked tight, head ducked, cape compressed, about to spring to the left. Black background.",
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing left. Mid-dodge roll: body airborne and tumbling to the left, curled into a ball, sword tucked under one arm, cape streaming to the right. Black background.",
            ],
            "down_left": [
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing diagonally toward the lower left. Dodge start: knees bent, body crouching low, sword and arms tucked tight, head ducked, cape compressed. Black background.",
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing diagonally toward the lower left. Mid-dodge roll: body airborne and tumbling toward the lower left, curled into a ball, sword tucked, cape streaming opposite. Black background.",
            ],
            "up_left": [
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing diagonally toward the upper left. Dodge start: knees bent, body crouching low, sword and arms tucked tight, head ducked, cape compressed. Black background.",
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing diagonally toward the upper left. Mid-dodge roll: body airborne and tumbling toward the upper left, curled into a ball, sword tucked, cape streaming opposite. Black background.",
            ],
        },
    },
    "hit": {
        "frames": 1,
        "direction_aware": True,
        "prompts": {
            "down": [
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing toward the viewer. Hit reaction: upper body snaps backward, head tilted back, left arm flung out defensively, right hand still gripping sword loosely at his side, knees buckling slightly, cape jolted upward from the impact. Black background.",
            ],
            "up": [
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing away from the viewer. Hit reaction: upper body snaps forward toward us, head ducked, left arm flung out, right hand still gripping sword, knees buckling, cape jolted forward toward us from the impact. Black background.",
            ],
            "left": [
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing left. Hit reaction: upper body snaps to the right from a hit on the left side, head tilted right, left arm flung out, right hand still gripping sword, knees buckling, cape jolted to the right. Black background.",
            ],
            "down_left": [
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing diagonally toward the lower left. Hit reaction: upper body snaps toward the upper right from impact, head tilted back, left arm flung out defensively, right hand still gripping sword, knees buckling, cape jolted toward upper right. Black background.",
            ],
            "up_left": [
                "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. He is facing diagonally toward the upper left. Hit reaction: upper body snaps toward the lower right from impact, head ducked, left arm flung out defensively, right hand still gripping sword, knees buckling, cape jolted toward lower right. Black background.",
            ],
        },
    },
    "dead": {
        "frames": 1,
        "prompts": [
            "Keep this exact pixel art character with the same armor, cape, helmet, sword, colors, and pixel art style. Death pose: body collapsed face down flat on the ground, limbs splayed outward limp, sword fallen loose beside his right hand, cape spread out around the body like a pool of fabric, completely motionless. Black background.",
        ],
        "single_direction": True,  # Dead looks the same from all angles
    },
}



WATERMARK_MARGIN = 80  # pixels from bottom-right corner to clear for Gemini watermark


BG_WHITE_THRESHOLD = 225  # pixels with R,G,B all above this are treated as white background


def post_process(image: Image.Image, size: int = SPRITE_SIZE) -> Image.Image:
    """Remove black/white background and Gemini watermark, making them transparent."""
    # Convert to RGBA
    image = image.convert("RGBA")
    data = np.array(image)
    h, w = data.shape[:2]

    r, g, b, a = data[:, :, 0], data[:, :, 1], data[:, :, 2], data[:, :, 3]

    # Pixels where R, G, B are all below threshold -> transparent (black bg)
    black_mask = (r < BG_THRESHOLD) & (g < BG_THRESHOLD) & (b < BG_THRESHOLD)
    data[black_mask] = [0, 0, 0, 0]

    # Pixels where R, G, B are all above threshold -> transparent (white bg)
    white_mask = (r > BG_WHITE_THRESHOLD) & (g > BG_WHITE_THRESHOLD) & (b > BG_WHITE_THRESHOLD)
    data[white_mask] = [0, 0, 0, 0]

    # Clear Gemini watermark in bottom-right corner
    data[h - WATERMARK_MARGIN:, w - WATERMARK_MARGIN:] = [0, 0, 0, 0]

    image = Image.fromarray(data)

    # Resize to target sprite size
    if image.size[0] != size or image.size[1] != size:
        # Step down gradually for best quality: halve repeatedly, then final LANCZOS
        while image.size[0] > size * 2:
            half = (image.size[0] // 2, image.size[1] // 2)
            image = image.resize(half, Image.LANCZOS)
        image = image.resize((size, size), Image.LANCZOS)

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


def generate_image(client, base_image: Image.Image, prompt: str) -> Image.Image | None:
    """Send an image + text prompt to Gemini and return the generated PIL image."""
    for attempt in range(RETRY_ATTEMPTS):
        try:
            response = client.models.generate_content(
                model=MODEL,
                contents=[prompt, base_image],
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


def main():
    parser = argparse.ArgumentParser(description="Generate player sprites from base idle image")
    parser.add_argument("base_image", help="Path to the base idle image (facing up/away)")
    parser.add_argument("--output", "-o", default=str(OUTPUT_DIR), help="Output directory")
    parser.add_argument("--states", "-s", nargs="+", help="Only generate specific states (e.g., moving attacking)")
    parser.add_argument("--directions", "-d", nargs="+", help="Only generate specific directions (e.g., down left)")
    parser.add_argument("--dry-run", action="store_true", help="Print prompts without calling API")
    parser.add_argument("--no-post-process", action="store_true", help="Save raw images without background removal")
    parser.add_argument("--post-process-only", action="store_true", help="Only run post-processing on existing images in output dir (no API calls)")
    parser.add_argument("--size", type=int, default=SPRITE_SIZE, help="Output sprite size (default: 64)")
    parser.add_argument("--only", nargs="+", help="Only generate specific files (e.g., player_attacking_up_f2 player_hit_down_f1)")
    args = parser.parse_args()

    # Post-process only mode: apply post-processing to all PNGs in output dir
    if args.post_process_only:
        output_dir = Path(args.output)
        raw_dir = output_dir / "raw"
        # Read from raw/ if it exists, otherwise from main folder
        source_dir = raw_dir if raw_dir.exists() else output_dir
        if not source_dir.exists():
            print(f"ERROR: Source directory not found: {source_dir}")
            sys.exit(1)
        pngs = sorted(source_dir.glob("*.png"))
        print(f"Post-processing {len(pngs)} images from {source_dir} -> {output_dir}")
        for png_path in pngs:
            img = Image.open(png_path)
            processed = post_process(img, args.size)
            processed.save(output_dir / png_path.name)
            print(f"  [PP] {png_path.name}")
        print("Done.")
        return

    # Load .env file
    load_dotenv()

    # Validate API key
    api_key = os.environ.get("GEMINI_API_KEY")
    if not api_key and not args.dry_run:
        print("ERROR: GEMINI_API_KEY environment variable not set")
        sys.exit(1)

    # Load base image
    base_path = Path(args.base_image)
    if not base_path.exists():
        print(f"ERROR: Base image not found: {base_path}")
        sys.exit(1)

    base_image = Image.open(base_path)
    print(f"Loaded base image: {base_path} ({base_image.size[0]}x{base_image.size[1]})")

    # Setup output
    output_dir = Path(args.output)
    output_dir.mkdir(parents=True, exist_ok=True)
    raw_dir = output_dir / "raw"
    raw_dir.mkdir(parents=True, exist_ok=True)

    # Build cherry-pick set if --only is specified
    only_set = set()
    if args.only:
        for name in args.only:
            # Strip .png extension if provided
            only_set.add(name.replace(".png", ""))

    # Filter states/directions if specified
    states_to_gen = {k: v for k, v in STATES.items() if not args.states or k in args.states}
    dirs_to_gen = {k: v for k, v in DIRECTIONS.items() if not args.directions or k in args.directions}

    # Initialize client
    client = None
    if not args.dry_run:
        client = genai.Client(api_key=api_key)

    # --- Phase 1: Generate idle direction bases ---
    print("\n=== Phase 1: Generating idle direction bases ===")
    idle_bases = {}

    for dir_name, dir_prompt in dirs_to_gen.items():
        idle_filename = f"player_idle_{dir_name}"
        idle_path = output_dir / f"{idle_filename}.png"

        # If cherry-picking or idle not in requested states, try to load from disk
        skip_gen = (only_set and idle_filename not in only_set) or (args.states and "idle" not in args.states)
        if skip_gen:
            if idle_path.exists():
                idle_bases[dir_name] = Image.open(idle_path)
                print(f"  [LOAD] {idle_filename}.png (from disk)")
            else:
                print(f"  [SKIP] {idle_filename}.png (not targeted, not on disk)")
            continue

        if dir_prompt is None:
            # This is the base direction (up) - use the input image directly
            idle_bases[dir_name] = base_image
            filename = f"player_idle_{dir_name}.png"
            base_image.save(raw_dir / filename)
            save_img = base_image if args.no_post_process else post_process(base_image, args.size)
            save_img.save(output_dir / filename)
            print(f"  [COPY] {filename} (base image)")
            continue

        prompt = dir_prompt
        print(f"  [GEN] player_idle_{dir_name}.png")

        if args.dry_run:
            print(f"    Prompt: {prompt}")
            idle_bases[dir_name] = base_image  # placeholder for dry run
            continue

        result = generate_image(client, base_image, prompt)
        if result:
            idle_bases[dir_name] = result  # keep raw for feeding into next generation
            filename = f"player_idle_{dir_name}.png"
            result.save(raw_dir / filename)
            save_img = result if args.no_post_process else post_process(result, args.size)
            save_img.save(output_dir / filename)
            print(f"    Saved: {filename}")
        else:
            print(f"    FAILED - skipping direction {dir_name}")

    # --- Phase 2: Generate state frames from each idle base ---
    print("\n=== Phase 2: Generating state frames ===")

    total_generated = 0
    total_failed = 0

    for state_name, state_config in states_to_gen.items():
        if state_name == "idle":
            # Already generated in Phase 1
            continue

        print(f"\n--- {state_name.upper()} ---")
        is_single_dir = state_config.get("single_direction", False)
        directions = {"down": dirs_to_gen.get("down")} if is_single_dir else dirs_to_gen

        for dir_name in directions:
            if dir_name not in idle_bases:
                print(f"  Skipping {dir_name} (no idle base)")
                continue

            dir_base = idle_bases[dir_name]

            # Get prompts - either direction-aware (dict) or shared (list)
            if state_config.get("direction_aware"):
                frame_prompts = state_config["prompts"].get(dir_name, [])
            else:
                frame_prompts = state_config["prompts"]

            for frame_idx, frame_prompt in enumerate(frame_prompts):
                frame_num = frame_idx + 1
                filename = f"player_{state_name}_{dir_name}_f{frame_num}.png"
                file_key = filename.replace(".png", "")

                # Skip if cherry-picking and this file isn't targeted
                if only_set and file_key not in only_set:
                    continue

                prompt = frame_prompt + " Black background."

                print(f"  [GEN] {filename}")

                if args.dry_run:
                    print(f"    Prompt: {prompt}")
                    continue

                result = generate_image(client, dir_base, prompt)
                if result:
                    result.save(raw_dir / filename)
                    save_img = result if args.no_post_process else post_process(result, args.size)
                    save_img.save(output_dir / filename)
                    total_generated += 1
                    print(f"    Saved: {filename}")
                else:
                    total_failed += 1
                    print(f"    FAILED")

    # --- Summary ---
    idle_count = len(idle_bases)
    print(f"\n=== Summary ===")
    print(f"Idle bases generated: {idle_count}")
    print(f"State frames generated: {total_generated}")
    print(f"Failed: {total_failed}")
    print(f"Total sprites: {idle_count + total_generated}")
    print(f"\nOutput directory: {output_dir}")
    print(f"Note: Mirror left->right, down_left->down_right, up_left->up_right in Unity for full 8 directions.")


if __name__ == "__main__":
    main()
