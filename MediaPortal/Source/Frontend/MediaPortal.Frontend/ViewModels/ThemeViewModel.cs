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
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Theming;

namespace MediaPortal.Frontend.ViewModels;

/// <summary>
/// One theme on the settings screen: the name, the sentence that says what picking it would do, and the three
/// colours the eye actually wants to see before choosing - the accent, a panel, and the text on the panel.
/// </summary>
public partial class ThemeChoice : ObservableObject
{
  public ThemeChoice(string name, string description)
  {
    Name = name;
    Description = description;
  }

  public string Name { get; }
  public string Description { get; }

  public IBrush AccentBrush => FrontendTheme.Brush(Name, "AccentBrush");
  public IBrush PanelBrush => FrontendTheme.Brush(Name, "PanelBrush");
  public IBrush TextBrush => FrontendTheme.Brush(Name, "TextBrush");

  /// <summary>Whether this is the theme the application is wearing right now.</summary>
  [ObservableProperty]
  private bool _isCurrent;
}

/// <summary>
/// The settings screen of the front end itself, which is one question: which colours does it wear.
/// </summary>
/// <remarks>
/// <para>
/// Picking a theme <em>is</em> the setting. There is no Apply button, because a screen that repaints the moment
/// a row is selected and only asks for confirmation of something already visible would be theatre: the change
/// lands on the screen the viewer is looking at, and pressing Back leaves them wearing it. That is also why the
/// choice is saved the moment it is made - a preview you lose by leaving is a preview, not a setting.
/// </para>
/// <para>
/// A worn skin may ship more colours than the one it stands in by default, and the reference kept exactly that
/// as a setting of the skin. So the screen asks its one question in two places: the list of skins, and - only
/// while a skin that ships them is worn - a second list of that skin's own colour themes, applying the full
/// 'skin-theme' name. WMC answers with Dark and Turquoise; Nereus shipped one theme file and so adds no rows.
/// </para>
/// <para>
/// The saved name is read again at the next start by <see cref="FrontendTheme.SelectedName"/>, behind the
/// command line and the environment: whoever typed <c>--theme</c> meant it for that run and outranks a choice
/// made earlier on the couch, but the couch choice outranks the factory colours. Saving is best-effort and its
/// outcome is said on the status line either way; a theme that repaints but cannot be saved has still done the
/// viewer the favour it could.
/// </para>
/// <para>
/// A name the theme list no longer knows - a file hand-edited to a theme that was since removed - is not this
/// screen's problem to hide: <see cref="FrontendTheme.Apply"/> says that sentence out loud and changes nothing,
/// and the screen repeats it and steps the selection back to what is actually being worn.
/// </para>
/// </remarks>
public partial class ThemeViewModel : ObservableObject
{
  private readonly Application _app;
  private readonly string? _settingsPath;
  private bool _applying;

  public ThemeViewModel(Application app, string? settingsPath = null)
  {
    _app = app;
    _settingsPath = settingsPath;

    foreach (string name in FrontendTheme.Names)
      Themes.Add(new ThemeChoice(name, DescriptionOf(name)));

    string wearing = FrontendTheme.CurrentName(_app);
    Mark(wearing);
    _applying = true;
    Selected = Find(FrontendTheme.BasePaletteOf(wearing)) ?? Themes[0];
    RefreshColours(wearing, true);
    _applying = false;

    Heading = "Which skin this machine wears. Picking a row changes it right here, and it stays worn after the next start.";
    Status = "Wearing " + wearing + ".";
  }

  public ObservableCollection<ThemeChoice> Themes { get; } = new ObservableCollection<ThemeChoice>();

  /// <summary>
  /// The colours the worn skin ships besides its own default - empty for a skin that ships none, which is what
  /// keeps the second section off the screen rather than showing a heading over nothing.
  /// </summary>
  public ObservableCollection<ThemeChoice> Colours { get; } = new ObservableCollection<ThemeChoice>();

  public bool HasColours => Colours.Count > 0;

  public string ColourHeading { get; private set; } = "";

  public string Heading { get; }

  [ObservableProperty]
  private string _status = "";

  /// <summary>
  /// The picked row. Selecting is applying and saving at once; a refusal is said and the selection steps back
  /// to what is really being worn.
  /// </summary>
  [ObservableProperty]
  private ThemeChoice? _selected;

  /// <summary>
  /// The picked colour of the worn skin. Null while the skin stands in its own default colours, which is what
  /// the absence of a marked colour row says.
  /// </summary>
  [ObservableProperty]
  private ThemeChoice? _selectedColour;

  partial void OnSelectedChanged(ThemeChoice? value)
  {
    // The step-back below writes Selected too, and the list's own binding re-pushes the row it found as it
    // opens; neither is a choice. A row selected that already names what is worn asks for nothing - it must
    // not save, or merely opening the screen would promote this run's --theme over the viewer's saved pick.
    if (_applying || value == null
        || string.Equals(value.Name, FrontendTheme.BasePaletteOf(FrontendTheme.CurrentName(_app)),
            StringComparison.OrdinalIgnoreCase))
      return;

    ApplyAndSave(value.Name);
  }

  partial void OnSelectedColourChanged(ThemeChoice? value)
  {
    // Rebuilding the colour rows moves this property too, and no rebuild is a choice.
    if (_applying || value == null
        || string.Equals(value.Name, FrontendTheme.CurrentName(_app), StringComparison.OrdinalIgnoreCase))
      return;

    ApplyAndSave(value.Name);
  }

