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

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// What Nereus did with a story too long for its panel: the plot of the details read itself down the
  /// screen. <c>EnableAutoScrolling</c> decided whether the text moved by itself at all - off, the panel
  /// stayed still and showed a bar for the viewer's own scroll, which is the reference's focus-revealed
  /// ScrollViewer; on, the text waited <c>AutoScrollDelay</c> seconds and then ran at
  /// <c>AutoScrollSpeed</c> pixels a second, and <c>EnableLoopScrolling</c> decided whether it started
  /// again from the top or stopped at the bottom. A face without those switches (the plain front end, WMC)
  /// gets the still panel and the bar, because a switch never offered must not silently answer on the
  /// reference's behalf.
  /// </summary>
  public sealed class PlotAutoScroll
  {
    private const int TICK_MILLISECONDS = 80;

    private readonly ScrollViewer _scroller;
    private readonly MediaBrowserViewModel _browser;
    private readonly DispatcherTimer _timer;
    private long _startedTimestamp;
    private double _startedFrequency;
    private bool _userTookOver;
    private bool _wanted;

    public PlotAutoScroll(ScrollViewer scroller, MediaBrowserViewModel browser)
    {
      _scroller = scroller;
      _browser = browser;

      _timer = new DispatcherTimer(DispatcherPriority.Render);
      _timer.Interval = TimeSpan.FromMilliseconds(TICK_MILLISECONDS);
      _timer.Tick += delegate { Tick(); };

      // A new title is a new story: the reading starts from the top with a fresh wait, whatever the old
      // one had reached. Hiding the panel stops the clock; showing it starts it again.
      _browser.PropertyChanged += delegate (object? sender,
          System.ComponentModel.PropertyChangedEventArgs args)
      {
        if (args.PropertyName == nameof(MediaBrowserViewModel.DetailsText)
            || args.PropertyName == nameof(MediaBrowserViewModel.DetailsVisible)
            || args.PropertyName == nameof(MediaBrowserViewModel.Selected))
          Restart();
      };

      // The moment a viewer scrolls the plot themselves the automatic reading yields - a panel that pulls
      // the text back down while somebody is reading it is a panel that cannot be read. The next title
      // gives the automatic reading another turn. Handled for any pointer event rather than only the wheel
      // one: a routed handler whose delegate demands a more specific argument type than the event carries
      // is an ArgumentException on the first scroll, which is to say a dead application.
      _scroller.AddHandler(InputElement.PointerPressedEvent,
          delegate { Yield(); }, RoutingStrategies.Tunnel);
      _scroller.AddHandler(InputElement.PointerWheelChangedEvent,
          delegate { Yield(); }, RoutingStrategies.Tunnel);

      // A skin put on or taken off while the screen stands open changes whether the text should move at
      // all - the switches belong to the worn face.
      _scroller.AttachedToVisualTree += delegate { SkinEngine.Changed += OnSkinChanged; Restart(); };
      _scroller.DetachedFromVisualTree += delegate { SkinEngine.Changed -= OnSkinChanged; _timer.Stop(); };

      Restart();
    }

    private void OnSkinChanged(object? sender, EventArgs args)
    {
      Restart();
    }

    /// <summary>The viewer's own pointer took the plot over: the clock stops until the next title.</summary>
    private void Yield()
    {
      if (!_wanted)
        return;

      _userTookOver = true;
      _timer.Stop();
    }

    private void Restart()
    {
      _timer.Stop();
      _userTookOver = false;

      _wanted = Dial(out double delaySeconds, out double speedPixelsPerSecond, out bool loop)
          && _browser.DetailsVisible;
      _scroller.VerticalScrollBarVisibility = _wanted
          ? ScrollBarVisibility.Hidden
          : ScrollBarVisibility.Auto;

      if (!_wanted)
        return;

      _startedTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
      _startedFrequency = System.Diagnostics.Stopwatch.Frequency;
      _wantedDelay = delaySeconds;
      _wantedSpeed = speedPixelsPerSecond;
      _wantedLoop = loop;
      _timer.Start();
    }

    private double _wantedDelay;
    private double _wantedSpeed;
    private bool _wantedLoop;

    private void Tick()
    {
      if (_userTookOver)
        return;

      double seconds = (System.Diagnostics.Stopwatch.GetTimestamp() - _startedTimestamp) / _startedFrequency;
      double maxOffset = Math.Max(0, _scroller.Extent.Height - _scroller.Viewport.Height);
      double offset = OffsetAt(seconds, _wantedDelay, _wantedSpeed, maxOffset, _wantedLoop);
      _scroller.Offset = new Vector(0, offset);

      if (!_wantedLoop && offset >= maxOffset)
        _timer.Stop();
    }

    /// <summary>
    /// Where the reading stands a given number of seconds after the story appeared: at the top while the
    /// delay lasts, then <paramref name="speedPixelsPerSecond"/> pixels a second, stopped at the bottom or
    /// running round to the top again. The whole behaviour is this one line of arithmetic, which is why it
    /// answers on its own - the scroller only ever does what it says.
    /// </summary>
    internal static double OffsetAt(double seconds, double delaySeconds, double speedPixelsPerSecond,
        double maxOffset, bool loop)
    {
      if (maxOffset <= 0 || speedPixelsPerSecond <= 0)
        return 0;

      double running = seconds - delaySeconds;
      if (running <= 0)
        return 0;

      double raw = running * speedPixelsPerSecond;
      if (!loop)
        return Math.Min(raw, maxOffset);

      return raw % maxOffset;
    }

    /// <summary>
    /// The worn face's three switches about the reading. False - and a still panel - when the face ships no
    /// switches or has the automatic reading switched off.
    /// </summary>
    private static bool Dial(out double delaySeconds, out double speedPixelsPerSecond, out bool loop)
    {
      delaySeconds = 2;
      speedPixelsPerSecond = 20;
      loop = true;

      if (Application.Current is not Application app)
        return false;

      if (SkinSettingsBase.LoadFor(FrontendTheme.CurrentName(app), null, out _) is not NereusSkinSettings nereus)
        return false;

      delaySeconds = nereus.AutoScrollDelay;
      speedPixelsPerSecond = nereus.AutoScrollSpeed;
      loop = nereus.EnableLoopScrolling;
      return nereus.EnableAutoScrolling;
    }
  }
}
