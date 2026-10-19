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

      _slideshow = new DispatcherTimer { Interval = PicturesViewModel.SLIDESHOW_TIME };
      _slideshow.Tick += delegate
      {
        // The same command the viewer's key runs, so the timer cannot drift away from what pressing means.
        if (_pictures.IsViewing && _pictures.Slideshow)
          _pictures.NextCommand.Execute(null);
      };
      _pictures.PropertyChanged += OnPicturesChanged;

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
      _pictures.PropertyChanged -= OnPicturesChanged;
      _pictures.Dispose();
    }

    /// <summary>
    /// Starts or stops the timer with the view model's say-so, which is the only place that knows whether a
    /// slideshow was asked for.
    /// </summary>
    private void OnPicturesChanged(object? sender, PropertyChangedEventArgs args)
    {
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

      SetFullBleedForViewing();
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