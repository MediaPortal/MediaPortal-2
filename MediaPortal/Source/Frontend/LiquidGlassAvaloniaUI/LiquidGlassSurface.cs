using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SkiaSharp;

namespace LiquidGlassAvaloniaUI
{
    /// <summary>
    /// A composited “liquid glass” surface:
    /// vibrancy + blur + lens refraction + (optional) highlights + shadows.
    /// </summary>
    public class LiquidGlassSurface : ContentControl
    {
        public static readonly StyledProperty<double> BackdropZoomProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, double>(nameof(BackdropZoom), 1.0);

        public static readonly StyledProperty<Vector> BackdropOffsetProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, Vector>(nameof(BackdropOffset), new Vector(0.0, 0.0));

        public static readonly StyledProperty<double> RefractionHeightProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, double>(nameof(RefractionHeight), 12.0);

        public static readonly StyledProperty<double> RefractionAmountProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, double>(nameof(RefractionAmount), 24.0);

        public static readonly StyledProperty<bool> DepthEffectProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, bool>(nameof(DepthEffect), false);

        public static readonly StyledProperty<bool> ChromaticAberrationProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, bool>(nameof(ChromaticAberration), false);

        public static readonly StyledProperty<double> BlurRadiusProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, double>(nameof(BlurRadius), 2.0);

        public static readonly StyledProperty<double> VibrancyProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, double>(nameof(Vibrancy), 1.5);

        public static readonly StyledProperty<double> BrightnessProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, double>(nameof(Brightness), 0.0);

        public static readonly StyledProperty<double> ContrastProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, double>(nameof(Contrast), 1.0);

        public static readonly StyledProperty<double> ExposureEvProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, double>(nameof(ExposureEv), 0.0);

        public static readonly StyledProperty<double> GammaPowerProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, double>(nameof(GammaPower), 1.0);

        public static readonly StyledProperty<double> BackdropOpacityProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, double>(nameof(BackdropOpacity), 1.0);

        public static readonly StyledProperty<Color> TintColorProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, Color>(nameof(TintColor), Colors.Transparent);

        public static readonly StyledProperty<Color> SurfaceColorProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, Color>(nameof(SurfaceColor), Colors.Transparent);

        public static readonly StyledProperty<bool> ProgressiveBlurEnabledProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, bool>(nameof(ProgressiveBlurEnabled), false);

        /// <summary>
        /// Progressive blur start position (0..1, relative to control height).
        /// Above this fraction the blur is fully visible.
        /// </summary>
        public static readonly StyledProperty<double> ProgressiveBlurStartProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, double>(nameof(ProgressiveBlurStart), 0.5);

        /// <summary>
        /// Progressive blur end position (0..1, relative to control height).
        /// Below this fraction the blur fades out to fully transparent.
        /// </summary>
        public static readonly StyledProperty<double> ProgressiveBlurEndProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, double>(nameof(ProgressiveBlurEnd), 1.0);

        public static readonly StyledProperty<Color> ProgressiveTintColorProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, Color>(nameof(ProgressiveTintColor), Colors.Transparent);

        public static readonly StyledProperty<double> ProgressiveTintIntensityProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, double>(nameof(ProgressiveTintIntensity), 0.8);

        public static readonly StyledProperty<bool> AdaptiveLuminanceEnabledProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, bool>(nameof(AdaptiveLuminanceEnabled), false);

        public static readonly StyledProperty<double> AdaptiveLuminanceUpdateIntervalMsProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, double>(nameof(AdaptiveLuminanceUpdateIntervalMs), 250.0);

        public static readonly StyledProperty<double> AdaptiveLuminanceSmoothingProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, double>(nameof(AdaptiveLuminanceSmoothing), 0.2);

        public static readonly DirectProperty<LiquidGlassSurface, double> AdaptiveLuminanceProperty =
            AvaloniaProperty.RegisterDirect<LiquidGlassSurface, double>(
                nameof(AdaptiveLuminance),
                o => o.AdaptiveLuminance);

