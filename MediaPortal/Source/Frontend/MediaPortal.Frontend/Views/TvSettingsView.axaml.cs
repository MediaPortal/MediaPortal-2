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
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.ViewModels;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The television backend's address as a screen, and next to it the twelve answers the guide and the zap buttons
  /// ask with: five boxes for where the set-top box is, three boxes and eight switches for what to do with it, two
  /// presses, and the answer underneath. The answers are read from the file as the screen opens - not when the hub
  /// opens - so a hand-edited <c>tv.json</c> that was changed while the hub stood there is still what one sees.
  /// </summary>
  public partial class TvSettingsView : UserControl
  {
    private readonly List<Control> _ring = new List<Control>();
    private readonly TvSettingsViewModel? _model;

    /// <summary>For the XAML loader and the designer.</summary>
    public TvSettingsView()
        : this(null)
    {
    }

    /// <summary>
    /// Builds the screen. The answers may be left out, which is the same as saying "read them from where
    /// they are kept, and say what that reading had to complain about".
    /// </summary>
    public TvSettingsView(TvBackendSettings? settings)
    {
      // The screen hands its model the way onto this thread: what "ask it" brings back arrives on a thread of the
      // network stack's own, and the sentence it is said in belongs to the screen.
      TvSettingsViewModel model = new TvSettingsViewModel(null, ScreenThread.Run);
      _model = model;
      DataContext = model;

      // Read before the controls are built, so that a box is built with the answer it is going to show rather than
      // with an empty box that has to be corrected afterwards. The correction would work - the model says so out
      // loud - but a screen that opens wrong and fixes itself a moment later is a screen a viewer catches lying.
      string? problem = null;
      TvBackendSettings shown = settings ?? TvBackendSettings.Load(null, out problem);
      model.Show(shown, problem);

      InitializeComponent();

      _ring.Add(hostBox);
      _ring.Add(portBox);
      _ring.Add(userBox);
      _ring.Add(passwordBox);
      _ring.Add(daysBox);
      _ring.Add(demoCheck);
      _ring.Add(hoursBox);
      _ring.Add(rowsBox);
      _ring.Add(zapBox);
      _ring.Add(namesCheck);
      _ring.Add(numbersCheck);
      _ring.Add(genreCheck);
      _ring.Add(seriesCheck);
      _ring.Add(zapGuideCheck);
      _ring.Add(zapIndexCheck);
      _ring.Add(autoStartCheck);
      _ring.Add(hideAllCheck);
      _ring.Add(checkButton);
      _ring.Add(saveButton);
      DpadNavigation.Attach(this, _ring);
    }
  }
}