  private void ApplyAndSave(string name)
  {
    string applied = FrontendTheme.Apply(_app, name, out string? problem);
    if (problem != null)
    {
      Status = problem;
      _applying = true;
      Selected = Find(FrontendTheme.BasePaletteOf(FrontendTheme.CurrentName(_app)));
      RefreshColours(FrontendTheme.CurrentName(_app),
          FrontendTheme.BasePaletteOf(FrontendTheme.CurrentName(_app)) != _colourBase);
      _applying = false;
      return;
    }

    ThemeSettings settings = new ThemeSettings { Theme = applied };
    string says = settings.Save(_settingsPath, out bool saved);
    Status = saved
        ? string.Format("Wearing {0}. {1}", applied, says)
        : string.Format("Wearing {0} for this session; saving it failed - {1}", applied, says);

    Mark(applied);

    // Wearing another skin changes which colours exist to choose from, so the second section is asked again
    // whenever the skin under it changed; a apply that only picked another colour of the same skin leaves the
    // rows standing and just moves the marker, because rebuilding the rows a viewer is standing in would pull
    // the floor out from under the selection they just made.
    _applying = true;
    RefreshColours(applied, FrontendTheme.BasePaletteOf(applied) != _colourBase);
    _applying = false;
  }

  private string _colourBase = "";

  /// <summary>
  /// Marks the colour row that names what is worn - none of them, while the skin stands in its own default
  /// colours - and says so to the selection. Called with the full worn name, under the applying guard, because
  /// a marker moving is not a viewer choosing.
  /// </summary>
  private void MarkColours(string current)
  {
    foreach (ThemeChoice colour in Colours)
      colour.IsCurrent = string.Equals(colour.Name, current, StringComparison.OrdinalIgnoreCase);

    SelectedColour = FindColour(current);
  }

  /// <summary>
  /// Fills the colour rows with the themes the skin <paramref name="current"/> is drawn from ships, when
  /// <paramref name="rebuild"/> says that skin changed under them, and marks what is worn either way. Called
  /// with the full worn name, every time something was applied and once when the screen opens.
  /// </summary>
  private void RefreshColours(string current, bool rebuild)
  {
    string baseName = FrontendTheme.BasePaletteOf(current);

    if (rebuild)
    {
      SkinManifest? manifest = SkinEngine.Find(baseName);

      Colours.Clear();
      if (manifest != null)
      {
        foreach (SkinTheme theme in manifest.Themes)
        {
          if (theme.IsDefault)
            continue;

          Colours.Add(new ThemeChoice(manifest.Name + "-" + theme.Name, ColourDescriptionOf(manifest, theme)));
        }
      }

      ColourHeading = Colours.Count > 0
          ? string.Format("The colours {0} ships. Picking one recolours the skin right here.", manifest!.Title)
          : "";
      _colourBase = baseName;

      OnPropertyChanged(nameof(HasColours));
      OnPropertyChanged(nameof(ColourHeading));
    }

    MarkColours(current);
  }

  private ThemeChoice? Find(string name)
  {
    foreach (ThemeChoice choice in Themes)
      if (string.Equals(choice.Name, name, StringComparison.OrdinalIgnoreCase))
        return choice;
    return null;
  }

  private ThemeChoice? FindColour(string name)
  {
    foreach (ThemeChoice choice in Colours)
      if (string.Equals(choice.Name, name, StringComparison.OrdinalIgnoreCase))
        return choice;
    return null;
  }

  private void Mark(string current)
  {
    string baseName = FrontendTheme.BasePaletteOf(current);
    foreach (ThemeChoice choice in Themes)
      choice.IsCurrent = string.Equals(choice.Name, baseName, StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  /// What each row promises. The sentences point at the files the colours came from rather than at this screen's
  /// own opinion, because the whole claim of these themes is that they are the skins MP2 shipped, and a viewer
  /// who doubts a row can go and look.
  /// </summary>
  private static string DescriptionOf(string name)
  {
    switch (name.ToLowerInvariant())
    {
      case FrontendTheme.DEFAULT_NAME:
        return "This front end's own colours, as they come from the factory";
      case "nereus": return "The Nereus skin, worn from its own files - colours, measurements, styles and tiles";
      case "wmc": return "The Windows Media Center skin, worn from its own files, standing on its blue wash";
      default: return "a skin";
    }
  }

  /// <summary>
  /// What a colour row promises, in terms of what it does to the skin it belongs to. The sentences name the
  /// skin's own scheme back at the viewer, because a colour row is only ever that skin wearing one of its own
  /// files differently - and a scheme nobody wrote a sentence for still says which file it is.
  /// </summary>
  private static string ColourDescriptionOf(SkinManifest manifest, SkinTheme theme)
  {
    if (string.Equals(manifest.Name, "wmc", StringComparison.OrdinalIgnoreCase))
    {
      switch (theme.Name.ToLowerInvariant())
      {
        case "dark": return "WMC without the blue: charcoal behind, grey wherever a panel is raised";
        case "turquoise": return "The same WMC in teal on black";
      }
    }

    return string.Format("{0} in its {1} colours, from {0}'s own files", manifest.Title, theme.Name);
  }
}
