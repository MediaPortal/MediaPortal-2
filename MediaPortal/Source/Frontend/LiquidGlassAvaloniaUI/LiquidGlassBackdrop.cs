using System;
using Avalonia;
using Avalonia.VisualTree;

namespace LiquidGlassAvaloniaUI
{
    public sealed class LiquidGlassBackdrop
    {
        private LiquidGlassBackdrop()
        {
        }

        public static readonly AttachedProperty<bool> IsExcludedFromCaptureProperty =
            AvaloniaProperty.RegisterAttached<LiquidGlassBackdrop, Visual, bool>(
                "IsExcludedFromCapture",
                false);

        public static bool GetIsExcludedFromCapture(Visual visual)
        {
            if (visual is null)
                throw new ArgumentNullException(nameof(visual));

            return visual.GetValue(IsExcludedFromCaptureProperty);
        }

        public static void SetIsExcludedFromCapture(Visual visual, bool value)
        {
            if (visual is null)
                throw new ArgumentNullException(nameof(visual));

            visual.SetValue(IsExcludedFromCaptureProperty, value);
        }
    }
}
