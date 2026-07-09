# RANGKUMAN — Photo Frame Inspection System

## Scene
- **Scene**: `Assets/Scenes/BookFlipScene.unity`
- **Camera**: `Main Camera` (from `SampleScene`, loaded additively/DontDestroyOnLoad)
  - Components: Transform, Camera, AudioListener, UniversalAdditionalCameraData, **Physics2DRaycaster** (already attached)
- **EventSystem**: present (with `InputSystemUIInputModule`)

---

## Book System (working reference)

**Root**: `BookCanvas` (ScreenSpaceOverlay, sortingOrder=0)
- `BookCanvas` → CanvasScaler: **ScaleWithScreenSize, 1920x1080, Match=0.5**
- Component: `UIBookFlip` → controls book open/close, page flip, preview panel

**Hierarchy:**
```
BookCanvas (active)
  ├── DarkBackgroundOverlay (active, full-screen)
  ├── OpenBook (inactive)
  │     ├── LeftText / RightText
  │     ├── PrevButton / NextButton / CloseButton / CaptureButton
  ├── BookPreviewPanel (inactive — shown on click)
  │     ├── LargeBookImage → anchor 0.5,0.5 sizeDelta 480×480
  │     ├── TitleText → anchor 0.5,0.5 sizeDelta 800×120
  │     ├── DescriptionText → anchor 0.5,0.5 sizeDelta 800×80
  │     ├── OpenButton → anchor 0.5,0.5 sizeDelta 220×60
  │     └── ESCButton → anchor 0.5,0.5 sizeDelta 220×60
  └── PhoneCameraOverlay (active)
```

**World trigger**: `ClosedBook` (SpriteRenderer + BoxCollider2D + UIBookFlipTrigger)
- Material: `SpriteOutline` (shader `Custom/SpriteOutline`)
- `UIBookFlipTrigger` → uses `spriteRenderer.sharedMaterial.SetFloat("_Outline", ...)` for hover

---

## Photo Frame System (current state)

**Root**: `FrameCanvas` (ScreenSpaceOverlay, sortingOrder=10)
- `FrameCanvas` → CanvasScaler: ScaleWithScreenSize, 1920×1080, Match=0.5 **(matching BookCanvas)**
- Component: `UIItemInspect` → controls inspect open/close, flip, capture

**Hierarchy:**
```
FrameCanvas (active)
  ├── DarkBackgroundOverlay_Inspect_FrameCanvas (active, alpha=0 — created by UIItemInspect.Start)
  ├── DarkBg (active, alpha=0 — pre-made, redundant but harmless)
  └── FrameView (inactive — activated on inspect trigger)
        ├── FrameImage → stretch 5%–95% × 12%–92%
        ├── DescText → bottom-anchored
        └── Buttons (HorizontalLayoutGroup) → BtnClose / BtnFlip / BtnCapture
```

**World trigger**: `WorldPhotoFrame` (SpriteRenderer + BoxCollider2D + UIItemInspectTrigger)
- Material: `SpriteOutline` (shader `Custom/SpriteOutline`)
- `UIItemInspectTrigger` → **already patched** to use instance material (`new Material(sharedMaterial)` + `spriteRenderer.material = instance`)

---

## Scripts

| Script | Path | Function |
|--------|------|----------|
| `UIItemInspect.cs` | `Assets/Assets/Scripts/` | Controller for FrameCanvas: open/close/flip/capture, dynamic description |
| `UIItemInspectTrigger.cs` | `Assets/Assets/Scripts/` | IPointerClickHandler + IPointerEnterHandler/IPointerExitHandler on WorldPhotoFrame |
| `SetupPhotoFrameUI.cs` | `Assets/Editor/` | Editor tool (Tools/Setup Photo Frame UI) builds FrameCanvas from scratch |
| `SpriteOutline.shader` | `Assets/Assets/Scripts/` | Custom shader with `_Outline` toggle + `_OutlineColor` + `_OutlineSize` |

**UIItemInspect serialized fields:**
- `closedItemObject` → WorldPhotoFrame
- `inspectPanel` → FrameView (the brown panel)
- `inspectImage` → FrameImage (Image component showing the photo sprite)
- `frontSprite` → `Assets/Assets/Gambar/bingkaidepan.png`
- `backSprite` → `Assets/Assets/Gambar/bingkaibelakangNT.png`
- `flipButton` → BtnFlip (labeled "BUKA")
- `closeButton` → BtnClose (labeled "TUTUP")
- `captureButton` → BtnCapture (labeled "KAMERA", starts inactive)
- `descriptionText` → DescText (TextMeshProUGUI)
- `frontDescription` → "Foto Frame — Tekan BUKA untuk lihat isi"
- `backDescription` → "Sisi belakang — Tekan KAMERA untuk foto"

