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
  /// The worn skin's own settings as a list of switchable rows: a checkbox per two-answer switch, a button
  /// per choice. The hub embeds this screen the way it embeds the folders and the server's answers; nothing
  /// here reaches past the skin it was opened for.
  /// </summary>
  /// <remarks>
  /// The mouse works the controls themselves - the checkbox's own binding and the button's click - and the
  /// remote needs no control under its highlight: Enter works the selected row through the same door. There
  /// is deliberately no click-anywhere-on-the-row handler; the checkbox would fight it, flipping once for
  /// the row and once for itself.
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

      // Enter and the remote's OK work the highlighted row; Back stays the shell's key - the hub behind
      // this screen keeps it.
      settingsList.KeyDown += OnListKeyDown;
    }

    private void OnListKeyDown(object? sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter || e.Key == Key.Return)
      {
        _model?.PressSelected();
        e.Handled = true;
      }
    }

    /// <summary>A choice row's button: the click lands on the row's own option, and the option saves itself.</summary>
    private void OnChoiceClick(object? sender, RoutedEventArgs e)
    {
      if (sender is Button { DataContext: SkinSettingOption option })
        option.Press();
    }
  }
}