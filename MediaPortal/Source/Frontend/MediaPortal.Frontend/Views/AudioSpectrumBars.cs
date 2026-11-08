/*
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.
*/

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using MediaPortal.Frontend.Player;
using SkiaSharp;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The thirty-two bars that stand beside the record on the audio screen: one bar per slice of the audible
  /// range, each rising to the level its slice is playing at, each with a cap that the level lifts at once and
  /// that falls back slowly on its own.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Everything about its shape is the reference's, taken from the control the worn skin already had rather than
  /// invented beside it: the count, the spacing, the fall, the decibel scale and the quarter-of-a-second clock
  /// are that control's own constructor answers (AudioSpectrumAnalyzer.cs:119-136), which its screens never
  /// touched (screens/FullScreenContentAudio.xaml:274-276 hands it a size and a style and nothing else), the
  /// bars' uneven spread over the frequency range is its logarithmic bucketing (same file:411-425), the height
  /// of a bar out of a band's value is its decibel branch (same file:497-500 with the constants at :91-96), and
  /// the cap's fall is its <c>PeakFallDelay</c> formula (same file:536-540). The cap is half a bar wide for the
  /// same reason the reference makes it <c>barWidth / 2</c> (same file:485).
  /// </para>
  /// <para>
  /// <b>Where the numbers come from is not this class's business.</b> It asks <see cref="IAudioSpectrumSource"/>
  /// - the seam the reference put between its spectrum and its player - and an engine that does not answer is
  /// answered the way the reference answers a player that is not active: level nought, keep drawing, so the caps
  /// fall instead of standing where the last loud moment left them (AudioSpectrumAnalyzer.cs:490-493). No engine
  /// in this frontend answers yet, so on a television today these bars stand at their floor; the shape is built
  /// and proved, the listening is a separate thing to win.
  /// </para>
  /// <para>
  /// The bar is the reference's <c>SpectrumBarBrush</c>
  /// (Skins/Nereus/Skin/Nereus/themes/default/styles/AudioSpectrumAnalyzer.xaml:27-30). The cap is <b>flat white
  /// with five pixel corners</b>, which is neither that skin's <c>SpectrumPeakBrush</c> (same file:32-36, a
  /// gradient from its dark panel to its accent) nor the colour the mockup was measured with: over a fanart that
  /// drifts from green to grey, a white cap that is the same white as the record beside it reads more clearly
  /// than an accent colour looked up from whichever palette happens to be worn. The lookup went with the colour.
  /// </para>
  /// </remarks>
  public sealed class AudioSpectrumBars : Control
  {
    // The reference control's own constructor answers, kept as they are.
    private const int BarCount = 32;
    /// <summary>
    /// Room between two bars. The reference control had five (AudioSpectrumAnalyzer.cs:124); the viewer asked for
    /// ten on this screen, because at five the caps' glow could not reach anywhere without meeting its neighbour.
    /// </summary>
    private const double BarSpacing = 10d;
    private const int PeakFallDelay = 10;
    private const int MinimumFrequency = 20;
    private const int MaximumFrequency = 20000;
    private const int FrequencyBands = 2048;
    private const double MinDecibels = -90;
    private const double DecibelScale = 90;
    private const int RefreshMilliseconds = 25;

    public static readonly StyledProperty<IAudioSpectrumSource?> SourceProperty =
        AvaloniaProperty.Register<AudioSpectrumBars, IAudioSpectrumSource?>(nameof(Source));

    /// <summary>
    /// An engine that answers in bands rather than in magnitudes. Where both are offered this one is asked,
    /// because a band answer that arrived is a measurement while an empty magnitude answer is silence - see
    /// <see cref="IAudioBandSource"/> for why the two exist side by side.
    /// </summary>
    public static readonly StyledProperty<IAudioBandSource?> BandsProperty =
        AvaloniaProperty.Register<AudioSpectrumBars, IAudioBandSource?>(nameof(Bands));

    static AudioSpectrumBars()
    {
      AffectsRender<AudioSpectrumBars>(SourceProperty);

      // An engine that appears or disappears while the screen stands is the only thing that decides whether
      // there is anything to listen to, so the clock is answered here and not left running regardless.
      SourceProperty.Changed.AddClassHandler<AudioSpectrumBars>(
          delegate (AudioSpectrumBars bars, AvaloniaPropertyChangedEventArgs e)
          {
            bars.HearOrStandStill();
          });
      BandsProperty.Changed.AddClassHandler<AudioSpectrumBars>(
          delegate (AudioSpectrumBars bars, AvaloniaPropertyChangedEventArgs e)
          {
            bars.HearOrStandStill();
          });
    }

    public IAudioSpectrumSource? Source
    {
      get => GetValue(SourceProperty);
      set => SetValue(SourceProperty, value);
    }

    public IAudioBandSource? Bands
    {
      get => GetValue(BandsProperty);
      set => SetValue(BandsProperty, value);
    }

    /// <summary>The bands of the newest answer, held between the clock and the drawing.</summary>
    private readonly float[] _bands = new float[FrequencyBands];

    /// <summary>Where the newest band answer goes, resized when an engine answers with a different count.</summary>
    private float[]? _bandLevels;

    /// <summary>Every bar's height as a fraction of the room a bar has, filled by <see cref="Listen"/>.</summary>
    private readonly double[] _levels = new double[BarCount];

    /// <summary>The caps, in the same fractions, falling on their own.</summary>
    private readonly double[] _caps = new double[BarCount];

    /// <summary>
    /// Where each bar's slice of the band range ends, so that the low octaves get the fine end of the range and
    /// the high ones share the coarse end - the reference's logarithmic spread, kept as it computed it.
    /// </summary>
    private readonly int[] _bandEnd = new int[BarCount];

    /// <summary>The rate <see cref="_bandEnd"/> was worked out for, because the bands only mean hertz at one rate.</summary>
    private int _bandsFor;

    private DispatcherTimer? _clock;
    private readonly IBrush _barBrush;

    /// <summary>
    /// The white the caps wear. Flat and unmoving, because the picture behind these bars is a fanart that changes
    /// colour with the album and an accent looked up from the palette made the caps disappear into it.
    /// </summary>
    private static readonly IBrush CAP_BRUSH = new SolidColorBrush(Colors.White);

    /// <summary>
    /// How round the corners of a bar and of its cap are, in screen pixels. Kept under half the narrower of the
    /// piece's own width and height, so that a bar drawn narrow cannot turn its top into a lens.
    /// </summary>
    private const double CORNER_RADIUS = 5;

    /// <summary>
    /// How far a cap stands off the floor when nothing lifts it. The bars end at the floor and the caps used to
    /// rest on it, which made the two read as one piece at rest; the viewer asked for five lines more between a
    /// cap and the bar under it, and the room comes off the top of the field so that the strip's foot stays where
    /// the mockup put it.
    /// </summary>
    private const double REST_LIFT = 5;

    // A cap's light is a real gaussian blur, asked of Skia and not imitated with plates (see CapHalo). Three
    // passes make it: a tight bright one hugging the cap, a middle one, and a wide faint one around both, which
    // is how a lit edge reads. The numbers are what the viewer is asked to judge, so they stand here.
    private const float GLOW_SIGMA_TIGHT = 4.5f;

    private const float GLOW_SIGMA_MID = 10f;

    private const float GLOW_SIGMA_WIDE = 18f;

    private const byte GLOW_ALPHA_TIGHT = 230;

    private const byte GLOW_ALPHA_MID = 130;

    // Weakest of the three on purpose: thirty-two wide passes at the bar distance of this strip flood the rooms
    // between the bars into one sheet of light, and the caps stop reading as thirty-two separate things.
    private const byte GLOW_ALPHA_WIDE = 85;

    public AudioSpectrumBars()
    {
      _barBrush = new LinearGradientBrush
      {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
        GradientStops = new GradientStops
        {
            new GradientStop(Color.Parse("#FFB5B9C5"), 0),
            new GradientStop(Color.Parse("#00565C74"), 1)
        }
      };

      AttachedToVisualTree += delegate
      {
        _standing = true;
        HearOrStandStill();
      };
      DetachedFromVisualTree += delegate
      {
        // The clock belongs to the visit, not to the song: a screen nobody is looking at has no business reading
        // the engine forty times a second.
        _standing = false;
        _clock?.Stop();
        _clock = null;
      };
    }

    /// <summary>Whether this control is standing in a visual tree right now.</summary>
    private bool _standing;

    /// <summary>
    /// Runs the clock while there is an engine to ask and stands still while there is none. A screen whose bars
    /// have nothing to read would otherwise repaint a strip that never moves forty times a second for as long as
    /// it is open, and an engine that arrives later - which is the ordinary answer today, since the application
    /// hands the source over once it knows the player - starts the clock by itself.
    /// </summary>
    private void HearOrStandStill()
    {
      if (!_standing || (Source == null && Bands == null))
      {
        _clock?.Stop();
        InvalidateVisual();
        return;
      }

      StartListening();
    }

    private void StartListening()
    {
      if (_clock != null)
        return;

      // A quarter of a second, which is what the reference's control refreshes at and what the mockup was
      // measured with; slower and the caps stop looking like they are catching up with the sound.
      _clock = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(RefreshMilliseconds) };
      _clock.Tick += delegate { Listen(); };
      _clock.Start();
    }

    /// <summary>
    /// Asks the engine once and turns its answer into the fractions the next frame will be drawn from. An
    /// engine that says nothing is not an error: every bar is set to nought, which is what makes the caps fall.
    /// </summary>
    private void Listen()
    {
      // Asked every tick rather than answered once, because the screen these bars stand on is switched over
      // between the two arrangements without asking the bars about it.
      if (!IsEffectivelyVisible)
        return;

      // Bands if the engine answers in bands, magnitudes if it answers in magnitudes, and nought if it answers
      // neither, which is the answer that lets the caps fall instead of hanging where the noise left them.
      IAudioBandSource? bandSource = Bands;
      if (bandSource != null)
        LevelsFromBands(bandSource);
      else
        LevelsFromMagnitudes(Source);

      for (int bar = 0; bar < BarCount; bar++)
      {
        // A cap jumps to its bar and slides down: every tick it keeps ten elevenths of itself and takes a
        // eleventh of where the bar stands now, which is the reference's fall and nothing else.
        _caps[bar] = _caps[bar] < _levels[bar]
            ? _levels[bar]
            : (_levels[bar] + PeakFallDelay * _caps[bar]) / (PeakFallDelay + 1);
      }

      InvalidateVisual();
    }

    /// <summary>
    /// Put an engine's band answer straight onto the bars. One band per bar where the counts agree, which they do
    /// for the engine that answers this way (<see cref="VisualSpectrumTap.Bands"/> is the reference's bar count);
    /// where they do not, a bar takes the band standing where its own slice of the range begins, which keeps the
    /// engine's order and invents no number in between.
    /// </summary>
    private void LevelsFromBands(IAudioBandSource bands)
    {
      int count = bands.BandCount;
      if (_bandLevels == null || _bandLevels.Length != count)
        _bandLevels = count > 0 ? new float[count] : null;

      if (_bandLevels == null || !bands.GetBandLevels(_bandLevels))
      {
        Array.Clear(_levels, 0, _levels.Length);
        return;
      }

      for (int bar = 0; bar < BarCount; bar++)
        _levels[bar] = Math.Clamp(_bandLevels![(int) ((long) bar * count / BarCount)], 0f, 1f);
    }

    /// <summary>
    /// Turn an engine's magnitudes into bar heights the reference's own way: every bar's slice of the band range,
    /// the loudest band in it read as decibels, and the decibels placed between ninety of silence and the loudest
    /// thing the scale knows (AudioSpectrumAnalyzer.cs:497-500).
    /// </summary>
    private void LevelsFromMagnitudes(IAudioSpectrumSource? source)
    {
      bool answered = false;
      if (source != null)
      {
        LayOutBands(source.SampleRate);
        answered = source.GetFftData(_bands);
      }

      int from = 0;
      for (int bar = 0; bar < BarCount; bar++)
      {
        double highest = 0;
        if (answered)
        {
          int to = Math.Min(Math.Max(_bandEnd[bar], from + 1), _bands.Length);
          for (int band = from; band < to; band++)
          {
            float value = _bands[band];
            if (value <= 0f)
              continue;

            double decibels = 20 * Math.Log10(value);
            double fraction = (decibels - MinDecibels) / DecibelScale;
            if (fraction > highest)
              highest = fraction;
          }

          from = Math.Max(from, to);
        }

        _levels[bar] = Math.Clamp(highest, 0, 1);
      }
    }

    /// <summary>
    /// Works out which slice of the bands each bar answers for. The reference spreads the bars logarithmically
    /// over the range its two frequencies name (AudioSpectrumAnalyzer.cs:411-425) - the low end of music is
    /// where the eye wants to see movement, and a linear spread would give the whole upper half of the picture
    /// to cymbals. The frequencies themselves are the control's defaults, twenty hertz to twenty thousand
    /// (same file:121-122).
    /// </summary>
    private void LayOutBands(int sampleRate)
    {
      // A band of an answer spans half a band width of the rate, so a frequency's band is its frequency over
      // that. The reference asks its stream the same question through BASS (PlaybackBuffer.cs:389-395); a rate
      // that was never answered is treated as a CD's, which is the rate the arrangement's own figures carry.
      int rate = sampleRate > 0 ? sampleRate : 44100;
      if (_bandsFor == rate)
        return;

      int bandFor(int frequency)
      {
        return Math.Clamp(frequency * FrequencyBands * 2 / rate, 0, FrequencyBands - 1);
      }

      int lowest = bandFor(MinimumFrequency);
      int highest = bandFor(MaximumFrequency);
      int span = Math.Max(highest - lowest, BarCount);

      for (int bar = 0; bar < BarCount - 1; bar++)
      {
        // The reference's curve, kept term for term: the bar count is its base, and the bars nearer the right
        // hand edge take wider and wider bites out of the range.
        _bandEnd[bar] = lowest
            + (int) ((1.0 - Math.Log(BarCount + 1 - (bar + 1), BarCount + 1.0)) * span);
      }

      _bandEnd[BarCount - 1] = lowest + span;
      _bandsFor = rate;
    }

    public override void Render(DrawingContext context)
    {
      double width = Bounds.Width;
      double height = Bounds.Height;
      if (width < BarCount + BarSpacing * (BarCount + 1) || height < 8)
        return;

      // The reference's arithmetic for how wide a bar is and how much room a bar has to rise in: the spacing
      // borders the field on both sides as well as between the bars, and the cap's own height comes off the top
      // (AudioSpectrumAnalyzer.cs:409,485-486).
      double barWidth = Math.Max((width - BarSpacing * (BarCount + 1)) / BarCount, 1);
      double capHeight = Math.Max(barWidth / 2.0, 1);
      double rise = Math.Max(height - capHeight - REST_LIFT, 1);
      double capCorner = Math.Min(CORNER_RADIUS, capHeight / 2.0);

      // Where every cap stands is written down once and used twice: the light is laid under the caps from these
      // same rectangles, so a cap can never sit crooked in its own glow. The list is the operation's own, because a
      // renderer that keeps its picture for later must not read numbers this control has already moved on from.
      double[] caps = new double[BarCount * 4];

      for (int bar = 0; bar < BarCount; bar++)
      {
        double x = BarSpacing + barWidth * bar + BarSpacing * bar + 1;

        double level = _levels[bar] * rise;
        if (level > 0.5)
        {
          if (level > height)
            level = height;

          double corner = Math.Min(CORNER_RADIUS, Math.Min(barWidth, level) / 2.0);
          context.DrawRectangle(_barBrush, null, new Rect(x, height - level, barWidth, level), corner, corner);
        }

        // A cap stands with its lower edge at its height - five lines off the floor when nothing lifts it - and
        // never above the line where its bar could reach.
        caps[bar * 4] = x;
        caps[bar * 4 + 1] = height - REST_LIFT - Math.Min(_caps[bar] * rise, rise) - capHeight;
        caps[bar * 4 + 2] = barWidth;
        caps[bar * 4 + 3] = capHeight;
      }

      // The light first, the crisp cap over it: a cap is white, so its own light cannot dull it, and everything the
      // light reaches outside a cap is light and nothing else. Nothing here is cut - the control no longer clips
      // itself, so a cap at its fullest and a cap at its floor both glow all the way around.
      context.Custom(new CapHalo(new Rect(0, 0, width, height), caps, capCorner));

      for (int bar = 0; bar < BarCount; bar++)
      {
        context.DrawRectangle(CAP_BRUSH, null,
            new Rect(caps[bar * 4], caps[bar * 4 + 1], caps[bar * 4 + 2], caps[bar * 4 + 3]), capCorner, capCorner);
      }
    }

    /// <summary>
    /// The caps' light, blurred by Skia itself: the cap's own rounded shape is drawn through a gaussian mask
    /// filter, which is what light off a lit edge looks like, as opposed to the translucent plates this class wore
    /// before and which the viewer sent back.
    /// <para>
    /// <b>Why this is affordable and the earlier refusal no longer applies.</b> The blur is asked per cap, so the
    /// mask Skia softens is the size of one cap grown by three sigmas - at the widths this strip draws, about
    /// seventy by sixty pixels - and not the size of the whole strip, which is what the fanart comparison feared
    /// and what made a full-strip layer look extravagant. Thirty-two small masks are less work than one large one,
    /// and this is the same blur the vendored glass already uses for its button shadows on every frame
    /// (LiquidGlassAvaloniaUI/LiquidGlassShadowDrawOperations.cs), so it is proven on this frontend's renderers.
    /// </para>
    /// </summary>
    private sealed class CapHalo : ICustomDrawOperation
    {
      private readonly double[] _caps;
      private readonly float _corner;
      private readonly Rect _bounds;

      public CapHalo(Rect strip, double[] caps, double corner)
      {
        _caps = caps;
        _corner = (float)corner;

        // A gaussian is visibly spent at three sigmas. The bounds say how much room the renderer has to give this
        // operation, so they are grown by the widest pass; without that the outer light would be cut, which is the
        // very thing this control keeps being accused of.
        double pad = GLOW_SIGMA_WIDE * 3.0;
        _bounds = new Rect(-pad, -pad, strip.Width + 2 * pad, strip.Height + 2 * pad);
      }

      public Rect Bounds => _bounds;

      public void Dispose()
      {
      }

      public bool HitTest(Point p)
      {
        return false;
      }

      public bool Equals(ICustomDrawOperation? other)
      {
        return false;
      }

      public void Render(ImmediateDrawingContext context)
      {
        ISkiaSharpApiLeaseFeature? leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (leaseFeature is null)
          return;  // a renderer that is not Skia gets its caps without their light; the caps themselves never fail

        using ISkiaSharpApiLease lease = leaseFeature.Lease();
        Soften(lease.SkCanvas, GLOW_SIGMA_TIGHT, GLOW_ALPHA_TIGHT);
        Soften(lease.SkCanvas, GLOW_SIGMA_MID, GLOW_ALPHA_MID);
        Soften(lease.SkCanvas, GLOW_SIGMA_WIDE, GLOW_ALPHA_WIDE);
      }

      private void Soften(SKCanvas canvas, float sigma, byte alpha)
      {
        SKMaskFilter? created = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, sigma);
        if (created is null)
          return;

        using SKMaskFilter blur = created;
        using SKPaint paint = new SKPaint
        {
          Color = new SKColor(255, 255, 255, alpha),
          MaskFilter = blur,
          IsAntialias = true
        };

        for (int at = 0; at + 3 < _caps.Length; at += 4)
        {
          canvas.DrawRoundRect(SKRect.Create((float)_caps[at], (float)_caps[at + 1], (float)_caps[at + 2],
              (float)_caps[at + 3]), _corner, _corner, paint);
        }
      }
    }

  }
}
