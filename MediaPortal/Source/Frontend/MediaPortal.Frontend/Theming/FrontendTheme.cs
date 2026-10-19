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
  /// <b>Where the palettes come from.</b> <c>nereus</c>, <c>wmc</c>, <c>wmc-dark</c> and <c>wmc-turquoise</c> are
  /// the skins MP2 shipped, and their colour files are still in this tree
  /// (<c>Source/UI/Skins/Nereus/Skin/Nereus/themes/default/styles/Colors.xaml</c> and
  /// <c>Source/UI/Skins/WMC/Skin/WMCSkin/themes/{default,Dark-WMC,Turquoise-WMC}/styles/Colors.xaml</c>). Every
  /// value below was read out of one of those files and names the key it came from, so that a viewer who thinks a
  /// colour is wrong can look the same key up in the skin and see which of the two is wrong. Values a screen of
  /// this front end has no use for - the gradients, the button bevels, the scroll bars, the star ratings - stay
  /// where they are instead of being invented a place to appear.
  /// </para>
  /// <para>
  /// <b>Two kinds of value are not verbatim in a skin, and both say so where they stand.</b> WMC had no colour for a
  /// complaint, so the amber the folders screen shows when a share refuses to be removed stays the front end's own
  /// in all three of its palettes; and where WMC drew button text with a partly transparent white the front end uses
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

    /// <summary>Every palette, in the order a settings screen would list them.</summary>
    public static readonly string[] Names =
    {
        DEFAULT_NAME, "nereus", "wmc", "wmc-dark", "wmc-turquoise"
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
            },

            // The same skin with the blue taken out, which WMC called Dark.
            ["wmc-dark"] = new Dictionary<string, string>
            {
                ["ScreenBackgroundBrush"] = "#FF101010",   // BackgroundGradientDarkColor
                ["PanelBrush"] = "#F2647887",              // MediaItemBackgroundColor1
                ["PanelRaisedBrush"] = "#F22F4256",        // MediaItemBackgroundColor2
                ["PanelPressedBrush"] = "#FF757575",       // WindowHeaderColor
                ["OutlineBrush"] = "#8CFFFFFF",            // DialogBorderColor, which this theme leaves alone
                ["VideoOutlineBrush"] = "#80000000",       // ProgressBarBorderColor
                ["TextBrush"] = "#FFFFFFFF",               // TextColor
                ["MutedTextBrush"] = "#FFBBBBBB",          // MediaItemUnwatchedColor
                ["DimTextBrush"] = "#FF5C5C5C",            // MediaItemOfflineColor
                ["ButtonTextBrush"] = "#FFE0E0E0",         // ButtonStrokeColor, without its alpha
                ["AccentBrush"] = "#FF757575",             // ProgressBarFillColor
                ["WarningBrush"] = "#FFFFB74D",            // this front end's own amber; WMC had no colour for a complaint
                ["ScrimBrush"] = "#66000000",              // SlideMenuUnfocusedColor
                ["VideoBrush"] = "#FF000000"
            },

            // ... and the same again with turquoise, the third palette WMC shipped.
            ["wmc-turquoise"] = new Dictionary<string, string>
            {
                ["ScreenBackgroundBrush"] = "#FF101010",   // BackgroundGradientDarkColor
                ["PanelBrush"] = "#F2166B7A",              // MediaItemBackgroundColor1
                ["PanelRaisedBrush"] = "#F2053C45",        // MediaItemBackgroundColor2
                ["PanelPressedBrush"] = "#FF3D8795",       // WindowHeaderColor
                ["OutlineBrush"] = "#8CFFFFFF",            // DialogBorderColor, which this theme leaves alone
                ["VideoOutlineBrush"] = "#80000000",       // ProgressBarBorderColor
                ["TextBrush"] = "#FFFFFFFF",               // TextColor
                ["MutedTextBrush"] = "#FF3D8795",          // MediaItemUnwatchedColor
                ["DimTextBrush"] = "#FF33707B",            // MediaItemOfflineColor
                ["ButtonTextBrush"] = "#FFE0E0E0",         // ButtonStrokeColor, without its alpha
                ["AccentBrush"] = "#FF3D8795",             // ProgressBarFillColor
                ["WarningBrush"] = "#FFFFB74D",            // this front end's own amber; WMC had no colour for a complaint
                ["ScrimBrush"] = "#66000000",              // SlideMenuUnfocusedColor
                ["VideoBrush"] = "#FF000000"
            }
        };

    /// <summary>
    /// Whether <paramref name="name"/> names a palette, ignoring case and surrounding spaces.
    /// </summary>
    public static bool IsKnown(string? name)
    {
      return name != null && PALETTES.ContainsKey(name.Trim());
    }

    /// <summary>
    /// Which palette to draw with: what the command line said, else what the environment said, else the default.
    /// </summary>
    /// <remarks>
    /// A name that is a palette is answered as <see cref="Names"/> spells it, so that whatever a person typed is
    /// printed back the same way everywhere. A name that is not one is passed along untouched, because the sentence
    /// about it has to quote what was actually said.
    /// </remarks>
    public static string SelectedName(string? fromCommandLine)
    {
      string? wanted = !string.IsNullOrWhiteSpace(fromCommandLine)
          ? fromCommandLine
          : Environment.GetEnvironmentVariable(ENVIRONMENT_VARIABLE);

      if (string.IsNullOrWhiteSpace(wanted))
        return DEFAULT_NAME;

      wanted = wanted.Trim();
      return IsKnown(wanted) ? CanonicalName(wanted) : wanted;
    }

    /// <summary>
    /// The brushes of one palette, as the application holds them.
    /// </summary>
    /// <exception cref="ArgumentException">No such palette.</exception>
    /// <exception cref="InvalidOperationException">The palette does not answer every key.</exception>
    public static ResourceDictionary Build(string name)
    {
      Dictionary<string, string> palette = PaletteFor(name);

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
      if (!PaletteFor(name).TryGetValue(key, out string? argb))
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

      problem = null;
      return chosen;
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
      foreach (string known in Names)
      {
        if (string.Equals(known, name.Trim(), StringComparison.OrdinalIgnoreCase))
          return known;
      }

      throw new ArgumentException(string.Format("'{0}' is not a theme. Themes: {1}.", name,
          string.Join(", ", Names)), nameof(name));
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

    private static Dictionary<string, string> PaletteFor(string name)
    {
      if (name == null || !PALETTES.TryGetValue(name.Trim(), out Dictionary<string, string>? palette))
        throw new ArgumentException(string.Format("'{0}' is not a theme. Themes: {1}.", name,
            string.Join(", ", Names)), nameof(name));

      return palette;
    }
  }
}