        public static readonly StyledProperty<bool> HighlightEnabledProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, bool>(nameof(HighlightEnabled), true);

        public static readonly StyledProperty<double> HighlightWidthProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, double>(nameof(HighlightWidth), 0.5);

        public static readonly StyledProperty<double> HighlightBlurRadiusProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, double>(nameof(HighlightBlurRadius), 0.25);

        public static readonly StyledProperty<double> HighlightOpacityProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, double>(nameof(HighlightOpacity), 0.5);

        public static readonly StyledProperty<double> HighlightAngleProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, double>(nameof(HighlightAngle), 45.0);

        public static readonly StyledProperty<double> HighlightFalloffProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, double>(nameof(HighlightFalloff), 1.0);

        public static readonly StyledProperty<bool> ShadowEnabledProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, bool>(nameof(ShadowEnabled), true);

        public static readonly StyledProperty<double> ShadowRadiusProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, double>(nameof(ShadowRadius), 24.0);

        public static readonly StyledProperty<Vector> ShadowOffsetProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, Vector>(nameof(ShadowOffset), new Vector(0.0, 4.0));

        public static readonly StyledProperty<Color> ShadowColorProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, Color>(nameof(ShadowColor), Color.FromArgb(26, 0, 0, 0));

        public static readonly StyledProperty<double> ShadowOpacityProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, double>(nameof(ShadowOpacity), 1.0);

        public static readonly StyledProperty<bool> InnerShadowEnabledProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, bool>(nameof(InnerShadowEnabled), false);

        public static readonly StyledProperty<double> InnerShadowRadiusProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, double>(nameof(InnerShadowRadius), 24.0);

        public static readonly StyledProperty<Vector> InnerShadowOffsetProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, Vector>(nameof(InnerShadowOffset), new Vector(0.0, 24.0));

        public static readonly StyledProperty<Color> InnerShadowColorProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, Color>(nameof(InnerShadowColor), Color.FromArgb(38, 0, 0, 0));

        public static readonly StyledProperty<double> InnerShadowOpacityProperty =
            AvaloniaProperty.Register<LiquidGlassSurface, double>(nameof(InnerShadowOpacity), 1.0);

        static LiquidGlassSurface()
        {
            // TemplatedControl defaults ClipToBounds=true, which would clip the shadow pass to the control bounds.
            // We clip the content via the template's Border and clip shader passes explicitly.
            ClipToBoundsProperty.OverrideDefaultValue<LiquidGlassSurface>(false);

            AffectsRender<LiquidGlassSurface>(
                CornerRadiusProperty,
                BackdropZoomProperty,
                BackdropOffsetProperty,
                RefractionHeightProperty,
                RefractionAmountProperty,
                DepthEffectProperty,
                ChromaticAberrationProperty,
                BlurRadiusProperty,
                VibrancyProperty,
                BrightnessProperty,
                ContrastProperty,
                ExposureEvProperty,
                GammaPowerProperty,
                BackdropOpacityProperty,
                TintColorProperty,
                SurfaceColorProperty,
                ProgressiveBlurEnabledProperty,
                ProgressiveBlurStartProperty,
                ProgressiveBlurEndProperty,
                ProgressiveTintColorProperty,
                ProgressiveTintIntensityProperty,
                AdaptiveLuminanceEnabledProperty,
                AdaptiveLuminanceUpdateIntervalMsProperty,
                AdaptiveLuminanceSmoothingProperty,
                ShadowEnabledProperty,
                ShadowRadiusProperty,
                ShadowOffsetProperty,
                ShadowColorProperty,
                ShadowOpacityProperty);

            TemplateProperty.OverrideDefaultValue<LiquidGlassSurface>(CreateDefaultTemplate());
        }

