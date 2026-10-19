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
using MediaPortal.Frontend.Workflow;
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
  public partial class ThemeView : UserControl, IWorkflowScreen
  {
    private readonly List<Control> _ring = new List<Control>();

    /// <summary>
    /// For the XAML loader and the designer: the screen pointed at the running application.
    /// </summary>
    public ThemeView()
        : this(Avalonia.Application.Current!)
    {
    }

    public ThemeView(Application app, string? settingsPath = null)
        : this(new ThemeViewModel(app, settingsPath))
    {
    }

    public ThemeView(ThemeViewModel themes)
    {
      DataContext = themes;
      InitializeComponent();

      _ring.Add(themesList);
      DpadNavigation.Attach(this, _ring);

      // The list opens focused so the arrow keys and the screen's own colour logic agree from the first
      // moment; a single click already picks a theme, so the mouse needs no second path here.  Back is the
      // shell's key now: this screen has no claim on it, and the workflow pops whatever opened this screen.
    }

    /// <summary>
    /// Runs every time the workflow shows this screen: the first showing loads, every showing takes
    /// the focus.  See IWorkflowScreen.
    /// </summary>
    public void OnScreenActivated()
    {
 themesList.Focus();
    }
  }
}
