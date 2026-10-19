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
using System.Globalization;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Theming;

namespace MediaPortal.Frontend.ViewModels;

/// <summary>
/// This machine's own answers, in the reference's shape: a name, a wake-up screen, a window or a
/// television, and a language for its dates - plus the "about this box" words the reference's system
/// status screen carried, kept as a quiet block at the foot of the same section rather than as a dialog
/// of its own until dialogs exist (B5).
/// </summary>
/// <remarks>
/// Every row saves at once and says so in the status line, which is the rule the skin settings screen
/// set; the difference is <see cref="ClientNameText"/>, the one row whose answer is a word rather than a
/// choice, worked with the keyboard until the skin's own keyboard dialog arrives with B5. Writing a
/// setting also speaks through <see cref="ClientSettings.Changed"/>, so the window's full screen answer
/// and the head line's calendar follow the keystroke rather than the restart.
/// </remarks>
public partial class ClientSettingsViewModel : ObservableObject
{
  private readonly ClientSettings _settings;
  private readonly string? _settingsPath;

  public ClientSettingsViewModel(string? settingsPath = null)
  {
    _settingsPath = settingsPath;
    _settings = ClientSettings.Load(settingsPath, out string? problem);
    Status = problem ?? "";
    _clientNameText = _settings.ClientName;

    Options.Add(new SkinSettingOption("Full screen", "Fill the television, or sit in a window on the desktop",
        () => _settings.FullScreen, v => _settings.FullScreen = v, Changed));

    Options.Add(new SkinSettingOption("Start screen", "What this machine opens when it wakes",
        StartScreenText, AdvanceStartScreen, Changed));

    Options.Add(new SkinSettingOption("Language", "The language of dates, numbers and weather words",
        LanguageText, AdvanceLanguage, Changed));
  }

  public ObservableCollection<SkinSettingOption> Options { get; } = new ObservableCollection<SkinSettingOption>();

  public string Heading => "This machine";

  public string Subtitle =>
      "What the front end on this box was told about itself - its name, its window, its wake-up and its "
      + "calendar. Everything here is saved at once and belongs to this machine only; the library's own "
      + "answers live on the server.";

  [ObservableProperty]
  private string _status;

  /// <summary>
  /// The name row, as the only answer on this screen that is a word and not a choice. Typed answers save
  /// when they are typed (the box's own binding); the save line says so like every other row.
  /// </summary>
  [ObservableProperty]
  private string _clientNameText;

  partial void OnClientNameTextChanged(string value)
  {
    if (string.Equals(_settings.ClientName, value, StringComparison.Ordinal))
      return;

    _settings.ClientName = value;
    string says = _settings.Save(_settingsPath, out bool saved);
    Status = saved
        ? string.Format("This machine is called '{0}'. {1}", value, says)
        : string.Format("'{0}' for this session; saving failed - {1}", value, says);
  }

  /// <summary>The row the highlight stands on; Enter and the remote's OK work it like its control.</summary>
  [ObservableProperty]
  private SkinSettingOption? _selected;

  public void PressSelected()
  {
    Selected?.Press();
  }

  // The two button rows. Their answers are saved in the same breath they change, exactly like the skin
  // settings' rows - and the save's own sentence lands in the status line.
  private string StartScreenText()
  {
    if (_settings.StartScreen == ClientSettings.START_LAST_SHELF && _settings.LastShelf.Length == 0)
      return ClientSettings.START_LAST_SHELF + " (nothing remembered yet)";

    return _settings.StartScreen;
  }

  private void AdvanceStartScreen()
  {
    _settings.StartScreen = _settings.StartScreen == ClientSettings.START_HOME
        ? ClientSettings.START_LAST_SHELF
        : ClientSettings.START_HOME;
  }

  private string LanguageText()
  {
    foreach ((string code, string label) in ClientSettings.LANGUAGES)
      if (string.Equals(code, _settings.Culture, StringComparison.OrdinalIgnoreCase))
        return label;

    return _settings.Culture;
  }

  private void AdvanceLanguage()
  {
    for (int i = 0; i < ClientSettings.LANGUAGES.Length; i++)
    {
      if (string.Equals(ClientSettings.LANGUAGES[i].Code, _settings.Culture, StringComparison.OrdinalIgnoreCase))
      {
        _settings.Culture = ClientSettings.LANGUAGES[(i + 1) % ClientSettings.LANGUAGES.Length].Code;
        return;
      }
    }

    _settings.Culture = ClientSettings.LANGUAGES[0].Code;
  }

  /// <summary>
  /// The about-block the reference's system status screen carried, in the order a viewer reads it: what
  /// this box calls itself, what it wears, what it talks to, and which version is answering.
  /// </summary>
  public string SystemInfo
  {
    get
    {
      string version = typeof(ClientSettingsViewModel).Assembly.GetName().Version?.ToString() ?? "unknown";
      string skin = FrontendTheme.CurrentName(Avalonia.Application.Current!);
      CultureInfo calendar = ClientSettings.UiCulture(_settings.Culture);
      string today = DateTime.Now.ToString("dddd, dd MMMM yyyy", calendar.DateTimeFormat);

      return string.Format("{0} · front end {1} · wearing {2} · calendar '{3}' · today: {4}",
          _settings.ClientName, version, skin, calendar.Name, today);
    }
  }

  /// <summary>What every row does after its answer changed: save, say it, and let the window follow.</summary>
  internal void Changed(SkinSettingOption option)
  {
    option.Refresh();
    string says = _settings.Save(_settingsPath, out bool saved);
    Status = saved
        ? string.Format("{0} is now {1}. {2}", option.Label, option.ValueText, says)
        : string.Format("{0} is now {1} for this session; saving failed - {2}",
            option.Label, option.ValueText, says);
    OnPropertyChanged(nameof(SystemInfo));
  }
}
