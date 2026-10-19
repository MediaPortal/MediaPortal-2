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
using MediaPortal.Frontend.Theming;

namespace Tests.Frontend
{
  /// <summary>
  /// The skins that ship as loose XAML, and the engine that reads them.
  /// </summary>
  /// <remarks>
  /// <para>
  /// These tests are the difference between a skin that is a colour table in a C# file and a skin that is a
  /// skin: the files under the front end's Skins folder have to be read as they are - manifest, colours,
  /// measurements, master templates, control styles, overlays - and answer through the application exactly as
  /// the reference SkinEngine answered from its folders of XAML. Everything asserted here is asked of the real
  /// shipped files, not of copies, because a typo in a skin file is precisely the kind of wrong these tests
  /// exist to catch.
  /// </para>
  /// <para>
  /// The default palette is put back after every test, so no other fixture finds the application dressed in
  /// Nereus; the one test that wears its own broken skin puts the engine's asset root back too.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestSkinEngine
  {
    private string _previousAssetsRoot = "";

    [SetUp]
    public void RememberTheAssetRoot()
    {
      _previousAssetsRoot = SkinEngine.AssetsRoot;
    }

    [TearDown]
    public void PutTheDefaultBack()
    {
      Application? app = Application.Current;
      if (app != null)
        FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);

      SkinEngine.AssetsRoot = _previousAssetsRoot;
    }

    [AvaloniaTest]
    public void BothRescuedSkinsAreInstalled()
    {
      string[] names = SkinEngine.Installed().Select(skin => skin.Name).ToArray();

      Assert.That(names, Does.Contain("nereus"));
      Assert.That(names, Does.Contain("wmc"));
    }

    [Test]
    public void TheSkinIndexAndTheSkinFoldersAgree()
    {
      // Avalonia cannot enumerate its asset folders, so index.txt is the list of what is installed - which
      // makes it the one hand-written line in this system, and the one place a new skin folder could be
      // added and forgotten. Read the shipped index and the source tree's folders and compare them here,
      // where forgetting the second half of "copy a folder and list it" fails a test instead of a couch.
      string skinsDirectory = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      string[] folders = Directory.GetDirectories(skinsDirectory)
          .Where(directory => File.Exists(Path.Combine(directory, "skin.xml")))
          .Select(directory => Path.GetFileName(directory))
          .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
          .ToArray();

      string[] listed = File.ReadAllLines(Path.Combine(skinsDirectory, "index.txt"))
          .Select(line => line.Trim())
          .Where(line => line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal))
          .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
          .ToArray();

