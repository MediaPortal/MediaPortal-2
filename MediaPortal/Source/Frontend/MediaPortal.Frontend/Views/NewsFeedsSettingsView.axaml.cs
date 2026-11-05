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
  /// The news feeds as a settings page: the viewer's own feeds, the typing that adds one, and the rhythm
  /// the news screen asks on. The feeds are read from their file as the page opens - not when the hub opens
  /// - so a hand-edited <c>news-feeds.json</c> that was changed while the hub stood there is still what
  /// one sees.
  /// </summary>
  public partial class NewsFeedsSettingsView : UserControl
  {
    private readonly List<Control> _ring = new List<Control>();

    /// <summary>Builds the page over the file where the feeds keep themselves.</summary>
    public NewsFeedsSettingsView()
    {
      NewsFeedsEditorViewModel model = new NewsFeedsEditorViewModel(null);
      DataContext = model;

      InitializeComponent();

      _ring.Add(feedsList);
      _ring.Add(removeButton);
      _ring.Add(nameBox);
      _ring.Add(urlBox);
      _ring.Add(addButton);
      _ring.Add(refreshBox);
      DpadNavigation.Attach(this, _ring);
    }
  }
}
