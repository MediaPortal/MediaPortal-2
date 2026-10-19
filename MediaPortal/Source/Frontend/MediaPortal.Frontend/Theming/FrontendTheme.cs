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
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace MediaPortal.Frontend.Theming
{
  /// <summary>
  /// The colours the front end draws with, and the two old skins they were taken from.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>What this is.</b> Every screen used to carry its own hex values - twenty-eight of them across seven files -
  /// which meant that "make it look like the skin I have used for ten years" was a programming job. Now a screen
  /// asks for a thing by what it is for (<c>ScreenBackgroundBrush</c>, <c>AccentBrush</c>, <c>WarningBrush</c>) and
  /// a palette says what that looks like. The keys are named after the job rather than the colour on purpose:
  /// <c>DarkBlue1</c> tells the next reader nothing about why the video surface is the darkest thing on screen.
  /// </para>
  /// <para>
  /// <b>What may be chosen.</b> Exactly what the reference shipped: this front end's own colours
  /// (<see cref="DEFAULT_NAME"/>) and the two skins MP2 had - <c>nereus</c> and <c>wmc</c>. There are three and
  /// there were three; anything else a viewer may once have typed names one of these two skins in another tint
  /// and is read as that skin (see <c>RETIRED</c>). The colour files are still in this tree
  /// (<c>Source/UI/Skins/Nereus/Skin/Nereus/themes/default/styles/Colors.xaml</c> and
  /// <c>Source/UI/Skins/WMC/Skin/WMCSkin/themes/default/styles/Colors.xaml</c>). Every
  /// value below was read out of one of those files and names the key it came from, so that a viewer who thinks a
  /// colour is wrong can look the same key up in the skin and see which of the two is wrong. Values a screen of
  /// this front end has no use for - the gradients, the button bevels, the scroll bars, the star ratings - stay
  /// where they are instead of being invented a place to appear. A skin's own colours - the schemes the
  /// reference filed as themes beside a skin's default - are <em>settings of the skin</em>, not skins: they are
  /// worn by naming them '<c>skin-theme</c>', so '<c>wmc-dark</c>' is the WMC skin in its Dark-WMC colours, and
  /// what recolours the fourteen keys is the theme's own <c>colors.xaml</c>, read off the skin's folder rather
  /// than copied into this file. That is what the settings screen's colour rows choose. Nereus's seven accent
  /// families stay out of both lists: the reference kept them as switches inside its own settings screen and
  /// filed no theme file for one of them, so there is nothing here to wear until a file exists.
  /// </para>
  /// <para>
  /// <b>Two kinds of value are not verbatim in a skin, and both say so where they stand.</b> WMC had no colour for a
  /// complaint, so the amber the folders screen shows when a share refuses to be removed stays the front end's own
  /// in its palette; and where WMC drew button text with a partly transparent white the front end uses
  /// that same white opaque, because the transparency was counted against the panel behind it and this front end's
  /// panels are opaque. Everything else is checked against those files by
  /// <c>EveryColourOfASkinPaletteIsInThatSkin</c> in the tests, so a value that drifts away from the skin it claims
  /// to come from is caught by a run rather than by somebody's memory.
  /// </para>
  /// <para>
  /// <b>What a palette has to answer.</b> All of <see cref="Keys"/>, always. A key missing from a palette is a
  /// screen that silently draws with no brush, and the one thing a viewer would notice about that is a black
  /// television; <c>EveryThemeAnswersEveryKey</c> in the tests exists to make that impossible.
  /// </para>
  /// <para>
  /// <b>Where the choice comes from.</b> A <c>--theme</c> on the command line, then <c>MP2_THEME</c> in the
  /// environment - the same two ways the server's address is chosen, for the same reason: the gates have to be able
  /// to say what they are drawing without editing anything. After that comes <see cref="DEFAULT_NAME"/>, which is
  /// what this front end has always looked like. There is no settings screen to pick it in yet because the server
  /// has no settings API to ask; when it has, the choice belongs there and this class need not change.
  /// </para>
  /// <para>
  /// <b>Chrome.</b> Avalonia 12's Fluent theme computes its own button and list colours at build time and reads
  /// no resource for them - measured, a button sits on <c>#33ffffff</c> whatever any palette says - so a palette
  /// alone left the controls Fluent's. When a real skin is in charge, <see cref="FrontendChrome"/> joins the
  /// application as author styles, which outrank the control theme, and paints buttons, lists, rows, text entry
  /// and the progress line out of the same keys the screens use. The default palette never gets it: that
  /// television keeps exactly the pixels it always had.
  /// </para>
  /// <para>
  /// <b>One thing the default palette loses.</b> The details panel of the browser used to be <c>#FF161F29</c> while
  /// the tiles behind it were <c>#FF17202A</c> - two shades two units apart, which is not a distinction anybody
  /// could point at. They are the same key now, so <see cref="DEFAULT_NAME"/> carries one colour fewer than the
  /// screens it replaced, and the difference between the two screens is invisible to anyone not told to look for it.
  /// </para>
  /// </remarks>
  public static class FrontendTheme
  {
    /// <summary>The palette this front end was drawn with before any of this existed.</summary>
    public const string DEFAULT_NAME = "default";

    /// <summary>Environment variable that picks a palette, alongside <c>MP2_SERVER</c>.</summary>
    public const string ENVIRONMENT_VARIABLE = "MP2_THEME";

    /// <summary>
    /// Where the name of the palette in use is filed in the application's resources, so that a screen, a test or
    /// the render gate can ask afterwards what it is looking at.
    /// </summary>
    public const string CURRENT_KEY = "ThemeName";

    /// <summary>The three that can be worn: this front end's own colours and the two skins MP2 shipped.</summary>
    public static readonly string[] Names =
    {
        DEFAULT_NAME, "nereus", "wmc"
    };

    /// <summary>
    /// What every palette has to say something about. Named after the job on the screen, not after the colour.
    /// </summary>
    public static readonly string[] Keys =
    {
        "ScreenBackgroundBrush", "PanelBrush", "PanelRaisedBrush", "PanelPressedBrush", "OutlineBrush",
        "VideoOutlineBrush", "TextBrush", "MutedTextBrush", "DimTextBrush", "ButtonTextBrush", "AccentBrush",
        "WarningBrush", "ScrimBrush", "VideoBrush"
    };

    /// <summary>
    /// The palettes. Each comment names the key in that skin's own colour file the value was read from.
    /// </summary>
    /// <remarks>
    /// Where a skin drew the same thing with a gradient, the stop the gradient ends on was taken: a flat background
    /// that matches the darkest part of the old one is nearer the old look than a mid-grey would be.
    /// </remarks>
    private static readonly Dictionary<string, Dictionary<string, string>> PALETTES =
        new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            [DEFAULT_NAME] = new Dictionary<string, string>
            {
                ["ScreenBackgroundBrush"] = "#FF101418",
                ["PanelBrush"] = "#FF17202A",
                ["PanelRaisedBrush"] = "#FF1B2A38",
                ["PanelPressedBrush"] = "#FF16212C",
                ["OutlineBrush"] = "#FF2E4A63",
                ["VideoOutlineBrush"] = "#FF243546",
                ["TextBrush"] = "#FFE8EEF4",
                ["MutedTextBrush"] = "#FF9FB6CC",
                ["DimTextBrush"] = "#FF3D556B",
                ["ButtonTextBrush"] = "#FFD3E0EC",
                ["AccentBrush"] = "#FF63A8FF",
                ["WarningBrush"] = "#FFFFB74D",
                ["ScrimBrush"] = "#F2000000",
                ["VideoBrush"] = "#FF000000"
            },

            // Nereus: black behind, a dark tint of the chosen accent behind every tile, white text, and the accent
            // itself on whatever is focused. Its accent family is turquoise, the first one its colour file lists.
            ["nereus"] = new Dictionary<string, string>
            {
                ["ScreenBackgroundBrush"] = "#FF000000",   // DialogBackgroundColor
                ["PanelBrush"] = "#FF244043",              // NereusColorDark_Torquoise
                ["PanelRaisedBrush"] = "#FF175F5D",        // NereusColorMedium_Torquoise
                ["PanelPressedBrush"] = "#FF444444",       // NereusGreyDark
                ["OutlineBrush"] = "#33FFFFFF",            // DialogBorderColor
                ["VideoOutlineBrush"] = "#FF666666",       // NereusGreyMedium
                ["TextBrush"] = "#FFFFFFFF",               // TextColor
                ["MutedTextBrush"] = "#FF9D9D9D",          // NereusColorLight_Grey
                ["DimTextBrush"] = "#FF474747",            // NereusColorDark_Grey
                ["ButtonTextBrush"] = "#FFFFFFFF",         // MenuTextForeground
                ["AccentBrush"] = "#FF009592",             // NereusColorLight_Torquoise
                ["WarningBrush"] = "#FFED7D31",            // NereusColorLight_Orange
                ["ScrimBrush"] = "#80000000",              // DialogScreenBackgroundColor
                ["VideoBrush"] = "#FF000000"               // behind a picture is black in every skin
            },

            // WMC's own blue: the tiles were the blue panels and everything else stood back from them.
            ["wmc"] = new Dictionary<string, string>
            {
                ["ScreenBackgroundBrush"] = "#FF101010",   // BackgroundGradientDarkColor
                ["PanelBrush"] = "#F2447DA7",              // MediaItemBackgroundColor1
                ["PanelRaisedBrush"] = "#F2124273",        // MediaItemBackgroundColor2
                ["PanelPressedBrush"] = "#FF4B76A0",       // WindowHeaderColor
                ["OutlineBrush"] = "#8CFFFFFF",            // DialogBorderColor
                ["VideoOutlineBrush"] = "#80000000",       // ProgressBarBorderColor
                ["TextBrush"] = "#FFFFFFFF",               // TextColor
                ["MutedTextBrush"] = "#FFA4BCD2",          // MediaItemUnwatchedColor
                ["DimTextBrush"] = "#FF475671",            // MediaItemOfflineColor
                ["ButtonTextBrush"] = "#FFE0E0E0",         // ButtonStrokeColor, without its alpha
                ["AccentBrush"] = "#FF4B76A0",             // ProgressBarFillColor
                ["WarningBrush"] = "#FFFFB74D",            // this front end's own amber; WMC had no colour for a complaint
                ["ScrimBrush"] = "#66000000",              // SlideMenuUnfocusedColor
                ["VideoBrush"] = "#FF000000"
            }
        };

    /// <summary>
    /// Names that used to be on the list and are not skins, were never skins: they are Nereus in another accent,
    /// which the reference kept as a switch inside its own settings screen and filed no theme file for. A viewer
    /// whose saved file still says one of these wears Nereus in its shipped colours - the accent comes back as
    /// the setting it was the day a theme file for it exists. WMC's Dark and Turquoise are not in here: the
    /// reference filed theme files for those, and a theme with a file is worn by its own name.
    /// </summary>
    private static readonly Dictionary<string, string> RETIRED =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["nereus-blue"] = "nereus", ["nereus-green"] = "nereus", ["nereus-orange"] = "nereus",
            ["nereus-purple"] = "nereus", ["nereus-red"] = "nereus", ["nereus-yellow"] = "nereus"
        };

    /// <summary>
    /// Whether <paramref name="name"/> names a theme the front end can wear, ignoring case and surrounding
    /// spaces: one of <see cref="Names"/>, or a colour a shipped skin really carries - 'wmc-dark' is true when
    /// the WMC skin stands with its Dark-WMC folder, and false on a machine where that skin is not installed.
    /// </summary>
    public static bool IsKnown(string? name)
    {
      if (name == null)
        return false;

      return PALETTES.ContainsKey(name.Trim()) || Resolve(name, out _) != null;
    }

    /// <summary>
    /// Which palette to draw with: what the command line said, else what the environment said, else what the
    /// viewer picked on the settings screen, else the default.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The order is loud to quiet: a person who typed <c>--theme</c> or set <c>MP2_THEME</c> means it for this
    /// run and outranks a choice made earlier on the couch, but the couch choice outranks the default, because
    /// that choice was the viewer's own. <paramref name="savedName"/> is what the theme settings file holds;
    /// null or empty means nobody ever picked.
    /// </para>
    /// <para>
    /// A name the front end can wear - a palette, or a colour one of them ships - is answered as the theme list
    /// spells it, so that whatever a person typed is printed back the same way everywhere. A name it cannot wear
    /// is passed along untouched, because the sentence about it has to quote what was actually said.
    /// </para>
    /// </remarks>
    public static string SelectedName(string? fromCommandLine, string? savedName = null)
    {
      string? wanted = !string.IsNullOrWhiteSpace(fromCommandLine)
          ? fromCommandLine
          : Environment.GetEnvironmentVariable(ENVIRONMENT_VARIABLE);

      if (string.IsNullOrWhiteSpace(wanted))
        wanted = savedName;

      if (string.IsNullOrWhiteSpace(wanted))
        return DEFAULT_NAME;

      wanted = wanted.Trim();
      if (IsKnown(wanted))
        return CanonicalName(wanted);

      // A retired name is not an unknown one: it says a skin in another tint, and the skin is real even though
      // the tint is not on this list. It wears its skin's shipped colours until the tint returns where it
      // belonged - on the skin's own settings screen.
      return RETIRED.TryGetValue(wanted, out string? baseName) ? baseName : wanted;
    }

    /// <summary>
    /// The brushes of one palette, as the application holds them.
    /// </summary>
    /// <exception cref="ArgumentException">No such palette.</exception>
    /// <exception cref="InvalidOperationException">The palette does not answer every key.</exception>
    public static ResourceDictionary Build(string name)
    {
      Dictionary<string, string> palette = ResolvePalette(name);

      ResourceDictionary brushes = new ResourceDictionary();
      foreach (string key in Keys)
      {
        if (!palette.TryGetValue(key, out string? argb))
          throw new InvalidOperationException(string.Format(
              "The theme '{0}' says nothing about {1}, which every theme has to answer.", name, key));

        brushes[key] = new SolidColorBrush(ParseColor(name, key, argb));
      }

      return brushes;
    }

    /// <summary>
    /// One brush of one palette, for a test or a gate that has to know what the screen was meant to be painted
    /// with.
    /// </summary>
    public static IBrush Brush(string name, string key)
    {
      return new SolidColorBrush(ColorOf(name, key));
    }

    /// <summary>
    /// The colour a palette paints a part of the screen with.
    /// </summary>
    /// <exception cref="ArgumentException">No such palette.</exception>
    /// <exception cref="InvalidOperationException">The palette says nothing about that key, or wrote it in a way
    /// that is not a colour.</exception>
    public static Color ColorOf(string name, string key)
    {
      if (!ResolvePalette(name).TryGetValue(key, out string? argb))
        throw new InvalidOperationException(string.Format("The theme '{0}' says nothing about {1}.", name, key));

      return ParseColor(name, key, argb);
    }

    /// <summary>
    /// Puts one palette on the application, replacing whatever was on it before.
    /// </summary>
    /// <returns>The palette that is in use afterwards.</returns>
    /// <remarks>
    /// A name that is not a palette leaves the colours alone and answers with the sentence to show the viewer: the
    /// front end draws with the colours it has rather than refusing to start over a typo, which is the same bargain
    /// the settings file strikes. What is in use is filed under <see cref="CURRENT_KEY"/>.
    /// </remarks>
    public static string Apply(Application app, string? name, out string? problem)
    {
      string wanted = (name ?? DEFAULT_NAME).Trim();

      if (!IsKnown(wanted))
      {
        problem = string.Format("'{0}' is not a theme, so the front end draws with '{1}'. Themes: {2}.", wanted,
            CurrentName(app), string.Join(", ", Names));
        return CurrentName(app);
      }

      string chosen = CanonicalName(wanted);
      ResourceDictionary brushes = Build(chosen);

      // Replaced rather than merely added: switching twice must not leave the first palette underneath, where it
      // would still answer every key the second theme happened not to name. The interface hands the list of merged
      // dictionaries over read-only, but the list itself is mutable and always present.
      IList<IResourceProvider> merged = app.Resources.MergedDictionaries;
      if (APPLIED.TryGetValue(app, out ResourceDictionary? previous))
        merged.Remove(previous);

      merged.Add(brushes);
      APPLIED.Remove(app);
      APPLIED.Add(app, brushes);
      app.Resources[CURRENT_KEY] = chosen;

      ApplyChrome(app, chosen);

      // The skin folder, if this name has one, dresses on top of the palette: colours the two share keep the
      // palette's - and when the name said a colour theme, the palette was just built from that theme's own
      // colour file, so what wins is the theme either way - and everything only the skin files speak -
      // the reference's own colour names, its measurements, its master templates, its control styles, its
      // overlays - arrives with it. A name with no folder, 'default' above all, takes the skin back off.
      SkinEngine.Apply(app, chosen, out string? skinProblem);

      problem = skinProblem;
      Changed?.Invoke(app, EventArgs.Empty);
      return chosen;
    }

    /// <summary>
    /// Raised after <see cref="Apply"/> has put a palette (and its skin) on the application, so that windows
    /// which paint with the screen colour outside the merged dictionaries - the shell's own background, above
    /// all - can follow the switch without being asked.
    /// </summary>
    public static event EventHandler? Changed;

    /// <summary>
    /// The chrome styles each application was given, and for which palette, so that switching palettes rebuilds
    /// them (their brushes are concrete) and dropping back to <see cref="DEFAULT_NAME"/> takes them off again.
    /// </summary>
    private static readonly ConditionalWeakTable<Application, Styles> CHROME =
        new ConditionalWeakTable<Application, Styles>();

    private static readonly ConditionalWeakTable<Application, string> CHROME_PALETTE =
        new ConditionalWeakTable<Application, string>();

    private static void ApplyChrome(Application app, string chosen)
    {
      if (CHROME.TryGetValue(app, out Styles? old))
      {
        CHROME_PALETTE.TryGetValue(app, out string? wasPalette);
        if (chosen != DEFAULT_NAME && wasPalette == chosen)
          return; // Same skin as last time; its chrome is already on.

        app.Styles.Remove(old);
        CHROME.Remove(app);
        CHROME_PALETTE.Remove(app);
      }

      if (chosen == DEFAULT_NAME)
      {
        // The default palette is what this front end always looked like, chrome and all - Fluent's own buttons
        // and lists stay exactly where they are.
        return;
      }

      Styles chrome = FrontendChrome.Build(chosen);
      app.Styles.Add(chrome);
      CHROME.Add(app, chrome);
      CHROME_PALETTE.Add(app, chosen);
    }

    /// <summary>
    /// The palette this application draws with - the default until <see cref="Apply"/> says otherwise.
    /// </summary>
    public static string CurrentName(Application app)
    {
      return app.Resources.TryGetValue(CURRENT_KEY, out object? name) && name is string text ? text : DEFAULT_NAME;
    }

    /// <summary>
    /// The name as it is spelled in <see cref="Names"/>, so that however it was typed it is printed the same way.
    /// </summary>
    public static string CanonicalName(string name)
    {
      SkinTheme? theme = Resolve(name, out string? baseName);
      if (baseName == null)
        throw new ArgumentException(string.Format("'{0}' is not a theme. Themes: {1}.", name,
            string.Join(", ", Names)), nameof(name));

      if (theme != null)
        return baseName + "-" + theme.Name;

      // The skin behind the name is real but what stands behind its dash is not a theme it ships. Names like
      // that are nobody's theme, and saying so belongs here; the callers let them travel untouched so their
      // sentence can quote them.
      if (name.Trim().IndexOf('-') > 0)
        throw new ArgumentException(string.Format("'{0}' is not a theme. Themes: {1}.", name,
            string.Join(", ", Names)), nameof(name));

      return baseName;
    }

    /// <summary>
    /// The palette each application was last given, so that giving it another one takes the old one back off. Keyed
    /// weakly because a test suite builds and throws away applications, and a theme outliving the windows it painted
    /// would be a leak with a colour chart on it.
    /// </summary>
    private static readonly ConditionalWeakTable<Application, ResourceDictionary> APPLIED =
        new ConditionalWeakTable<Application, ResourceDictionary>();

    private static Color ParseColor(string name, string key, string argb)
    {
      try
      {
        return Color.Parse(argb);
      }
      catch (Exception e)
      {
        throw new InvalidOperationException(string.Format(
            "The theme '{0}' gives {1} as '{2}', which is not a colour.", name, key, argb), e);
      }
    }

    /// <summary>
    /// The palette a name is drawn from: the table's own when the name is one of <see cref="Names"/>, and the
    /// base skin's colours recoloured by the theme's own colour files when the name reaches a theme a shipped
    /// skin carries. A key the theme's files do not speak keeps the base skin's answer, so a palette always
    /// answers all fourteen, whatever a theme forgot to say.
    /// </summary>
    /// <exception cref="ArgumentException">No such theme.</exception>
    private static Dictionary<string, string> ResolvePalette(string name)
    {
      SkinTheme? theme = Resolve(name, out string? baseName);
      if (baseName == null)
        throw new ArgumentException(string.Format("'{0}' is not a theme. Themes: {1}.", name,
            string.Join(", ", Names)), nameof(name));

      if (theme == null)
        return PALETTES[baseName];

      Dictionary<string, string> colours = new Dictionary<string, string>(PALETTES[baseName]);
      foreach (KeyValuePair<string, string> spoken in SkinEngine.ReadThemeColours(theme))
      {
        // Only the fourteen keys are palette business; the reference's own colour names travel in the theme
        // files too, and they arrive through the skin engine when the theme is worn, not through a palette.
        if (colours.ContainsKey(spoken.Key))
          colours[spoken.Key] = spoken.Value;
      }

      return colours;
    }

    /// <summary>
    /// The palette a name draws its base colours from: itself when it is one of <see cref="Names"/>, the skin
    /// before the dash when it is a colour theme. A name that is neither is answered as it came, because there
    /// is nothing here to say about it.
    /// </summary>
    public static string BasePaletteOf(string name)
    {
      return PaletteNamed(name) ?? name;
    }

    /// <summary>
    /// What a name splits into: the palette it draws from, and the theme it asks that skin to wear, so long as
    /// the skin is installed and ships the theme. A plain palette answers with no theme; nothing answers a name
    /// whose head is not a palette or whose tail is not one of that skin's themes.
    /// </summary>
    private static SkinTheme? Resolve(string? name, out string? baseName)
    {
      baseName = name == null ? null : PaletteNamed(name);
      if (baseName == null)
        return null;

      string trimmed = name!.Trim();
      int dash = trimmed.IndexOf('-');
      if (dash <= 0)
        return null;

      return SkinEngine.ThemeNamed(baseName, trimmed.Substring(dash + 1));
    }

    /// <summary>
    /// Which of <see cref="Names"/> a name names, whole or as the head before a dash - <c>'WMC-Dark'</c> names
    /// <c>'wmc'</c> - or null when no palette does.
    /// </summary>
    private static string? PaletteNamed(string name)
    {
      string trimmed = name.Trim();
      int dash = trimmed.IndexOf('-');
      string head = dash > 0 ? trimmed.Substring(0, dash) : trimmed;

      foreach (string known in Names)
      {
        if (string.Equals(known, head, StringComparison.OrdinalIgnoreCase))
          return known;
      }

      return null;
    }
  }
}
