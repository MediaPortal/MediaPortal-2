using Avalonia;
using Avalonia.Rendering.Composition;

namespace LiquidGlassAvaloniaUI
{
    public static class LiquidGlassAppBuilderExtensions
    {
        public static AppBuilder UseLiquidGlassPerformanceDefaults(
            this AppBuilder builder,
            long skiaMaxGpuResourceSizeBytes = 256L * 1024L * 1024L,
            int maxDirtyRects = 8)
        {
            return builder
                .With(new CompositionOptions
                {
                    UseRegionDirtyRectClipping = true,
                    MaxDirtyRects = maxDirtyRects
                })
                .With(new SkiaOptions
                {
                    MaxGpuResourceSizeBytes = skiaMaxGpuResourceSizeBytes
                });
        }
    }
}
