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
using System.Windows.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Theming;

namespace MediaPortal.Frontend.ViewModels;

/// <summary>
/// One switch of the worn skin, and the control the viewer works it with: a checkbox for a switch with
/// two answers, a button for everything that has a list of them. The row IS the control - a screen that
/// only shows what a switch answers, and asks the viewer to guess how to change it, is a wall chart and
/// not a settings screen.
/// </summary>
public sealed class SkinSettingOption : ObservableObject
{
  private readonly Func<bool> _getFlag;
  private readonly Action<bool> _setFlag;
  private readonly Func<string> _getText;
  private readonly Action _advance;
  private readonly Action<string> _setWords;
  private readonly Action<SkinSettingOption> _changed;

  /// <summary>A switch with two answers: the row wears a checkbox.</summary>
  public SkinSettingOption(string label, string description, Func<bool> getFlag, Action<bool> setFlag,
      Action<SkinSettingOption> changed)
  {
    Label = label;
    Description = description;
    IsToggle = true;
    _getFlag = getFlag;
    _setFlag = setFlag;
    _changed = changed;
  }

  /// <summary>A choice with a list of answers: the row wears a button that walks them.</summary>
  public SkinSettingOption(string label, string description, Func<string> getText, Action advance,
      Action<SkinSettingOption> changed)
  {
    Label = label;
    Description = description;
    IsToggle = false;
    _getText = getText;
    _advance = advance;
    _changed = changed;
  }

  /// <summary>
  /// An answer one types: the row wears a box. The reference had exactly this for its two date and time
  /// formats - a line to write a format into, in its configuration program - and a format has no list of
  /// answers to walk through, because it is not a choice but a sentence.
  /// </summary>
  /// <summary>
  /// A row whose answer is written rather than picked: the box, the skin's property, and the save behind it.
  /// </summary>
  /// <remarks>
  /// Built through a name rather than a fourth constructor, because a row made of "a sentence and a way of
  /// working it" is indistinguishable from one made of "a sentence and a next answer" wherever an anonymous
  /// method is handed over - and the two rows do different things with the press.
  /// </remarks>
  public static SkinSettingOption Written(string label, string description, Func<string> getText,
      Action<string> setWords, Action<SkinSettingOption> changed)
  {
    return new SkinSettingOption(label, description, getText, setWords, changed);
  }

  private SkinSettingOption(string label, string description, Func<string> getText, Action<string> setWords,
      Action<SkinSettingOption> changed)
  {
    Label = label;
    Description = description;
    IsToggle = false;
    IsText = true;
    _getText = getText;
    _setWords = setWords;
    _changed = changed;
  }

  public string Label { get; }
  public string Description { get; }

  /// <summary>Whether the row works its answer with a checkbox (true) or with a button (false).</summary>
  public bool IsToggle { get; }

  /// <summary>Whether the row's answer is written rather than picked.</summary>
  public bool IsText { get; }

  /// <summary>Whether the row wears the button that walks a list; a typed row wears a box instead.</summary>
  public bool ShowsButton => !IsToggle && !IsText;

  /// <summary>
  /// What the checkbox shows and what working it does: writing it is the change, straight through to the
  /// skin's own property and the save - the same way a configuration checkbox always worked.
  /// </summary>
  public bool IsChecked
  {
    get => _getFlag != null && _getFlag();
    set
    {
      if (_getFlag == null || _getFlag() == value)
        return;
      _setFlag(value);
      _changed(this);
    }
  }

  /// <summary>
  /// What the box holds and what the skin stores, in one movement: a typed answer is saved as it is written,
  /// exactly as a checkbox is. The reference's configuration tool kept the same bargain - its format line
  /// wrote the settings class the moment it was accepted.
  /// </summary>
  public string Words
  {
    get => _getText();
    set
    {
      string text = (value ?? "").Trim();
      if (string.Equals(_getText(), text, StringComparison.Ordinal))
        return;
      _setWords(text);
      _changed(this);
    }
  }

  /// <summary>The answer as words - what the checkbox means, and what the button shows.</summary>
  public string ValueText => _getFlag != null ? (_getFlag() ? "On" : "Off") : _getText();