        internal LiquidGlassInteractiveOverlay? InteractiveOverlay { get; private set; }
        internal LiquidGlassFrontOverlay? FrontOverlay { get; private set; }

        private DispatcherTimer? _adaptiveLuminanceTimer;
        private EventHandler? _adaptiveLuminanceTickHandler;
        private double _adaptiveLuminance;

        /// <summary>
        /// Last sampled backdrop luminance (0..1) when <see cref="AdaptiveLuminanceEnabled"/> is true.
        /// </summary>
        public double AdaptiveLuminance
        {
            get => _adaptiveLuminance;
            private set => SetAndRaise(AdaptiveLuminanceProperty, ref _adaptiveLuminance, value);
        }

        public Control? Child
        {
            get => Content as Control;
            set => Content = value;
        }

        /// <summary>
        /// Magnifier-style zoom applied to backdrop sampling (1 = no zoom).
        /// </summary>
        public double BackdropZoom
        {
            get => GetValue(BackdropZoomProperty);
            set => SetValue(BackdropZoomProperty, value);
        }

        /// <summary>
        /// Additional translation applied to the sampled backdrop (in DIPs).
        /// </summary>
        public Vector BackdropOffset
        {
            get => GetValue(BackdropOffsetProperty);
            set => SetValue(BackdropOffsetProperty, value);
        }

        public double RefractionHeight
        {
            get => GetValue(RefractionHeightProperty);
            set => SetValue(RefractionHeightProperty, value);
        }

        public double RefractionAmount
        {
            get => GetValue(RefractionAmountProperty);
            set => SetValue(RefractionAmountProperty, value);
        }

        public bool DepthEffect
        {
            get => GetValue(DepthEffectProperty);
            set => SetValue(DepthEffectProperty, value);
        }

        public bool ChromaticAberration
        {
            get => GetValue(ChromaticAberrationProperty);
            set => SetValue(ChromaticAberrationProperty, value);
        }

        public double BlurRadius
        {
            get => GetValue(BlurRadiusProperty);
            set => SetValue(BlurRadiusProperty, value);
        }

        public double Vibrancy
        {
            get => GetValue(VibrancyProperty);
            set => SetValue(VibrancyProperty, value);
        }

        public double Saturation
        {
            get => Vibrancy;
            set => Vibrancy = value;
        }

        public double Brightness
        {
            get => GetValue(BrightnessProperty);
            set => SetValue(BrightnessProperty, value);
        }

        public double Contrast
        {
            get => GetValue(ContrastProperty);
            set => SetValue(ContrastProperty, value);
        }

        public double ExposureEv
        {
            get => GetValue(ExposureEvProperty);
            set => SetValue(ExposureEvProperty, value);
        }

        public double GammaPower
        {
            get => GetValue(GammaPowerProperty);
            set => SetValue(GammaPowerProperty, value);
        }

        public double BackdropOpacity
        {
            get => GetValue(BackdropOpacityProperty);
            set => SetValue(BackdropOpacityProperty, value);
        }

        public Color TintColor
        {
            get => GetValue(TintColorProperty);
            set => SetValue(TintColorProperty, value);
        }

        public Color SurfaceColor
        {
            get => GetValue(SurfaceColorProperty);
            set => SetValue(SurfaceColorProperty, value);
        }

        /// <summary>
        /// Enables the progressive blur mask stage.
        /// </summary>
        public bool ProgressiveBlurEnabled
        {
            get => GetValue(ProgressiveBlurEnabledProperty);
            set => SetValue(ProgressiveBlurEnabledProperty, value);
        }

        public double ProgressiveBlurStart
        {
            get => GetValue(ProgressiveBlurStartProperty);
            set => SetValue(ProgressiveBlurStartProperty, value);
        }

        public double ProgressiveBlurEnd
        {
            get => GetValue(ProgressiveBlurEndProperty);
            set => SetValue(ProgressiveBlurEndProperty, value);
        }

