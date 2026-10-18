# SkinEngine Multiplatform Proposal

**Scope:** replacing the Direct3D 9 backend of `Source\UI\SkinEngine` with a multiplatform rendering architecture (Windows / Linux / Android), while preserving MP2's skin system.
**Companion to:** [Multiplatform-Assessment-and-Plan.md](./Multiplatform-Assessment-and-Plan.md) — this document supersedes its §8 Phase 3.
**Date:** 2026-10-17

---

## 1. Headline: do not rewrite SkinEngine

SkinEngine looks like a monolithic Windows renderer. Measured, it is not.

| Measurement | Value | Reading |
|---|---|---|
| SkinEngine total | **491 files / 82,952 LOC** | |
| Files mentioning `SharpDX` | 140 | misleading |
| Files with **actual Direct3D/DXGI** usage | **77 files / 19,877 LOC = 24%** | this is the only real portability debt |
| Files using `SharpDX` for **math types only** | 64 | `SharpDX.Mathematics` is pure managed C# → **not a blocker**; swap for `System.Numerics` |
| **Engine-agnostic, directly reusable** | **414 files / ~63,075 LOC = 76%** | control tree, layout, MPF, XAML parser, animations, skin mgmt, geometry, input |
| Distinct Direct3D 9 operations used | **≈20** | see §2 — this is a small 2D renderer |
| HLSL shaders | 35 files / ~1,250 LOC, all `vs_2_0`/`ps_2_0` | a weeks-long port, not a months-long one |

**The valuable IP in SkinEngine is not the renderer.** It is the ~63k LOC of Windows-independent framework sitting above it:

- `MpfElements` (80 files / 7,845 LOC) — MP2's **own** `DependencyObject`, `RoutedEvent`, `EventManager`, data-binding and `MPF` attached-property layer. **Independent of WPF's runtime.**
- `Xaml` (32 files / 5,334 LOC) — MP2's **own runtime XAML parser** (`Parser.cs`, `XmlReader`-based, `DefaultNamespaceHandler`, `TypeConverter`, markup-extension activator). The 1,090 skin XAML files are *interpreted at runtime*, not compiled against WPF. **Skins survive the port unchanged.**
- `Controls` (196 files / 34,782 LOC) — visual tree, layout passes, transforms, animations, hit-testing. Only 84 of these files mention SharpDX, and **47 of them touch D3D** — and then only inside their `RenderOverride`.
- `Geometry` (533 LOC) — a full constrained-Delaunay triangulator, **pure C#**, portable as-is.
- `SkinManagement`, `ContentManagement` (asset cache/loader), `InputManagement`, `ScreenManagement`, `MarkupExtensions` (4,315 LOC) — engine-agnostic.

> **Proposal in one line:** *keep the brain, replace the muscle.* Extract a ~20-operation GPU abstraction, put the existing D3D9 code behind it as backend #1, then add Skia backends for Linux and Android. Never a rewrite.

---

## 2. What the renderer actually needs to do

Extracted from the 77 D3D-coupled files. This is the entire required capability of a backend:

| Capability | D3D9 today | Usage count |
|---|---|---|
| Device create / reset / present / lost-device recovery | `CreateDevice`, `Reset` (45), `Present`, `BeginScene`/`EndScene` | small, centralised in `DirectX\GraphicsDevice.cs` |
| Adapter + capability enumeration | `Direct3D.Adapters`, `CheckDeviceFormat`, `GetDeviceCaps` | display-config UI only |
| Clear (render target / stencil) | `Clear` | **128 sites** — trivial everywhere |
| Render-state push/pop (blend, alpha-test, z, scissor) | `SetRenderState`, `TemporaryRenderState` | state stack already abstracted |
| Textures: create CPU→GPU, create RT, copy/stretch, CPU lock | `CreateTexture` (5), `CreateRenderTarget`, `StretchRectangle` (8), `Lock/UnlockRectangle` | ~15 sites total |
| Sampler state (incl. `BORDER` addressing w/ border colour) | `SetSamplerState`, `SetTexture` | used by every brush — must-have |
| Geometry submit | `PositionColoredTextured[]` + `PrimitiveBuffer.Render()` | **one vertex format**, one draw call site |
| Shader effects (HLSL, params, multi-pass) | DX9 Effect framework via `EffectAssetCore` | see §5 |
| Offscreen render targets (`VisualBrush`) | `RenderTargetAsset.Surface`, `RenderToSurfaceInternal` | RT-to-texture |
| Render pipelines incl. SBS/TAB stereo | `IRenderPipeline` | RT indirection + viewport split |

