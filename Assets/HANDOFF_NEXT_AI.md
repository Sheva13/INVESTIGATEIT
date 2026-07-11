# Level 3 — Warehouse Infiltration: Handoff Document

## Overview

Top-down 2D stealth game level built on a spacious **28x24 grid**.
Genre: Robbery Bob–style stealth. Player infiltrates a warehouse exterior, avoids 5 guards, and reaches the warehouse entrance.

---

## 🛠️ Work Completed & Features Added

### 1. Taller Organic Cluttered Divider Walls (Robbery Bob style)
- **Concrete Walls Replaced**: Horizontal concrete barriers dividing the zones have been replaced with organic, cluttered piles of **barrels, tires, and stacked pallets**.
- **Triple-Stacked Pallets**: Pallets are now stacked 3-high using child GameObjects at offsets `(0f, 0.2f, -0.1f)` and `(0f, 0.4f, -0.2f)` with sorting orders 5/6/7 for maximum height and visual separation between zones.
- **Double-Stacked Barrels & Tires**: Barrels and tires now have a second stack on top at `(0f, 0.18f, -0.1f)` with sorting order 6, making all divider types uniformly tall.
- **Deterministic Placement**: Placement is generated using a coordinate-based hash `(pos.x * 73 + pos.y * 31) % 3` to remain 100% deterministic across rebuilds, keeping patrol paths and dots perfectly aligned.

### 2. Robbery Bob–style Transparent Wall Fading (`GameManager.cs`)
- **Zone Entry Transparency**: When Aksa enters a zone, the divider walls separating that zone from adjacent zones smoothly fade to a translucent alpha of **`0.35f`** (and fade back to solid `1.0f` when leaving).
- **Cached Renderers**: GameManager caches all parent and child sprite renderers belonging to `Divider_1_2`, `Divider_2_3`, and `Divider_3_4` and interpolates their alpha using `Mathf.MoveTowards` for a smooth transition.

### 3. Closer Camera Focus (`CameraFollow.cs`)
- Camera `targetZoom` is updated to **`3.8f`** (from `5.0f`) to bring the camera closer to the player for a more focused stealth perspective.

### 4. Universal Stuck Recovery & Jitter-Free AI (`GuardAI.cs`)
- **Stuck Checking in All States**: Stuck checks (displacement check) now run in **all movement states** (`Patrol`, `Return`, `Investigate`, `Alert`) at a faster `1.0s` sampling interval.
- **Sideways Steering Escape (Alert State)**: If a guard gets stuck while chasing the player, the AI chooses a perpendicular steering offset direction (`alertSteerOffset`) and pushes the guard sideways for `0.8s` to steer around obstacles.
- **Investigate State Stuck Recovery**: If stuck while checking a noise, the guard immediately aborts investigation and returns to patrol.
- **Smooth Turn Speed**: Guards rotate smoothly at a limit of `360f` degrees per second, and visual orientation is decoupled from physical collision-avoidance sliding vectors to completely eliminate jittering.

### 5. Physics Layer Ignored Collisions
- Set `Physics2D.IgnoreLayerCollision(9, 9, true)` so guards (Layer 9) ignore collision with other guards. They can cross paths smoothly without blocking each other, but still collide with the Player (Layer 8) and Obstacles (Layer 10).