        public Color ProgressiveTintColor
        {
            get => GetValue(ProgressiveTintColorProperty);
            set => SetValue(ProgressiveTintColorProperty, value);
        }

        public double ProgressiveTintIntensity
        {
            get => GetValue(ProgressiveTintIntensityProperty);
            set => SetValue(ProgressiveTintIntensityProperty, value);
        }

        /// <summary>
        /// Enables adaptive luminance mode which samples the backdrop behind this surface and adjusts
        /// rendering parameters to improve legibility.
        /// </summary>
        public bool AdaptiveLuminanceEnabled
        {
            get => GetValue(AdaptiveLuminanceEnabledProperty);
            set => SetValue(AdaptiveLuminanceEnabledProperty, value);
        }

        public double AdaptiveLuminanceUpdateIntervalMs
        {
            get => GetValue(AdaptiveLuminanceUpdateIntervalMsProperty);
            set => SetValue(AdaptiveLuminanceUpdateIntervalMsProperty, value);
        }

        public double AdaptiveLuminanceSmoothing
        {
            get => GetValue(AdaptiveLuminanceSmoothingProperty);
            set => SetValue(AdaptiveLuminanceSmoothingProperty, value);
        }

        public bool HighlightEnabled
        {
            get => GetValue(HighlightEnabledProperty);
            set => SetValue(HighlightEnabledProperty, value);
        }

        public double HighlightWidth
        {
            get => GetValue(HighlightWidthProperty);
            set => SetValue(HighlightWidthProperty, value);
        }

        public double HighlightBlurRadius
        {
            get => GetValue(HighlightBlurRadiusProperty);
            set => SetValue(HighlightBlurRadiusProperty, value);
        }

        public double HighlightOpacity
        {
            get => GetValue(HighlightOpacityProperty);
            set => SetValue(HighlightOpacityProperty, value);
        }

        public double HighlightAngle
        {
            get => GetValue(HighlightAngleProperty);
            set => SetValue(HighlightAngleProperty, value);
        }

        public double HighlightFalloff
        {
            get => GetValue(HighlightFalloffProperty);
            set => SetValue(HighlightFalloffProperty, value);
        }

        public bool ShadowEnabled
        {
            get => GetValue(ShadowEnabledProperty);
            set => SetValue(ShadowEnabledProperty, value);
        }

        public double ShadowRadius
        {
            get => GetValue(ShadowRadiusProperty);
            set => SetValue(ShadowRadiusProperty, value);
        }

        public Vector ShadowOffset
        {
            get => GetValue(ShadowOffsetProperty);
            set => SetValue(ShadowOffsetProperty, value);
        }

        public Color ShadowColor
        {
            get => GetValue(ShadowColorProperty);
            set => SetValue(ShadowColorProperty, value);
        }

        public double ShadowOpacity
        {
            get => GetValue(ShadowOpacityProperty);
            set => SetValue(ShadowOpacityProperty, value);
        }

        public bool InnerShadowEnabled
        {
            get => GetValue(InnerShadowEnabledProperty);
            set => SetValue(InnerShadowEnabledProperty, value);
        }

        public double InnerShadowRadius
        {
            get => GetValue(InnerShadowRadiusProperty);
            set => SetValue(InnerShadowRadiusProperty, value);
        }

        public Vector InnerShadowOffset
        {
            get => GetValue(InnerShadowOffsetProperty);
            set => SetValue(InnerShadowOffsetProperty, value);
        }

        public Color InnerShadowColor
        {
            get => GetValue(InnerShadowColorProperty);
            set => SetValue(InnerShadowColorProperty, value);
        }

        public double InnerShadowOpacity
        {
            get => GetValue(InnerShadowOpacityProperty);
            set => SetValue(InnerShadowOpacityProperty, value);
        }

