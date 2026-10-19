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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Styling;
using NUnit.Framework;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;

namespace Tests.Frontend
{
  /// <summary>
  /// The colours the screens draw with, and the promise that the old skins can be put back on them.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A theme is the rare kind of code that cannot be checked by looking at it: a wrong key name in a screen, a
  /// palette missing one entry, or a value that quietly drifts away from the skin it claims all leave a front end
  /// that starts fine, tests green, and draws something nobody chose. So these tests read the screens' XAML and the
  /// skins' own colour files and compare them with the table in <see cref="FrontendTheme"/>. What is being proved is
  /// not that a brush object was created but that the colour on the television is the colour somebody picked years
  /// ago.
  /// </para>
  /// <para>
  /// The tests that resolve a colour need the application object, so they are <c>[AvaloniaTest]</c> tests, and they
  /// put the default palette back afterwards - a suite that ended with the whole front end painted turquoise would
  /// leave the next test looking at something it did not ask for.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestTheme
  {
    /// <summary>Every "DynamicResource" reference in the front end's XAML.</summary>
    private static readonly Regex USED_KEY =
        new Regex(@"\{DynamicResource\s+([A-Za-z0-9_.]+)\s*\}", RegexOptions.Compiled);

    /// <summary>A colour written in a skin's colour file; the group is the part that survives a theme variant.</summary>
    private static readonly Regex SKIN_COLOUR =
        new Regex(@"#(?:[0-9A-Fa-f]{2})?([0-9A-Fa-f]{6})", RegexOptions.Compiled);

    /// <summary>
    /// Where each skin palette's colours are supposed to come from: the theme's own file first, then the one it
    /// inherits from, because a skin theme states only what it changes and MP2 lays the default theme under it.
    /// </summary>
    private static readonly Dictionary<string, string[]> SKIN_FILES = new Dictionary<string, string[]>
    {
        ["nereus"] = new[] { "Nereus/Skin/Nereus/themes/default/styles/Colors.xaml" },
        ["wmc"] = new[] { "WMC/Skin/WMCSkin/themes/default/styles/Colors.xaml" },
        ["wmc-dark"] = new[]
        {
            "WMC/Skin/WMCSkin/themes/Dark-WMC/styles/Colors.xaml",
            "WMC/Skin/WMCSkin/themes/default/styles/Colors.xaml"
        },
        ["wmc-turquoise"] = new[]
        {
            "WMC/Skin/WMCSkin/themes/Turquoise-WMC/styles/Colors.xaml",
            "WMC/Skin/WMCSkin/themes/default/styles/Colors.xaml"
        }
    };

    /// <summary>
    /// The values that provably cannot come from a skin, with the reason. Everything else is checked against the
    /// skin's colour files by <see cref="EveryColourOfASkinPaletteIsInThatSkin"/>.
    /// </summary>
    private static readonly Dictionary<string, string> NOT_FROM_A_SKIN = new Dictionary<string, string>
    {
        ["wmc|WarningBrush"] = "WMC had no colour for a complaint, so the front end's own amber stands",
        ["wmc-dark|WarningBrush"] = "WMC had no colour for a complaint, so the front end's own amber stands",
        ["wmc-turquoise|WarningBrush"] = "WMC had no colour for a complaint, so the front end's own amber stands"
    };

    [TearDown]
    public void PutTheDefaultColoursBack()
    {
      Application? app = Application.Current;
      if (app != null)
        FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);
    }

    [Test]
    public void EveryPaletteAnswersEveryKey()
    {
      foreach (string name in FrontendTheme.Names)
      {
        ResourceDictionary brushes = FrontendTheme.Build(name);

        foreach (string key in FrontendTheme.Keys)
        {
          Assert.That(brushes.TryGetValue(key, out object? brush), Is.True,
              "the theme '" + name + "' says nothing about " + key + ", so a screen asking for it draws nothing");
          Assert.That(brush, Is.InstanceOf<SolidColorBrush>(), name + "." + key + " is not a brush");
        }
      }
    }