### 6. Guard Visual Replacement & Sprite Pivot Fix — Preman AI Sprites (`GuardAI.cs` + `Level3_Builder.cs`)
- **Replaced placeholder white circles** with actual pixel-art character sprites from `Assets/Assets/Character/Preman AI/thug_with_leather_jacket/rotations/`.
- **8-directional sprite system**: GuardAI picks the correct sprite (N/NE/E/SE/S/SW/W/NW) based on `facingDir` using `UpdateSpriteDirection()`, called every frame.
- **No transform rotation**: `RotateTowards()` no longer rotates the GameObject. Sprite swapping handles visual direction, and the vision cone (`LineRenderer`) now uses **world-space coordinates** calculated from `facingDir`.
- **State tint disabled** when real sprites are assigned; the vision cone colors still indicate guard state (green=patrol, red=alert, orange=investigate, yellow=return).
- **Builder assigns sprites**: `Level3_Builder.cs` loads all 8 PNGs and assigns them to each guard's `GuardAI.directionSprites` array. Initial sprites are set per guard (Guard_1 faces West, others face South).
- **Sprite pivot fix**: `EnsureSpriteImport()` now sets guard sprites to `SpriteAlignment.BottomCenter` so the character's feet align with the ground/transform position, matching the player's footprint.

### 7. Texture Import Fix — Auto-Convert to Sprite
- **Problem**: Preman PNGs and Kaleng.png were imported as `Texture` type (not Sprite), so `AssetDatabase.LoadAssetAtPath<Sprite>()` returned null → guards/kaleng invisible.
- **Fix**: Added `EnsureSpriteImport()` in `Level3_Builder.cs` which checks if a texture is set to `TextureImporterType.Sprite` + `SpriteImportMode.Single` with `alphaIsTransparency=true`, `filterMode=Point`, `mipmapEnabled=false`. If not, it fixes the importer settings and calls `SaveAndReimport()`.
- Guard sprites use `pixelsPerUnit=68f`; Kaleng uses `pixelsPerUnit=3000f` (~0.42 world units).

### 8. Kaleng / Can Sprite Replacement
- **Updated to `Kaleng 1.png`** (1254x1254 px, replacement art).
- **PPU set to 3000** so the can renders small at ~0.42 world units (appropriate for a pickup/throwable on a 1-unit tile grid).
- **Throwable cans** (`ThrowableObject.cs` prefab) use this sprite via builder assignment.
- **CanPickup objects** in the scene get the sprite with `sortingOrder = 10` during builder execution.
- Player's `throwablePrefab` SpriteRenderer is also updated.

---

## Project Structure

```
Assets/
├── Editor/
│   └── Level3_Builder.cs          # Menu: Tools → Build Level 3 Now (or Level 3 Builder window)
├── Scenes/
│   └── Level3_Warehouse.unity     # Main scene
├── Assets/
│   ├── Scripts/Level3/            # Gameplay scripts
│   │   ├── PlayerController.cs    # WASD movement, right-click directional throw
│   │   ├── GuardAI.cs             # Directional sprites, vision cone, stuck recovery
│   │   ├── ThrowableObject.cs     # Can projectile (Kaleng sprite)
│   │   ├── NoiseSource.cs         # Noise source
│   │   ├── CanPickup.cs           # Clicking range-checked can collector (Kaleng sprite)
│   │   ├── EscapeZone.cs          # Win trigger (replaces WinZone)
│   │   ├── GameManager.cs         # Zone transition transparency fade & Game State
│   │   └── CameraFollow.cs        # Zoom-in focus camera (zoom = 3.8)
│   ├── Character/Preman AI/       # Guard character sprites
│   │   └── thug_with_leather_jacket/rotations/  # 8-directional PNGs (N/NE/E/SE/S/SW/W/NW)
│   ├── Gambar/
│   │   └── Kaleng.png             # Can throwable/pickup sprite
│   ├── Textures/
│   │   └── ZeroFrictionMat.physicsMaterial2D   # Zero friction physical material
│   └── Font/
│       ├── Pix32.ttf              # Custom TTF font
│       └── Pix32_SDF.asset        # TextMesh Pro SDF Font Asset
└── HANDOFF_NEXT_AI.md            # ← You are here
```

## Physics Layers

- **Layer 8**: `Player` (Aksa)
- **Layer 9**: `Enemy` (Guards 1-5, collision ignored between 9 and 9 to prevent blocking)
- **Layer 10**: `Obstacle` (Wall barriers, concrete dividers, and prop obstacles)
