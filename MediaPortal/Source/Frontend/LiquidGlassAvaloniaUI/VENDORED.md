# LiquidGlassAvaloniaUI, vendored

Third-party code, not ours. **MIT License, Copyright (c) 2025 KaranocaVe** — the full text is in
[LICENSE.txt](LICENSE.txt) next to these sources and it has to travel with them.

- Upstream: <https://github.com/KaranocaVe/LiquidGlassAvaloniaUI>
- Taken at commit: `cde864d6efebc5b32484eb055900308ef4f65754`
- Copied: the whole `LiquidGlassAvaloniaUI/` library folder of that repository — every `.cs` file (thirteen) and
  the six `.sksl` shaders under `Assets/Shaders/`. Its demo and test projects were left behind; the front end's
  own tests cover what this tree uses.

## Why it lives here instead of coming from a feed

The package was never published. `dotnet package search LiquidGlassAvaloniaUI` answers with an unrelated
`liquidglass.avalonia`, and the upstream repository ships no nupkg. The alternative was to transcribe the
shader pipeline into the front end by hand, and that is the one part of this nobody should hand-copy: about a
thousand lines of SkSL in six files, plus the refraction arithmetic that feeds them.

## What this tree changed

The sources are byte-for-byte upstream's, so that syncing with the repository later is a diff and not a merge.
Everything this tree did differently is in the project file, not in the code:

| What | Upstream | Here | Why |
| --- | --- | --- | --- |
| `TargetFramework` | `net8.0` | `net10.0` | one runtime in the process, same as the rest of this tree |
| `Avalonia`, `Avalonia.Skia` | `12.0.1` | `12.1.4` | the front end's version; two Avalonias in one process is a crash, not a warning |
| `SkiaSharp` | `3.119.3-preview.1.1` | `3.119.4` | the version `Avalonia.Skia 12.1.4` already resolves — the refraction is written against Skia's runtime-effect API and must run against the SkiaSharp Avalonia itself draws with |
| `LangVersion` | `9` | SDK default | the tree's own C# level; nothing in these files needed pinning |
| `AssemblyName` / `RootNamespace` | `LiquidGlassAvaloniaUI` | unchanged | the shaders are loaded as `avares://LiquidGlassAvaloniaUI/Assets/Shaders/…`, and that URI is built from the assembly name |

Nothing in `LiquidGlassAvaloniaUI/` is referenced by name outside `Views/HomeView.axaml` (the shortcut tiles)
and `Program.cs` (the builder call that puts the compositor and Skia settings the shader is tuned for in place).
If a later phase decides the glass is not worth its cost, deleting this folder and those two places is the whole
removal.

## How it is used here

`Views/HomeView.axaml` puts one `LiquidGlassSurface` in each shortcut tile of the home wall, and that surface is
the tile's only glass: the `Fluid.Avalonia.Acrylic` surface the tile wore before was taken out once the lens had
been measured against it live - same median frame time, same p95, same number of slow frames - rather than left
behind a switch, because a tile frosted and lensed at once is two glasses nobody chose. Where the tile's
rounding, veil, glyph, name, ring and shadow come from is written out in the comments on that template.

One thing the consumer has to do by hand: **the surface sets `ClipToBounds="False"` in its statics** so that its
own shadow can be drawn outside the control, and on a rail of tiles twelve pixels apart that paints one plate
over the whole rail - rings, gaps and all. Every `LiquidGlassSurface` in this skin therefore sets
`ClipToBounds="True"` on the instance (its shadow is off anyway, `ShadowEnabled="False"`), and
`Tests/Tests.Frontend/TestTheShortcutTileWearsOneGlass.cs` holds that setting, because the overflow the property
prevents is drawn past the control's bounds and so is invisible to any rectangle measured on the visual tree.
