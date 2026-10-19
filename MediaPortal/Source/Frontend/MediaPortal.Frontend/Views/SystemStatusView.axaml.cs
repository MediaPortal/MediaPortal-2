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
  /// The system status as a screen: the lines on a list the arrows can walk and one press that makes them
  /// true again. Built for the settings hub the way the other screens are - the hub embeds it and hands the
  /// view model over as DataContext.
  /// </summary>
  public partial class SystemStatusView : UserControl
  {
    private readonly List<Control> _ring = new List<Control>();

    /// <summary>For the XAML loader, the designer and the hub, which sets the real view model as DataContext.</summary>
    public SystemStatusView()
    {
      InitializeComponent();

      _ring.Add(linesList);
      _ring.Add(reloadButton);
      DpadNavigation.Attach(this, _ring);

      // Opening the section reads the lines, so the screen never shows a start-up's answers as if they were
      // now's. The model is read from the DataContext at that moment because it is the hub that hands it over.
      Loaded += delegate
      {
        SystemStatusViewModel? model = DataContext as SystemStatusViewModel;
        if (model != null && model.Lines.Count == 0)
          _ = model.ReloadAsync();
      };
    }
  }
}
