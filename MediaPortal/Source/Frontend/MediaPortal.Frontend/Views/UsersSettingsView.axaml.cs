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
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.ViewModels;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The server's users as a screen: the list of names, the editor beside it, the arrows walking from the
  /// list through the boxes to the presses. Built for the settings hub the way the other server screens
  /// are - the hub embeds it, and every change is written to the server where it is answered.
  /// </summary>
  public partial class UsersSettingsView : UserControl
  {
    private readonly List<Control> _ring = new List<Control>();

    /// <summary>For the XAML loader, the designer and the hub, which sets the real view model as DataContext.</summary>
    public UsersSettingsView()
    {
      InitializeComponent();

      _ring.Add(usersList);
      _ring.Add(nameBox);
      _ring.Add(passwordBox);
      _ring.Add(addButton);
      _ring.Add(saveButton);
      _ring.Add(removePasswordButton);
      _ring.Add(removeButton);
      _ring.Add(reloadButton);
      DpadNavigation.Attach(this, _ring);

      // Opening the section reads the list, exactly like the server-settings row asks when it is opened -
      // there is nothing local to show before that. The model is read from the DataContext at that moment
      // because it is the hub that hands it over.
      Loaded += delegate
      {
        UsersSettingsViewModel? model = DataContext as UsersSettingsViewModel;
        if (model != null && model.Users.Count == 0)
          _ = model.LoadAsync();
      };

      usersList.KeyDown += OnListKeyDown;
    }

    /// <summary>Enter on a name works the row it stands on; on a button it presses it, which the ring does.</summary>
    private void OnListKeyDown(object? sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter || e.Key == Key.Return)
      {
        UsersSettingsViewModel? model = DataContext as UsersSettingsViewModel;
        if (usersList.SelectedItem == null)
          _ = model?.LoadAsync();
        e.Handled = true;
      }
    }
  }
}
