using System;
using Avalonia;
using Avalonia.Media;
using SkiaSharp;

namespace LiquidGlassAvaloniaUI
{
    internal static class LiquidGlassPathUtils
    {
        public static float[] GetCornerRadii(CornerRadius cornerRadius, float maxRadius)
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

        public static SKPath CreateRoundRectPath(SKRect rect, float[] cornerRadii)
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

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