    [Test]
    public void EveryPaletteIsSpellableHoweverItWasTyped()
    {
      foreach (string name in FrontendTheme.Names)
      {
        Assert.That(FrontendTheme.CanonicalName("  " + name.ToUpperInvariant() + " "), Is.EqualTo(name),
            "a name typed in capitals has to reach the same palette and be printed the same way");
        Assert.That(FrontendTheme.IsKnown(name.ToUpperInvariant()), Is.True);
      }

      Assert.That(FrontendTheme.IsKnown("graveyard"), Is.False);
      Assert.That(FrontendTheme.IsKnown(null), Is.False);
      Assert.That(() => FrontendTheme.CanonicalName("graveyard"), Throws.ArgumentException);
    }

    [Test]
    public void TheDefaultPaletteIsTheColoursTheScreensWereDrawnWith()
    {
      // These are the values that used to be written into the screens by hand. If one of them changes, the front end
      // changes appearance for everybody who never asked for a theme, which is not what a theme is for.
      Assert.That(Hex(FrontendTheme.DEFAULT_NAME, "ScreenBackgroundBrush"), Is.EqualTo("#FF101418"));
      Assert.That(Hex(FrontendTheme.DEFAULT_NAME, "PanelBrush"), Is.EqualTo("#FF17202A"));
      Assert.That(Hex(FrontendTheme.DEFAULT_NAME, "OutlineBrush"), Is.EqualTo("#FF2E4A63"));
      Assert.That(Hex(FrontendTheme.DEFAULT_NAME, "TextBrush"), Is.EqualTo("#FFE8EEF4"));
      Assert.That(Hex(FrontendTheme.DEFAULT_NAME, "MutedTextBrush"), Is.EqualTo("#FF9FB6CC"));
      Assert.That(Hex(FrontendTheme.DEFAULT_NAME, "AccentBrush"), Is.EqualTo("#FF63A8FF"));
      Assert.That(Hex(FrontendTheme.DEFAULT_NAME, "WarningBrush"), Is.EqualTo("#FFFFB74D"));
      Assert.That(Hex(FrontendTheme.DEFAULT_NAME, "VideoBrush"), Is.EqualTo("#FF000000"));
    }

    [Test]
    public void TheSkinsPaintDifferentlyFromEachOther()
    {
      // Palettes that resolve to the same colours would pass every other test here while being themes nobody would
      // choose, so the difference between them is worth stating. The default, Nereus and WMC are three different
      // looks and have to differ on all three of the colours that make a look.
      List<string> seen = new List<string>
      {
          Hex(FrontendTheme.DEFAULT_NAME, "ScreenBackgroundBrush"), Hex("nereus", "ScreenBackgroundBrush"),
          Hex("wmc", "ScreenBackgroundBrush")
      };
      Assert.That(seen.Distinct().Count(), Is.EqualTo(3), "two skins share a background, so one is not a skin");

      seen = new List<string>
      {
          Hex(FrontendTheme.DEFAULT_NAME, "AccentBrush"), Hex("nereus", "AccentBrush"), Hex("wmc", "AccentBrush")
      };
      Assert.That(seen.Distinct().Count(), Is.EqualTo(3), "two skins share an accent, so one is not a skin");

      // The WMC variants are the same skin with a different colour family, so they keep that skin's background on
      // purpose and differ where the family shows.
      Assert.That(Hex("wmc-dark", "ScreenBackgroundBrush"), Is.EqualTo(Hex("wmc", "ScreenBackgroundBrush")));
      Assert.That(Hex("wmc-dark", "AccentBrush"), Is.Not.EqualTo(Hex("wmc", "AccentBrush")));
      Assert.That(Hex("wmc-turquoise", "AccentBrush"), Is.Not.EqualTo(Hex("wmc", "AccentBrush")));
      Assert.That(Hex("wmc-turquoise", "PanelBrush"), Is.Not.EqualTo(Hex("wmc-dark", "PanelBrush")));
    }

