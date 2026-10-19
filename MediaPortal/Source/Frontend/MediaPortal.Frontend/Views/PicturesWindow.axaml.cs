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
  public partial class PicturesWindow : Window
  {
    private readonly PicturesViewModel _pictures;
    private readonly List<Control> _ring = new List<Control>();
    private readonly DispatcherTimer _slideshow;

    /// <summary>
    /// For the XAML loader and the designer: the screen a front end with no server shows.
    /// </summary>
    public PicturesWindow()
        : this(new PicturesViewModel())
    {
    }

    public PicturesWindow(PicturesViewModel pictures)
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

      Opened += delegate
      {
        _pictures.RefreshCommand.Execute(null);

        // The tiles take the focus so the first Enter opens one; see HomeWindow for the general reason.
        tilesList.Focus();
      };
      Closed += delegate
      {
        _slideshow.Stop();
        _pictures.PropertyChanged -= OnPicturesChanged;
        _pictures.Dispose();
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
              // the key steps up and only the list of shares - the one place with nowhere up to go - lets it
              // leave the screen. The button underneath the grid does the same thing for a viewer with a mouse.
              if (_pictures.CanGoUp)
                _pictures.GoUpCommand.Execute(null);
              else
                Close();
              args.Handled = true;
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
    }
  }
}