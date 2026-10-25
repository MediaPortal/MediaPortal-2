using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace LiquidGlassAvaloniaUI
{
    internal sealed class LiquidGlassInteractiveOverlay : Control
    {
        public override void Render(DrawingContext context)
        {
            if (LiquidGlassBackdropProvider.IsCapturing)
                return;

            if (TemplatedParent is not LiquidGlassInteractiveSurface surface)
                return;

            if (!surface.InteractiveHighlightEnabled)
                return;

            double progress = surface.GetInteractiveHighlightProgress();
            if (progress <= 0.001)
                return;

            Rect bounds = new(0, 0, Bounds.Width, Bounds.Height);
            LiquidGlassDrawParameters parameters = surface.CreateDrawParameters();
            parameters.InteractiveProgress = progress;
            parameters.InteractivePosition = surface.GetInteractiveHighlightPosition();

            context.Custom(new LiquidGlassDrawOperation(bounds, parameters, null, LiquidGlassDrawPass.InteractiveHighlight));
        }
    }

    internal sealed class LiquidGlassFrontOverlay : Control
    {
        public override void Render(DrawingContext context)
        {
            if (LiquidGlassBackdropProvider.IsCapturing)
                return;

            if (TemplatedParent is not LiquidGlassSurface surface)
                return;

            Rect bounds = new(0, 0, Bounds.Width, Bounds.Height);
            LiquidGlassDrawParameters parameters = surface.CreateDrawParameters();

            if (surface.HighlightEnabled)
                context.Custom(new LiquidGlassDrawOperation(bounds, parameters, null, LiquidGlassDrawPass.Highlight));

            if (surface.InnerShadowEnabled && parameters.InnerShadowOpacity > 0.001 && parameters.InnerShadowColor.A > 0)
                context.Custom(new LiquidGlassInnerShadowDrawOperation(bounds, parameters));
        }
    }
}