    [Test]
    public void TheCommandLineIsAskedBeforeTheEnvironment()
    {
      string? saved = Environment.GetEnvironmentVariable(FrontendTheme.ENVIRONMENT_VARIABLE);

      try
      {
        Environment.SetEnvironmentVariable(FrontendTheme.ENVIRONMENT_VARIABLE, "wmc");
        Assert.That(FrontendTheme.SelectedName(null), Is.EqualTo("wmc"),
            "the environment has to be enough on its own, because a device has no command line");
        Assert.That(FrontendTheme.SelectedName("nereus"), Is.EqualTo("nereus"),
            "what somebody typed out loud beats what is set for the whole machine");
        Assert.That(FrontendTheme.SelectedName("  Nereus "), Is.EqualTo("nereus"),
            "the name in use is what a palette is called, not what was typed");

        Environment.SetEnvironmentVariable(FrontendTheme.ENVIRONMENT_VARIABLE, "   ");
        Assert.That(FrontendTheme.SelectedName(null), Is.EqualTo(FrontendTheme.DEFAULT_NAME),
            "an empty variable means nobody said anything, not a theme called empty");
      }
      finally
      {
        Environment.SetEnvironmentVariable(FrontendTheme.ENVIRONMENT_VARIABLE, saved);
      }
    }
    [AvaloniaTest]
    public void ApplyingAPaletteChangesWhatAScreenResolves()
    {
      Application app = RequireApplication();

      FrontendTheme.Apply(app, "nereus", out string? problem);
      Assert.That(problem, Is.Null);
      Assert.That(FrontendTheme.CurrentName(app), Is.EqualTo("nereus"));
      Assert.That(Resolved(app, "AccentBrush"), Is.EqualTo(FrontendTheme.ColorOf("nereus", "AccentBrush")));

      FrontendTheme.Apply(app, "wmc-dark", out problem);
      Assert.That(problem, Is.Null);

      // The point of the test: the second palette replaced the first instead of being stacked under it. A stacked
      // dictionary would answer the accent from the newer theme and everything else from the older one, which looks
      // on a television like a theme that only half applied itself.
      Assert.That(FrontendTheme.CurrentName(app), Is.EqualTo("wmc-dark"));
      foreach (string key in FrontendTheme.Keys)
        Assert.That(Resolved(app, key), Is.EqualTo(FrontendTheme.ColorOf("wmc-dark", key)),
            key + " still resolves to the theme that was applied before it");
    }

    [AvaloniaTest]
    public void AThemeThatDoesNotExistIsSaidOutLoudAndChangesNothing()
    {
      Application app = RequireApplication();
      FrontendTheme.Apply(app, "nereus", out _);

      string applied = FrontendTheme.Apply(app, "graveyard", out string? problem);

      Assert.That(applied, Is.EqualTo("nereus"), "an unknown name must not quietly pick a palette");
      Assert.That(problem, Is.Not.Null.And.Contains("graveyard"), "the viewer has to be told what was not understood");
      Assert.That(problem, Does.Contain("nereus"), "... and what is being drawn instead");
      Assert.That(Resolved(app, "AccentBrush"), Is.EqualTo(FrontendTheme.ColorOf("nereus", "AccentBrush")),
          "a typo is not a reason to repaint anything");
    }

    [AvaloniaTest]
    public void ASkinPaintsTheControlsNoScreenDrawsItself()
    {
      Application app = RequireApplication();
      FrontendTheme.Apply(app, "nereus", out _);

      Button button = new Button { Content = "start" };
      ListBox list = new ListBox();
      ListBoxItem row = new ListBoxItem { Content = "a song", IsSelected = true };
      list.Items.Add(row);
      TextBox entry = new TextBox();
      Window window = new Window { Content = new StackPanel { Children = { button, list, entry } } };
      window.Show();
      for (int pump = 0; pump < 20; pump++)
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

      Assert.That(((SolidColorBrush)button.Background).Color, Is.EqualTo(FrontendTheme.ColorOf("nereus", "PanelBrush")),
          "a button is the one thing every screen has and no screen paints; Fluent leaves it on #33ffffff for "
          + "everybody unless a skin says otherwise");
      Assert.That(((SolidColorBrush)button.BorderBrush).Color, Is.EqualTo(FrontendTheme.ColorOf("nereus", "OutlineBrush")));
      Assert.That(((SolidColorBrush)button.Foreground).Color, Is.EqualTo(FrontendTheme.ColorOf("nereus", "ButtonTextBrush")));
      Assert.That(((SolidColorBrush)list.Background).Color, Is.EqualTo(FrontendTheme.ColorOf("nereus", "PanelBrush")));
      Assert.That(((SolidColorBrush)row.Background).Color, Is.EqualTo(FrontendTheme.ColorOf("nereus", "AccentBrush")),
          "a selected row wears the accent, because that is where the skin put its brightest colour");
      Assert.That(((SolidColorBrush)entry.Background).Color, Is.EqualTo(FrontendTheme.ColorOf("nereus", "PanelBrush")));

      window.Close();
    }

