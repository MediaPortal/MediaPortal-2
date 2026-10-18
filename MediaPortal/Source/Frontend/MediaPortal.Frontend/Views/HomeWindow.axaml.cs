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

using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.ViewModels;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The screen that shows the server and its media aspects.
  /// </summary>
  public partial class HomeWindow : Window
  {
    /// <summary>
    /// For the XAML loader and the designer: a screen with a view model that has no server, which is what it
    /// says on screen.
    /// </summary>
    public HomeWindow()
        : this(new HomeViewModel())
    {
    }

    public HomeWindow(HomeViewModel home)
    {
      DataContext = home;
      InitializeComponent();

      // The ring is in the order the areas are arranged: the button on top, the list below it. Left/right (and
      // up/down between the two areas) walk it; the list keeps its own up/down over its items, because it
      // handles those keys before they reach the ring.
      DpadNavigation.Attach(this, new List<Control> { refreshButton, aspectsList });

      // The screen fills itself in when it appears: a home screen that waits for someone to press "Refresh" shows
      // an empty list in front of a server that is answering perfectly, which is the worse of the two answers.
      // The command is the one the button on the screen uses, so opening and pressing run the same code.
      Opened += delegate
      {
        if (DataContext is HomeViewModel viewModel)
          viewModel.RefreshCommand.Execute(null);
      };

      // The back key of a remote closes the screen. There is nowhere else to go yet; when the front end has
      // deeper screens this becomes "go up one level".
      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Key == Key.Escape || args.Key == Key.Back)
              Close();
          },
          RoutingStrategies.Tunnel);
    }
  }
}