Two structural facts make this tractable:

1. **The draw contract is already almost an interface.** `RenderContext` carries only `(Matrix transform, RectangleF clip bounds, float opacity)` and flows down the tree via `Render(RenderContext)` / `RenderOverride(RenderContext)`. `Brush`→`ImageContext.StartRender(renderContext, targetSize, texture, clip, borderColor, frameData)` → draw → `EndRender()`. That *is* a sprite-batch contract.
2. **There is exactly one vertex format:** `PositionColoredTextured` (position, colour, UV). The classic 2D sprite vertex. Every modern 2D GPU API accepts it natively.

## 3. The video path — the only genuinely hard part

`Players\ISharpDXVideoPlayer.cs` documents itself as the coupling: *"Video players always have a tight coupling with the underlaying video rendering engine."* The contract is: player exposes a GPU `Texture`, a `CropVideoRect`, a `SurfaceLock`, and optional `TexturePlanes[]` for OSD/subtitles; `VideoBrush` samples it through `ImageContext`.

This is an **external GPU texture import** problem, and cross-API texture interop (D3D11↔GL↔Vulkan↔Android AHardwareBuffer) is where such ports die. Do not fight it.

**Recommendation: adopt a two-plane compositing model** — video on a player-owned surface behind a transparent UI surface:

```
┌─────────────────────────────────────────┐
│  UI plane   — Skia, transparent, ARGB   │  ← SkinEngine, all skins/controls/OSD/subtitle UI
├─────────────────────────────────────────┤
│  Video plane — player-owned             │  ← EVR/D3D9 (Win) · libmpv (Win/Linux) · Media3 (Android)
└─────────────────────────────────────────┘
```

Rationale:
- It is the **industry-standard HTPC architecture** — Kodi (which had exactly this problem: custom skin engine, Direct3D + OpenGL + DRM backends) composes video and UI as separate layers; so do Android TV apps and every set-top box.
- It removes 100% of cross-API texture interop from the critical path.
- Each player keeps its own optimal YUV→RGB, deinterlace, tone-mapping and frame-paced path — you do **not** want to reimplement those in a UI renderer's shader.
- Subtitles, audio/video sync and refresh-rate switching stay with the player, where they already work.
- Costs one thing: per-pixel alpha compositing of UI over video moves to the **display compositor** rather than a pixel shader. Verify HDR/alpha-blend quality early on each target — this is the single biggest technical risk in this proposal (§8, R1).

`IVideoProvider` (the renamed `ISharpDXVideoPlayer`) then only needs: `SetRenderDelegate`, `CropRect`, `Lock`, `PlaneCount`, `PresentToNativeHandle()` — plus a "same-device texture" fast path that the Windows D3D9/D3D11 backend can still use for zero-copy.

---

## 4. Proposed architecture

```
MediaPortal.UI.Graphics.Abstractions        (net8.0, NO native deps, no SharpDX)
MediaPortal.UI.Graphics.Skia                (SkiaSharp — Win / Linux / Android)  ← primary
MediaPortal.UI.Graphics.Direct3D9           (existing code, moved)                ← Windows, phase 1
MediaPortal.UI.Graphics.Direct3D11          (later: HDR/swap-chain/DWM quality)
MediaPortal.UI.Graphics.Software            (Skia raster — headless, CI, low-end)
MediaPortal.UI.Skins.*                      (1,090 skin XAML files — UNTOUCHED)
```

### 4.1 Interface sketch

