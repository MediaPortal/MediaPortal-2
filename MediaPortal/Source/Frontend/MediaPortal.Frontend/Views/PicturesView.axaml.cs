#region Copyright (C) 2007-2026 Team MediaPortal

/*
    Copyright (C) 2007-2026 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Workflow;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The pictures screen: a grid of folders and photographs, and the picture being looked at.
  /// </summary>
  /// <remarks>
  /// What only a screen knows, and the view model deliberately does not: the folders are read when the screen
  /// opens, Enter walks into a folder or raises a photograph to full size, back steps out of a folder and only
  /// leaves the screen from the list of shares, the arrow keys along the bottom of the grid are the tile
  /// navigation while the viewer is the left and right keys, and the slideshow is a timer here rather than a task
  /// in the view model - it is a thing that happens on screen, and it stops when the screen closes without
  /// anybody having to remember to cancel it.
  /// </remarks>
  public partial class PicturesView : UserControl, IWorkflowScreen
  {
    private readonly PicturesViewModel _pictures;
    private readonly List<Control> _ring = new List<Control>();
    private readonly DispatcherTimer _slideshow;

    /// <summary>
    /// For the XAML loader and the designer: the screen a front end with no server shows.
    /// </summary>
    public PicturesView()
        : this(new PicturesViewModel())
    {
    }

    public PicturesView(PicturesViewModel pictures)
    {
      DataContext = pictures;
      _pictures = pictures;
      InitializeComponent();

      _ring.Add(tilesList);
      _ring.Add(upButton);
      DpadNavigation.Attach(this, _ring);

      // A double-click on a tile means the Enter that walks into the folder or raises the photograph;
      // see HomeWindow for the general reason.
      MouseRemote.Attach(this, tilesList);

      _slideshow = new DispatcherTimer { Interval = SlideshowPace() };
      _slideshow.Tick += delegate
      {
        // The same command the viewer's key runs, so the timer cannot drift away from what pressing means.
        if (_pictures.IsViewing && _pictures.Slideshow)
          _pictures.NextCommand.Execute(null);
      };
      _pictures.PropertyChanged += OnPicturesChanged;
      // A raised photograph the screen has already reached before it was listening - a fast viewer, a warm
      // screen - gets its picture painted all the same.
      RefreshViewingPicture();
      // The grid of a folder belongs to the library and wears its chrome; a raised photograph belongs to the
      // television and covers it - the window is remembered on the way in because on the way out there is no
      // tree left to ask, and the chrome has to come back whatever happens to this screen.
      AttachedToVisualTree += delegate
      {
        _shell = TopLevel.GetTopLevel(this) as ShellWindow;
        SetFullBleedForViewing();
      };
      DetachedFromVisualTree += delegate
      {
        _shell?.SetViewFullBleed(false);
        _shell = null;
      };

      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (_pictures.IsViewing)
            {
              switch (args.Key)
              {
                case Key.Escape:
                case Key.Back:
                  _pictures.CloseViewerCommand.Execute(null);
                  args.Handled = true;
                  return;
                case Key.Left:
                  _pictures.PreviousCommand.Execute(null);
                  args.Handled = true;
                  return;
                case Key.Right:
                  _pictures.NextCommand.Execute(null);
                  args.Handled = true;
                  return;
                case Key.Enter:
                case Key.Space:
                  _pictures.ToggleSlideshowCommand.Execute(null);
                  args.Handled = true;
                  return;
              }
              return;
            }

            if (args.Key == Key.Escape || args.Key == Key.Back)
            {
              // Pressing back in the middle of a folder means "out of this folder", not "away from pictures", so
              // the key steps up and only the list of shares - the one place with nowhere up to go - hands the
              // key to the shell behind, which leaves the screen. The button underneath the grid does the same
              // thing for a viewer with a mouse.
              if (_pictures.CanGoUp)
              {
                _pictures.GoUpCommand.Execute(null);
                args.Handled = true;
              }
              return;
            }

            if (args.Key == Key.Enter || args.Key == Key.Return)
            {
              // Enter on a folder walks into it; Enter on a photograph raises it. The same command the tile
              // itself would run, so the keyboard and a touch cannot disagree about what "open" means.
              _pictures.OpenCommand.Execute(null);
              args.Handled = true;
            }
          },
          RoutingStrategies.Tunnel);

      // The folders are read the first time the screen appears; coming back from looking at a photograph finds
      // the grid where the viewer left it.
      _firstActivation = true;
    }

    private bool _firstActivation;

    /// <summary>
    /// Runs every time the workflow shows this screen: the first showing reads the folders, and every showing
    /// gives the tiles the focus, so the first Enter opens one - see HomeView for the general reason.
    /// </summary>
    public void OnScreenActivated()
    {
      if (_firstActivation)
      {
        _firstActivation = false;
        _pictures.RefreshCommand.Execute(null);
      }

      tilesList.Focus();
    }

    /// <summary>
    /// Stops the slideshow and lets go of the view model: the screen owns the timer, and it stops when the
    /// screen closes without anybody having to remember to cancel it.
    /// </summary>
    public void OnScreenClosed()
    {
      _slideshow.Stop();
      if (_kenBurnsTimer != null)
        _kenBurnsTimer.Stop();
      _pictures.PropertyChanged -= OnPicturesChanged;
      _pictures.Dispose();
    }

    /// <summary>
    /// How long one picture stands: the viewer's answer in playback.json when they gave one, and the
    /// shipped seven seconds otherwise - read once when the screen is built, because a slideshow that
    /// changed pace mid-run would be a different kind of surprise.
    /// </summary>
    private static TimeSpan SlideshowPace()
    {
      int seconds = Settings.PlaybackSettings.Load(null, out _).SlideshowSeconds;
      return seconds == 7 ? PicturesViewModel.SLIDESHOW_TIME : TimeSpan.FromSeconds(seconds);
    }

    /// <summary>
    /// Starts or stops the timer with the view model's say-so, which is the only place that knows whether a
    /// slideshow was asked for.
    /// </summary>
    private void OnPicturesChanged(object? sender, PropertyChangedEventArgs args)
    {
      if (args.PropertyName == nameof(PicturesViewModel.ViewingBytes))
      {
        RefreshViewingPicture();
        return;
      }

      if (args.PropertyName != nameof(PicturesViewModel.Slideshow)
          && args.PropertyName != nameof(PicturesViewModel.IsViewing))
        return;

      if (_pictures.IsViewing && _pictures.Slideshow)
      {
        if (!_slideshow.IsEnabled)
          _slideshow.Start();
      }
      else
        _slideshow.Stop();

      UpdateKenBurns();
      SetFullBleedForViewing();
    }

    // The raised photograph is painted through an image brush whose crop moves, because that is exactly how
    // the reference ran its Ken Burns effect: the animation never touched the picture, it moved the window
    // that was looked through. An ImageBrush's Viewbox is that window.
    private KenBurnsAnimator? _kenBurns;
    private Avalonia.Media.ImageBrush? _viewingBrush;
    private DispatcherTimer? _kenBurnsTimer;
    private Avalonia.Size _viewingPixels;

    /// <summary>
    /// Paint the photograph the view model is holding, or clear the surface when it holds none. Decoded
    /// through the artwork converter, so the raised picture and the tile of the same file share one decode.
    /// </summary>
    private void RefreshViewingPicture()
    {
      byte[]? bytes = _pictures.ViewingBytes;
      Avalonia.Media.Imaging.Bitmap? picture = bytes == null
          ? null
          : ArtworkConverter.Instance.Convert(bytes, typeof(Avalonia.Media.Imaging.Bitmap), null,
              System.Globalization.CultureInfo.InvariantCulture) as Avalonia.Media.Imaging.Bitmap;

      _viewingPixels = picture == null
          ? default
          : new Avalonia.Size(picture.PixelSize.Width, picture.PixelSize.Height);
      _viewingBrush = picture == null
          ? null
          : new Avalonia.Media.ImageBrush(picture)
          {
            Stretch = Avalonia.Media.Stretch.Uniform,
            ViewboxUnits = Avalonia.RelativeUnit.Relative,
            Viewbox = new Avalonia.RelativeRect(0, 0, 1, 1)
          };
      viewingSurface.Fill = _viewingBrush;

      // The reference started its animation per picture, and so here does the moving window: the new
      // photograph hangs still for a moment and then begins to drift or deepen.
      if (_viewingBrush != null)
        _viewingBrush.Viewbox = new Avalonia.RelativeRect(0, 0, 1, 1);
      if (_kenBurns != null)
        _kenBurns.Initialize(DateTime.Now);
      else
        UpdateKenBurns(); // a slideshow that started before these bytes arrived arms itself now
    }

    /// <summary>
    /// Arm or disarm the Ken Burns animation with the slideshow: it belongs to a slideshow that the viewer
    /// asked for and to the viewer's own answer in playback.json, and the picture is shown still without
    /// either - a paused slideshow shows the whole photograph, not a held frame of the drift.
    /// </summary>
    private void UpdateKenBurns()
    {
      bool running = _pictures.IsViewing && _pictures.Slideshow;

      if (!running)
      {
        if (_kenBurnsTimer != null)
          _kenBurnsTimer.Stop();
        _kenBurns = null;
        if (_viewingBrush != null)
          _viewingBrush.Viewbox = new Avalonia.RelativeRect(0, 0, 1, 1);
        return;
      }

      if (_kenBurns != null || _viewingPixels.Width < 2)
        return;

      if (!Settings.PlaybackSettings.Load(null, out _).KenBurnsEffect)
        return;

      _kenBurns = new KenBurnsAnimator { Duration = SlideshowPace().TotalSeconds };
      _kenBurnsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
      _kenBurnsTimer.Tick += delegate { StepKenBurns(); };
      _kenBurns.Initialize(DateTime.Now);
      _kenBurnsTimer.Start();
      StepKenBurns();
    }

    /// <summary>Move the window: one frame of the reference's animation, cropped straight onto the brush.</summary>
    private void StepKenBurns()
    {
      if (_kenBurns == null || _viewingBrush == null)
        return;

      double width = viewingSurface.Bounds.Width;
      double height = viewingSurface.Bounds.Height;
      if (width < 2 || height < 2 || _viewingPixels.Width < 2)
        return;

      Avalonia.Rect crop = _kenBurns.RectAt(
          DateTime.Now, _viewingPixels, new Avalonia.Size(width, height));
      _viewingBrush.Viewbox = new Avalonia.RelativeRect(crop.X, crop.Y, crop.Width, crop.Height);
    }

    /// <summary>The window this screen is currently standing in, or null between visits.</summary>
    private ShellWindow? _shell;

    /// <summary>
    /// Say to the window what the viewer is looking at: a folder full of photographs (chrome stays) or one
    /// photograph filling the television (chrome goes, until it does not).
    /// </summary>
    private void SetFullBleedForViewing()
    {
      _shell?.SetViewFullBleed(_pictures.IsViewing);
    }
  }
}