      Assert.That(listed, Is.EquivalentTo(folders),
          "Skins/index.txt and the skin folders under Skins/ have to name each other");
    }

    [AvaloniaTest]
    public void AManifestSaysWhatItsSkinIs()
    {
      SkinManifest nereus = SkinEngine.Installed().First(skin => skin.Name == "nereus");

      Assert.That(nereus.Title, Is.EqualTo("Nereus"));
      Assert.That(nereus.Author, Is.EqualTo("Team MediaPortal"));
      Assert.That(nereus.DefaultTheme.Name, Is.EqualTo("default"));
      Assert.That(nereus.SuperlayersFile, Is.Not.Null.And.Not.Empty);
    }

    [AvaloniaTest]
    public void WmcShipsItsThreeReferenceThemes()
    {
      SkinManifest wmc = SkinEngine.Installed().First(skin => skin.Name == "wmc");

      Assert.That(wmc.ThemeByName("dark"), Is.Not.Null, "Dark-WMC was one of the three schemes WMC shipped");
      Assert.That(wmc.ThemeByName("turquoise"), Is.Not.Null, "Turquoise-WMC was one of the three");
      Assert.That(wmc.DefaultTheme.IsDefault, Is.True);
    }

    [AvaloniaTest]
    public void WearingNereusAnswersWhatOnlyItsFilesAnswer()
    {
      Application app = Application.Current!;
      Assert.That(SkinEngine.Apply(app, "nereus", out string? problem), Is.True, problem ?? "applied");

      Assert.That(Find(app, "NereusColorLight_Torquoise"), Is.EqualTo(Color.Parse("#FF009592")),
          "the reference's own colour name, from the skin's colour file");
      Assert.That(Find(app, "HeaderFontSize"), Is.EqualTo(80.0), "the reference's measurement, from its Consts file");
      Assert.That(Find(app, "ChromeHeaderTemplate"), Is.InstanceOf<Avalonia.Controls.Templates.IControlTemplate>(),
          "the master template every screen is drawn inside");
      Assert.That(Find(app, "Superlayer_Volume"), Is.InstanceOf<Avalonia.Controls.Templates.IControlTemplate>(),
          "the overlay that floats above every screen");
    }

    [AvaloniaTest]
    public void BothShippedSkinsAnswerTheSkipStepLayer()
    {
      // The playback screen names the jump on the skin's own layer, for every skin and not just Nereus -
      // a screen asking for a layer the worn skin does not have would say nothing on that skin, which is
      // exactly the complaint the view-switcher once was.
      Application app = Application.Current!;

      Assert.That(SkinEngine.Apply(app, "nereus", out string? nereusProblem), Is.True, nereusProblem ?? "applied");
      Assert.That(Find(app, "Superlayer_SkipStep"),
          Is.InstanceOf<Avalonia.Controls.Templates.IControlTemplate>(),
          "Nereus says the skip step on its slab, like the reference's SkipStepOSD superlayer");

      Assert.That(SkinEngine.Apply(app, "wmc", out string? wmcProblem), Is.True, wmcProblem ?? "applied");
      Assert.That(Find(app, "Superlayer_SkipStep"),
          Is.InstanceOf<Avalonia.Controls.Templates.IControlTemplate>(),
          "and WMC wears its own answer to the same key");
    }

    [AvaloniaTest]
    public void BothShippedSkinsAnswerThePlaybackBar()
    {
      // The playback screen hands the bar under its picture to the skin, the same way its lists hand over their
      // tiles, and both skins have to answer: a screen asking for a bar the worn skin does not draw would show
      // the reading of the playback on one skin and not on the next, which is exactly the complaint the
      // view-switcher once was. The palette alone answers nothing, so a viewer wearing no skin keeps the rows the
      // screen was written with and sees no second bar beside them.
      Application app = Application.Current!;

      Assert.That(SkinEngine.Apply(app, "nereus", out string? nereusProblem), Is.True, nereusProblem ?? "applied");
      Assert.That(Find(app, "NowPlayingOverlayTemplate"),
          Is.InstanceOf<Avalonia.Controls.Templates.IDataTemplate>(),
          "Nereus draws the playback bar the way its CurrentlyPlaying screens drew the reading of what plays");

      Assert.That(SkinEngine.Apply(app, "wmc", out string? wmcProblem), Is.True, wmcProblem ?? "applied");
      Assert.That(Find(app, "NowPlayingOverlayTemplate"),
          Is.InstanceOf<Avalonia.Controls.Templates.IDataTemplate>(),
          "and WMC wears its own answer to the same key, its band rather than Nereus' slab");

      FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);
      Assert.That(Find(app, "NowPlayingOverlayTemplate"), Is.Null,
          "the default theme keeps silent about the playback bar, and the screen draws what it was written with");
    }

    [AvaloniaTest]
    public void BothShippedSkinsAnswerTheDialogFrame()
    {
      // A dialog names the frame it stands on the way a list names its tiles and the playback screen names
      // its bar, and both shipped skins have to answer: a dialog the worn skin dresses on one skin and not on
      // the next is exactly the complaint the view-switcher once was. The two answers are meant to look
      // different - Nereus keeps its corners square until the dialog corner flag exists, WMC rounds by its
      // own DialogBorderCornerRadius and trims the card with the accent - and the default palette answers
      // nothing, so a machine wearing no skin keeps the frame the dialog's own view was written with.
      Application app = Application.Current!;

      Assert.That(SkinEngine.Apply(app, "nereus", out string? nereusProblem), Is.True, nereusProblem ?? "applied");
      Assert.That(Find(app, "DialogFrameTemplate"),
          Is.InstanceOf<Avalonia.Controls.Templates.IDataTemplate>(),
          "Nereus frames a dialog the way its dialog master did: a dark slab, outlined, square");

      Assert.That(SkinEngine.Apply(app, "wmc", out string? wmcProblem), Is.True, wmcProblem ?? "applied");
      Assert.That(Find(app, "DialogFrameTemplate"),
          Is.InstanceOf<Avalonia.Controls.Templates.IDataTemplate>(),
          "and WMC wears its own answer to the same key, its rounded card rather than Nereus' slab");

      FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);
      Assert.That(Find(app, "DialogFrameTemplate"), Is.Null,
          "the default theme keeps silent about dialog frames, and the dialog draws what it was written with");
    }

    [AvaloniaTest]
    public void BothShippedSkinsCarryTheTransportPicturesThePlaybackBarWears()
    {
      // Both bars say the state of play with the skin's own two pictures, asked by the name this front end
      // spells from the reference's transport buttons - PlayerControl/PLAY.NOFOCUS and its PAUSE sibling. A name
      // a skin does not carry is not an error but a quiet hole on the television, so the two names are asked of
      // the shipped folders here, the way the home tiles' pictures are.
      Application app = Application.Current!;
      string? previousFolder = SkinEngine.SkinsFolder;
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      try
      {
        Assert.That(SkinEngine.Apply(app, "nereus", out string? nereusProblem), Is.True,
            nereusProblem ?? "applied");
        SkinImages nereus = SkinEngine.Worn(app)!.Images;
        Assert.That(nereus.Has("PlayerControl/PLAY.NOFOCUS"), Is.True,
            "Nereus has no play picture for its bar, which would then say nothing while a film ran");
        Assert.That(nereus.Has("PlayerControl/PAUSE.NOFOCUS"), Is.True,
            "and it has no pause picture either, so a paused film would wear nothing");
        SkinEngine.Clear(app);

        Assert.That(SkinEngine.Apply(app, "wmc", out string? wmcProblem), Is.True, wmcProblem ?? "applied");
        SkinImages wmc = SkinEngine.Worn(app)!.Images;
        Assert.That(wmc.Has("PlayerControl/PLAY.NOFOCUS"), Is.True,
            "WMC's band asks for the same two words and its skin carries them under the same names");
        Assert.That(wmc.Has("PlayerControl/PAUSE.NOFOCUS"), Is.True, "both spellings included");
      }
      finally
      {
        SkinEngine.SkinsFolder = previousFolder;
      }
    }

    [AvaloniaTest]
    public void WearingADarkThemeReadsItsOwnColoursOverTheDefaults()
    {
      Application app = Application.Current!;
      Assert.That(SkinEngine.Apply(app, "wmc-dark", out string? problem), Is.True, problem ?? "applied");

      Assert.That(Find(app, "WindowHeaderColor"), Is.EqualTo(Color.Parse("#FF757575")),
          "the dark scheme's own value, read over the default theme's file");
      Assert.That(Find(app, "HeaderFontSize"), Is.EqualTo(80.0),
          "the structure of the skin's default theme is still underneath");
    }

    [AvaloniaTest]
    public void ARetiredNameWearsItsSkin()
    {
      Application app = Application.Current!;
      FrontendTheme.Apply(app, FrontendTheme.SelectedName(null, "wmc-turquoise"), out _);

      WornSkin? worn = SkinEngine.Worn(app);
      Assert.That(worn, Is.Not.Null, "a retired name still names a real skin, and that skin is what gets worn");
      Assert.That(worn!.Manifest.Name, Is.EqualTo("wmc"));
      Assert.That(worn.Theme.Name, Is.EqualTo("default"),
          "the tint was a setting of the skin, not one of its themes; until it returns as a setting, "
          + "the skin wears what it shipped with");
    }

    [AvaloniaTest]
    public void TheSkinsColourFilesAgreeWithThePaletteTable()
    {
      Application app = Application.Current!;
      foreach (string name in new[] { "nereus", "wmc" })
      {
        Assert.That(SkinEngine.Apply(app, name, out string? problem), Is.True, problem ?? name + " applied");
        WornSkin worn = SkinEngine.Worn(app)!;

        foreach (string key in FrontendTheme.Keys)
        {
          Assert.That(worn.Resources.TryGetValue(key, out object? fromSkin), Is.True,
              name + "." + key + " is answered by neither the skin file nor the engine");
          Assert.That(((ISolidColorBrush)fromSkin!).Color, Is.EqualTo(FrontendTheme.ColorOf(name, key)),
              string.Format("{0}.{1}: the skin file and the palette table say different colours, and one of them "
                  + "is no longer the skin", name, key));
        }
      }
    }

    [AvaloniaTest]
    public void ThePaletteAndTheSkinStandOnTheApplicationTogether()
    {
      // Two layers ride on the application when a skin is worn: the palette, which answers the fourteen keys the
      // screens ask for by name, and the skin's own dictionary, which answers the reference's names. Neither may
      // hide the other - a screen asking for AccentBrush and a screen asking for NereusColorLight_Torquoise have
      // to be answered from one layer and the other in the same frame.
      Application app = Application.Current!;
      FrontendTheme.Apply(app, "nereus", out _);

      Assert.That(app.TryFindResource("AccentBrush", out object? brush), Is.True);
      Assert.That(((ISolidColorBrush)brush!).Color,
          Is.EqualTo(FrontendTheme.ColorOf("nereus", "AccentBrush")));
      Assert.That(SkinEngine.Worn(app), Is.Not.Null, "the palette does not displace the skin around it");
      Assert.That(app.TryFindResource("NereusColorLight_Torquoise", out object? fromSkin), Is.True,
          "and the reference's own colour names are still answered while the palette speaks");
    }

    [AvaloniaTest]
    public void GoingBackToDefaultTakesTheSkinOff()
    {
      Application app = Application.Current!;
      SkinEngine.Apply(app, "nereus", out _);
      FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);

      Assert.That(SkinEngine.Worn(app), Is.Null);
      Assert.That(app.TryFindResource("NereusColorLight_Torquoise", out _), Is.False,
          "a key only the skin file speaks must go quiet with the skin");
    }

    [AvaloniaTest]
    public void TheSkinXamlDressesTheButtons()
    {
      Application app = Application.Current!;
      FrontendTheme.Apply(app, "nereus", out _);

      Button button = new Button { Content = "Ok" };
      Window window = new Window { Content = button, Width = 200, Height = 100 };
      window.Show();
      window.Measure(new Size(200, 100));
      window.Arrange(new Rect(0, 0, 200, 100));

      Assert.That(button.Background, Is.InstanceOf<ISolidColorBrush>());
      Assert.That(((ISolidColorBrush)button.Background!).Color,
          Is.EqualTo(FrontendTheme.ColorOf("nereus", "PanelBrush")),
          "the resting button is painted by the skin's styles.xaml, from the skin's colours.xaml");

      window.Close();
    }

    [AvaloniaTest]
    public void ABrokenSkinIsSaidAndTheOldOneStaysOn()
    {
      // The tests carry their own skin under TestSkins/, so the broken one cannot be confused with a shipped
      // skin: a manifest that lists a file which is not there.
      SkinEngine.AssetsRoot = "avares://Tests.Frontend/TestSkins/";

      Assert.That(SkinEngine.Installed().Select(skin => skin.Name), Does.Contain("broken"),
          "the test skin has to be found by the same reading as a real one");

      bool applied = SkinEngine.Apply(Application.Current!, "broken", out string? problem);

      Assert.That(applied, Is.False, "a skin whose manifest names a file that is not there does not get worn");
      Assert.That(problem, Is.Not.Null.And.Contains("broken"), "and the reason is said, not swallowed");
    }

    [AvaloniaTest]
    public void ANameWithoutASkinFolderAsksForNone()
    {
      Assert.That(SkinEngine.IsSkin("nereus"), Is.True);
      Assert.That(SkinEngine.IsSkin("nereus-yellow"), Is.True, "an accent family still names the Nereus folder");
      Assert.That(SkinEngine.IsSkin(FrontendTheme.DEFAULT_NAME), Is.False);
      Assert.That(SkinEngine.IsSkin("graveyard"), Is.False);
    }

    private static object? Find(Application app, string key)
    {
      return app.TryFindResource(key, out object? value) ? value : null;
    }

    private static string MetadataPath(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestSkinEngine).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(attribute => attribute.Key == name);

      Assert.That(value, Is.Not.Null, "the tests are built without the " + name + " location");
      return value!.Value!;
    }
  }
}
