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

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using MediaPortal.Frontend.Input;
using MediaPortal.Frontend.ViewModels;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The plans and the recordings, as one list and one band of words beside it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The screen only draws what the model says and hands presses back to it, which is how every screen here works;
  /// what is worth saying is why the four presses are four buttons rather than the dialog the reference put up. Its
  /// guide asked "Choose program action..." and offered a list because the same OK key had to serve a programme
  /// that was running, one that was coming up, and one that was already over. This screen has a row to stand on and
  /// a state written under its name, so the questions that are worth answering can each have a place of their own -
  /// and the one that is never asked twice in a row ("are you sure?") is not asked at all, because every answer a
  /// viewer gives here the backend can be asked again about.
  /// </para>
  /// <para>
  /// The list is what takes the focus, and the D-pad walks out of it to the switch above and the buttons beside it.
  /// A remote's left from a row therefore leaves the row selected rather than leaving the screen, which is what
  /// Back is for.
  /// </para>
  /// </remarks>
  public partial class TvSchedulesView : UserControl
  {
    private readonly List<Control> _ring = new List<Control>();
    private readonly TvSchedulesViewModel _model;

    /// <summary>For the XAML loader and the designer, which have no backend to read and see an empty screen.</summary>
    public TvSchedulesView()
        : this(new TvSchedulesViewModel())
    {
    }

    /// <summary>
    /// Builds the screen over a model that already knows which table it is to read. The model is handed in rather
    /// than built here because the application is the one that knows whether this screen was opened for the plans
    /// or for the recordings.
    /// </summary>
    public TvSchedulesView(TvSchedulesViewModel model)
    {
      _model = model;
      DataContext = model;
      InitializeComponent();

      reloadButton.Click += delegate { Fire(_model.ReloadAsync); };
      stopButton.Click += delegate { Fire(_model.StopAsync); };
      deleteButton.Click += delegate { Fire(_model.DeleteAsync); };
      renewButton.Click += delegate { Fire(_model.RenewSeriesAsync); };

      _ring.Add(tablePicker);
      _ring.Add(reloadButton);
      _ring.Add(entryList);
      _ring.Add(seriesModeBox);
      _ring.Add(stopButton);
      _ring.Add(renewButton);
      _ring.Add(deleteButton);
      DpadNavigation.Attach(this, _ring);

      // The model outlives no screen. A question still out when the viewer walks away has nowhere to put its
      // answer, and a screen that is not on the television has no business going on asking after it.
      DetachedFromVisualTree += delegate { model.Dispose(); };
    }

    /// <summary>
    /// Asks a question and forgets about the answer, which the model has already written into its own line. A press
    /// that waited here would hold the screen still while the backend thought, and a button that could not be
    /// pressed twice is a button a viewer presses twice anyway.
    /// </summary>
    private static void Fire(Func<System.Threading.Tasks.Task> ask)
    {
      _ = ask();
    }
  }
}
