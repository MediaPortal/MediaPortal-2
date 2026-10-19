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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.ViewModels;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The front end's settings screen: the themes, each wearing its own colours, the one it wears marked.
  /// </summary>
  /// <remarks>
  /// There is exactly one control on this screen, on purpose: the list. Selecting a row is the whole interaction
  /// - it repaints the screen itself along with everything behind it - so the d-pad ring is a single control and
  /// Back is the only other key the screen answers.
  /// </remarks>
  public partial class ThemeWindow : Window
  {
    private readonly List<Control> _ring = new List<Control>();

    /// <summary>
    /// For the XAML loader and the designer: the screen pointed at the running application.
    /// </summary>
    public ThemeWindow()
        : this(Avalonia.Application.Current!)
    {
    }

    public ThemeWindow(Application app, string? settingsPath = null)
        : this(new ThemeViewModel(app, settingsPath))
    {
    }

    public ThemeWindow(ThemeViewModel themes)
    {
      DataContext = themes;
      InitializeComponent();

      _ring.Add(themesList);
      DpadNavigation.Attach(this, _ring);

      AddHandler(KeyDownEvent,
          delegate (object? sender, KeyEventArgs args)
          {
            if (args.Key == Key.Escape || args.Key == Key.Back)
            {
              Close();
              args.Handled = true;
            }
          },
          RoutingStrategies.Tunnel);
    }
  }
}