        public override void Render(DrawingContext context)
        {
            if (LiquidGlassBackdropProvider.IsCapturing)
                return;

            if (Bounds.Width <= 0 || Bounds.Height <= 0)
                return;

            LiquidGlassDrawParameters parameters = CreateDrawParameters();
            Rect controlBounds = new(0, 0, Bounds.Width, Bounds.Height);

            if (ShadowEnabled && ShadowOpacity > 0.001 && ShadowColor.A > 0 && ShadowRadius > 0.001)
                context.Custom(new LiquidGlassShadowDrawOperation(controlBounds, parameters));

            LiquidGlassBackdropProvider.EnsureSnapshot(this);
            LiquidGlassBackdropSnapshot? snapshot = LiquidGlassBackdropProvider.TryGetSnapshot(this);

            context.Custom(new LiquidGlassDrawOperation(controlBounds, parameters, snapshot, LiquidGlassDrawPass.Lens));
        }

        internal LiquidGlassDrawParameters CreateDrawParameters()
        {
            LiquidGlassDrawParameters parameters = new()
            {
                CornerRadius = CornerRadius,
                BackdropZoom = BackdropZoom,
                BackdropOffset = BackdropOffset,
                RefractionHeight = RefractionHeight,
                RefractionAmount = RefractionAmount,
                DepthEffect = DepthEffect,
                ChromaticAberration = ChromaticAberration,
                BlurRadius = BlurRadius,
                Vibrancy = Vibrancy,
                Brightness = Brightness,
                Contrast = Contrast,
                ExposureEv = ExposureEv,
                GammaPower = GammaPower,
                BackdropOpacity = BackdropOpacity,
                TintColor = TintColor,
                SurfaceColor = SurfaceColor,
                ProgressiveBlurEnabled = ProgressiveBlurEnabled,
                ProgressiveBlurStart = ProgressiveBlurStart,
                ProgressiveBlurEnd = ProgressiveBlurEnd,
                ProgressiveTintColor = ProgressiveTintColor,
                ProgressiveTintIntensity = ProgressiveTintIntensity,
                HighlightEnabled = HighlightEnabled,
                HighlightWidth = HighlightWidth,
                HighlightBlurRadius = HighlightBlurRadius,
                HighlightOpacity = HighlightOpacity,
                HighlightAngleDegrees = HighlightAngle,
                HighlightFalloff = HighlightFalloff,
                ShadowEnabled = ShadowEnabled,
                ShadowRadius = ShadowRadius,
                ShadowOffset = ShadowOffset,
                ShadowColor = ShadowColor,
                ShadowOpacity = ShadowOpacity,
                InnerShadowEnabled = InnerShadowEnabled,
                InnerShadowRadius = InnerShadowRadius,
                InnerShadowOffset = InnerShadowOffset,
                InnerShadowColor = InnerShadowColor,
                InnerShadowOpacity = InnerShadowOpacity
            };

            if (AdaptiveLuminanceEnabled)
            {
                // Tone-mapping heuristic based on sampled backdrop luminance.
                double luminance = Clamp(AdaptiveLuminance, 0.0, 1.0);
                double l = luminance * 2.0 - 1.0;
                l = Math.Sign(l) * l * l;

                parameters.Vibrancy = 1.5;

                parameters.Brightness =
                    l > 0.0
                        ? Lerp(0.1, 0.5, l)
                        : Lerp(0.1, -0.2, -l);

                parameters.Contrast =
                    l > 0.0
                        ? Lerp(1.0, 0.0, l)
                        : 1.0;

                parameters.BlurRadius =
                    l > 0.0
                        ? Lerp(8.0, 16.0, l)
                        : Lerp(8.0, 2.0, -l);
            }

            return parameters;
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            InteractiveOverlay = e.NameScope.Find<LiquidGlassInteractiveOverlay>("PART_InteractiveOverlay");
            FrontOverlay = e.NameScope.Find<LiquidGlassFrontOverlay>("PART_FrontOverlay");
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (IsSubscriberOnlyRenderProperty(change.Property))
                LiquidGlassBackdropProvider.NotifySubscriberOnlyInvalidation(this);

            if (change.Property == CornerRadiusProperty)
            {
                InteractiveOverlay?.InvalidateVisual();
                FrontOverlay?.InvalidateVisual();
                return;
            }

            if (change.Property == HighlightEnabledProperty
                || change.Property == HighlightWidthProperty
                || change.Property == HighlightBlurRadiusProperty
                || change.Property == HighlightOpacityProperty
                || change.Property == HighlightAngleProperty
                || change.Property == HighlightFalloffProperty
                || change.Property == InnerShadowEnabledProperty
                || change.Property == InnerShadowRadiusProperty
                || change.Property == InnerShadowOffsetProperty
                || change.Property == InnerShadowColorProperty
                || change.Property == InnerShadowOpacityProperty)
            {
                FrontOverlay?.InvalidateVisual();
                return;
            }

            if (change.Property == AdaptiveLuminanceEnabledProperty
                || change.Property == AdaptiveLuminanceUpdateIntervalMsProperty)
            {
                UpdateAdaptiveLuminanceTimer();
            }
        }

