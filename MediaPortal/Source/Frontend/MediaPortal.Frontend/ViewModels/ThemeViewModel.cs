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
    Selected = Find(wearing) ?? Themes[0];
    _applying = false;

    Heading = "Which skin this machine wears. Picking a row changes it right here, and it stays worn after the next start.";
    Status = "Wearing " + wearing + ".";
  }

  public ObservableCollection<ThemeChoice> Themes { get; } = new ObservableCollection<ThemeChoice>();

  public string Heading { get; }

  [ObservableProperty]
  private string _status = "";

  /// <summary>
  /// The picked row. Selecting is applying and saving at once; a refusal is said and the selection steps back
  /// to what is really being worn.
  /// </summary>
  [ObservableProperty]
  private ThemeChoice? _selected;

  partial void OnSelectedChanged(ThemeChoice? value)
  {
    // The step-back below writes Selected too, and the list's own binding re-pushes the row it found as it
    // opens; neither is a choice. A row selected that already names what is worn asks for nothing - it must
    // not save, or merely opening the screen would promote this run's --theme over the viewer's saved pick.
    if (_applying || value == null
        || string.Equals(value.Name, FrontendTheme.CurrentName(_app), StringComparison.OrdinalIgnoreCase))
      return;

    string applied = FrontendTheme.Apply(_app, value.Name, out string? problem);
    if (problem != null)
    {
      Status = problem;
      _applying = true;
      Selected = Find(FrontendTheme.CurrentName(_app));
      _applying = false;
      return;
    }

    ThemeSettings settings = new ThemeSettings { Theme = applied };
    string says = settings.Save(_settingsPath, out bool saved);
    Status = saved
        ? string.Format("Wearing {0}. {1}", applied, says)
        : string.Format("Wearing {0} for this session; saving it failed - {1}", applied, says);

    Mark(applied);
  }

  private ThemeChoice? Find(string name)
  {
    foreach (ThemeChoice choice in Themes)
      if (string.Equals(choice.Name, name, StringComparison.OrdinalIgnoreCase))
        return choice;
    return null;
  }

  private void Mark(string current)
  {
    foreach (ThemeChoice choice in Themes)
      choice.IsCurrent = string.Equals(choice.Name, current, StringComparison.OrdinalIgnoreCase);
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
}
