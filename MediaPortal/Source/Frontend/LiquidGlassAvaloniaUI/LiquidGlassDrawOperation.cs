using System;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace LiquidGlassAvaloniaUI
{
    internal class LiquidGlassDrawOperation : ICustomDrawOperation
    {
        private static SKRuntimeEffect? s_lensEffect;
        private static SKRuntimeEffect? s_highlightEffect;
        private static SKRuntimeEffect? s_gammaEffect;
        private static SKRuntimeEffect? s_interactiveHighlightEffect;
        private static SKRuntimeEffect? s_progressiveMaskEffect;
        private static SKRuntimeEffect? s_backdropTransformEffect;
        private static bool s_loaded;

        private readonly Rect _bounds;
        private readonly LiquidGlassDrawParameters _parameters;
        private readonly LiquidGlassBackdropSnapshot? _backdropSnapshot;
        private readonly LiquidGlassDrawPass _pass;

        public LiquidGlassDrawOperation(
            Rect bounds,
            LiquidGlassDrawParameters parameters,
            LiquidGlassBackdropSnapshot? snapshot,
            LiquidGlassDrawPass pass)
        {
            _bounds = bounds;
            _parameters = parameters;
            _backdropSnapshot = snapshot is not null && snapshot.TryAddLease() ? snapshot : null;
            _pass = pass;
        }

        public void Dispose()
        {
            _backdropSnapshot?.ReleaseLease();
        }

        public bool HitTest(Point p)
        {
            return false;
        }

        public Rect Bounds
        {
            get => _bounds;
        }

        public bool Equals(ICustomDrawOperation? other)
        {
            return false;
        }

        public void Render(ImmediateDrawingContext context)
        {
            ISkiaSharpApiLeaseFeature? leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature is null)
                return;

            LoadShaders();

            using ISkiaSharpApiLease lease = leaseFeature.Lease();
            SKCanvas canvas = lease.SkCanvas;

            switch (_pass)
            {
                case LiquidGlassDrawPass.Lens:
                    RenderLens(canvas, lease.GrContext);
                    break;
                case LiquidGlassDrawPass.InteractiveHighlight:
                    RenderInteractiveHighlight(canvas);
                    break;
                case LiquidGlassDrawPass.Highlight:
                    RenderHighlight(canvas);
                    break;
            }
        }

        private static void LoadShaders()
        {
            if (s_loaded)
                return;
            s_loaded = true;

            s_lensEffect = LoadRuntimeEffect("avares://LiquidGlassAvaloniaUI/Assets/Shaders/LiquidGlassShader.sksl");
            s_highlightEffect = LoadRuntimeEffect("avares://LiquidGlassAvaloniaUI/Assets/Shaders/LiquidGlassHighlight.sksl");
            s_gammaEffect = LoadRuntimeEffect("avares://LiquidGlassAvaloniaUI/Assets/Shaders/LiquidGlassGamma.sksl");
            s_interactiveHighlightEffect = LoadRuntimeEffect("avares://LiquidGlassAvaloniaUI/Assets/Shaders/LiquidGlassInteractiveHighlight.sksl");
            s_progressiveMaskEffect = LoadRuntimeEffect("avares://LiquidGlassAvaloniaUI/Assets/Shaders/LiquidGlassProgressiveMask.sksl");
            s_backdropTransformEffect = LoadRuntimeEffect("avares://LiquidGlassAvaloniaUI/Assets/Shaders/LiquidGlassBackdropTransform.sksl");
        }

        private static SKRuntimeEffect? LoadRuntimeEffect(string assetUriString)
        {
            try
            {
                Uri assetUri = new(assetUriString);
                using Stream stream = AssetLoader.Open(assetUri);
                using StreamReader reader = new(stream);
                string shaderCode = reader.ReadToEnd();

                SKRuntimeEffect? effect = SKRuntimeEffect.CreateShader(shaderCode, out string? errorText);
                if (effect == null)
                    Console.WriteLine($"[LiquidGlass] Failed to create SKRuntimeEffect ({assetUriString}): {errorText}");

                return effect;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LiquidGlass] Exception while loading shader ({assetUriString}): {ex.Message}");
                return null;
            }
        }

        private void RenderLens(SKCanvas canvas, GRContext? grContext)
        {
            if (s_lensEffect is null)
            {
                DrawErrorHint(canvas);
                return;
            }

            if (_backdropSnapshot is null)
            {
                DrawBackdropNotReady(canvas);
                return;
            }

            SKMatrix currentTransform = canvas.TotalMatrix;
            if (!currentTransform.TryInvert(out SKMatrix currentInvertedTransform))
                return;

            SKSize size = new((float)_bounds.Width, (float)_bounds.Height);
            if (size.Width <= 0 || size.Height <= 0)
                return;

            float maxRadius = Math.Min(size.Width, size.Height) * 0.5f;
            float[] cornerRadii = GetCornerRadii(_parameters.CornerRadius, maxRadius);

            // Pipeline order:
            //   color controls (brightness/contrast/vibrancy) -> blur -> lens -> (optional gamma)
            //
            // In Avalonia we approximate the RenderEffect chain by applying an SKImageFilter pipeline to the
            // captured backdrop snapshot, then sampling the filtered image in the lens runtime shader.
            LiquidGlassBackdropSnapshot.FilteredResult filtered = GetOrCreateFilteredBackdrop(
                _backdropSnapshot,
                _parameters,
                grContext,
                currentTransform,
                size);

            using SKShader? backdropShader = SKShader.CreateImage(
                filtered.Image,
                SKShaderTileMode.Clamp,
                SKShaderTileMode.Clamp,
                WithPixelOrigin(currentInvertedTransform, filtered.OriginInPixels));

            SKShader? lensInput = (SKShader)backdropShader;

            SKShader? backdropTransformShader = null;
            try
            {
                double zoomValue = _parameters.BackdropZoom;
                if (zoomValue <= 0.0005 || double.IsNaN(zoomValue) || double.IsInfinity(zoomValue))
                    zoomValue = 1.0;

                float zoom = (float)Clamp(zoomValue, 0.1, 10.0);
                Vector offset = _parameters.BackdropOffset;
                bool needsTransform =
                    Math.Abs(zoom - 1.0f) > 0.0005f
                    || Math.Abs(offset.X) > 0.0005
                    || Math.Abs(offset.Y) > 0.0005;

                if (needsTransform && s_backdropTransformEffect is not null)
                {
                    using SKRuntimeEffectUniforms uniforms = new(s_backdropTransformEffect);
                    uniforms["size"] = new[]
                    {
                        size.Width, size.Height
                    };
                    uniforms["zoom"] = zoom;
                    uniforms["offset"] = new[]
                    {
                        (float)offset.X, (float)offset.Y
                    };

                    using SKRuntimeEffectChildren children = new(s_backdropTransformEffect);
                    children["content"] = lensInput;

                    backdropTransformShader = s_backdropTransformEffect.ToShader(uniforms, children);
                    if (backdropTransformShader is not null)
                        lensInput = backdropTransformShader;
                }

                float refractionHeight = (float)Clamp(_parameters.RefractionHeight, 0.0, Math.Min(size.Width, size.Height) * 0.5);
                float refractionAmount = (float)_parameters.RefractionAmount;
                bool applyLens = refractionHeight > 0.001f && Math.Abs(refractionAmount) > 0.001f;

                SKShader? lensShader = null;
                if (applyLens)
                {
                    using SKRuntimeEffectUniforms lensUniforms = new(s_lensEffect);
                    lensUniforms["size"] = new[]
                    {
                        size.Width, size.Height
                    };
                    lensUniforms["cornerRadii"] = cornerRadii;
                    lensUniforms["refractionHeight"] = refractionHeight;
                    // The lens shader expects a negative refraction amount.
                    lensUniforms["refractionAmount"] = -refractionAmount;
                    lensUniforms["depthEffect"] = _parameters.DepthEffect ? 1.0f : 0.0f;
                    lensUniforms["chromaticAberration"] = _parameters.ChromaticAberration ? 1.0f : 0.0f;

                    using SKRuntimeEffectChildren lensChildren = new(s_lensEffect);
                    lensChildren["content"] = lensInput;

                    lensShader = s_lensEffect.ToShader(lensUniforms, lensChildren);
                }

                SKShader? baseShader = lensShader ?? lensInput;

                SKShader? progressiveMaskShader = null;
                try
                {
                    if (_parameters.ProgressiveBlurEnabled
                        && s_progressiveMaskEffect is not null)
                    {
                        using SKRuntimeEffectUniforms uniforms = new(s_progressiveMaskEffect);
                        uniforms["size"] = new[]
                        {
                            size.Width, size.Height
                        };
                        uniforms["start"] = (float)Clamp(_parameters.ProgressiveBlurStart, 0.0, 1.0);
                        uniforms["end"] = (float)Clamp(_parameters.ProgressiveBlurEnd, 0.0, 1.0);

                        Color tint = _parameters.ProgressiveTintColor;
                        uniforms["tint"] = new[]
                        {
                            tint.R / 255f, tint.G / 255f, tint.B / 255f, tint.A / 255f
                        };

                        float tintIntensity = tint.A > 0
                            ? (float)Clamp(_parameters.ProgressiveTintIntensity, 0.0, 1.0)
                            : 0.0f;
                        uniforms["tintIntensity"] = tintIntensity;

                        using SKRuntimeEffectChildren children = new(s_progressiveMaskEffect);
                        children["content"] = baseShader;

                        progressiveMaskShader = s_progressiveMaskEffect.ToShader(uniforms, children);
                        if (progressiveMaskShader is not null)
                            baseShader = progressiveMaskShader;
                    }

                    SKShader? gammaShader = null;
                    try
                    {
                        float gammaPower = (float)Clamp(_parameters.GammaPower, 0.0, 10.0);
                        if (s_gammaEffect is not null && Math.Abs(gammaPower - 1.0f) > 0.0005f)
                        {
                            using SKRuntimeEffectUniforms uniforms = new(s_gammaEffect);
                            uniforms["power"] = gammaPower;
                            using SKRuntimeEffectChildren children = new(s_gammaEffect);
                            children["content"] = baseShader;
                            gammaShader = s_gammaEffect.ToShader(uniforms, children);
                        }

                        using SKPaint paint = new()
                        {
                            Shader = gammaShader ?? baseShader,
                            IsAntialias = true
                        };

                        SKRect rect = SKRect.Create(0, 0, size.Width, size.Height);
                        using SKPath clipPath = CreateRoundRectPath(rect, cornerRadii);

                        canvas.Save();
                        canvas.ClipPath(clipPath, SKClipOperation.Intersect, true);
                        canvas.DrawRect(rect, paint);

                        DrawSurfaceOverlay(canvas, rect);

                        canvas.Restore();
                    }
                    finally
                    {
                        gammaShader?.Dispose();
                    }
                }
                finally
                {
                    progressiveMaskShader?.Dispose();
                    lensShader?.Dispose();
                }
            }
            finally
            {
                backdropTransformShader?.Dispose();
            }

        }

        private static LiquidGlassBackdropSnapshot.FilteredResult GetOrCreateFilteredBackdrop(
            LiquidGlassBackdropSnapshot snapshot,
            LiquidGlassDrawParameters parameters,
            GRContext? grContext,
            SKMatrix currentTransform,
            SKSize localSize)
        {
            float brightness = (float)Clamp(parameters.Brightness, -1.0, 1.0);
            float contrast = (float)Clamp(parameters.Contrast, 0.0, 4.0);
            float saturation = (float)Clamp(parameters.Vibrancy, 0.0, 4.0);
            float exposureEv = (float)Clamp(parameters.ExposureEv, -8.0, 8.0);
            float opacity = (float)Clamp(parameters.BackdropOpacity, 0.0, 1.0);

            float blurSigmaPx = (float)(Clamp(parameters.BlurRadius, 0.0, 256.0) * snapshot.Scaling);

            bool isIdentity =
                Math.Abs(brightness) < 0.0005f
                && Math.Abs(contrast - 1.0f) < 0.0005f
                && Math.Abs(saturation - 1.0f) < 0.0005f
                && Math.Abs(exposureEv) < 0.0005f
                && Math.Abs(opacity - 1.0f) < 0.0005f
                && blurSigmaPx <= 0.0005f;

            if (isIdentity)
            {
                LiquidGlassDiagnostics.RecordFilterIdentityHit();
                return new LiquidGlassBackdropSnapshot.FilteredResult(snapshot.Image, snapshot.OriginInPixels);
            }

            SKRectI cropRect = CalculateFilterCrop(snapshot, parameters, currentTransform, localSize);

            // Quantize to keep the cache size stable during slider drags.
            const float q = 1000.0f;
            LiquidGlassBackdropSnapshot.FilteredKey key = new(
                (int)Math.Round(brightness * q),
                (int)Math.Round(contrast * q),
                (int)Math.Round(saturation * q),
                (int)Math.Round(exposureEv * q),
                (int)Math.Round(opacity * q),
                (int)Math.Round(blurSigmaPx * q),
                cropRect.Left,
                cropRect.Top,
                cropRect.Width,
                cropRect.Height,
                GetRenderContextId(grContext));

            if (snapshot.TryGetFiltered(key, out LiquidGlassBackdropSnapshot.FilteredResult cached))
            {
                LiquidGlassDiagnostics.RecordFilterCacheHit();
                return cached;
            }

            LiquidGlassDiagnostics.RecordFilterCacheMiss();

            LiquidGlassBackdropSnapshot.FilteredResult filtered = CreateFilteredBackdrop(snapshot, cropRect, brightness, contrast, saturation, exposureEv, opacity, blurSigmaPx, grContext)
                                                                  ?? new LiquidGlassBackdropSnapshot.FilteredResult(snapshot.Image, snapshot.OriginInPixels);

            // Never cache the unfiltered snapshot image (it is owned/disposed separately).
            if (!ReferenceEquals(filtered.Image, snapshot.Image))
                snapshot.StoreFiltered(key, filtered);

            return filtered;
        }

        private static LiquidGlassBackdropSnapshot.FilteredResult? CreateFilteredBackdrop(
            LiquidGlassBackdropSnapshot snapshot,
            SKRectI cropRect,
            float brightness,
            float contrast,
            float saturation,
            float exposureEv,
            float opacity,
            float blurSigmaPx,
            GRContext? grContext)
        {
            SKImage source = snapshot.Image;
            bool isFullCrop =
                cropRect.Left == 0
                && cropRect.Top == 0
                && cropRect.Width == source.Width
                && cropRect.Height == source.Height;

            using SKImage? croppedSource = isFullCrop ? null : source.Subset(cropRect);
            SKImage filterSource = croppedSource ?? source;
            PixelPoint filteredOrigin = isFullCrop
                ? snapshot.OriginInPixels
                : new PixelPoint(snapshot.OriginInPixels.X + cropRect.Left, snapshot.OriginInPixels.Y + cropRect.Top);

            SKImageFilter? filter = null;

            bool needsColorControls =
                Math.Abs(brightness) > 0.0005f
                || Math.Abs(contrast - 1.0f) > 0.0005f
                || Math.Abs(saturation - 1.0f) > 0.0005f;

            if (needsColorControls)
            {
                using SKColorFilter? colorFilter = SKColorFilter.CreateColorMatrix(CreateColorControlsColorMatrix(brightness, contrast, saturation));
                filter = SKImageFilter.CreateColorFilter(colorFilter, filter);
            }

            if (Math.Abs(exposureEv) > 0.0005f)
            {
                using SKColorFilter? colorFilter = SKColorFilter.CreateColorMatrix(CreateExposureColorMatrix(exposureEv));
                filter = SKImageFilter.CreateColorFilter(colorFilter, filter);
            }

            if (Math.Abs(opacity - 1.0f) > 0.0005f)
            {
                using SKColorFilter? colorFilter = SKColorFilter.CreateColorMatrix(CreateOpacityColorMatrix(opacity));
                filter = SKImageFilter.CreateColorFilter(colorFilter, filter);
            }

            if (blurSigmaPx > 0.0005f)
            {
                // Use TileMode.Clamp: Skia's default tile mode can introduce alpha falloff near image edges (kDecal),
                // which shows up as darkened borders when the snapshot is clipped by the window bounds.
                SKRect filterCropRect = new(0, 0, filterSource.Width, filterSource.Height);
                filter = SKImageFilter.CreateBlur(blurSigmaPx, blurSigmaPx, SKShaderTileMode.Clamp, filter, filterCropRect);
            }

            if (filter is null)
                return null;

            using (filter)
            {
                // ApplyImageFilter() can return a varying offset/subset for blur radii, which can cause the
                // sampled backdrop to appear to "drift" while dragging the blur slider. Instead, render the
                // filtered snapshot into an explicit same-size surface at (0,0) so the origin remains stable.
                SKImageInfo info = new(filterSource.Width, filterSource.Height, filterSource.ColorType, filterSource.AlphaType, filterSource.ColorSpace);
                using SKSurface? surface = CreateFilterSurface(info, grContext);
                if (surface is null)
                {
                    LiquidGlassDiagnostics.RecordFilterSurfaceFailure();
                    return null;
                }

                SKCanvas? canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                using SKPaint paint = new()
                {
                    ImageFilter = filter,
                    BlendMode = SKBlendMode.Src
                };

                canvas.DrawImage(filterSource, 0, 0, paint);
                canvas.Flush();

                SKImage? filteredImage = surface.Snapshot();
                return new LiquidGlassBackdropSnapshot.FilteredResult(filteredImage, filteredOrigin);
            }
        }

        private static SKRectI CalculateFilterCrop(
            LiquidGlassBackdropSnapshot snapshot,
            LiquidGlassDrawParameters parameters,
            SKMatrix currentTransform,
            SKSize localSize)
        {
            SKImage source = snapshot.Image;
            SKRectI full = new(0, 0, source.Width, source.Height);

            if (localSize.Width <= 0 || localSize.Height <= 0)
                return full;

            if (Math.Abs(currentTransform.Persp0) > 0.000001f
                || Math.Abs(currentTransform.Persp1) > 0.000001f
                || Math.Abs(currentTransform.Persp2 - 1.0f) > 0.000001f)
            {
                return full;
            }

            SKRect deviceBounds = MapRect(currentTransform, SKRect.Create(0, 0, localSize.Width, localSize.Height));
            if (deviceBounds.Width <= 0 || deviceBounds.Height <= 0)
                return full;

            double margin = CalculateFilterMarginPx(parameters, localSize, snapshot.Scaling);

            int left = (int)Math.Floor(deviceBounds.Left - snapshot.OriginInPixels.X - margin);
            int top = (int)Math.Floor(deviceBounds.Top - snapshot.OriginInPixels.Y - margin);
            int right = (int)Math.Ceiling(deviceBounds.Right - snapshot.OriginInPixels.X + margin);
            int bottom = (int)Math.Ceiling(deviceBounds.Bottom - snapshot.OriginInPixels.Y + margin);

            left = Math.Clamp(left, 0, source.Width);
            top = Math.Clamp(top, 0, source.Height);
            right = Math.Clamp(right, left, source.Width);
            bottom = Math.Clamp(bottom, top, source.Height);

            if (right <= left || bottom <= top)
                return full;

            return new SKRectI(left, top, right, bottom);
        }

        private static double CalculateFilterMarginPx(LiquidGlassDrawParameters parameters, SKSize localSize, double scaling)
        {
            double zoomValue = parameters.BackdropZoom;
            if (zoomValue <= 0.0005 || double.IsNaN(zoomValue) || double.IsInfinity(zoomValue))
                zoomValue = 1.0;

            double zoom = Clamp(zoomValue, 0.1, 10.0);
            Vector offset = parameters.BackdropOffset;
            double offsetMargin = Math.Max(Math.Abs(offset.X), Math.Abs(offset.Y)) / zoom;

            double zoomOutMargin = 0.0;
            if (zoom < 1.0)
            {
                double halfMaxSize = Math.Max(localSize.Width, localSize.Height) * 0.5;
                zoomOutMargin = (1.0 / zoom - 1.0) * halfMaxSize;
            }

            double refractionMargin = Math.Abs(parameters.RefractionAmount) * (parameters.ChromaticAberration ? 2.0 : 1.0);
            double blurMargin = parameters.BlurRadius * 3.0;

            return Math.Max(8.0, refractionMargin + blurMargin + offsetMargin + zoomOutMargin + 8.0) * scaling;
        }

        private static SKRect MapRect(SKMatrix matrix, SKRect rect)
        {
            float x0 = matrix.ScaleX * rect.Left + matrix.SkewX * rect.Top + matrix.TransX;
            float y0 = matrix.SkewY * rect.Left + matrix.ScaleY * rect.Top + matrix.TransY;
            float x1 = matrix.ScaleX * rect.Right + matrix.SkewX * rect.Top + matrix.TransX;
            float y1 = matrix.SkewY * rect.Right + matrix.ScaleY * rect.Top + matrix.TransY;
            float x2 = matrix.ScaleX * rect.Right + matrix.SkewX * rect.Bottom + matrix.TransX;
            float y2 = matrix.SkewY * rect.Right + matrix.ScaleY * rect.Bottom + matrix.TransY;
            float x3 = matrix.ScaleX * rect.Left + matrix.SkewX * rect.Bottom + matrix.TransX;
            float y3 = matrix.SkewY * rect.Left + matrix.ScaleY * rect.Bottom + matrix.TransY;

            float left = Math.Min(Math.Min(x0, x1), Math.Min(x2, x3));
            float top = Math.Min(Math.Min(y0, y1), Math.Min(y2, y3));
            float right = Math.Max(Math.Max(x0, x1), Math.Max(x2, x3));
            float bottom = Math.Max(Math.Max(y0, y1), Math.Max(y2, y3));

            return new SKRect(left, top, right, bottom);
        }

        private static SKSurface? CreateFilterSurface(SKImageInfo info, GRContext? grContext)
        {
            if (grContext is not null)
            {
                SKSurface? gpuSurface = SKSurface.Create(grContext, false, info);
                if (gpuSurface is not null)
                {
                    LiquidGlassDiagnostics.RecordFilterGpuSurface();
                    return gpuSurface;
                }
            }

            SKSurface? cpuSurface = SKSurface.Create(info);
            if (cpuSurface is not null)
                LiquidGlassDiagnostics.RecordFilterCpuSurface();

            return cpuSurface;
        }

        private static int GetRenderContextId(GRContext? grContext)
        {
            return grContext is null ? 0 : RuntimeHelpers.GetHashCode(grContext);
        }

        private static float[] CreateColorControlsColorMatrix(float brightness, float contrast, float saturation)
        {
            // Color-controls matrix (brightness/contrast/saturation).
            // Note: Skia's color matrix operates on normalized (0..1) colors.
            float invSat = 1f - saturation;
            float r = 0.213f * invSat;
            float g = 0.715f * invSat;
            float b = 0.072f * invSat;

            float c = contrast;
            // Translation terms must also be normalized.
            float t = 0.5f - c * 0.5f + brightness;
            float s = saturation;

            float cr = c * r;
            float cg = c * g;
            float cb = c * b;
            float cs = c * s;

            return new[]
            {
                cr + cs, cg, cb, 0f, t, cr, cg + cs, cb, 0f, t, cr, cg, cb + cs, 0f, t, 0f, 0f, 0f, 1f, 0f
            };
        }

        private static float[] CreateExposureColorMatrix(float ev)
        {
            float scale = (float)Math.Pow(2.0, ev / 2.2);
            return new[]
            {
                scale, 0f, 0f, 0f, 0f, 0f, scale, 0f, 0f, 0f, 0f, 0f, scale, 0f, 0f, 0f, 0f, 0f, 1f, 0f
            };
        }

        private static float[] CreateOpacityColorMatrix(float alpha)
        {
            return new[]
            {
                1f, 0f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 0f, alpha, 0f
            };
        }

        private void RenderInteractiveHighlight(SKCanvas canvas)
        {
            if (s_interactiveHighlightEffect is null)
                return;

            float progress = (float)Clamp(_parameters.InteractiveProgress, 0.0, 1.0);
            if (progress <= 0.001f)
                return;

            SKSize size = new((float)_bounds.Width, (float)_bounds.Height);
            if (size.Width <= 0 || size.Height <= 0)
                return;

            float maxRadius = Math.Min(size.Width, size.Height) * 0.5f;
            float[] cornerRadii = GetCornerRadii(_parameters.CornerRadius, maxRadius);

            SKRect rect = SKRect.Create(0, 0, size.Width, size.Height);
            using SKPath clipPath = CreateRoundRectPath(rect, cornerRadii);

            canvas.Save();
            canvas.ClipPath(clipPath, SKClipOperation.Intersect, true);

            using (SKPaint basePaint = new()
                {
                    Color = new SKColor(255, 255, 255, (byte)Clamp(0.08f * progress * 255f, 0f, 255f)),
                    BlendMode = SKBlendMode.Plus,
                    IsAntialias = true
                })
            {
                canvas.DrawRect(rect, basePaint);
            }

            using SKRuntimeEffectUniforms uniforms = new(s_interactiveHighlightEffect);
            uniforms["size"] = new[]
            {
                size.Width, size.Height
            };
            uniforms["color"] = new[]
            {
                1.0f, 1.0f, 1.0f, (float)Clamp(0.15 * progress, 0.0, 1.0)
            };
            uniforms["radius"] = Math.Min(size.Width, size.Height) * 1.5f;
            uniforms["position"] = new[]
            {
                (float)Clamp(_parameters.InteractivePosition.X, 0.0, size.Width), (float)Clamp(_parameters.InteractivePosition.Y, 0.0, size.Height)
            };

            using SKRuntimeEffectChildren children = new(s_interactiveHighlightEffect);
            using SKShader? shader = s_interactiveHighlightEffect.ToShader(uniforms, children);

            if (shader is not null)
            {
                using SKPaint paint = new()
                {
                    Shader = shader,
                    BlendMode = SKBlendMode.Plus,
                    IsAntialias = true
                };
                canvas.DrawRect(rect, paint);
            }

            canvas.Restore();
        }

        private void RenderHighlight(SKCanvas canvas)
        {
            if (s_highlightEffect is null)
                return;

            if (_parameters.HighlightOpacity <= 0.001 || _parameters.HighlightWidth <= 0.001)
                return;

            SKSize size = new((float)_bounds.Width, (float)_bounds.Height);
            if (size.Width <= 0 || size.Height <= 0)
                return;

            float maxRadius = Math.Min(size.Width, size.Height) * 0.5f;
            float[] cornerRadii = GetCornerRadii(_parameters.CornerRadius, maxRadius);

            using SKRuntimeEffectUniforms uniforms = new(s_highlightEffect);
            uniforms["size"] = new[]
            {
                size.Width, size.Height
            };
            uniforms["cornerRadii"] = cornerRadii;

            float alpha = (float)Clamp(_parameters.HighlightOpacity, 0.0, 1.0);
            uniforms["color"] = new[]
            {
                1.0f, 1.0f, 1.0f, alpha
            };

            float angleRad = (float)(_parameters.HighlightAngleDegrees * (Math.PI / 180.0));
            uniforms["angle"] = angleRad;
            uniforms["falloff"] = (float)Clamp(_parameters.HighlightFalloff, 0.0, 8.0);

            using SKRuntimeEffectChildren children = new(s_highlightEffect);
            using SKShader? shader = s_highlightEffect.ToShader(uniforms, children);
            if (shader is null)
                return;

            float blurRadius = (float)Clamp(_parameters.HighlightBlurRadius, 0.0, 20.0);
            using SKMaskFilter? maskFilter = blurRadius > 0.001f
                ? SKMaskFilter.CreateBlur(SKBlurStyle.Normal, blurRadius)
                : null;

            float strokeWidth = (float)(Math.Ceiling(Clamp(_parameters.HighlightWidth, 0.0, 100.0)) * 2.0);

            using SKPaint paint = new()
            {
                Shader = shader,
                IsAntialias = true,
                BlendMode = SKBlendMode.Plus,
                Style = SKPaintStyle.Stroke,
                StrokeJoin = SKStrokeJoin.Round,
                StrokeCap = SKStrokeCap.Round,
                StrokeWidth = Math.Max(0.5f, strokeWidth),
                MaskFilter = maskFilter
            };

            SKRect rect = SKRect.Create(0, 0, size.Width, size.Height);
            using SKPath path = CreateRoundRectPath(rect, cornerRadii);

            // Pad the highlight layer to avoid edge artifacts when transformed and/or rasterized into an intermediate surface.
            const float safePad = 1.0f;

            canvas.Save();
            canvas.Translate(-safePad, -safePad);
            SKRect layerBounds = SKRect.Create(0, 0, size.Width + safePad * 2.0f, size.Height + safePad * 2.0f);
            canvas.SaveLayer(layerBounds, null);

            canvas.Translate(safePad, safePad);
            canvas.ClipPath(path, SKClipOperation.Intersect, true);
            canvas.DrawPath(path, paint);

            canvas.Restore();
            canvas.Restore();
        }

        private void DrawSurfaceOverlay(SKCanvas canvas, SKRect rect)
        {
            // Optional tint/surface overlays. If TintColor is specified, it draws it twice: Hue blend + alpha fill.
            if (_parameters.TintColor.A > 0)
            {
                Color tint = _parameters.TintColor;
                using SKPaint huePaint = new()
                {
                    Color = new SKColor(tint.R, tint.G, tint.B, 255),
                    IsAntialias = true,
                    BlendMode = SKBlendMode.Hue
                };
                canvas.DrawRect(rect, huePaint);

                using SKPaint fillPaint = new()
                {
                    Color = new SKColor(tint.R, tint.G, tint.B, (byte)Clamp(tint.A * 0.75, 0.0, 255.0)),
                    IsAntialias = true,
                    BlendMode = SKBlendMode.SrcOver
                };
                canvas.DrawRect(rect, fillPaint);
            }

            if (_parameters.SurfaceColor.A > 0)
            {
                Color surface = _parameters.SurfaceColor;
                using SKPaint paint = new()
                {
                    Color = new SKColor(surface.R, surface.G, surface.B, surface.A),
                    IsAntialias = true,
                    BlendMode = SKBlendMode.SrcOver
                };
                canvas.DrawRect(rect, paint);
            }
        }

        private static float[] GetCornerRadii(CornerRadius cornerRadius, float maxRadius)
        {
            float tl = (float)Clamp(cornerRadius.TopLeft, 0.0, maxRadius);
            float tr = (float)Clamp(cornerRadius.TopRight, 0.0, maxRadius);
            float br = (float)Clamp(cornerRadius.BottomRight, 0.0, maxRadius);
            float bl = (float)Clamp(cornerRadius.BottomLeft, 0.0, maxRadius);
            return new[]
            {
                tl, tr, br, bl
            };
        }

        private static SKPath CreateRoundRectPath(SKRect rect, float[] cornerRadii)
        {
            using SKRoundRect rr = new();
            rr.SetRectRadii(rect, new[]
            {
                new SKPoint(cornerRadii[0], cornerRadii[0]), new SKPoint(cornerRadii[1], cornerRadii[1]), new SKPoint(cornerRadii[2], cornerRadii[2]), new SKPoint(cornerRadii[3], cornerRadii[3])
            });

            SKPath path = new();
            path.AddRoundRect(rr, SKPathDirection.Clockwise);
            return path;
        }

        private void DrawErrorHint(SKCanvas canvas)
        {
            using SKPaint errorPaint = new()
            {
                Color = new SKColor(255, 0, 0, 120),
                Style = SKPaintStyle.Fill
            };

            canvas.DrawRect(SKRect.Create(0, 0, (float)_bounds.Width, (float)_bounds.Height), errorPaint);
        }

        private void DrawBackdropNotReady(SKCanvas canvas)
        {
            SKSize size = new((float)_bounds.Width, (float)_bounds.Height);
            SKRect rect = SKRect.Create(0, 0, size.Width, size.Height);

            float maxRadius = Math.Min(size.Width, size.Height) * 0.5f;
            float[] cornerRadii = GetCornerRadii(_parameters.CornerRadius, maxRadius);
            using SKPath path = CreateRoundRectPath(rect, cornerRadii);

            using SKPaint paint = new()
            {
                Color = new SKColor(255, 255, 255, 32),
                IsAntialias = true
            };

            canvas.Save();
            canvas.ClipPath(path, SKClipOperation.Intersect, true);
            canvas.DrawRect(rect, paint);
            canvas.Restore();
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static SKMatrix WithPixelOrigin(SKMatrix invertedTransform, PixelPoint originInPixels)
        {
            // localMatrix = invertedTransform * Translate(+originInPixels)
            //
            // We first cancel the current canvas transform (so shader coordinates become device pixels),
            // then shift into the clipped snapshot's coordinate system.
            float ox = (float)originInPixels.X;
            float oy = (float)originInPixels.Y;
            invertedTransform.TransX = invertedTransform.TransX + invertedTransform.ScaleX * ox + invertedTransform.SkewX * oy;
            invertedTransform.TransY = invertedTransform.TransY + invertedTransform.SkewY * ox + invertedTransform.ScaleY * oy;
            return invertedTransform;
        }
    }
}