```csharp
namespace MediaPortal.UI.Graphics;               // net8.0 — no platform references

public interface IGpuDevice : IDisposable {
    GpuCapabilities Caps { get; }
    IRenderTarget CreateRenderTarget(IntSize size, SurfaceFormat format);
    ITexture      CreateTexture(IntSize size, SurfaceFormat format);
    IMappedTexture Map(ITexture texture);                       // CPU <-> GPU staging
    ITexture      WrapNativeTexture(NativeTextureInfo info);    // video fast path (optional)
    IShaderEffect CompileShader(ShaderSource source);
    IVertexBatch  CreateVertexBatch(PrimitiveTopology topology);
    void BeginFrame(TimeSpan vsyncDeadline);
    void EndFrame();                                            // present
    bool TryRestoreAfterLost(out bool needsResourceReload);      // device-lost handling
}

public interface IRenderTarget { ITexture Color { get; } void Clear(Color4 c, float z, int stencil); }

public interface IVertexBatch : IDisposable {
    void SetTransform(Matrix4x4 worldViewProj);
    void Draw(ReadOnlySpan<VertexPositionColorTexture> verts, PrimitiveTopology topology);
}

public interface IShaderEffect : IDisposable {
    void SetMatrix(string name, in Matrix4x4 m);
    void SetVector(string name, in Vector4 v);
    void SetFloat(string name, float v);
    void SetTexture(string name, ITexture texture, SamplerState sampler);
    int PassCount { get; }
    void BeginPass(int index);  void EndPass();
}

public interface IFontEngine : IDisposable {          // FreeType is already Skia's backend
    IGlyphAtlas GetOrBuildAtlas(FontDescription font, float size, GlyphRenderMode mode);
}

public interface IPresentationTarget : IDisposable {   // window / surface abstraction
    event Action<IntSize> Resized;
    event Action<bool>    Occluded;                    // minimize -> stop rendering
    NativeWindowHandle   Native { get; }
    IReadOnlyList<DisplayMode> EnumerateDisplayModes();
    DisplayMode CurrentDisplayMode { get; }
    void SetVsyncMode(VsyncMode mode);                 // replaces RenderStrategy
    void SetSwapChainRotation(int degrees);            // fixes today's 90/270 crash
}

public interface IVideoProvider {                      // was ISharpDXVideoPlayer
    bool SetRenderDelegate(Action perFrameRender);
    RectangleF CropRect { get; }
    object FrameLock { get; }
    void CompositeTo(IRenderTarget target, in DrawState state);  // via video plane, or fast-path texture
    void ReleaseNativeResources();  void ReallocateNativeResources();
}
```

### 4.2 Rules that keep the port honest

1. `MediaPortal.UI.Graphics.Abstractions` compiles to `net8.0` with **zero** package references. CI arch-test: no `SharpDX`, `SkiaSharp`, `System.Windows.Forms`, `Microsoft.Win32` in it or in `Controls`/`MpfElements`/`Xaml`.
2. The visual tree may only reach the GPU through `RenderContext` → `IGpuDevice`. **No backend types in public skin-facing API** — this is what lets 1,090 skins keep working.
3. One vertex format (`VertexPositionColorTexture`) forever. If a feature can't be expressed as transformed, textured, coloured quads + an effect, it belongs in a render target, not a new primitive.
4. Backends are selected by `ServiceRegistration` at startup (`--renderer skia|d3d9|d3d11|raster`), so every backend stays testable and Windows can bisect regressions per backend.

---

## 5. Shader migration (35 `.fx` → one portable dialect)

Total HLSL is ~1,250 LOC. `normal.fx` is the base technique (textured quad × vertex colour, premultiplied alpha); `video_base.fx` is a **template** that calls `PixelTransform()` / `PixelEffect()` fragments supplied by effect files — i.e. MP2 already has a composable shader-fragment system, which maps cleanly onto SkSL `SkRuntimeEffect` composition or onto GLSL/SPIR-V includes.

Uniform vocabulary is only 7 names: `worldViewProj`, `g_transform`, `g_relativetransform`, `g_imagetransform`, `g_opacity`, `g_framedata`, `g_texture`.