    [AvaloniaTest]
    public void TheDefaultPaletteLeavesTheControlsToFluentAlone()
    {
      Application app = RequireApplication();
      Button fluent = new Button { Content = "unchanged" };
      Window window = new Window { Content = fluent };
      window.Show();
      for (int pump = 0; pump < 20; pump++)
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
      Color untouched = ((SolidColorBrush)fluent.Background).Color;

      FrontendTheme.Apply(app, "nereus", out _);
      FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);
      for (int pump = 0; pump < 20; pump++)
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

      Assert.That(untouched, Is.Not.EqualTo(FrontendTheme.ColorOf(FrontendTheme.DEFAULT_NAME, "PanelBrush")),
          "the point of the test: what Fluent draws by its own rules is not the palette's panel colour, so "
          + "getting it back proves the chrome came off rather than being painted over");
      Assert.That(((SolidColorBrush)fluent.Background).Color, Is.EqualTo(untouched),
          "back to the default palette means back to exactly the pixels this front end always had - the promise "
          + "the palette table makes to everybody who never heard of Nereus");

      window.Close();
    }

    [AvaloniaTest]
    public void SwitchingOneSkinForAnotherRepaintsTheControlsWithoutStacking()
    {
      Application app = RequireApplication();
      int stylesBefore = app.Styles.OfType<Styles>().Count();

      Button button = new Button { Content = "again" };
      Window window = new Window { Content = button };
      window.Show();

      FrontendTheme.Apply(app, "nereus", out _);
      for (int pump = 0; pump < 20; pump++)
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
      Assert.That(((SolidColorBrush)button.Background).Color, Is.EqualTo(FrontendTheme.ColorOf("nereus", "PanelBrush")));

      FrontendTheme.Apply(app, "wmc", out _);
      for (int pump = 0; pump < 20; pump++)
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
      Assert.That(((SolidColorBrush)button.Background).Color, Is.EqualTo(FrontendTheme.ColorOf("wmc", "PanelBrush")),
          "the second skin replaced the first on the button too, not just on the resources under it");
      Assert.That(app.Styles.OfType<Styles>().Count(), Is.EqualTo(stylesBefore + 1),
          "two skins added one set of chrome styles between them - stacking them would leave last evening's "
          + "buttons under tonight's");

      window.Close();
    }

    [Test]
    public void TheSavedChoiceRidesBehindTheTypedOneAndTheEnvironment()
    {
      string saved = Environment.GetEnvironmentVariable(FrontendTheme.ENVIRONMENT_VARIABLE);
      try
      {
        Environment.SetEnvironmentVariable(FrontendTheme.ENVIRONMENT_VARIABLE, "wmc");
        Assert.That(FrontendTheme.SelectedName(null, "nereus"), Is.EqualTo("wmc"),
            "the environment is a voice raised about this particular run; a choice made earlier on the couch "
            + "does not shout it down");
        Assert.That(FrontendTheme.SelectedName("nereus-red", "wmc-dark"), Is.EqualTo("nereus-red"),
            "and whoever typed --theme outranks both");

        Environment.SetEnvironmentVariable(FrontendTheme.ENVIRONMENT_VARIABLE, "");
        Assert.That(FrontendTheme.SelectedName(null, "nereus-red"), Is.EqualTo("nereus-red"),
            "with nothing said about this run, the choice the viewer made on the screen is what gets worn");
        Assert.That(FrontendTheme.SelectedName("  nereus-RED ", ""), Is.EqualTo("nereus-red"),
            "however it was spelled, a palette is answered as Names spells it");
        Assert.That(FrontendTheme.SelectedName(null, "   "), Is.EqualTo(FrontendTheme.DEFAULT_NAME),
            "a file that names nothing is nobody having chosen");
        Assert.That(FrontendTheme.SelectedName(null, "nereus-pink"), Is.EqualTo("nereus-pink"),
            "a name that is no palette travels through untouched, so that Apply can quote it in its sentence "
            + "rather than quietly wearing the default");
      }
      finally
      {
        Environment.SetEnvironmentVariable(FrontendTheme.ENVIRONMENT_VARIABLE, saved);
      }
    }

    [Test]
    public void AThemeFileThatIsNotJsonIsSaidOutLoudAndChoosesNothing()
    {
      string file = Path.Combine(Path.GetTempPath(), "mp2-theme-" + Guid.NewGuid().ToString("N") + ".json");
      File.WriteAllText(file, "this is not json {{{");
      try
      {
        ThemeSettings saved = ThemeSettings.Load(file, out string? problem);
        Assert.That(problem, Is.Not.Null.And.Contains(file),
            "an unreadable settings file is a fact the application reports, not one it swallows");
        Assert.That(saved.Theme, Is.Empty,
            "and it chooses nothing on the viewer's behalf; nobody picked this");
        Assert.That(FrontendTheme.SelectedName(null, saved.Theme), Is.EqualTo(FrontendTheme.DEFAULT_NAME),
            "so the chain falls through to the factory colours");
      }
      finally
      {
        File.Delete(file);
      }
    }

    [AvaloniaTest]
    public void PickingAThemeOnTheSettingsScreenPaintsTheAppAndOutlivesTheScreen()
    {
      Application app = RequireApplication();
      string file = Path.Combine(Path.GetTempPath(), "mp2-theme-" + Guid.NewGuid().ToString("N") + ".json");

      try
      {
        ThemeViewModel settings = new ThemeViewModel(app, file);
        Assert.That(settings.Themes.Select(choice => choice.Name), Is.EqualTo(FrontendTheme.Names),
            "the screen offers exactly the palettes there are, in the order the theme list keeps them");
        Assert.That(settings.Themes.Count(choice => choice.Description == "a theme"), Is.Zero,
            "a row without a sentence is a name added to Names without growing DescriptionOf");
        Assert.That(settings.Selected!.Name, Is.EqualTo(FrontendTheme.CurrentName(app)),
            "the screen opens admitting what it is wearing, including after a start-up where --theme outranked "
            + "the saved choice");
        Assert.That(settings.Themes.Single(choice => choice.IsCurrent).Name, Is.EqualTo(FrontendTheme.CurrentName(app)));

        settings.Selected = settings.Themes.First(choice => choice.Name == "wmc-turquoise");
        Assert.That(FrontendTheme.CurrentName(app), Is.EqualTo("wmc-turquoise"),
            "selecting is applying - there is no second button to press, and the screen itself repaints");
        Assert.That(settings.Status, Does.StartWith("Wearing wmc-turquoise"));
        Assert.That(settings.Themes.Single(choice => choice.IsCurrent).Name, Is.EqualTo("wmc-turquoise"),
            "the marker moves with the theme, so the screen never claims two are worn");

        // The restart the viewer believes in: a new application would ask SelectedName, and SelectedName asks
        // this file. Here both halves are checked without pretending to restart the process.
        ThemeSettings onDisk = ThemeSettings.Load(file, out string? readProblem);
        Assert.That(readProblem, Is.Null, "a file this screen just wrote should read back cleanly");
        Assert.That(onDisk.Theme, Is.EqualTo("wmc-turquoise"));
        Assert.That(FrontendTheme.SelectedName(null, onDisk.Theme), Is.EqualTo("wmc-turquoise"),
            "and it would be worn again at the next start");

        ThemeViewModel secondVisit = new ThemeViewModel(app, file);
        Assert.That(secondVisit.Selected!.Name, Is.EqualTo("wmc-turquoise"),
            "backing out of the screen and coming back shows the choice, not where the viewer started");
      }
      finally
      {
        try
        {
          File.Delete(file);
        }
        catch (IOException)
        {
        }
      }
    }

    [AvaloniaTest]
    public void TheSettingsScreenOpensAsAListOfItsThemes()
    {
      Application app = RequireApplication();
      string file = Path.Combine(Path.GetTempPath(), "mp2-theme-" + Guid.NewGuid().ToString("N") + ".json");

      try
      {
        FrontendTheme.Apply(app, "nereus", out _);
        ThemeView view = new ThemeView(app, file);
        ShellWindow window = new ShellWindow(view);
        window.Show();
        for (int pump = 0; pump < 20; pump++)
          Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        Assert.That(((SolidColorBrush) view.Background!).Color,
            Is.EqualTo(FrontendTheme.ColorOf("nereus", "ScreenBackgroundBrush")),
            "the settings screen wears the theme it is offering, live - the honest preview is the screen itself");
        ListBox? themesList = view.FindControl<ListBox>("themesList");
        Assert.That(themesList, Is.Not.Null, "the list has to exist for anything on this screen to be pressable");
        Assert.That(themesList!.Items.Count, Is.EqualTo(FrontendTheme.Names.Length),
            "every palette is a row, and nothing else is");

        window.Close();
      }
      finally
      {
        try
        {
          File.Delete(file);
        }
        catch (IOException)
        {
        }
      }
    }

    [Test]
    public void EveryColourTheScreensAskForIsAKeyEveryPaletteAnswers()
    {
      // The failure this exists for: a screen asking for a key spelled one letter out still compiles, still loads,
      // and draws that part of itself with nothing on it. Nothing at runtime complains, so the names are checked.
      List<string> unknown = new List<string>();
      HashSet<string> keys = new HashSet<string>(FrontendTheme.Keys, StringComparer.Ordinal);

      foreach (string file in ScreenFiles())
      {
        foreach (Match used in USED_KEY.Matches(File.ReadAllText(file)))
        {
          if (!keys.Contains(used.Groups[1].Value))
            unknown.Add(Path.GetFileName(file) + " asks for " + used.Groups[1].Value);
        }
      }

      Assert.That(unknown, Is.Empty, string.Join(Environment.NewLine, unknown));
    }

    [Test]
    public void EveryKeyIsAskedForByAScreen()
    {
      // The other half of the same coin: a key no screen asks for is a colour copied out of a skin for nothing, and
      // it also hides the case where a screen stopped asking for its colour altogether.
      HashSet<string> used = new HashSet<string>(StringComparer.Ordinal);

      foreach (string file in ScreenFiles())
      {
        foreach (Match match in USED_KEY.Matches(File.ReadAllText(file)))
          used.Add(match.Groups[1].Value);
      }

      foreach (string key in FrontendTheme.Keys)
        Assert.That(used, Contains.Item(key), key + " is in every palette and on no screen");
    }

    [Test]
    public void ANereusFamilyIsNereusInAnotherTintAndNothingElse()
    {
      // The families were a colour choice in the skin, not a different skin: a family palette may change only
      // the three keys Nereus itself tinted with the family (the two panel shades and the accent) and has to
      // leave every other key - including the orange of a complaint - exactly where the base palette puts it.
      string[] tinted = { "AccentBrush", "PanelBrush", "PanelRaisedBrush" };

      foreach (string family in new[]
      {
          "nereus-blue", "nereus-green", "nereus-orange", "nereus-purple", "nereus-red", "nereus-yellow"
      })
      {
        foreach (string key in FrontendTheme.Keys)
        {
          Color mine = FrontendTheme.ColorOf(family, key);
          Color baseColour = FrontendTheme.ColorOf("nereus", key);
          if (tinted.Contains(key))
            Assert.That(mine, Is.Not.EqualTo(baseColour), family + "." + key + " is still the base palette's colour");
          else
            Assert.That(mine, Is.EqualTo(baseColour), family + "." + key + " was retinted; the family is a "
                + "colour choice, not a second skin");
        }
      }

      // And the families are seven different accents, not one accent spelled seven ways.
      int distinct = new[] { "nereus", "nereus-blue", "nereus-green", "nereus-orange", "nereus-purple",
          "nereus-red", "nereus-yellow" }
          .Select(name => FrontendTheme.ColorOf(name, "AccentBrush")).Distinct().Count();
      Assert.That(distinct, Is.EqualTo(7), "seven families that answer with the same colour are one family");
    }

    [Test]
    public void EveryColourOfASkinPaletteIsInThatSkin()
    {
      // "Rescued from the Nereus and WMC skins" is a claim, and both skins are in this tree, so the claim can be
      // checked: every colour of a skin palette has to appear - ignoring alpha, which the different panels account
      // for - in that theme's colour file or in the default theme it inherits from.
      List<string> unfounded = new List<string>();

      foreach (string name in FrontendTheme.Names)
      {
        if (name == FrontendTheme.DEFAULT_NAME)
          continue;

        HashSet<string> skinColours = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Nereus's accent families are the same skin in another tint: they state no colours of their own beyond
        // the family shades, and those live in the same colour file as everything else the skin said.
        string[] skinFiles = name.StartsWith("nereus", StringComparison.OrdinalIgnoreCase)
            ? SKIN_FILES["nereus"]
            : SKIN_FILES[name];

        foreach (string relative in skinFiles)
        {
          string file = Path.Combine(MetadataPath("SkinsRoot"), relative);
          Assert.That(File.Exists(file), Is.True,
              "the theme '" + name + "' is said to come from '" + relative + "', which is not in the tree");

          foreach (Match colour in SKIN_COLOUR.Matches(File.ReadAllText(file)))
            skinColours.Add(colour.Groups[1].Value);
        }

        foreach (string key in FrontendTheme.Keys)
        {
          if (NOT_FROM_A_SKIN.ContainsKey(name + "|" + key))
            continue;

          if (!skinColours.Contains(Hex(name, key).Substring(3)))
            unfounded.Add(name + "." + key + " = " + Hex(name, key) + ", which no colour file of that skin contains");
        }
      }

      Assert.That(unfounded, Is.Empty, string.Join(Environment.NewLine, unfounded));
    }

    /// <summary>Every XAML file of the front end, screens and styles alike.</summary>
    private static IEnumerable<string> ScreenFiles()
    {
      return Directory.EnumerateFiles(MetadataPath("FrontendSource"), "*.axaml", SearchOption.AllDirectories);
    }

    /// <summary>
    /// A palette's colour for a key, written the way the palettes are written, so a test compares what a person
    /// would type against what the code says.
    /// </summary>
    private static string Hex(string theme, string key)
    {
      // Written out digit by digit rather than with Color.ToString, because Avalonia answers a well-known colour with
      // its name - "BLACK" - and a test that compared names with the hex a person would have typed would fail on the
      // plainest values in the table.
      Color c = FrontendTheme.ColorOf(theme, key);
      return string.Format(System.Globalization.CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}{3:X2}",
          c.A, c.R, c.G, c.B);
    }

    private static Color Resolved(Application app, string key)
    {
      Assert.That(app.TryFindResource(key, out object? found), Is.True,
          "nothing on the application answers " + key);
      Assert.That(found, Is.InstanceOf<SolidColorBrush>(), key + " resolved to something that is not a brush");
      return ((SolidColorBrush) found!).Color;
    }

    private static Application RequireApplication()
    {
      Application? app = Application.Current;
      Assert.That(app, Is.Not.Null, "these tests need the application object, so they have to be AvaloniaTest tests");
      return app!;
    }

    /// <summary>
    /// A directory the test project was told about at build time. Read from the assembly rather than from the
    /// working directory, because a test run's working directory is wherever the runner happened to be started.
    /// </summary>
    private static string MetadataPath(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestTheme).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.Ordinal));

      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project has to hand " + name + " over for this to be checkable");

      string path = Path.GetFullPath(value!.Value!);
      Assert.That(Directory.Exists(path), Is.True, name + " points at '" + path + "', which is not a directory");
      return path;
    }
  }
}