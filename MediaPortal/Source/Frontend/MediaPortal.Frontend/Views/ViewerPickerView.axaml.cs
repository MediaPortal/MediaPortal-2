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
  /// The who-is-watching screen: the server's people in one list and the three answers under it. Opening
  /// it asks the server for the names - there is nothing local to show before that - and Enter on a name
  /// takes it, which is what a remote does to a row.
  /// </summary>
  public partial class ViewerPickerView : UserControl
  {
    private readonly List<Control> _ring = new List<Control>();

    /// <summary>For the XAML loader and the workflow, which sets the real view model as DataContext.</summary>
    public ViewerPickerView()
    {
      InitializeComponent();

      _ring.Add(peopleList);
      _ring.Add(pickButton);
      _ring.Add(ownButton);
      _ring.Add(reloadButton);
      DpadNavigation.Attach(this, _ring);

      Loaded += delegate
      {
        ViewerPickerViewModel? model = DataContext as ViewerPickerViewModel;
        if (model != null && model.People.Count == 0)
          _ = model.LoadAsync();
        // The question screen takes the keyboard the way every list screen does: a remote pressing a
        // button should reach the names at once, not have to hunt for the list first.
        peopleList.Focus();
      };

      peopleList.KeyDown += OnListKeyDown;
    }

    /// <summary>The screen over its model, the way every workflow screen is built.</summary>
    public ViewerPickerView(ViewerPickerViewModel picker)
        : this()
    {
      DataContext = picker;
    }

    /// <summary>Enter on a name picks it - the one press the whole screen exists for.</summary>
    private void OnListKeyDown(object? sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter || e.Key == Key.Return)
      {
        (DataContext as ViewerPickerViewModel)?.PickSelectedCommand.Execute(null);
        e.Handled = true;
      }
    }
  }
}
