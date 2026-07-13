# Plan: Fix Throw Position & Duplicate GuardAI

## Issue 1: Throw muncul di badan player, bukan di depan
- **File**: `Assets/Assets/Scripts/Level3/PlayerController.cs`
- **Method**: `TryThrow()`
- **Problem**: `spawnPos = throwSpawnPoint ? throwSpawnPoint.position : transform.position` → spawn di posisi player
- **Fix**: Ubah jadi `spawnPos = transform.position + (Vector3)facingDir * 1f` (1 unit di depan player sesuai arah hadap)

## Issue 2: Semua guard punya 2 component GuardAI (konflik)
- Setiap guard (`Guard_1` s/d `Guard_5`) memiliki **dua** `GuardAI` component → menyebabkan freeze/konflik state
- **Fix**: Hapus 1 GuardAI component dari masing-masing guard via `manage_components`
  - Guard_1 (instanceID: 81952)
  - Guard_2 (instanceID: 69554)
  - Guard_3 (instanceID: 85818)
  - Guard_4 (instanceID: 81490)
  - Guard_5 (instanceID: 81344)

## Step-by-step Eksekusi:
1. Edit `PlayerController.cs` → `spawnPos` offset by `facingDir * 1f`
2. Compile & cek error
3. Remove duplicate GuardAI dari 5 guard (batch)
4. Save scene
5. Selesai — user test sendiri