        private static bool IsSubscriberOnlyRenderProperty(AvaloniaProperty property)
        {
            return property == CornerRadiusProperty
                   || property == BackdropZoomProperty
                   || property == BackdropOffsetProperty
                   || property == RefractionHeightProperty
                   || property == RefractionAmountProperty
                   || property == DepthEffectProperty
                   || property == ChromaticAberrationProperty
                   || property == BlurRadiusProperty
                   || property == VibrancyProperty
                   || property == BrightnessProperty
                   || property == ContrastProperty
                   || property == ExposureEvProperty
                   || property == GammaPowerProperty
                   || property == BackdropOpacityProperty
                   || property == TintColorProperty
                   || property == SurfaceColorProperty
                   || property == ProgressiveBlurEnabledProperty
                   || property == ProgressiveBlurStartProperty
                   || property == ProgressiveBlurEndProperty
                   || property == ProgressiveTintColorProperty
                   || property == ProgressiveTintIntensityProperty
                   || property == AdaptiveLuminanceEnabledProperty
                   || property == AdaptiveLuminanceUpdateIntervalMsProperty
                   || property == AdaptiveLuminanceSmoothingProperty
                   || property == HighlightEnabledProperty
                   || property == HighlightWidthProperty
                   || property == HighlightBlurRadiusProperty
                   || property == HighlightOpacityProperty
                   || property == HighlightAngleProperty
                   || property == HighlightFalloffProperty
                   || property == ShadowEnabledProperty
                   || property == ShadowRadiusProperty
                   || property == ShadowOffsetProperty
                   || property == ShadowColorProperty
                   || property == ShadowOpacityProperty
                   || property == InnerShadowEnabledProperty
                   || property == InnerShadowRadiusProperty
                   || property == InnerShadowOffsetProperty
                   || property == InnerShadowColorProperty
                   || property == InnerShadowOpacityProperty;
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            UpdateAdaptiveLuminanceTimer();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            StopAdaptiveLuminanceTimer();
            base.OnDetachedFromVisualTree(e);
        }

        private void UpdateAdaptiveLuminanceTimer()
        {
            if (!this.IsAttachedToVisualTree())
            {
                StopAdaptiveLuminanceTimer();
                return;
            }

            if (AdaptiveLuminanceEnabled)
                StartAdaptiveLuminanceTimer();
            else
                StopAdaptiveLuminanceTimer();
        }

        private void StartAdaptiveLuminanceTimer()
        {
            if (_adaptiveLuminanceTimer is not null)
            {
                TimeSpan desired = TimeSpan.FromMilliseconds(Math.Max(16.0, AdaptiveLuminanceUpdateIntervalMs));
                if (_adaptiveLuminanceTimer.Interval != desired)
                    _adaptiveLuminanceTimer.Interval = desired;
                if (!_adaptiveLuminanceTimer.IsEnabled)
                    _adaptiveLuminanceTimer.Start();
                return;
            }

            _adaptiveLuminanceTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(Math.Max(16.0, AdaptiveLuminanceUpdateIntervalMs))
            };