**UIItemInspectTrigger serialized fields:**
- `itemInspect` → FrameCanvas's UIItemInspect component

---

## Issue 1: Hover Outline Tidak Muncul

**Symptoms:**
- Saat kursor mendekati/mengarah ke `WorldPhotoFrame` di play mode, tidak ada outline hitam
- `UIBookFlipTrigger` (pada `ClosedBook`) seharusnya punya mekanisme yang SAMA, dan kemungkinan juga tidak muncul

**What's been tried/fixed:**
- `UIItemInspectTrigger.cs` sudah di-patch: `Start()` membuat material instance, pakai `spriteRenderer.material` (instance) bukan `sharedMaterial`
- `spriteRenderer.material.SetFloat("_Outline", 1)` **berfungsi** jika dipanggil langsung via code (verified in play mode)

**Root cause yang mungkin:**
1. **IPointerEnterHandler tidak terpanggil** — kemungkinan `EventSystem` + `InputSystemUIInputModule` tidak mengirim event ke `WorldPhotoFrame`. Padahal `Physics2DRaycaster` sudah ada di Main Camera. Mungkin WorldPhotoFrame perlu berada di layer yang di-*raycast* atau ada masalah dengan Input System bindings.
2. **Input System action maps** — Mungkin perlu "Point" action yang benar-benar aktif. Cek Project Settings → Input System.
3. **WorldPhotoFrame BoxCollider2D** — Mungkin collider-nya terlalu kecil, offset, atau tidak overlap dengan posisi mouse/kursor.

**Suggested fix:**
- Tambahkan `Debug.Log("OnPointerEnter/Exit")` di `UIItemInspectTrigger` untuk konfirmasi apakah event dipanggil
- Jika tidak, cek apakah `Physics2DRaycaster` eventCamera benar-benar Main Camera
- Alternatif: gunakan proximity check di `Update()` sebagai fallback:
  ```
  Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
  float dist = Vector2.Distance(mousePos, transform.position);
  SetOutline(dist < someRadius);
  ```

---

## Issue 2: Layout Inspect Panel Tidak Cocok dengan Book

**BookPreviewPanel layout (reference — works correctly):**
- anchorMin: (0,0), anchorMax: (1,1) → **full-screen overlay**
- Children semua pakai **anchor 0.5,0.5 + anchoredPosition** (center-anchored, not stretched)
- Children positioned by anchoredPosition + sizeDelta

**FrameView layout (current — needs fixing):**
- anchorMin: (0.15,0.1), anchorMax: (0.85,0.9) → **stretched, centered panel** (70% width, 80% height)
- FrameImage stretched within panel → image might look squished if panel proportions don't match sprite
- DescText/Buttons bottom-anchored → different from book's center-positioned approach

**Fix needed:**
Redesign FrameView to **exactly match** BookPreviewPanel's approach:
- FrameView should be **full-screen** (0,0 to 1,1), same as BookPreviewPanel
- FrameImage should use **anchor 0.5,0.5** with anchoredPosition + fixed sizeDelta (not stretch)
- DescText should be positioned similarly to book's DescriptionText (center-anchored)
- Buttons should be positioned like book's OpenButton/ESCButton (center-anchored)
- Background color should also match the book's dark overlay (BookPreviewPanel doesn't have a "brown panel" — it just has the LargeBookImage + text + buttons over the dark overlay)

---

## Additional Notes

### Screenshots available at:
- `Assets/Screenshots/screenshot-20260707-100810.png` — game view saat _Outline = 1 (di-set manual via code)

### CameraManager
- GameObject `[CameraManager]` ada di scene dengan script `CameraManager` (global singleton, DontDestroyOnLoad)
- Memiliki method `Capture()` yang dipanggil oleh `UIItemInspect.OnCaptureClicked()` dan `UIBookFlip.OnCaptureButtonClicked()`

### PhoneCameraController
- Prefab: `Assets/Prefabs/PhoneCamera.prefab`
- Di-instantiate oleh `CameraManager` saat `Capture()` dipanggil
- Setelah selesai (save/retake), kamera ditutup

### Photo frame sprites:
- Front: `Assets/Assets/Gambar/bingkaidepan.png`
- Back: `Assets/Assets/Gambar/bingkaibelakangNT.png`
- Prefab: `Assets/Prefabs/WorldPhotoFrame.prefab`
