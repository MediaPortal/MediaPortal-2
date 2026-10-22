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
        ["wmc"] = new[] { "WMC/Skin/WMCSkin/themes/default/styles/Colors.xaml" }
    };

    /// <summary>
    /// The values that provably cannot come from a skin, with the reason. Everything else is checked against the
    /// skin's colour files by <see cref="EveryColourOfASkinPaletteIsInThatSkin"/>.
    /// </summary>
    private static readonly Dictionary<string, string> NOT_FROM_A_SKIN = new Dictionary<string, string>
    {
        ["wmc|WarningBrush"] = "WMC had no colour for a complaint, so the front end's own amber stands"
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

      FrontendTheme.Apply(app, "wmc", out problem);
      Assert.That(problem, Is.Null);

      // The point of the test: the second palette replaced the first instead of being stacked under it. A stacked
      // dictionary would answer the accent from the newer theme and everything else from the older one, which looks
      // on a television like a theme that only half applied itself.
      Assert.That(FrontendTheme.CurrentName(app), Is.EqualTo("wmc"));
      foreach (string key in FrontendTheme.Keys)
        Assert.That(Resolved(app, key), Is.EqualTo(FrontendTheme.ColorOf("wmc", key)),
            key + " still resolves to the theme that was applied before it");
    }

    [AvaloniaTest]
    public void NereusColourSwitchesPaintThePaletteTheyBelongTo()
    {
      // The eight colour switches are the reference's colour question and Nereus' Colors.xaml answers it
      // with a light, a medium and a dark colour per family. The skin code now reads that answer: the
      // accent, the raised panels and the panels are the chosen family, not a painted-in turquoise.
      string previousDir = Environment.GetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR") ?? "";
      Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR",
          Path.Combine(Path.GetTempPath(), "mp2-tint-" + Guid.NewGuid().ToString("N")));
      try
      {
        Application app = RequireApplication();

        FrontendTheme.Apply(app, "nereus", out _);
        Assert.That(Resolved(app, "AccentBrush"), Is.EqualTo(FrontendTheme.ColorOf("nereus", "AccentBrush")),
            "a Nereus nobody has changed wears turquoise, the colour the reference shipped");

        NereusSkinSettings chosen = new NereusSkinSettings();
        chosen.UseTorquoise = false;
        chosen.UseBlue = true;
        chosen.Save(null, out _);

        FrontendTheme.Reapply(app);
        Assert.That(Resolved(app, "AccentBrush"), Is.EqualTo(Color.Parse("#FF4472C4")),
            "the light blue of NereusColorLight_Blue is the accent once the switch says Blue");
        Assert.That(Resolved(app, "PanelRaisedBrush"), Is.EqualTo(Color.Parse("#FF2F5597")));
        Assert.That(Resolved(app, "PanelBrush"), Is.EqualTo(Color.Parse("#FF203864")));

        FrontendTheme.Apply(app, "wmc", out _);
        Assert.That(Resolved(app, "AccentBrush"), Is.EqualTo(FrontendTheme.ColorOf("wmc", "AccentBrush")),
            "the tint belongs to Nereus' file and dyes no other face");
      }
      finally
      {
        Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR", previousDir.Length == 0 ? null : previousDir);
      }
    }

    /// <summary>
    /// The reference's dialog master read three dials off the skin settings when it drew a dialog: the glow
    /// round the panel (white, focus-coloured, or nothing), the panel's transparency, and whether the corners
    /// are round. The dialogs have shipped, so those dials have to steer the shipped frame - through the three
    /// palette keys the frame binds, DialogBorderBrush, DialogPanelBrush and DialogCornerRadius.
    /// </summary>
    [AvaloniaTest]
    public void TheDialogDialsPaintTheFrameTheReferenceHungOnThem()
    {
      string previousDir = Environment.GetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR") ?? "";
      Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR",
          Path.Combine(Path.GetTempPath(), "mp2-dialog-" + Guid.NewGuid().ToString("N")));
      try
      {
        Application app = RequireApplication();

        FrontendTheme.Apply(app, "nereus", out _);
        // The shipped defaults: focus colour dyes the frame, the panel stands at the reference's 0.85, and
        // the corners are square because the rounding switch ships off.
        Assert.That(((SolidColorBrush) DialogResource(app, "DialogBorderBrush")).Color,
            Is.EqualTo(FrontendTheme.ColorOf("nereus", "AccentBrush")),
            "UseFocusColor is on by default, and the reference frames its dialogs with the light accent then");
        Assert.That(((SolidColorBrush) DialogResource(app, "DialogPanelBrush")).Color.A, Is.EqualTo((byte) 217),
            "0.85 is the reference's DialogBackgroundOpacity, and transparency is on by default - 217/255 is that");
        Assert.That(DialogResource(app, "DialogCornerRadius"), Is.EqualTo(new CornerRadius(0)),
            "and Nereus ships its corners square");

        NereusSkinSettings dials = new NereusSkinSettings();
        dials.UseWhiteColor = true;
        dials.UseRoundedDialogCorners = true;
        dials.UseTransparency = false;
        dials.Save(null, out _);

        FrontendTheme.Reapply(app);
        Assert.That(((SolidColorBrush) DialogResource(app, "DialogBorderBrush")).Color, Is.EqualTo(Colors.White),
            "the white dial wins the frame, exactly as the reference's expression puts it first");
        Assert.That(DialogResource(app, "DialogCornerRadius"), Is.EqualTo(new CornerRadius(6)),
            "the rounding dial is the reference's DialogBorderCornerRadius, which Consts.xaml sets to six");
        Assert.That(((SolidColorBrush) DialogResource(app, "DialogPanelBrush")).Color.A, Is.EqualTo((byte) 255),
            "and turning transparency off makes the panel opaque");

        dials = new NereusSkinSettings();
        dials.UseFocusColor = false;
        dials.Save(null, out _);

        FrontendTheme.Reapply(app);
        Assert.That(((SolidColorBrush) DialogResource(app, "DialogBorderBrush")).Color,
            Is.EqualTo(Colors.Transparent),
            "no white and no focus colour is the 'no colour' answer: the reference frame is Transparent then");
      }
      finally
      {
        Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR", previousDir.Length == 0 ? null : previousDir);
      }
    }

    /// <summary>A dialog-frame resource on the worn palette, with a failure that names the key.</summary>
    private static object DialogResource(Application app, string key)
    {
      Assert.That(app.TryFindResource(key, out object? found), Is.True,
          "nothing on the application answers " + key + " - the dialog frame would stand there undressed");
      return found!;
    }

    /// <summary>
    /// Every <c>DynamicResource</c> name the front end and its skins ask for must be a name somebody answers.
    /// A <c>DynamicResource</c> that finds nothing does not fall back and does not complain - it never paints,
    /// and the screen shows an unset brush forever. This front end has already shipped this mistake twice:
    /// the superlayers and then the chrome asked for bare <c>Text</c>, <c>Outline</c> and friends, which no
    /// palette has ever carried. So the whole tree is re-read here - views, chrome, templates, overlays - and
    /// every asked name is looked for in the places a resource can be born: an <c>x:Key</c> in any XAML file
    /// of the front end, a key in the palette's own table, or a key the palette code writes into the merged
    /// dictionary while tinting.
    /// </summary>
    [Test]
    public void NoDynamicResourceAsksForANameNobodyAnswers()
    {
      string root = MetadataPath("FrontendSource");

      Dictionary<string, List<string>> asked = new Dictionary<string, List<string>>(StringComparer.Ordinal);
      HashSet<string> defined = new HashSet<string>(StringComparer.Ordinal);
      defined.UnionWith(FrontendTheme.Keys);

      foreach (string file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
          .Where(f => f.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase)
                   || f.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)
                   || f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
          .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\")
                   && !f.Contains("/bin/") && !f.Contains("/obj/"))
          .OrderBy(f => f, StringComparer.Ordinal))
      {
        string text = File.ReadAllText(file);
        string shown = file.Substring(root.Length).TrimStart('\\', '/');

        foreach (Match m in Regex.Matches(text, @"DynamicResource\s+([A-Za-z0-9_]+)\s*\}"))
        {
          string key = m.Groups[1].Value;
          if (!asked.TryGetValue(key, out List<string>? where))
            asked[key] = where = new List<string>();
          if (!where.Contains(shown))
            where.Add(shown);
        }

        foreach (Match m in Regex.Matches(text, @"x:Key=""([A-Za-z0-9_]+)"""))
          defined.Add(m.Groups[1].Value);
        foreach (Match m in Regex.Matches(text, "brushes\\[\"([A-Za-z0-9_]+)\"\\]"))
          defined.Add(m.Groups[1].Value);
      }

      List<string> dead = new List<string>();
      foreach (KeyValuePair<string, List<string>> entry in asked.OrderBy(e => e.Key, StringComparer.Ordinal))
        if (!defined.Contains(entry.Key))
          dead.Add(entry.Key + " (asked by " + string.Join(", ", entry.Value) + ")");

      Assert.That(dead, Is.Empty,
          "a DynamicResource that answers nothing paints nothing, silently and forever - these names are "
          + "asked for but defined nowhere: " + string.Join("; ", dead));
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
      Assert.That(app.Styles.OfType<Styles>().Count(), Is.EqualTo(stylesBefore + 2),
          "two skins added one pair between them - the chrome's style set and the skin XAML's - and switching "
          + "skins swapped both; stacking would leave last evening's buttons under tonight's");

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
        Assert.That(FrontendTheme.SelectedName("nereus", "wmc"), Is.EqualTo("nereus"),
            "and whoever typed --theme outranks both");

        Environment.SetEnvironmentVariable(FrontendTheme.ENVIRONMENT_VARIABLE, "");
        Assert.That(FrontendTheme.SelectedName(null, "nereus"), Is.EqualTo("nereus"),
            "with nothing said about this run, the choice the viewer made on the screen is what gets worn");
        Assert.That(FrontendTheme.SelectedName("  NEREUS ", ""), Is.EqualTo("nereus"),
            "however it was spelled, a palette is answered as Names spells it");
        Assert.That(FrontendTheme.SelectedName(null, "   "), Is.EqualTo(FrontendTheme.DEFAULT_NAME),
            "a file that names nothing is nobody having chosen");

        // A retired name was one skin in another tint. The tint was the skin's own setting, not a skin, so the
        // file's owner now wears that skin in its shipped colours instead of losing their choice to a typo list.
        Assert.That(FrontendTheme.SelectedName(null, "wmc-dark"), Is.EqualTo("wmc"),
            "yesterday's 'wmc-dark' is today's WMC skin, not a complaint");
        Assert.That(FrontendTheme.SelectedName(null, "nereus-red"), Is.EqualTo("nereus"),
            "and yesterday's accent family is today's Nereus");

        Assert.That(FrontendTheme.SelectedName(null, "nereus-pink"), Is.EqualTo("nereus-pink"),
            "a name that was never a palette travels through untouched, so that Apply can quote it in its sentence "
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

        settings.Selected = settings.Themes.First(choice => choice.Name == "wmc");
        Assert.That(FrontendTheme.CurrentName(app), Is.EqualTo("wmc"),
            "selecting is applying - there is no second button to press, and the screen itself repaints");
        Assert.That(settings.Status, Does.StartWith("Wearing wmc"));
        Assert.That(settings.Themes.Single(choice => choice.IsCurrent).Name, Is.EqualTo("wmc"),
            "the marker moves with the theme, so the screen never claims two are worn");

        // The restart the viewer believes in: a new application would ask SelectedName, and SelectedName asks
        // this file. Here both halves are checked without pretending to restart the process.
        ThemeSettings onDisk = ThemeSettings.Load(file, out string? readProblem);
        Assert.That(readProblem, Is.Null, "a file this screen just wrote should read back cleanly");
        Assert.That(onDisk.Theme, Is.EqualTo("wmc"));
        Assert.That(FrontendTheme.SelectedName(null, onDisk.Theme), Is.EqualTo("wmc"),
            "and it would be worn again at the next start");

        ThemeViewModel secondVisit = new ThemeViewModel(app, file);
        Assert.That(secondVisit.Selected!.Name, Is.EqualTo("wmc"),
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
    public void ARetiredNameIsNotALostChoice()
    {
      // The eight retired names were never skins: they were one of the two real skins in another tint, which the
      // reference kept as a setting of the skin. A viewer whose saved file still names one must not come back to
      // a factory screen - the name resolves to its skin, in the colours that skin shipped, until the tint comes
      // back as the skin setting it was.
      foreach (string retired in new[]
      {
          "nereus-blue", "nereus-green", "nereus-orange", "nereus-purple", "nereus-red", "nereus-yellow"
      })
        Assert.That(FrontendTheme.SelectedName(null, retired), Is.EqualTo("nereus"),
            retired + " is Nereus in a tint, and the tint is a setting, not a choice of skin");

      Assert.That(FrontendTheme.SelectedName(null, "wmc-dark"), Is.EqualTo("wmc"));
      Assert.That(FrontendTheme.SelectedName(null, "wmc-turquoise"), Is.EqualTo("wmc"));

      // And none of them is on the list anybody can pick: the picker says three, and three it is.
      Assert.That(FrontendTheme.Names.Length, Is.EqualTo(3),
          "the reference had one default look and two skins; the list says exactly that");
      foreach (string retired in new[] { "nereus-blue", "wmc-dark", "wmc-turquoise" })
        Assert.That(FrontendTheme.Names, Does.Not.Contain(retired));
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

    /// <summary>
    /// The home head line's weather obeys the reference's three guards at once: the slot is the home
    /// screen's (master_home read the weather and no other master did), the number and the day's picture
    /// answer their own dials (ShowTemperature, ShowWeatherCondition), and nothing at all shows before a
    /// watched place has actually been read.
    /// </summary>
    [Test]
    public void TheWeatherSlotShowsWhatItsDialsAllowAndNothingTheyDoNot()
    {
      ChromeInfo chrome = new ChromeInfo();
      Assert.That(chrome.HasWeatherTemperature, Is.False, "no reading yet, so the head line says nothing");
      Assert.That(chrome.HasWeatherIcon, Is.False);

      chrome.ReportWeather("8°C", "3");
      Assert.That(chrome.HasWeatherTemperature, Is.True, "read, at home, dials open by default");
      Assert.That(chrome.HasWeatherIcon, Is.True);

      chrome.ShowTemperature = false;
      Assert.That(chrome.HasWeatherTemperature, Is.False, "the temperature dial off hides the number");
      Assert.That(chrome.HasWeatherIcon, Is.True, "and only the number - the picture answers to its own dial");

      chrome.ShowWeatherCondition = false;
      Assert.That(chrome.HasWeatherIcon, Is.False,
          "ShowWeatherCondition is the dial that guarded master_home's WeatherBigIcon");

      chrome.ShowTemperature = true;
      chrome.ShowWeatherCondition = true;
      chrome.ShowWeatherSlot = false;
      Assert.That(chrome.HasWeatherTemperature, Is.False,
          "one screen deeper than home and the slot is gone, whatever the dials say - it is master_home's");
      Assert.That(chrome.HasWeatherIcon, Is.False);

      chrome.ShowWeatherSlot = true;
      chrome.ReportWeather("", null);
      Assert.That(chrome.HasWeatherTemperature, Is.False, "a walk back home with no reading says nothing");
      Assert.That(chrome.WeatherIcon, Is.Empty, "and the picture goes with it, not last hour's left standing");
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