            _adaptiveLuminanceTickHandler ??= (_, _) => TickAdaptiveLuminance();
            _adaptiveLuminanceTimer.Tick += _adaptiveLuminanceTickHandler;
            _adaptiveLuminanceTimer.Start();
        }

        private void StopAdaptiveLuminanceTimer()
        {
            if (_adaptiveLuminanceTimer is null)
                return;

            _adaptiveLuminanceTimer.Stop();
            if (_adaptiveLuminanceTickHandler is not null)
                _adaptiveLuminanceTimer.Tick -= _adaptiveLuminanceTickHandler;
            _adaptiveLuminanceTimer = null;
        }

        private void TickAdaptiveLuminance()
        {
            if (!AdaptiveLuminanceEnabled)
                return;

            if (LiquidGlassBackdropProvider.IsCapturing)
                return;

            LiquidGlassBackdropProvider.EnsureSnapshot(this);

            if (!TrySampleBackdropLuminance(out double sampled))
                return;

            double smoothing = Clamp(AdaptiveLuminanceSmoothing, 0.0, 1.0);
            double next = AdaptiveLuminance + (sampled - AdaptiveLuminance) * smoothing;
            if (Math.Abs(next - AdaptiveLuminance) > 0.0005)
            {
                AdaptiveLuminance = next;
                LiquidGlassBackdropProvider.NotifySubscriberOnlyInvalidation(this);
                InvalidateVisual();
            }
        }

        private bool TrySampleBackdropLuminance(out double luminance)
        {
            luminance = 0.0;

            TopLevel? topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is null)
                return false;

            LiquidGlassBackdropSnapshot? snapshot = LiquidGlassBackdropProvider.TryGetSnapshot(this);
            if (snapshot is null || !snapshot.TryAddLease())
                return false;

