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
  /// The client's own settings as a screen: a name box, three working rows, and the about-block at the
  /// foot. Built for the settings hub the way the skin settings screen is - the hub embeds it, the arrows
  /// walk it, and every answer saves where it is answered.
  /// </summary>
  public partial class ClientSettingsView : UserControl
  {
    private readonly List<Control> _ring = new List<Control>();
    private readonly ClientSettingsViewModel? _model;

    /// <summary>For the XAML loader and the designer.</summary>
    public ClientSettingsView()
        : this(new ClientSettingsViewModel())
    {
    }

    public ClientSettingsView(ClientSettingsViewModel model)
    {
      _model = model;
      DataContext = model;
      InitializeComponent();

      // The name box first, then the list: the arrows walk from the typed answer into the chosen ones.
      _ring.Add(clientNameBox);
      _ring.Add(settingsList);
      DpadNavigation.Attach(this, _ring);

      settingsList.KeyDown += OnListKeyDown;
    }

    private void OnListKeyDown(object? sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter || e.Key == Key.Return)
      {
        _model?.PressSelected();
        e.Handled = true;
      }
    }

    /// <summary>A choice row's button: the click lands on the row's own option, and the option saves itself.</summary>
    private void OnChoiceClick(object? sender, RoutedEventArgs e)
    {
      if (sender is Button { DataContext: SkinSettingOption option })
        option.Press();
    }
  }
}