  /// <summary>
  /// One press of the row: a checkbox flips, a button moves to its next answer. This is what Enter and the
  /// remote's OK do on the highlighted row; the mouse works the controls themselves.
  /// </summary>
  public void Press()
  {
    if (_getFlag != null)
      IsChecked = !_getFlag();
    else if (_advance != null)
    {
      _advance();
      _changed(this);
    }
    // A typed row has nothing to press to. It is stood on, typed into, and saved as it is typed.
  }

  /// <summary>
  /// The same press as a command, for a skin that draws the row as a button of its own: a loose template
  /// has no code-behind to catch a Click, so it binds this instead.
  /// </summary>
  private ICommand? _pressCommand;
  public ICommand PressCommand => _pressCommand ??= new PressOptionCommand(this);

  private sealed class PressOptionCommand : ICommand
  {
    private readonly SkinSettingOption _option;

    public PressOptionCommand(SkinSettingOption option) => _option = option;

    public event EventHandler? CanExecuteChanged { add { } remove { } }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _option.Press();
  }

  /// <summary>Tells the screen the answer changed, however it changed.</summary>
  public void Refresh()
  {
    OnPropertyChanged(nameof(ValueText));
    OnPropertyChanged(nameof(IsChecked));
    OnPropertyChanged(nameof(Words));
  }
}

/// <summary>
/// The skin's own settings, in the front end's own hands: the area the reference kept at
/// <c>/Appearance/SkinSettings</c> and filled per skin - a skin registers its properties, and the section
/// shows only the worn skin's, because asking a viewer about switches the skin they wear does not have is
/// how settings end up lying.
/// </summary>
/// <remarks>
/// <para>
/// Not the skin picker, and not next to it in any list of skins' colours: this is the second question the
/// settings ask. One row per switch, in the order the reference's own settings class filed them, named by
/// what it does rather than by its property name - and every row carries the control that changes it,
/// a checkbox or a button, which saves at once. The file beside <c>theme.json</c> in this viewer's own
/// folder, one per skin, so Nereus's answers and WMC's never mix.
/// </para>
/// <para>
/// The eight colour switches of Nereus are one button that walks through the eight families, because the
/// reference's colour dialog wore exactly one at a time and a list of eight checkboxes would let a viewer
/// answer "red and green" to one question. Everything else presses the reference's own property, with the
/// reference's spelling still behind the label for whoever opens the file.
/// </para>
/// <para>
/// The home menu's tiles are not here, though the reference stored them in the same class: they have
/// their own editor there and theirs will be its own screen here, not a row that shuffles guids.
/// </para>
/// </remarks>
public partial class SkinSettingsViewModel : ObservableObject
{
  private readonly SkinSettingsBase? _settings;
  private readonly string? _settingsPath;

  public SkinSettingsViewModel(Application app, string? settingsPath = null)
  {
    _settingsPath = settingsPath;

    string skinName = FrontendTheme.CurrentName(app);
    if (skinName == FrontendTheme.DEFAULT_NAME)
    {
      SkinName = "";
      Heading = "Skin settings";
      Subtitle = "This machine wears the front end's own face, which ships no switches of its own.";
      return;
    }

    SkinName = skinName;
    _settings = SkinSettingsBase.LoadFor(skinName, settingsPath, out string? problem);
    Status = problem ?? "";

    Heading = "Skin settings";
    Subtitle = _settings == null
        ? string.Format("'{0}' ships no settings of its own.", skinName)
        : string.Format("What {0} lets you change{1}. A checkbox switches, a button walks "
            + "through its answers; every change is saved at once and stays changed after the next start.",
            TitleOf(skinName),
            string.IsNullOrEmpty(SkinSettingsBase.FileOwner)
                ? " on this machine"
                : string.Format(" for '{0}' - signed in, these answers belong to the person, like the reference had them",
                    SkinSettingsBase.FileOwner));

    if (_settings != null)
      BuildRows();
  }

  public ObservableCollection<SkinSettingOption> Options { get; } = new ObservableCollection<SkinSettingOption>();

  public string SkinName { get; }
  public string Heading { get; }
  public string Subtitle { get; }

