"""
Generate simple solid floor tilesets for Level 3 Warehouse.
Run: python Assets/Editor/GenerateSimpleTilesets.py
Produces 64x64 PNG atlases (4x4 grid of 16x16 tiles).
"""
from PIL import Image, ImageDraw
import random, os

random.seed(42)

OUTPUT_DIR = os.path.dirname(os.path.abspath(__file__))
OUTPUT_DIR = os.path.join(OUTPUT_DIR, "..", "Assets", "Tileset")
os.makedirs(OUTPUT_DIR, exist_ok=True)

def make_solid_tileset(base_color, variation, filename, label):
    """Create a 64x64 tileset (4x4 grid of 16x16 tiles).
    All tiles are solid with subtle pixel-level grain for texture."""
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    r0, g0, b0 = base_color

    for ty in range(4):
        for tx in range(4):
            for py in range(16):
                for px in range(16):
                    # Subtle per-pixel noise
                    noise = random.randint(-variation, variation)
                    r = max(0, min(255, r0 + noise))
                    g = max(0, min(255, g0 + noise))
                    b = max(0, min(255, b0 + noise))
                    img.putpixel((tx * 16 + px, ty * 16 + py), (r, g, b, 255))

    out_path = os.path.join(OUTPUT_DIR, filename)
    img.save(out_path, "PNG")
    print(f"[OK] {label} saved: {out_path} ({img.size[0]}x{img.size[1]})")
    return out_path

# --- Parking Lot: Dark Asphalt ---
# Base: dark gray, very subtle grain
make_solid_tileset(
    base_color=(58, 58, 58),   # #3A3A3A dark asphalt
    variation=4,               # ±4 per channel — barely visible grain
    filename="parking_lot_tileset.png",
    label="Parking Lot (solid asphalt)"
)

# --- Warehouse Floor: Concrete ---
# Base: medium gray, slightly warmer
make_solid_tileset(
    base_color=(105, 100, 95),  # #69645F warm concrete
    variation=3,                # ±3 — very clean
    filename="warehouse_floor_tileset.png",
    label="Warehouse Floor (solid concrete)"
)

print("\nDone! Both tilesets generated.")
