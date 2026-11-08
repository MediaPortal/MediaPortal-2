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
using Avalonia.Threading;
using MediaPortal.Frontend.Player;

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
    private const double BarSpacing = 5d;
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

    public AudioSpectrumBars()
    {
      // A bar's rect is deliberately pushed one radius below the floor so that only its upper corners come out
      // round (see Render), and the floor is where this control's own bounds end - which is what cuts those two
      // corners off instead of letting them hang under the strip.
      ClipToBounds = true;

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
      double rise = height - capHeight;

      for (int bar = 0; bar < BarCount; bar++)
      {
        double x = BarSpacing + barWidth * bar + BarSpacing * bar + 1;

        double level = _levels[bar] * rise;
        if (level > 0.5)
        {
          if (level > height)
            level = height;

          // The bar wears the same corner as its cap, and wears it at the top only: its rect is pushed one radius
          // below the floor and the control is clipped to its own bounds, so the two lower corners are cut off by
          // the floor line instead of being notched into the bar.
          double corner = Math.Min(CORNER_RADIUS, Math.Min(barWidth, level) / 2.0);
          context.DrawRectangle(_barBrush, null, new Rect(x, height - level, barWidth, level + corner),
              corner, corner);
        }

        // A cap stands with its lower edge at its height, and never above the line where its bar could reach.
        double capTop = height - Math.Min(_caps[bar] * rise, rise) - capHeight;
        double capCorner = Math.Min(CORNER_RADIUS, capHeight / 2.0);
        context.DrawRectangle(CAP_BRUSH, null, new Rect(x, capTop, barWidth, capHeight), capCorner, capCorner);
      }
    }

  }
}