  [ObservableProperty]
  private string _status = "";

  /// <summary>The row the highlight stands on; the arrow keys and the remote move it, Enter works it.</summary>
  [ObservableProperty]
  private SkinSettingOption? _selected;

  /// <summary>Works the highlighted row - Enter and the remote's OK. The mouse needs none of this.</summary>
  public void PressSelected()
  {
    Selected?.Press();
  }

  /// <summary>
  /// What every row does after its answer changed: the skin's whole file is written, and the status line
  /// says both the new answer and where it went. A settings screen whose saves are silent is a settings
  /// screen whose saves are doubted.
  /// </summary>
  internal void Changed(SkinSettingOption option)
  {
    if (_settings == null)
      return;

    option.Refresh();
    string says = _settings.Save(_settingsPath, out bool saved);
    Status = saved
        ? string.Format("{0} is now {1}. {2}", option.Label, option.ValueText, says)
        : string.Format("{0} is now {1} for this session; saving failed - {2}",
            option.Label, option.ValueText, says);

    // The written file is not the end of it: the worn skin's colours and head line are derived from these
    // values while the palette goes on, so the television follows the answer at once rather than after the
    // next start - the colour family walks on, and a switched-off clock disappears from the head line now.
    Application? app = Application.Current;
    if (app != null)
      FrontendTheme.Reapply(app);
  }

  private void BuildRows()
  {
    SkinSettingsBase s = _settings!;

    Toggle("Fanart behind the media", "The picture behind the grid, from the library",
        () => s.EnableFanart, v => s.EnableFanart = v);
    Step("Fanart overlay opacity", "How dark the wash over that picture is",
        () => s.FanartOverlayOpacity, v => s.FanartOverlayOpacity = v, 0, 1, 0.05);

    Toggle("Watched flags in lists", "The tick on a row you have seen",
        () => s.EnableListWatchedFlags, v => s.EnableListWatchedFlags = v);
    Toggle("Watched flags in grids", "The tick on a cover you have seen",
        () => s.EnableGridWatchedFlags, v => s.EnableGridWatchedFlags = v);
    Toggle("Watched flags in cover flow", "The tick on the standing cover",
        () => s.EnableCoverWatchedFlags, v => s.EnableCoverWatchedFlags = v);

    Cycle("Movie grid", "The picture movies stand on in a grid",
        () => s.MovieGridViewType, v => s.MovieGridViewType = v);
    Cycle("Series grid", "The picture series stand on in a grid",
        () => s.SeriesGridViewType, v => s.SeriesGridViewType = v);
    Cycle("Season grid", "The picture seasons stand on in a grid",
        () => s.SeasonGridViewType, v => s.SeasonGridViewType = v);

    Toggle("Media item details view", "Open a media item in the detailed view straight away",
        () => s.EnableMediaItemDetailsView, v => s.EnableMediaItemDetailsView = v);
    Toggle("Age certification logos", "The certificate mark on items that carry one",
        () => s.EnableAgeCertificationLogos, v => s.EnableAgeCertificationLogos = v);

    // The two formats the reference kept in its skin base settings, in the same place it kept them - both
    // skins speak them, so they belong above the skins' own rows. Typed rather than picked, as its
    // configuration program had them: a format is a sentence, not a choice out of a list.
    Words("Time format", "How the clock is written, in the format letters: \"t\" is the short time of the "
        + "language you read in, \"HH:mm\" is that in twenty-four hours. A format the machine cannot write "
        + "is left as the one you had.",
        () => s.TimeFormat, v => s.TimeFormat = v);
    Words("Date format", "How the day is written: \"D\" spells it out in full, \"d\" is short, \"dd.MM.yyyy\" "
        + "is one of many you can write.",
        () => s.DateFormat, v => s.DateFormat = v);

    if (s is WMCSkinSettings wmc)
    {
      Toggle("Home content", "The panels the WMC home menu stands on",
          () => wmc.EnableHomeContent, v => wmc.EnableHomeContent = v);
      return;
    }

    NereusRows((NereusSkinSettings)s);
  }

