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
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.ViewModels;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The three columns a shelf is browsed through: the shelf, the grouping, the value.
  /// </summary>
  /// <remarks>
  /// What only a screen can add: the columns are walked with the arrow keys as one ring, Enter in the first two
  /// columns walks across to the next question - which is what the viewer meant by pressing it - and Enter on a
  /// value opens the titles wearing it. Back leaves the browse the way it found the library: nothing here has
  /// been changed, only counted.
  /// </remarks>
  public partial class BrowseWindow : Window
  {
    private readonly BrowseViewModel _browse;
    private readonly List<Control> _ring = new List<Control>();

    /// <summary>
    /// For the XAML loader and the designer: the screen a front end with no server shows.
    /// </summary>
    public BrowseWindow()
        : this(new BrowseViewModel())
    {
    }

    public BrowseWindow(BrowseViewModel browse, Func<HomeSection, Avalonia.Controls.Window>? openSection = null)
    {
      DataContext = browse;
      _browse = browse;
      InitializeComponent();

      // In the order they are drawn, which is the order the questions are asked.
      _ring.Add(shelvesList);
      _ring.Add(categoriesList);
      _ring.Add(valuesList);
      DpadNavigation.Attach(this, _ring);

      Opened += delegate
      {
        // The same command the arrow ring would reach by button; opening and pressing cannot drift apart.
        _browse.RefreshCommand.Execute(null);
      };

      if (openSection != null)
      {
        _browse.ValueChosen += delegate (object? sender, HomeSection section)
        {
          // The titles screen opened over this one, so Back lands the viewer on the column they picked from.
          openSection(section).Show();
        };
      }

      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Key == Key.Escape || args.Key == Key.Back)
            {
              Close();
              args.Handled = true;
              return;
            }

            if (args.Key != Key.Enter && args.Key != Key.Return)
              return;

            if (shelvesList.IsFocused)
            {
              if (categoriesList.Focus())
                args.Handled = true;
            }
            else if (categoriesList.IsFocused)
            {
              if (valuesList.Focus())
                args.Handled = true;
            }
            else if (valuesList.IsFocused)
            {
              if (_browse.OpenCommand.CanExecute(null))
              {
                _browse.OpenCommand.Execute(null);
                args.Handled = true;
              }
            }
          },
          RoutingStrategies.Tunnel);
    }
  }
}
