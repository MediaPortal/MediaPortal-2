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
using Avalonia.Media;
using Avalonia.Threading;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// A picture that changes the way the reference's fanart did: it waits for the new picture to settle,
  /// then dissolves into it, instead of replacing it in one sudden frame.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>What the reference did, and this repeats.</b> Every fanart picture in Nereus hung on an
  /// <c>ImageSourceWrapper</c> with <c>Delay="0.5" DelayInOut="true" Transition="transitions\dissolve"
  /// TransitionDuration="0.5"</c> (master_homemenu.xaml among others). Two numbers, two jobs. The delay is
  /// the politeness: a picture is only allocated once the question has stopped changing, so walking a rail
  /// of tiles past at three titles a second allocates and dissolves nothing - the screen keeps wearing the
  /// last picture that stood still long enough to deserve one. The dissolve is the courtesy: when a
  /// picture is replaced, the new one is faded in over the old one rather than cutting into it, which is
  /// what makes the background feel like weather changing rather than a slideshow advancing.
  /// </para>
  /// <para>
  /// <b>Why a control and not a transition on the plain picture.</b> A fade of the whole element would
  /// darken through the window between the two pictures; the reference crossed the two *over* each other,
  /// so both have to be on screen at once. Hence two stacked images in here: the standing one, which the
  /// viewer is looking at, and the coming one, which grows over it and hands the picture on when it has
  /// finished. The fade itself is driven by a timer rather than the animation API because the handover at
  /// the end of a fade has to be certain - a picture left in the wrong layer at the end of an interrupted
  /// animation is a background stuck on yesterday's title - and a timer that is stopped and restarted from
  /// the same two methods is certain by construction.
  /// </para>
  /// <para>
  /// <b>What happens between the frames.</b> Every new source starts the wait again, and a wait that ends
  /// while a dissolve is still running finishes that dissolve at once - the viewer asked for the newer
  /// picture, and the older one is over. A source that equals what is already standing is no news at all.
  /// </para>
  /// </remarks>
  public sealed class DissolveImage : Panel
  {
    /// <summary>The picture to wear, in the reference's sense: what to dissolve *to*.</summary>
    public static readonly StyledProperty<IImage?> SourceProperty =
        AvaloniaProperty.Register<DissolveImage, IImage?>(nameof(Source));

    /// <summary>How the picture is stretched; <see cref="Avalonia.Controls.Image.Stretch"/>.</summary>
    public static readonly StyledProperty<Stretch> StretchProperty =
        AvaloniaProperty.Register<DissolveImage, Stretch>(nameof(Stretch), Stretch.Uniform);

    /// <summary>The reference's <c>Delay</c>, in seconds: how long a picture must stand still before it is worn.</summary>
    public static readonly StyledProperty<double> DelaySecondsProperty =
        AvaloniaProperty.Register<DissolveImage, double>(nameof(DelaySeconds), 0.5);

    /// <summary>The length of the dissolve. The reference used half a second; on this screen that reads as
    /// dragging, so it dissolves in an eighth of a second - the same crossfade, a quarter of the time.</summary>
    public static readonly StyledProperty<double> DissolveSecondsProperty =
        AvaloniaProperty.Register<DissolveImage, double>(nameof(DissolveSeconds), 0.125);

    public IImage? Source
    {
      get { return GetValue(SourceProperty); }
      set { SetValue(SourceProperty, value); }
    }

    public Stretch Stretch
    {
      get { return GetValue(StretchProperty); }
      set { SetValue(StretchProperty, value); }
    }

    public double DelaySeconds
    {
      get { return GetValue(DelaySecondsProperty); }
      set { SetValue(DelaySecondsProperty, value); }
    }

    public double DissolveSeconds
    {
      get { return GetValue(DissolveSecondsProperty); }
      set { SetValue(DissolveSecondsProperty, value); }
    }

    /// <summary>The picture the viewer is looking at.</summary>
    private readonly Image standing = new Image();

    /// <summary>The picture being dissolved in over it, or none between dissolves.</summary>
    private readonly Image coming = new Image();

    /// <summary>The reference's wait: restarted by every new source, and the only door into a dissolve.</summary>
    private DispatcherTimer? waiting;

    /// <summary>The dissolve in progress, or <c>null</c> between them.</summary>
    private DispatcherTimer? dissolving;

    private IImage? waitedFor;
    private IImage? dissolveFrom;
    private IImage? dissolveTo;
    private double dissolveElapsed;
    private bool fadeStandingOut;

    public DissolveImage()
    {
      Children.Add(standing);
      Children.Add(coming);
      coming.IsVisible = false;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
      base.OnPropertyChanged(change);

      if (change.Property == SourceProperty)
        Ask(Source);
      else if (change.Property == StretchProperty)
      {
        standing.Stretch = Stretch;
        coming.Stretch = Stretch;
      }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
      base.OnDetachedFromVisualTree(e);
      waiting?.Stop();
      dissolving?.Stop();
    }

    /// <summary>
    /// A picture was asked for. It is not worn yet; the wait starts, and only a question that stays quiet
    /// for the whole wait gets its dissolve.
    /// </summary>
    private void Ask(IImage? picture)
    {
      waitedFor = picture;

      if (picture == standing.Source && !IsDissolving)
      {
        // The news is the picture already standing - the wait can keep its silence.
        waiting?.Stop();
        return;
      }

      Stop(ref waiting);
      if (DelaySeconds <= 0)
      {
        BeginDissolve(picture);
        return;
      }

      DispatcherTimer timer = waiting ??= NewTimer(Tick);
      timer.Interval = TimeSpan.FromSeconds(DelaySeconds);
      timer.Start();
    }

    private bool IsDissolving
    {
      get { return dissolving != null; }
    }

    /// <summary>
    /// The wait ended: the newest asked-for picture is worn now, dissolving over the old one.
    /// </summary>
    private void Tick()
    {
      Stop(ref waiting);
      BeginDissolve(waitedFor);
    }

    private void BeginDissolve(IImage? picture)
    {
      // A dissolve the viewer has meanwhile had better news about ends where it stands, so that the newer
      // picture starts over the one the screen actually shows - not over a half-faded third one.
      if (IsDissolving)
        FinishDissolve();

      dissolveTo = picture;

      if (picture == standing.Source)
        return;

      dissolveElapsed = 0;
      fadeStandingOut = picture == null;

      if (fadeStandingOut)
      {
        // There is nothing to grow; the standing picture is the one that goes, and what stands under this
        // control - the skin's own ground - is what the viewer ends up seeing.
        dissolveFrom = standing.Source;
        dissolving = NewTimer(Tack);
      }
      else if (standing.Source == null)
      {
        // Nothing on screen yet: the new picture is the standing one already, and it merely grows in.
        standing.Source = picture;
        standing.Opacity = 0;
        dissolveFrom = picture;
        dissolving = NewTimer(Tack);
      }
      else
      {
        coming.Source = picture;
        coming.Opacity = 0;
        coming.IsVisible = true;
        dissolveFrom = standing.Source;
        dissolving = NewTimer(Tack);
      }

      double seconds = Math.Max(DissolveSeconds, 0.02);
      dissolving.Interval = TimeSpan.FromSeconds(seconds / TICKS);
      dissolving.Start();
    }

    /// <summary>One step of the dissolve. The last one hands the picture from the coming layer to the standing one.</summary>
    private void Tack()
    {
      double seconds = Math.Max(DissolveSeconds, 0.02);
      dissolveElapsed += 1.0 / TICKS;
      double share = Math.Min(dissolveElapsed, 1.0);

      if (fadeStandingOut)
        standing.Opacity = 1.0 - share;
      else if (coming.IsVisible)
      {
        // The crossfade the reference made: the new picture grows over the old while the old shrinks
        // under it, both in the same breath.
        coming.Opacity = share;
        standing.Opacity = 1.0 - share;
      }
      else
        standing.Opacity = share;

      if (dissolveElapsed < 1.0)
        return;

      FinishDissolve();
    }

    private void FinishDissolve()
    {
      Stop(ref dissolving);

      if (fadeStandingOut)
      {
        standing.Source = null;
        standing.Opacity = 1;
      }
      else if (coming.IsVisible)
      {
        standing.Source = coming.Source;
        standing.Opacity = 1;
        coming.Source = null;
        coming.Opacity = 0;
        coming.IsVisible = false;
      }
      else
        standing.Opacity = 1;

      fadeStandingOut = false;
    }

    private static DispatcherTimer NewTimer(Action tick)
    {
      DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.0 / TICKS) };
      timer.Tick += delegate { tick(); };
      return timer;
    }

    private static void Stop(ref DispatcherTimer? timer)
    {
      timer?.Stop();
    }

    /// <summary>Steps per second the dissolve is driven in; thirty is smoother than a frame every tenth.</summary>
    private const int TICKS = 30;
  }
}
