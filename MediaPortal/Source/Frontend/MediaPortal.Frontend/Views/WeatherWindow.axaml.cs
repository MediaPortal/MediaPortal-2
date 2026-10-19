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
  /// The weather screen: the watched places and the search that adds them on the left, the days coming in the
  /// middle, the current sky on the right.
  /// </summary>
  /// <remarks>
  /// What only a screen can add: Back means "away with the newest thing" - the search results leave before the
  /// window does, the way the title list's search behaves, because somebody who typed a place name and pressed
  /// back once expects their places list, not the home screen. Enter in the search box asks the geocoder, Enter
  /// on a match keeps it, and R reads the watched place again without leaving the keyboard.
  /// </remarks>
  public partial class WeatherWindow : Window
  {
    private readonly WeatherViewModel _weather;
    private readonly List<Control> _ring = new List<Control>();

    /// <summary>
    /// For the XAML loader and the designer: the screen a front end with nowhere to read from shows.
    /// </summary>
    public WeatherWindow()
        : this(new WeatherViewModel())
    {
    }

    public WeatherWindow(WeatherViewModel weather)
    {
      DataContext = weather;
      _weather = weather;
      InitializeComponent();

      // In the order they are drawn: the watched places, the key that takes one off the list, the search box,
      // its matches, the forecast. The current panel on the right is read, not walked.
      _ring.Add(placesList);
      _ring.Add(removeButton);
      _ring.Add(searchBox);
      _ring.Add(foundList);
      _ring.Add(forecastList);
      DpadNavigation.Attach(this, _ring);

      // A double-click on a search match means the Enter that adopts it, and on a watched place the Enter that
      // walks to its forecast; see HomeWindow for the general reason.
      MouseRemote.Attach(this, foundList, placesList);

      Opened += delegate
      {
        // The places take the focus so the first Enter of the visit does something; see HomeWindow.
        placesList.Focus();
      };

      Closed += delegate
      {
        _weather.Dispose();
      };

      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Key == Key.R && !searchBox.IsFocused)
            {
              // R for re-read: the same press as opening the place again, no mouse needed.
              _weather.ReadCommand.Execute(null);
              args.Handled = true;
              return;
            }

            if (args.Key == Key.Escape || args.Key == Key.Back)
            {
              if (searchBox.IsFocused || _weather.Found.Count > 0)
              {
                // Back leaves the search before it leaves the screen.
                _weather.Found.Clear();
                _weather.SearchText = "";
                placesList.Focus();
                args.Handled = true;
                return;
              }

              Close();
              args.Handled = true;
              return;
            }

            if (args.Key != Key.Enter && args.Key != Key.Return)
              return;

            if (searchBox.IsFocused)
            {
              _weather.SearchCommand.Execute(null);
              if (foundList.Focus())
                args.Handled = true;
            }
            else if (foundList.IsKeyboardFocusWithin)
            {
              // Enter on a match is the wish to live under it: the place is kept, saved, and read.
              _weather.AddSelectedFoundCommand.Execute(null);
              if (placesList.Focus())
                args.Handled = true;
            }
            else if (placesList.IsKeyboardFocusWithin)
            {
              if (forecastList.Focus())
                args.Handled = true;
            }
          },
          RoutingStrategies.Tunnel);
    }
  }
}
