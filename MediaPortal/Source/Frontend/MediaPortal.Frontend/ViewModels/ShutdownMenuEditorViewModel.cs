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
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPortal.Frontend.Services;
using MediaPortal.Frontend.Settings;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// One line of the shutdown menu as the configuration page sees it: the reference's word for the way
  /// out, and whether the checkbox of the reference's configuration dialog stands on it.
  /// </summary>
  public partial class ShutdownMenuRow : ObservableObject
  {
    public ShutdownMenuRow(ShutdownAction action, bool shown)
    {
      Action = action;
      _isShown = shown;
    }

    /// <summary>Which way out this line is.</summary>
    public ShutdownAction Action { get; }

    /// <summary>The word the menu will write for it, in the reference's spelling.</summary>
    public string Name
    {
      get { return SleepTimerService.NameOf(Action); }
    }

    /// <summary>Whether the menu shows the line.</summary>
    [ObservableProperty]
    private bool _isShown;

    /// <summary>The second word on the row, the reference's checkbox put into letters.</summary>
    public string ShownWord
    {
      get { return IsShown ? "shown" : "hidden"; }
    }

    partial void OnIsShownChanged(bool value)
    {
      OnPropertyChanged(nameof(ShownWord));
    }
  }

  /// <summary>
  /// The shutdown menu as the viewer configures it - this front end's <c>dialogSystemStateConfiguration</c>
  /// (the reference hung it at '/Configuration/Settings/General/System/SystemStateDialog', beside its
  /// "Version" line, which is why it stands on this page): the lines of the menu in their order, each one
  /// shown or hidden and moved up or down, and the sleep timer's maximum. Like the weather's and the news'
  /// pages every answer saves itself on the spot; the reference asked for an OK press, our pages do not.
  /// </summary>
  public partial class ShutdownMenuEditorViewModel : ObservableObject
  {
    private readonly ShutdownMenuSettings _settings;
    private readonly string? _settingsPath;

    [ObservableProperty]
    private ShutdownMenuRow? _selectedRow;

    [ObservableProperty]
    private string _maxMinutesText = "";

    [ObservableProperty]
    private string _status = "";

    public ShutdownMenuEditorViewModel(string? settingsPath = null)
    {
      _settingsPath = settingsPath;
      _settings = ShutdownMenuSettings.Load(settingsPath, out string? problem);
      if (problem != null)
        Status = problem;

      foreach (ShutdownMenuItem item in _settings.Items)
        Rows.Add(new ShutdownMenuRow(item.Action, item.Visible));

      _maxMinutesText = _settings.MaxSleepTimeout.ToString();
    }

    /// <summary>The heading of the block, the reference's word for its dialog.</summary>
    public string Heading
    {
      get { return "Shutdown menu"; }
    }

    /// <summary>The lines of the menu, topmost first - the order the dialog will hang them.</summary>
    public ObservableCollection<ShutdownMenuRow> Rows { get; } = new ObservableCollection<ShutdownMenuRow>();

    /// <summary>Whether the buttons have a line to work on.</summary>
    public bool HasSelection
    {
      get { return SelectedRow != null; }
    }

    partial void OnSelectedRowChanged(ShutdownMenuRow? value)
    {
      OnPropertyChanged(nameof(HasSelection));
    }

    /// <summary>
    /// The reference's checkbox: this line shows on the menu or it does not. Saving on the spot means a
    /// viewer who walks away has already said what they meant.
    /// </summary>
    [RelayCommand]
    public void ToggleRow()
    {
      ShutdownMenuRow? row = SelectedRow;
      if (row == null)
        return;

      row.IsShown = !row.IsShown;
      Save();
    }

    /// <summary>The reference's "Move Up".</summary>
    [RelayCommand]
    public void MoveUp()
    {
      ShutdownMenuRow? row = SelectedRow;
      int at = row == null ? -1 : Rows.IndexOf(row);
      if (at <= 0)
        return;

      Rows.RemoveAt(at);
      Rows.Insert(at - 1, row!);
      Save();
    }

    /// <summary>The reference's "Move Down".</summary>
    [RelayCommand]
    public void MoveDown()
    {
      ShutdownMenuRow? row = SelectedRow;
      int at = row == null ? -1 : Rows.IndexOf(row);
      if (at < 0 || at >= Rows.Count - 1)
        return;

      Rows.RemoveAt(at);
      Rows.Insert(at + 1, row!);
      Save();
    }

    /// <summary>
    /// The reference's "Max Minutes" field: the longest wait the sleep timer may promise. Nonsense answers
    /// itself to the reference's default - a settings file nobody can read is worse than a file with a
    /// mistake in one number the reader can see.
    /// </summary>
    [RelayCommand]
    public void SaveMaximum()
    {
      if (!int.TryParse(MaxMinutesText.Trim(), out int minutes) || minutes < 1)
        minutes = ShutdownMenuSettings.DEFAULT_MAX_SLEEP_TIMEOUT;

      MaxMinutesText = minutes.ToString();
      _settings.MaxSleepTimeout = minutes;
      Save();
    }

    /// <summary>
    /// Saving the file from the page's list - order and visibility both, because the list IS the menu:
    /// its order is the dialog's order and each row's flag is the line's hiding.
    /// </summary>
    private void Save()
    {
      _settings.Items = new System.Collections.Generic.List<ShutdownMenuItem>(Rows.Count);
      foreach (ShutdownMenuRow row in Rows)
      {
        _settings.Items.Add(new ShutdownMenuItem { Action = row.Action, Visible = row.IsShown });
      }

      Status = _settings.Save(_settingsPath, out _);
    }
  }
}
