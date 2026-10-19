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
  /// The server's share credentials as a screen: the dictionary of paths on the left, the editor beside it,
  /// the arrows walking from the list through the boxes to the presses. Built for the settings hub the way
  /// the other server screens are - the hub embeds it, and every change is written to the server where it is
  /// answered.
  /// </summary>
  public partial class NetworkCredentialsView : UserControl
  {
    private readonly List<Control> _ring = new List<Control>();

    /// <summary>For the XAML loader, the designer and the hub, which sets the real view model as DataContext.</summary>
    public NetworkCredentialsView()
    {
      InitializeComponent();

      _ring.Add(credentialsList);
      _ring.Add(pathBox);
      _ring.Add(userBox);
      _ring.Add(domainBox);
      _ring.Add(passwordBox);
      _ring.Add(storeButton);
      _ring.Add(removeButton);
      _ring.Add(removeAllButton);
      _ring.Add(reloadButton);
      DpadNavigation.Attach(this, _ring);

      // Opening the section reads the dictionary, exactly like the users row asks when it is opened - there
      // is nothing local to show before that. The model is read from the DataContext at that moment because
      // it is the hub that hands it over.
      Loaded += delegate
      {
        NetworkCredentialsViewModel? model = DataContext as NetworkCredentialsViewModel;
        if (model != null && model.Credentials.Count == 0)
          _ = model.LoadAsync();
      };

      credentialsList.KeyDown += OnListKeyDown;
    }

    /// <summary>Enter on an entry works the row it stands on; on a button it presses it, which the ring does.</summary>
    private void OnListKeyDown(object? sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter || e.Key == Key.Return)
      {
        NetworkCredentialsViewModel? model = DataContext as NetworkCredentialsViewModel;
        if (credentialsList.SelectedItem == null)
          _ = model?.LoadAsync();
        e.Handled = true;
      }
    }
  }
}