| Group | Shaders | Effort | Notes |
|---|---|---|---|
| **Free** (native Skia equivalents) | `solid`, `none`, `dissolve`, `fade`, `normal` | ~0 | default blend / `SkImageFilter::Blend` |
| **Free** (Skia shaders/filters exist) | `lineargradient`, `radialgradient`, `tile`, `tile_simple`, `dialogbg` | 2–3 d | `SkShader::MakeLinear/Radial`, `MakeImage` with `kDecal`/border addressing |
| **Delete outright** | `font`, `font_fade`, `imagecontext_base`, `imagecontext_default`, `imagecontext_transition_base`, `video_base` | −2,000 LOC C# | text → Skia's FreeType glyph pipeline (§6); image-context → paint state; video → video plane (§3) |
| **Simple SkSL** (≤20 LOC each) | `wipe_*` (4), `granular_dissolve`, `invert`, `sepia`, `greyscale`, `pixelate`, `transforms/*` | 3–5 d | arithmetic on texcoord/colour |
| **Real SkSL ports** (tap/box sampling) | `blur`, `directional_blur`, `zoom_blur`, `sharpen`, `sharpen_complex`, `denoise`, `glass_tiles` | 1–2 wk | 7 shaders, ~350 LOC SkSL total; `SkImageFilters.Blur` covers `blur` for free |
| **Effect-state carriers** (`KenBurns*`) | 2 C# classes | 1–2 d | become uniform animation, not shaders |

**Recommendation on dialect:** author shaders once in **HLSL and cross-compile with DXC → SPIR-V → SPIRV-Cross** for GLSL/MSL, *plus* a hand-written SkSL emitter for the Skia backend. If the Skia backend is the only target for the first two years, start with hand-written SkSL (35 small files) and defer the cross-compiler until a Vulkan backend is actually funded. Do not build shader infrastructure before you have two real backends needing it.

---

## 6. Text rendering — an accidental big win

Today: `Fonts\FreeType.cs` (35 `DllImport`s) + `Rendering\TextBuffer.cs` (825 LOC) + `AssetCore\FontAssetCore.cs` + `font.fx`/`font_fade.fx`, building glyph geometry into `PositionColoredTextured` vertices manually.

SkiaSharp **already uses FreeType as its font backend on Linux and Android**, and already does sub-pixel-positioned glyph atlases, hinting and gamma-correct blending — and HarfBuzz is bundled for shaping.

Adopting the Skia backend therefore lets you **delete ~2,500 LOC of hand-rolled text stack** and *gain* bidi/shaping correctness that MP2 currently lacks. Treat `IFontEngine` as the seam: Skia implementation for Skia backends, existing FreeType path retained only inside `Graphics.Direct3D9` during migration.

---

## 7. Backend decision

| Option | Win | Linux | Android | Shader control | HTPC look/behaviour | Maint. risk | Effort (to 3 OS) |
|---|---|---|---|---|---|---|---|
| **A. SkiaSharp** | ✅ | ✅ | ✅ | SkSL runtime effects | excellent | low (Google/Skia, ships in Chrome, Flutter, Android) | **lowest** |
| B. Vulkan (Silk.NET/Vortice) | ✅ | ✅ | ✅ | full | excellent | medium (you own a 2D renderer) | high |
| C. D3D11 (Vortice) + Vulkan pair | ✅✅ | ✅ | ❌ | full | excellent | medium | high |
| D. Adopt Avalonia as the UI stack | ✅ | ✅ | ⚠️ | poor | regains less; 1,090 skins + MPF must be re-authored | low for you, high for skins | medium-high |
| E. MAUI / Uno / native UIs | ✅ | ⚠️ | ✅ | none | loses the skin engine entirely | low | high |
| **F. Skia UI + player-owned video plane** *(recommended)* | ✅ | ✅ | ✅ | SkSL for UI, player shaders for video | excellent, matches Kodi | low | medium |

**Choose A now, evolve to F.** One API covers all three targets, it is the only mainstream GPU API that is genuinely first-class on Android *and* Linux *and* Windows, and it has a **raster backend** — which is worth a great deal: it makes headless visual-regression tests, CI on `ubuntu-latest` with no GPU, and low-end SBC deployments all trivial. That raster path is an under-appreciated testing superpower that SharpDX never offered.

