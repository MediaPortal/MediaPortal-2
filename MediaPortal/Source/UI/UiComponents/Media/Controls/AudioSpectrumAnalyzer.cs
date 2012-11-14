#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

#region Copyright (C) 2011, Jacob Johnston

// Copyright (C) 2011, Jacob Johnston 
//
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"), 
// to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, 
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions: 
//
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software. 
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE. 

#endregion

using System;
using System.Collections.Generic;
using System.Timers;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.SkinEngine.Controls.Panels;
using MediaPortal.UI.SkinEngine.Controls.Transforms;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Styles;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UiComponents.Media.Controls
{
  /// <summary>
  /// The different ways that the bar height can be scaled by the spectrum analyzer.
  /// </summary>
  public enum BarHeightScalingStyles
  {
    /// <summary>
    /// A decibel scale. Formula: 20 * Log10(FFTValue). Total bar height
    /// is scaled from -90 to 0 dB.
    /// </summary>
    Decibel,

    /// <summary>
    /// A non-linear squareroot scale. Formula: Sqrt(FFTValue) * 2 * BarHeight.
    /// </summary>
    Sqrt,

    /// <summary>
    /// A linear scale. Formula: 9 * FFTValue * BarHeight.
    /// </summary>
    Linear
  }

  public class AudioSpectrumAnalyzer : Control
  {
    #region Consts

    protected const int FREQUENCY_BUFFER_SIZE = 2048;
    private const int SCALE_FACTOR_LINEAR = 9;
    private const int SCALE_FACTOR_SQR = 2;
    private const double MIN_DB_VALUE = -90;
    private const double MAX_DB_VALUE = 0;
    private const double DB_SCALE = (MAX_DB_VALUE - MIN_DB_VALUE);

    #endregion

    #region Fields

    private readonly Timer _animationTimer;
    private volatile bool _refreshShapes = false;
    private volatile bool _refreshValues = false;

    private Canvas _spectrumCanvas;
    private readonly List<Control> _barShapes = new List<Control>();
    private readonly List<Control> _peakShapes = new List<Control>();
    private readonly float[] _channelData = new float[FREQUENCY_BUFFER_SIZE];
    private float[] _channelPeakData;
    private double _barWidth = 1;
    private int _maximumFrequencyIndex = FREQUENCY_BUFFER_SIZE - 1;
    private int _minimumFrequencyIndex;
    private int[] _barIndexMax;
    private int[] _barLogScaleIndexMax;

    #endregion

    public AudioSpectrumAnalyzer()
    {
      MaximumFrequencyProperty = new SProperty(typeof(int), 20000);
      MinimumFrequencyProperty = new SProperty(typeof(int), 20);
      BarCountProperty = new SProperty(typeof(int), 32);
      BarSpacingProperty = new SProperty(typeof(double), 5d);
      PeakFallDelayProperty = new SProperty(typeof(int), 10);
      IsFrequencyScaleLinearProperty = new SProperty(typeof(bool), false);
      BarHeightScalingProperty = new SProperty(typeof(BarHeightScalingStyles), BarHeightScalingStyles.Decibel);
      AveragePeaksProperty = new SProperty(typeof(bool), false);
      BarStyleProperty = new SProperty(typeof(Style), null);
      PeakStyleProperty = new SProperty(typeof(Style), null);
      ActualBarWidthProperty = new SProperty(typeof(double), 0.0d);
      RefreshIntervalProperty = new SProperty(typeof(int), 25);
      PlayerContextProperty = new SProperty(typeof(PlayerChoice), PlayerChoice.PrimaryPlayer);

      _animationTimer = new Timer(RefreshInterval);
      _animationTimer.Elapsed += AnimationTimerElapsed;

      Attach();
    }

    private void Attach()
    {
      MaximumFrequencyProperty.Attach(OnRequireUpdateLayout);
      MinimumFrequencyProperty.Attach(OnRequireUpdateLayout);
      BarCountProperty.Attach(OnRequireUpdateLayout);
      BarSpacingProperty.Attach(OnRequireUpdateLayout);
      IsFrequencyScaleLinearProperty.Attach(OnRequireUpdateLayout);
      BarStyleProperty.Attach(OnRequireUpdateLayout);
      PeakStyleProperty.Attach(OnRequireUpdateLayout);
      RefreshIntervalProperty.Attach(OnRefreshIntervalChanged);
      TemplateProperty.Attach(OnSpectrumTemplateChanged);
    }

    private void Detach()
    {
      MaximumFrequencyProperty.Detach(OnRequireUpdateLayout);
      MinimumFrequencyProperty.Detach(OnRequireUpdateLayout);
      BarCountProperty.Detach(OnRequireUpdateLayout);
      BarSpacingProperty.Detach(OnRequireUpdateLayout);
      IsFrequencyScaleLinearProperty.Detach(OnRequireUpdateLayout);
      BarStyleProperty.Detach(OnRequireUpdateLayout);
      PeakStyleProperty.Detach(OnRequireUpdateLayout);
      RefreshIntervalProperty.Detach(OnRefreshIntervalChanged);
      TemplateProperty.Detach(OnSpectrumTemplateChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();

      base.DeepCopy(source, copyManager);

      AudioSpectrumAnalyzer c = (AudioSpectrumAnalyzer) source;
      BarStyle = copyManager.GetCopy(c.BarStyle);
      PeakStyle = copyManager.GetCopy(c.PeakStyle);

      MaximumFrequency = c.MaximumFrequency;
      MinimumFrequency = c.MinimumFrequency;
      BarCount = c.BarCount;
      BarSpacing = c.BarSpacing;
      IsFrequencyScaleLinear = c.IsFrequencyScaleLinear;
      RefreshInterval = c.RefreshInterval;
      
      Attach();
    }

    public override void Dispose()
    {
      Detach();
      _animationTimer.Stop();
      _animationTimer.Close();
      base.Dispose();
    }

    private void OnRequireUpdateLayout(AbstractProperty property, object oldvalue)
    {
      _refreshShapes = true;
    }

    private void OnRefreshIntervalChanged(AbstractProperty property, object oldvalue)
    {
      _animationTimer.Interval = (double) property.GetValue();
    }

    private void OnSpectrumTemplateChanged(AbstractProperty property, object oldvalue)
    {
      _spectrumCanvas = FindElement(new NameMatcher("PART_SpectrumCanvas")) as Canvas;
      _refreshShapes = true;
    }

    private void AnimationTimerElapsed(object sender, ElapsedEventArgs e)
    {
      _refreshValues = true;
    }

    // FIXME: Don't check values in the setters; Instead, use safe getters
    public AbstractProperty MaximumFrequencyProperty { get; internal set; }
    public int MaximumFrequency
    {
      get { return (int) MaximumFrequencyProperty.GetValue(); }
      set
      {
        int checkedValue = Math.Max(MinimumFrequency, value);
        MaximumFrequencyProperty.SetValue(checkedValue);
      }
    }

    public AbstractProperty MinimumFrequencyProperty { get; internal set; }
    public int MinimumFrequency
    {
      get { return (int) MinimumFrequencyProperty.GetValue(); }
      set
      {
        int checkedValue = Math.Min(MaximumFrequency, value);
        MinimumFrequencyProperty.SetValue(checkedValue);
      }
    }

    public AbstractProperty BarCountProperty { get; internal set; }
    public int BarCount
    {
      get { return (int) BarCountProperty.GetValue(); }
      set
      {
        int checkedValue = Math.Max(1, value);
        BarCountProperty.SetValue(checkedValue);
      }
    }

    public AbstractProperty BarSpacingProperty { get; internal set; }
    public double BarSpacing
    {
      get { return (double) BarSpacingProperty.GetValue(); }
      set
      {
        double checkedValue = Math.Max(0, value);
        BarSpacingProperty.SetValue(checkedValue);
      }
    }

    public AbstractProperty PeakFallDelayProperty { get; internal set; }
    public int PeakFallDelay
    {
      get { return (int) PeakFallDelayProperty.GetValue(); }
      set
      {
        int checkedValue = Math.Max(0, value);
        PeakFallDelayProperty.SetValue(checkedValue);
      }
    }

    public AbstractProperty IsFrequencyScaleLinearProperty { get; internal set; }
    public bool IsFrequencyScaleLinear
    {
      get { return (bool) IsFrequencyScaleLinearProperty.GetValue(); }
      set { IsFrequencyScaleLinearProperty.SetValue(value); }
    }

    public AbstractProperty BarHeightScalingProperty { get; internal set; }
    public BarHeightScalingStyles BarHeightScaling
    {
      get { return (BarHeightScalingStyles) BarHeightScalingProperty.GetValue(); }
      set { BarHeightScalingProperty.SetValue(value); }
    }

    public AbstractProperty AveragePeaksProperty { get; internal set; }
    public bool AveragePeaks
    {
      get { return (bool) AveragePeaksProperty.GetValue(); }
      set { AveragePeaksProperty.SetValue(value); }
    }

    public AbstractProperty BarStyleProperty { get; internal set; }
    public Style BarStyle
    {
      get { return (Style) BarStyleProperty.GetValue(); }
      set { BarStyleProperty.SetValue(value); }
    }

    public AbstractProperty PeakStyleProperty { get; internal set; }
    public Style PeakStyle
    {
      get { return (Style) PeakStyleProperty.GetValue(); }
      set { PeakStyleProperty.SetValue(value); }
    }

    public AbstractProperty ActualBarWidthProperty { get; internal set; }
    public double ActualBarWidth
    {
      get { return (double) ActualBarWidthProperty.GetValue(); }
      set { ActualBarWidthProperty.SetValue(value); }
    }

    public AbstractProperty RefreshIntervalProperty { get; internal set; }
    public int RefreshInterval
    {
      get { return (int) RefreshIntervalProperty.GetValue(); }
      set
      {
        int checkedValue = Math.Min(1000, Math.Max(10, value));
        RefreshIntervalProperty.SetValue(checkedValue);
      }
    }

    public AbstractProperty PlayerContextProperty { get; internal set; }

    /// <summary>
    /// Determines, which player's properties are tracked by this player control.
    /// </summary>
    public PlayerChoice PlayerContext
    {
      get { return (PlayerChoice) PlayerContextProperty.GetValue(); }
      set { PlayerContextProperty.SetValue(value); }
    }

    public override void Allocate()
    {
      base.Allocate();
      _animationTimer.Start();
    }

    public override void Deallocate()
    {
      base.Deallocate();
      _animationTimer.Stop();
    }

    public override void RenderOverride(RenderContext localRenderContext)
    {
      InitializeBars();
      UpdateSpectrum();
      base.RenderOverride(localRenderContext);
    }

    protected ISpectrumPlayer ActiveSpectrumPlayer
    {
      get
      {
        IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>(false);
        if (playerContextManager == null)
          return null;

        IPlayerContext playerContext = playerContextManager.GetPlayerContext(PlayerContext);
        if (playerContext == null)
          return null;
        return playerContext.CurrentPlayer as ISpectrumPlayer;
      }
    }

    private void UpdateSpectrum()
    {
      ISpectrumPlayer player = ActiveSpectrumPlayer;
      if (!_refreshValues || player == null || _spectrumCanvas == null)
        return;

      if (player.State == PlayerState.Active && !player.GetFFTData(_channelData))
        return;

      UpdateSpectrumShapes(player);
    }

    #region Private Drawing Methods

    private void InitializeBars()
    {
      ISpectrumPlayer player = ActiveSpectrumPlayer;

      if (player == null)
      {
        _barWidth = 1;
        _maximumFrequencyIndex = -1;
        _minimumFrequencyIndex = 0;
        return;
      }

      if (!_refreshShapes || _spectrumCanvas == null)
        return;

      int maxIndex;
      int minIndex;
      bool res = player.GetFFTFrequencyIndex(MaximumFrequency, out maxIndex);
      res |= player.GetFFTFrequencyIndex(MinimumFrequency, out minIndex);
      if (!res)
        return;
      _maximumFrequencyIndex = Math.Min(maxIndex + 1, FREQUENCY_BUFFER_SIZE - 1);
      _minimumFrequencyIndex = Math.Min(minIndex, FREQUENCY_BUFFER_SIZE - 1);

      _barWidth = Math.Max(((_spectrumCanvas.ActualWidth - (BarSpacing * (BarCount + 1))) / BarCount), 1);
      int actualBarCount = _barWidth >= 1.0d ? BarCount : Math.Max((int) ((_spectrumCanvas.ActualWidth - BarSpacing) / (_barWidth + BarSpacing)), 1);
      _channelPeakData = new float[actualBarCount];

      int indexCount = _maximumFrequencyIndex - _minimumFrequencyIndex;
      int linearIndexBucketSize = (int) Math.Round(indexCount / (double) actualBarCount, 0);
      List<int> maxIndexList = new List<int>();
      List<int> maxLogScaleIndexList = new List<int>();
      double maxLog = Math.Log(actualBarCount, actualBarCount);
      for (int i = 1; i < actualBarCount; i++)
      {
        maxIndexList.Add(_minimumFrequencyIndex + (i * linearIndexBucketSize));
        int logIndex = (int) ((maxLog - Math.Log((actualBarCount + 1) - i, (actualBarCount + 1))) * indexCount) + _minimumFrequencyIndex;
        maxLogScaleIndexList.Add(logIndex);
      }
      maxIndexList.Add(_maximumFrequencyIndex);
      maxLogScaleIndexList.Add(_maximumFrequencyIndex);
      _barIndexMax = maxIndexList.ToArray();
      _barLogScaleIndexMax = maxLogScaleIndexList.ToArray();

      FrameworkElementCollection canvasChildren = _spectrumCanvas.Children;
      canvasChildren.StartUpdate();
      try
      {
        canvasChildren.Clear();

        double height = _spectrumCanvas.ActualHeight;
        double peakDotHeight = Math.Max(_barWidth / 2.0f, 1);
        for (int i = 0; i < actualBarCount; i++)
        {
          // Deep copy the styles to each bar
          Style barStyleCopy = MpfCopyManager.DeepCopyCutLVPs(BarStyle);
          Style peakStyleCopy = MpfCopyManager.DeepCopyCutLVPs(PeakStyle);

          double xCoord = BarSpacing + (_barWidth * i) + (BarSpacing * i) + 1;
          Control barControl = new Control
              {
                Width = _barWidth,
                Height = height,
                Style = barStyleCopy
              };
          Canvas.SetLeft(barControl, xCoord);
          Canvas.SetBottom(barControl, height);
          _barShapes.Add(barControl);
          canvasChildren.Add(barControl);

          Control peakControl = new Control
              {
                Width = _barWidth,
                Height = peakDotHeight,
                Style = peakStyleCopy
              };
          Canvas.SetLeft(peakControl, xCoord);
          Canvas.SetBottom(peakControl, height);
          _peakShapes.Add(peakControl);
          canvasChildren.Add(peakControl);
        }
      }
      finally
      {
        canvasChildren.EndUpdate();
      }
      ActualBarWidth = _barWidth;
      _refreshShapes = false;
    }

    private void UpdateSpectrumShapes(ISpectrumPlayer player)
    {
      double fftBucketHeight = 0f;
      double barHeight = 0f;
      double lastPeakHeight = 0f;
      double height = _spectrumCanvas.ActualHeight;
      int barIndex = 0;
      double peakDotHeight = Math.Max(_barWidth / 2.0f, 1);
      double barHeightScale = (height - peakDotHeight);

      int minIndex = Math.Max(0, Math.Min(_minimumFrequencyIndex, _channelData.Length));
      int maxIndex = Math.Max(0, Math.Min(_maximumFrequencyIndex, _channelData.Length));
      for (int i = minIndex; i <= maxIndex; i++)
      {
        // If we're paused, keep drawing, but set the current height to 0 so the peaks fall.
        if (player == null || player.State != PlayerState.Active)
          barHeight = 0f;
        else // Draw the maximum value for the bar's band
        {
          switch (BarHeightScaling)
          {
            case BarHeightScalingStyles.Decibel:
              double dbValue = 20 * Math.Log10(_channelData[i]);
              fftBucketHeight = ((dbValue - MIN_DB_VALUE) / DB_SCALE) * barHeightScale;
              break;
            case BarHeightScalingStyles.Linear:
              fftBucketHeight = (_channelData[i] * SCALE_FACTOR_LINEAR) * barHeightScale;
              break;
            case BarHeightScalingStyles.Sqrt:
              fftBucketHeight = (((Math.Sqrt(_channelData[i])) * SCALE_FACTOR_SQR) * barHeightScale);
              break;
          }

          if (barHeight < fftBucketHeight)
            barHeight = fftBucketHeight;
          if (barHeight < 0f)
            barHeight = 0f;
        }

        // If this is the last FFT bucket in the bar's group, draw the bar.
        int currentIndexMax = IsFrequencyScaleLinear ? _barIndexMax[barIndex] : _barLogScaleIndexMax[barIndex];
        if (i != currentIndexMax) 
          continue;

        // Peaks can't surpass the height of the control.
        if (barHeight > height)
          barHeight = height;

        if (AveragePeaks && barIndex > 0)
          barHeight = (lastPeakHeight + barHeight) / 2;

        double peakYPos = barHeight;

        if (_channelPeakData[barIndex] < peakYPos)
          _channelPeakData[barIndex] = (float) peakYPos;
        else
          _channelPeakData[barIndex] = (float) (peakYPos + (PeakFallDelay * _channelPeakData[barIndex])) / (PeakFallDelay + 1);

        Control bar = _barShapes[barIndex];
        bar.RenderTransform = new ScaleTransform
          {
              CenterX = bar.ActualWidth / 2,
              CenterY = bar.ActualHeight,
              ScaleY = barHeight / height
          };

        Control peak = _peakShapes[barIndex];
        peak.RenderTransform = new TranslateTransform
          {
              Y = -_channelPeakData[barIndex]
          };

        lastPeakHeight = barHeight;
        barHeight = 0f;
        barIndex++;
      }
    }

    #endregion
  }
}
