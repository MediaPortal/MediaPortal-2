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
using MediaPortal.Frontend.ViewModels;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// The worn skin's own settings as a list of its switches. The hub embeds this screen the way it embeds
  /// the folders and the server's answers; nothing here reaches past the skin it was opened for.
  /// </summary>
  /// <remarks>
  /// One control on the ring, the list, same as the picker. What differs is what a press means: the picker's
  /// rows are whole choices, so its selection applies, while these rows are switches and only a press of
  /// Enter, the remote's OK, or a click flips one - see SkinSettingsViewModel.
  /// </remarks>
  public partial class SkinSettingsView : UserControl
  {
    private readonly List<Control> _ring = new List<Control>();
    private SkinSettingsViewModel? _model;

    /// <summary>For the XAML loader and the designer: the screen pointed at the running application.</summary>
    public SkinSettingsView()
        : this(Avalonia.Application.Current!)
    {
    }

    public SkinSettingsView(Application app, string? settingsPath = null)
        : this(new SkinSettingsViewModel(app, settingsPath))
    {
    }

    public SkinSettingsView(SkinSettingsViewModel model)
    {
      _model = model;
      DataContext = model;
      InitializeComponent();

      _ring.Add(settingsList);
      DpadNavigation.Attach(this, _ring);

      // A press, not a move, is the change: Enter and the remote's OK apply the highlighted row, and a click
      // on a row applies that row. Back stays the shell's key - the hub behind this screen keeps it.
      settingsList.KeyDown += OnListKeyDown;
      settingsList.PointerPressed += OnListPressed;
    }

    private void OnListKeyDown(object? sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter || e.Key == Key.Return || e.Key == Key.Space)
      {
        _model?.PressSelected();
        e.Handled = true;
      }
    }

    private void OnListPressed(object? sender, PointerPressedEventArgs e)
    {
      // The click lands on a row the platform has already highlighted, so the model's own row answers it.
      _model?.PressSelected();
    }
  }
}