Why not D (Avalonia): it is Skia-based and would remove renderer ownership, but it forces migration of MP2's own XAML dialect → Avalonia's, MPF `DependencyObject`/`RoutedEvent`/bindings → Avalonia's, and it drops the per-brush shader/transition/render-pipeline model that makes MP2 skins what they are. Cost lands in the same range as steps 1–4 below, with more visual-fidelity risk on the 10-foot UI and no path back. Choose D only if the team decides it wants to stop owning a renderer at all — a legitimate but different product decision.

---

## 8. Migration plan — 5 steps, each independently shippable

> **The rule that de-risks this:** never introduce an abstraction *and* a new backend in the same step. Step 2 exists purely to prove the interface is sufficient, using code you already know is correct.

| # | Step | Content | Gate (must pass before next step) | pm |
|---|---|---|---|---|
| **1** | **De-SharpDX the math** | `SharpDX.Mathematics` → `System.Numerics` (`Matrix4x4`, `Vector2/3/4`, `Color4` shim, `RectangleF`→`RectF`) across the 64 math-only files. Mechanical. Create `Graphics.Abstractions` with the §4.1 interfaces. | Windows client pixel-identical; SharpDX removed from all but the 77 D3D files + `DirectX\` | 0.5 |
| **2** | **D3D9 as backend #1** | Move all 77 D3D files into `Graphics.Direct3D9` implementing the interfaces. `RenderContext` gains no new capability. `GraphicsDevice`, `RenderStrategy`, `IRenderPipeline`, `Texture/RenderTarget/EffectAssetCore` all sit behind it. `MainForm` shrinks to an `IPresentationTarget`. | Windows pixel-identical; **this is the interface-completeness proof**. Add golden-image tests (Dx9/raster baselines) here. | 1.5–2.5 |
| **3** | **Skia backend, on Windows first** | Implement `Graphics.Skia`. Port shaders (§5), text (§6), border-addressed sampling, `VisualBrush` RT-to-texture, opacity/blend states, SBS/TAB via RT indirection. Run the golden-image suite **on Windows** under `--renderer skia` (GL *and* raster). | Skin parity on Windows/Skia. This proves Linux readiness with zero Linux-specific bugs in play — the highest-value gate in the plan. | 2–3 |
| **4** | **Linux presentation + video plane** | `IPresentationTarget` for X11/Wayland (via SDL3 or Skia's native windowing), input, DPI/multi-monitor, vsync; two-plane compositing with **libmpv**; FHS paths; deb/Flatpak. | Linux desktop milestone: library browsing + video playback + skins at agreed parity subset. | 2–3 |
| **5** | **Android presentation + video plane** | `net8.0-android`: `IPresentationTarget` on `SurfaceView`/`TextureView`, D-pad/touch input, `IVideoProvider` on Media3/ExoPlayer, audio focus/PiP, lifecycle/surface-restore. | Android app: browse + play + control. | 1–2 |
| later | **D3D11 backend** | HDR10/Dolby Vision, DX11 swap-chain with proper DWM presentation, per-monitor DPI, hardware decode 10-bit; retires SharpDX entirely and the EVRPresenter C++ project. | Windows quality parity or better vs. today. | 1.5–2 |

**Total: 7–11 pm** for a multiplatform engine across all three OSes — versus 8–14 pm in the earlier coarse estimate, revised down because the measured D3D surface is ~20 operations, 76% of the engine is already portable, and the two-plane video model deletes the interop problem rather than solving it.

Sequence note: step 5 depends on the portable server (Phase 2 of the main plan) more than on step 4, so steps 4 and 5 can run in parallel with two people.

---

## 9. Ledger: what you keep, move and delete

| | Files | LOC | Note |
|---|---|---|---|
| **Kept untouched** | 414 | ~63,000 | `MpfElements`, `Xaml`, layout/transforms/animations in `Controls`, `SkinManagement`, `Geometry`, `InputManagement`, `MarkupExtensions`, all 1,090 skin XAML |
| **Moved behind the interface** | 77 | ~19,900 | becomes `Graphics.Direct3D9`; still used on Windows until D3D11 lands |
| **Deleted once Skia lands** | ~12 | ~2,000–2,500 | `Fonts\FreeType.cs`, `Rendering\TextBuffer.cs`, `FontAssetCore`, `font*.fx`, `imagecontext_*.fx`, `video_base.fx` |
| **Rewritten as SkSL** | 35 `.fx` | ~350 SkSL | from ~1,250 HLSL |
| **New** | ~25 | ~4,000–6,000 | abstractions, Skia backend, windowing per OS |

Net: SkinEngine shrinks by ~2k LOC of text/shader plumbing while gaining three platforms.

---

## 10. Risks

| # | Risk | Sev | Mitigation |
|---|---|---|---|
| R1 | Compositor alpha quality when UI overlays video (banding, blending in wrong colour space, HDR tone-map mismatch) on some Wayland/Android compositors | 🔴 | Prototype two-plane compositing on **each** target in **week 1 of step 4**, before any large investment. Fallback: single-surface path with `WrapNativeTexture` interop for the one case that needs it. |
| R2 | Skia `BORDER` sampler addressing w/ border colour (used by every `TileBrush`) has no 1:1 `SkSamplingOptions` equivalent | 🟠 | Encode as `kDecal` wrap + clamped UV inset, or bake a 1px border into the atlas. Validate against Dx9 golden images in step 3. |
| R3 | Abstraction proves insufficient late (multi-pass effects, `KenBurns`, stereo pipelines) | 🟠 | That is exactly what step 2's gate exists to catch — using known-good D3D9 code, before a second backend exists. |
| R4 | SkiaSharp version churn (Ganesh→Graphite migration in flight upstream) | 🟡 | Pin; keep `Graphics.Skia` the only project referencing it. Re-evaluate Graphite for Vulkan later. |
| R5 | 7 real SkSL effect ports drift visually from HLSL | 🟡 | Per-shader golden-image diffs; agree a reduced Linux effect set rather than chasing float-exact parity. |
| R6 | Wayland surface/alpha/HDR immaturity for HTPC full-screen | 🟡 | Ship X11 first; Wayland behind a flag; SDL3 hides most of it. |
| R7 | Someone re-adds a D3D type into `Controls` | 🟡 | CI arch-test (§4.2 rule 1), fails the build. |
| R8 | Team bandwidth: this is 7–11 pm | 🟠 | Steps 1–3 also *improve* Windows (raster fallback, tests, crash fixes, DPI/rotation fixes). It is not a "for Linux only" cost — that framing is what gets it funded. |

## 11. Incidental bugs this refactor fixes

Worth stating in the funding pitch — all are consequences of the D3D9/Win32 coupling being spread through the UI layer rather than behind an interface:

- `ScreenManager` reads `Screen.PrimaryScreen.WorkingArea.Bounds` and sets `Left/Top` from it, with **no DPI handling** while `AppSettings` applies a `DPIScale` to render size → wrong monitor placement / blank window on scaled displays.
- `ScreenManager.GetResolution` reports the **pre-rotation** `screen.Bounds` while `ScreenRotation` is applied, with **no validation that the rotation exists**, so a 90/270 setting can present a mismatched back buffer and crash.
- `MainForm.ReinitializeDevice()` is empty with `// TODO: When DirectX is available` → the device-lost path is unimplemented. `IGpuDevice.TryRestoreAfterLost` makes it a backend requirement.
- No Wayland, no per-monitor DPI, no HDR, no software fallback.

---

## 12. Decision needed

| Question | My recommendation |
|---|---|
| Rewrite or abstract? | **Abstract.** 76% of the engine is already portable; the D3D surface is ~20 operations. |
| Which backend? | **SkiaSharp**, with a raster backend kept for CI/low-end; D3D11 later for Windows HDR. |
| Video? | **Two-plane compositing** (UI over player-owned video), matching Kodi/Android-TV practice. Fast-path same-device texture interop retained on Windows. |
| Skins? | Untouched. MP2's XAML is runtime-interpreted by its own parser, so skin files survive. Publish a "Linux skin feature subset" for the deferred stereo/`glass_tiles`-class effects. |
| Text? | Adopt Skia's FreeType/HarfBuzz stack; delete the hand-rolled glyph pipeline. |
| First commit? | Step 1 — the `System.Numerics` swap. Purely mechanical, touches 64 files, deletes a dependency on an abandoned package, and makes the real D3D surface countable at exactly 77 files. |