            try
            {
                if (!TryGetBoundsInTopLevelPixels(topLevel, out PixelRect boundsPx))
                    return false;

                PixelRect rel = new(
                    new PixelPoint(boundsPx.X - snapshot.OriginInPixels.X, boundsPx.Y - snapshot.OriginInPixels.Y),
                    boundsPx.Size);

                int imgW = snapshot.Image.Width;
                int imgH = snapshot.Image.Height;

                int x0 = Clamp((int)rel.X, 0, imgW - 1);
                int y0 = Clamp((int)rel.Y, 0, imgH - 1);
                int x1 = Clamp((int)rel.Right, 0, imgW);
                int y1 = Clamp((int)rel.Bottom, 0, imgH);
                int w = x1 - x0;
                int h = y1 - y0;
                if (w <= 1 || h <= 1)
                    return false;

                using SKPixmap? pixmap = snapshot.Image.PeekPixels();
                using SKBitmap? fallbackBitmap = pixmap is null
                    ? new SKBitmap(new SKImageInfo(imgW, imgH, SKColorType.Bgra8888, SKAlphaType.Premul))
                    : null;

                if (pixmap is null
                    && !snapshot.Image.ReadPixels(fallbackBitmap!.Info, fallbackBitmap.GetPixels(), fallbackBitmap.RowBytes, 0, 0))
                {
                    return false;
                }

                const int samplesX = 5;
                const int samplesY = 5;
                double sum = 0.0;
                int count = 0;

                for (int sy = 0; sy < samplesY; sy++)
                {
                    int yy = y0 + (int)Math.Round((double)sy * (h - 1) / (samplesY - 1));
                    for (int sx = 0; sx < samplesX; sx++)
                    {
                        int xx = x0 + (int)Math.Round((double)sx * (w - 1) / (samplesX - 1));
                        SKColor c = pixmap is not null
                            ? pixmap.GetPixelColor(xx, yy)
                            : fallbackBitmap!.GetPixel(xx, yy);
                        double r = c.Red / 255.0;
                        double g = c.Green / 255.0;
                        double b = c.Blue / 255.0;
                        sum += 0.2126 * r + 0.7152 * g + 0.0722 * b;
                        count++;
                    }
                }

                if (count == 0)
                    return false;

                luminance = sum / count;
                return true;
            }
            finally
            {
                snapshot.ReleaseLease();
            }
        }

        private bool TryGetBoundsInTopLevelPixels(TopLevel topLevel, out PixelRect pixelRect)
        {
            pixelRect = default;

            Matrix? transform = this.TransformToVisual(topLevel);
            if (transform is null)
                return false;

            Rect bounds = new(0, 0, Bounds.Width, Bounds.Height);
            Point p0 = bounds.TopLeft.Transform(transform.Value);
            Point p1 = bounds.TopRight.Transform(transform.Value);
            Point p2 = bounds.BottomRight.Transform(transform.Value);
            Point p3 = bounds.BottomLeft.Transform(transform.Value);

            double left = Math.Min(Math.Min(p0.X, p1.X), Math.Min(p2.X, p3.X));
            double right = Math.Max(Math.Max(p0.X, p1.X), Math.Max(p2.X, p3.X));
            double top = Math.Min(Math.Min(p0.Y, p1.Y), Math.Min(p2.Y, p3.Y));
            double bottom = Math.Max(Math.Max(p0.Y, p1.Y), Math.Max(p2.Y, p3.Y));

            Rect aabb = new(left, top, Math.Max(0.0, right - left), Math.Max(0.0, bottom - top));
            if (aabb.Width <= 0 || aabb.Height <= 0)
                return false;

            double scaling = topLevel.RenderScaling;
            int pxLeft = (int)Math.Floor(aabb.X * scaling);
            int pxTop = (int)Math.Floor(aabb.Y * scaling);
            int pxRight = (int)Math.Ceiling(aabb.Right * scaling);
            int pxBottom = (int)Math.Ceiling(aabb.Bottom * scaling);

            if (pxRight <= pxLeft || pxBottom <= pxTop)
                return false;

            pixelRect = new PixelRect(new PixelPoint(pxLeft, pxTop), new PixelPoint(pxRight, pxBottom));
            return true;
        }

        private static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * Clamp(t, 0.0, 1.0);
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static FuncControlTemplate CreateDefaultTemplate()
        {
            return new FuncControlTemplate<LiquidGlassSurface>((_, ns) =>
            {
                LiquidGlassInteractiveOverlay interactiveOverlay = new LiquidGlassInteractiveOverlay
                {
                    Name = "PART_InteractiveOverlay",
                    IsHitTestVisible = false
                }.RegisterInNameScope(ns);

                ContentPresenter presenter = new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [~ContentPresenter.ContentProperty] = new TemplateBinding(ContentProperty),
                    [~ContentPresenter.ContentTemplateProperty] = new TemplateBinding(ContentTemplateProperty),
                    [~ContentPresenter.VerticalContentAlignmentProperty] = new TemplateBinding(VerticalContentAlignmentProperty),
                    [~ContentPresenter.HorizontalContentAlignmentProperty] = new TemplateBinding(HorizontalContentAlignmentProperty)
                }.RegisterInNameScope(ns);

                LiquidGlassFrontOverlay frontOverlay = new LiquidGlassFrontOverlay
                {
                    Name = "PART_FrontOverlay",
                    IsHitTestVisible = false
                }.RegisterInNameScope(ns);

                Grid grid = new();
                grid.Children.Add(interactiveOverlay);
                grid.Children.Add(presenter);
                grid.Children.Add(frontOverlay);

                return new Border
                {
                    ClipToBounds = true,
                    [~Border.CornerRadiusProperty] = new TemplateBinding(CornerRadiusProperty),
                    Child = grid
                };
            });
        }
    }
}