  private void NereusRows(NereusSkinSettings n)
  {
    // The fanart group above is what both skins speak; how far that picture is thrown out of focus is this
    // face's own dial, because this face is the one that wears it across the whole screen.
    Step("Fanart blur", "How far the picture behind the media is thrown out of focus",
        () => n.FanartBlur, v => n.FanartBlur = (int)Math.Round(v), 0,
        NereusSkinSettings.FANART_BLUR_MAX, 1);

    Toggle("Grid details", "The details pane beside a grid",
        () => n.EnableGridDetails, v => n.EnableGridDetails = v);
    Toggle("Cover details", "The details beside the standing cover",
        () => n.EnableCoverDetails, v => n.EnableCoverDetails = v);
    Toggle("Touch display", "Bigger targets and touch behaviour",
        () => n.EnableTouchDisplay, v => n.EnableTouchDisplay = v);
    Toggle("Circle menu images", "Pictures on the circle menu's buttons",
        () => n.EnableCircleMenuImages, v => n.EnableCircleMenuImages = v);
    Toggle("Menu selection", "The sliding highlight on the home menu",
        () => n.EnableMenuSelection, v => n.EnableMenuSelection = v);

    Toggle("Auto scrolling", "Text that runs when it does not fit",
        () => n.EnableAutoScrolling, v => n.EnableAutoScrolling = v);
    Step("Auto scroll speed", "How fast that text runs",
        () => n.AutoScrollSpeed, v => n.AutoScrollSpeed = v, 5, 60, 5);
    Step("Auto scroll delay", "How long it waits before it starts",
        () => n.AutoScrollDelay, v => n.AutoScrollDelay = v, 0, 10, 1);
    Toggle("Loop scrolling", "Whether the running text starts again",
        () => n.EnableLoopScrolling, v => n.EnableLoopScrolling = v);

    Toggle("Header transparency", "The head line shows the screen through it",
        () => n.UseTransparency, v => n.UseTransparency = v);
    Toggle("Header without colour", "No tint on the head line at all",
        () => n.UseNoColor, v => n.UseNoColor = v);
    Toggle("Header in white", "A white head line instead of the tinted one",
        () => n.UseWhiteColor, v => n.UseWhiteColor = v);
    Toggle("Header in the accent colour", "The head line wears the skin's colour",
        () => n.UseFocusColor, v => n.UseFocusColor = v);

    Toggle("Show time", "The clock in the head line", () => n.ShowTime, v => n.ShowTime = v);
    Toggle("Show date", "The date in the head line", () => n.ShowDate, v => n.ShowDate = v);
    Toggle("Show temperature", "The temperature in the head line",
        () => n.ShowTemperature, v => n.ShowTemperature = v);
    Toggle("Show weather", "The sky's condition in the head line",
        () => n.ShowWeatherCondition, v => n.ShowWeatherCondition = v);

    Step("Dialog background opacity", "How solid a dialog's ground is",
        () => n.DialogBackgroundOpacity, v => n.DialogBackgroundOpacity = v, 0, 1, 0.05);
    Toggle("Rounded dialog corners", "Round instead of square corners",
        () => n.UseRoundedDialogCorners, v => n.UseRoundedDialogCorners = v);

    Options.Add(new SkinSettingOption("Skin colour", "The colour this skin is drawn in, one of the eight families the reference shipped",
        CurrentColourName, PressNextColour, Changed));

    Toggle("Help texts", "The one-line help at the bottom of a screen",
        () => n.EnableHelpTexts, v => n.EnableHelpTexts = v);
  }

