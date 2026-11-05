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
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.ViewModels;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The shutdown menu as the viewer configures it, on its own plate beside the System page: the lines
  /// in their order, each shown or hidden and moved, and the sleep timer's maximum. The answers are read
  /// from their file as the page opens - not when the hub opens - so a hand-edited
  /// <c>shutdown-menu.json</c> that was changed while the hub stood there is still what one sees, and the
  /// menu itself reads the same file again every time it is raised.
  /// </summary>
  public partial class ShutdownMenuSettingsView : UserControl
  {
    private readonly List<Control> _ring = new List<Control>();

    /// <summary>Builds the page over the file where the menu keeps itself.</summary>
    public ShutdownMenuSettingsView()
    {
      ShutdownMenuEditorViewModel model = new ShutdownMenuEditorViewModel(null);
      DataContext = model;

      InitializeComponent();

      _ring.Add(rowsList);
      _ring.Add(toggleRowButton);
      _ring.Add(moveUpButton);
      _ring.Add(moveDownButton);
      _ring.Add(maxMinutesBox);
      _ring.Add(saveMaximumButton);
      DpadNavigation.Attach(this, _ring);
    }
  }
}
