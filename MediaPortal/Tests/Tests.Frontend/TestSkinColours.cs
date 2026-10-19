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
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using NUnit.Framework;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// The colours a skin ships besides its default, worn by naming them 'skin-theme'.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference kept a skin's colour schemes as settings of the skin: WMC shipped Dark-WMC and Turquoise-WMC
  /// as theme folders beside its default, each a pile of XAML whose colour files say what every part of the
  /// screen is painted with. These tests read those very files and demand the same answers from the front end -
  /// that the name is wearable, that the colours it paints are the file's colours rather than the table's, that
  /// the screen offers the rows and saves the choice. The accent families the reference filed inside its own
  /// settings screen stay unwearable, and one test exists to keep them that way: a name with no theme file
  /// behind it is a name nobody shipped.
  /// </para>
  /// <para>
  /// The skins folder is named from the source tree for every test here, so the answers do not depend on
  /// whatever deployment happens to stand at the usual path on the machine running them. Every test in this
  /// fixture is an <c>[AvaloniaTest]</c> for a reason that only shows up when one is not: asking the engine
  /// which folder it reads resolves that question once for the whole run, and the run's isolation from the
  /// machine's own skins - which <c>TestAppBuilder</c> sets up - has to be in effect before the asking. The
  /// headless application is what makes sure it is.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestSkinColours
  {
    private string? _previousFolder;

    [SetUp]
    public void WearTheShippedSkins()
    {
      // Deterministic isolation first, whatever fixture ran before this one: TestAppBuilder's static
      // constructor is what keeps a test run from reading the machine's own skins, and the capture below has
      // to see that answer rather than the machine's - asking the engine which folder it reads is a question
      // the run only gets to answer once.
      System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(TestAppBuilder).TypeHandle);

      _previousFolder = SkinEngine.SkinsFolder;
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
    }

    [TearDown]
    public void PutTheDefaultBack()
    {
      Application? app = Application.Current;
      if (app != null)
        FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);

      SkinEngine.SkinsFolder = _previousFolder;
    }

    [AvaloniaTest]
    public void ANameSaysSkinThenColourAndIsAnsweredAsSpelled()
    {
      Assert.That(FrontendTheme.IsKnown("wmc-dark"), Is.True, "WMC ships a dark scheme, and its name can be worn");
      Assert.That(FrontendTheme.IsKnown("WMC-TURQUOISE"), Is.True, "ignoring how it happens to be capitalised");
      Assert.That(FrontendTheme.IsKnown("wmc-pink"), Is.False, "a colour WMC never shipped is nobody's theme");
      Assert.That(FrontendTheme.IsKnown("nereus-red"), Is.False,
          "and an accent family with no theme file behind it stays unwearable, however real its skin is");

      Assert.That(FrontendTheme.CanonicalName(" WMC-Dark "), Is.EqualTo("wmc-dark"),
          "typed however, a shipped colour theme is spelled as its files spell it");
      Assert.That(FrontendTheme.CanonicalName("WMC"), Is.EqualTo("wmc"), "and a skin as the list spells it");
      Assert.That(FrontendTheme.BasePaletteOf("wmc-dark"), Is.EqualTo("wmc"));
      Assert.That(FrontendTheme.BasePaletteOf("nereus"), Is.EqualTo("nereus"));
      Assert.That(FrontendTheme.BasePaletteOf("graveyard"), Is.EqualTo("graveyard"),
          "a name that is nobody's has no base to give it, so it says itself");

      Assert.That(FrontendTheme.Names.Length, Is.EqualTo(3),
          "colours are worn by name, but the palettes a screen is built from are still exactly three");
    }

    [AvaloniaTest]
    public void AThemePaintsItsOwnFilesColoursOverItsSkins()
    {
      Assert.That(FrontendTheme.ColorOf("wmc-dark", "AccentBrush"), Is.EqualTo(Color.Parse("#FF757575")),
          "Dark-WMC's own grey, read off Dark-WMC's colour file rather than copied into a table");
      Assert.That(FrontendTheme.ColorOf("wmc-dark", "PanelBrush"), Is.EqualTo(Color.Parse("#F2647887")));
      Assert.That(FrontendTheme.ColorOf("wmc-turquoise", "AccentBrush"), Is.EqualTo(Color.Parse("#FF3D8795")),
          "and Turquoise-WMC's teal for the same reason");
      Assert.That(FrontendTheme.ColorOf("wmc-turquoise", "PanelBrush"), Is.EqualTo(Color.Parse("#F2166B7A")));

      // A theme drawn from a palette leaves the palette it was drawn from exactly as it was.
      Assert.That(FrontendTheme.ColorOf("wmc", "AccentBrush"), Is.EqualTo(Color.Parse("#FF4B76A0")));

      foreach (string key in FrontendTheme.Keys)
      {
        string captured = key;
        Assert.DoesNotThrow(() => FrontendTheme.ColorOf("wmc-dark", captured),
            captured + " has to be answered even by a theme whose file forgets it - the skin's own value "
            + "stands under whatever the theme does not say");
      }
    }

    [AvaloniaTest]
    public void ChoosingAColourRepaintsTheScreenAndCanBeTakenBack()
    {
      Application app = Application.Current!;
      Assert.That(FrontendTheme.IsKnown("wmc-dark"), Is.True, "the shipped skins are under the test's own folder");

      string applied = FrontendTheme.Apply(app, "wmc-dark", out string? problem);
      Assert.That(applied, Is.EqualTo("wmc-dark"), problem ?? "the dark theme applied");
      Assert.That(BrushColour(app, "PanelBrush"), Is.EqualTo(Color.Parse("#F2647887")),
          "the merged screen answers with the dark scheme, because the palette a theme name builds carries "
          + "that theme's own file - and the palette is what keeps the final word");

      WornSkin? worn = SkinEngine.Worn(app);
      Assert.That(worn, Is.Not.Null);
      Assert.That(worn!.Theme.Name, Is.EqualTo("dark"), "and the skin itself stands in the theme the name said");

      FrontendTheme.Apply(app, "wmc", out problem);
      Assert.That(problem, Is.Null, problem ?? "back to blue");
      Assert.That(BrushColour(app, "PanelBrush"), Is.EqualTo(Color.Parse("#F2447DA7")),
          "and taking the colour back leaves the skin in the colours it shipped with");
    }

    [AvaloniaTest]
    public void TheSettingsScreenOffersTheWornSkinsColours()
    {
      Application app = Application.Current!;
      string file = Path.Combine(Path.GetTempPath(), "mp2-theme-" + Guid.NewGuid().ToString("N") + ".json");

      try
      {
        FrontendTheme.Apply(app, "wmc", out _);

        ThemeViewModel settings = new ThemeViewModel(app, file);
        Assert.That(settings.HasColours, Is.True, "WMC ships colours besides its default, and the screen says so");
        Assert.That(settings.Colours.Select(choice => choice.Name),
            Is.EqualTo(new[] { "wmc-dark", "wmc-turquoise" }),
            "every theme the skin ships except the one it stands in by default, named the way they are worn");
        Assert.That(settings.ColourHeading, Does.Contain("Windows Media Center"),
            "the heading says whose colours these are, in the skin's own words");
        Assert.That(settings.SelectedColour, Is.Null, "WMC in its blue: no colour row is worn");
        Assert.That(((SolidColorBrush) settings.Colours[0].PanelBrush).Color, Is.EqualTo(Color.Parse("#F2647887")),
            "a colour row's swatch promises the theme's own panel colour, not the skin's");

        settings.SelectedColour = settings.Colours.First(choice => choice.Name == "wmc-dark");
        Assert.That(FrontendTheme.CurrentName(app), Is.EqualTo("wmc-dark"),
            "selecting a colour is applying it - the same bargain the skin rows strike");
        Assert.That(settings.Status, Does.StartWith("Wearing wmc-dark"));
        Assert.That(settings.Colours.Single(choice => choice.IsCurrent).Name, Is.EqualTo("wmc-dark"));
        Assert.That(settings.Themes.Single(choice => choice.IsCurrent).Name, Is.EqualTo("wmc"),
            "and the skin keeps its worn marker: the colour is a setting of the skin, not a skin of its own");

        ThemeSettings onDisk = ThemeSettings.Load(file, out string? readProblem);
        Assert.That(readProblem, Is.Null, readProblem ?? "the file reads back");
        Assert.That(onDisk.Theme, Is.EqualTo("wmc-dark"), "and the choice outlives the run that made it");

        ThemeViewModel secondVisit = new ThemeViewModel(app, file);
        Assert.That(secondVisit.Selected!.Name, Is.EqualTo("wmc"));
        Assert.That(secondVisit.SelectedColour!.Name, Is.EqualTo("wmc-dark"),
            "backing out and coming back shows both halves of the choice");

        // The colour rows belong to the worn skin. Switching skins leaves the colour question behind.
        settings.Selected = settings.Themes.First(choice => choice.Name == "nereus");
        Assert.That(settings.HasColours, Is.False,
            "Nereus shipped one theme file, and a section over nothing is not offered");
        Assert.That(settings.Colours, Is.Empty);
        Assert.That(FrontendTheme.CurrentName(app), Is.EqualTo("nereus"));
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

    private static Color BrushColour(Application app, string key)
    {
      Assert.That(app.TryFindResource(key, out object? value), Is.True, key + " is on the application");
      Assert.That(value, Is.InstanceOf<SolidColorBrush>(), key + " is a solid colour");
      return ((SolidColorBrush) value!).Color;
    }

    /// <summary>
    /// A directory the test project was told about at build time; see TestTheme's helper of the same name.
    /// </summary>
    private static string MetadataPath(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestSkinColours).Assembly
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