  // The reference's eight families, in the order its dialog walked them.
  private static readonly (string Name, Action<NereusSkinSettings> Choose)[] COLOURS =
  {
    ("Turquoise", n => { n.UseTorquoise = true; n.UseYellow = false; n.UseOrange = false; n.UseRed = false;
        n.UsePurple = false; n.UseGreen = false; n.UseBlue = false; n.UseGrey = false; }),
    ("Yellow", n => { n.UseTorquoise = false; n.UseYellow = true; n.UseOrange = false; n.UseRed = false;
        n.UsePurple = false; n.UseGreen = false; n.UseBlue = false; n.UseGrey = false; }),
    ("Orange", n => { n.UseTorquoise = false; n.UseYellow = false; n.UseOrange = true; n.UseRed = false;
        n.UsePurple = false; n.UseGreen = false; n.UseBlue = false; n.UseGrey = false; }),
    ("Red", n => { n.UseTorquoise = false; n.UseYellow = false; n.UseOrange = false; n.UseRed = true;
        n.UsePurple = false; n.UseGreen = false; n.UseBlue = false; n.UseGrey = false; }),
    ("Purple", n => { n.UseTorquoise = false; n.UseYellow = false; n.UseOrange = false; n.UseRed = false;
        n.UsePurple = true; n.UseGreen = false; n.UseBlue = false; n.UseGrey = false; }),
    ("Green", n => { n.UseTorquoise = false; n.UseYellow = false; n.UseOrange = false; n.UseRed = false;
        n.UsePurple = false; n.UseGreen = true; n.UseBlue = false; n.UseGrey = false; }),
    ("Blue", n => { n.UseTorquoise = false; n.UseYellow = false; n.UseOrange = false; n.UseRed = false;
        n.UsePurple = false; n.UseGreen = false; n.UseBlue = true; n.UseGrey = false; }),
    ("Grey", n => { n.UseTorquoise = false; n.UseYellow = false; n.UseOrange = false; n.UseRed = false;
        n.UsePurple = false; n.UseGreen = false; n.UseBlue = false; n.UseGrey = true; })
  };

  private string CurrentColourName()
  {
    if (!(_settings is NereusSkinSettings n))
      return "";

    for (int i = 0; i < COLOURS.Length; i++)
    {
      NereusSkinSettings probe = new NereusSkinSettings();
      COLOURS[i].Choose(probe);
      if (Wears(n, probe))
        return COLOURS[i].Name;
    }

    return COLOURS[0].Name;
  }

  private void PressNextColour()
  {
    if (!(_settings is NereusSkinSettings n))
      return;

    string now = CurrentColourName();
    for (int i = 0; i < COLOURS.Length; i++)
    {
      if (COLOURS[i].Name == now)
      {
        COLOURS[(i + 1) % COLOURS.Length].Choose(n);
        return;
      }
    }
  }

  private static bool Wears(NereusSkinSettings s, NereusSkinSettings probe)
  {
    return s.UseTorquoise == probe.UseTorquoise && s.UseYellow == probe.UseYellow
        && s.UseOrange == probe.UseOrange && s.UseRed == probe.UseRed && s.UsePurple == probe.UsePurple
        && s.UseGreen == probe.UseGreen && s.UseBlue == probe.UseBlue && s.UseGrey == probe.UseGrey;
  }

  private void Toggle(string label, string description, Func<bool> get, Action<bool> set)
  {
    Options.Add(new SkinSettingOption(label, description, get, set, Changed));
  }

  /// <summary>An answer one types, for the settings whose value is a format rather than a choice.</summary>
  private void Words(string label, string description, Func<string> get, Action<string> set)
  {
    Options.Add(SkinSettingOption.Written(label, description, get, set, Changed));
  }

  private void Step(string label, string description, Func<double> get, Action<double> set,
      double min, double max, double step)
  {
    Options.Add(new SkinSettingOption(label, description,
        () => get().ToString("0.##", CultureInfo.InvariantCulture),
        () =>
        {
          double next = get() + step;
          if (next > max + 0.0001)
            next = min;
          set(Math.Round(next, 2));
        },
        Changed));
  }

  private void Cycle<T>(string label, string description, Func<T> get, Action<T> set) where T : struct
  {
    T[] choices = (T[])Enum.GetValues(typeof(T));
    Options.Add(new SkinSettingOption(label, description,
        () => get().ToString()!,
        () =>
        {
          int at = Array.IndexOf(choices, get());
          set(choices[(at + 1) % choices.Length]);
        },
        Changed));
  }

  private static string TitleOf(string skinName)
  {
    return skinName.Equals("wmc", StringComparison.OrdinalIgnoreCase) ? "Windows Media Center" : "Nereus";
  }
}