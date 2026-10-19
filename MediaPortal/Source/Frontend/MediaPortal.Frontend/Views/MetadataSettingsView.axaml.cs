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

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The extractors' settings, read from the server. The rows are the server's answer; this screen only
  /// hands them the two buttons.
  /// </summary>
  public partial class MetadataSettingsView : UserControl
  {
    public MetadataSettingsView()
    {
      InitializeComponent();

      // The walkable ring: the fields first, then the two buttons - reload left of save, the order of work.
      _ring.Add(rowsList);
      _ring.Add(reloadButton);
      _ring.Add(saveButton);
      DpadNavigation.Attach(this, _ring);
    }

    private readonly List<Control> _ring = new List<Control>();
  }
}
