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
using Avalonia.Controls.Presenters;
using Avalonia.Headless.NUnit;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Skins.Nereus;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// One question - what colour is this skin - has to have one answer on the screen.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The row a viewer stands on in a list, the box they tick and the bar of a slider are drawn by no file of the
  /// skin's: they belong to the theme the controls arrive in, and that theme colours them from its own accent,
  /// which it ships as <c>SystemAccentColor</c> and six steps of it. Left to itself it answers its stock blue, so
  /// a guide, a dialog's list and the settings rows stayed blue while everything the skin dresses itself wore the
  /// skin's colour. These tests read what is actually painted rather than what a property was set to, because the
  /// theme paints its selection inside the item's template, over the top of whatever the skin put on the item.
  /// </para>
  /// <para>
  /// The colour a viewer picks on the skin settings screen is the same question, so the row that walks the eight
  /// families has to move the painted row too - which is checked here through the real row, the real file and the
  /// real re-dress, with the settings folder pointed at a directory of the test's own.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestTheRowAListStandsOnWearsTheSkinsColour
  {
    /// <summary>The blue the platform's theme ships, which must not be seen anywhere in a skinned front end.</summary>
    private static readonly Color PLATFORM_BLUE = Color.Parse("#FF0078D7");

    private string _skinsFolderBefore = "";
    private string _settingsDirBefore = "";
    private string _ownSettingsDir = "";

    [SetUp]
    public void IsolateTheSkinsFolderAndTheSettingsFolder()
    {
      _skinsFolderBefore = SkinEngine.SkinsFolder;
      _settingsDirBefore = Environment.GetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR") ?? "";
      _ownSettingsDir = Path.Combine(Path.GetTempPath(), "mp2-accent-" + Guid.NewGuid().ToString("N"));
      Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR", _ownSettingsDir);
    }

    [TearDown]
    public void PutTheMachineBack()
    {
      Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR",
          _settingsDirBefore.Length == 0 ? null : _settingsDirBefore);
      if (Directory.Exists(_ownSettingsDir))
        Directory.Delete(_ownSettingsDir, true);

      Application? app = Application.Current;
      if (app != null)
      {
        SkinEngine.Clear(app);
        FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);
      }

      SkinEngine.SkinsFolder = _skinsFolderBefore;
    }

    private static string MetadataPath(string name)
    {
      AssemblyMetadataAttribute? value = typeof(TestTheRowAListStandsOnWearsTheSkinsColour).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, name, StringComparison.Ordinal));

      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project has to hand " + name + " over for this to be checkable");

      return value!.Value!;
    }

    private static void Pump()
    {
      for (int round = 0; round < 20; round++)
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>Wears one of the shipped faces, off the disk rather than off a build's copy.</summary>
    private static void Wear(string skin)
    {
      Application app = Application.Current!;
      SkinEngine.SkinsFolder = Path.Combine(MetadataPath("FrontendSource"), "Skins");
      FrontendTheme.Apply(app, skin, out _);
      Assert.That(FrontendTheme.CurrentName(app), Is.EqualTo(skin), "the face has to actually go on");

      if (!skin.Equals(FrontendTheme.DEFAULT_NAME, StringComparison.OrdinalIgnoreCase))
      {
        Assert.That(SkinEngine.Apply(app, skin, out string? problem), Is.True, problem ?? "");
      }
    }

    /// <summary>
    /// What a list paints the row it is standing on, read out of the item's own template - the brush the viewer
    /// sees, which is what the theme puts there and not what a style set on the item.
    /// </summary>
    private static Color PaintedRow(out ListBoxItem item)
    {
      Window window = new Window { Width = 900, Height = 500 };
      ListBox list = new ListBox { ItemsSource = new[] { "one", "two", "three" }, SelectedIndex = 1 };
      window.Content = list;
      window.Show();
      Pump();
      window.Focus();
      list.Focus();
      Pump();

      item = list.GetLogicalDescendants().OfType<ListBoxItem>().ElementAt(1);
      ContentPresenter? plate = item.GetVisualDescendants().OfType<ContentPresenter>()
          .FirstOrDefault(c => c.Name == "PART_ContentPresenter");
      Assert.That(plate, Is.Not.Null, "a list row paints itself through its template's presenter");

      Assert.That(plate!.Background, Is.InstanceOf<ISolidColorBrush>(),
          "the row a list stands on is a flat colour, so it can be read back");
      return ((ISolidColorBrush)plate.Background!).Color;
    }

    /// <summary>Resource lookup as a control sees it, which is how the theme's own rules reach these names.</summary>
    private static object? LookUp(Control at, string key)
    {
      Assert.That(at.TryFindResource(key, at.ActualThemeVariant, out object? value), Is.True,
          "the theme says nothing about " + key);
      return value;
    }

    private static double LightnessOf(Color colour)
    {
      double highest = Math.Max(colour.R, Math.Max(colour.G, colour.B)) / 255.0;
      double lowest = Math.Min(colour.R, Math.Min(colour.G, colour.B)) / 255.0;
      return (highest + lowest) / 2.0;
    }

    [AvaloniaTest]
    public void TheRowAListStandsOnIsTheSkinsAccentAndNotThePlatformsBlue()
    {
      Wear("nereus");
      Color painted = PaintedRow(out _);

      Assert.That(painted, Is.EqualTo(FrontendTheme.ColorOf("nereus", "AccentBrush")),
          "the row a list is standing on has to be the colour the skin calls its accent");
      Assert.That(painted, Is.Not.EqualTo(PLATFORM_BLUE),
          "the platform's stock blue is the one colour a skinned front end must never paint a selection with");
    }

    [AvaloniaTest]
    public void ThePlatformIsTaughtTheSkinsAccentAndTheSixStepsAroundIt()
    {
      Wear("nereus");
      Color accent = FrontendTheme.ColorOf("nereus", "AccentBrush");
      Window probe = new Window { Width = 200, Height = 200 };
      probe.Show();
      Pump();

      Assert.That((Color)LookUp(probe, "SystemAccentColor")!, Is.EqualTo(accent),
          "the accent the controls colour themselves from is the skin's own");
      Assert.That((Color)LookUp(probe, "SystemAccentColor")!, Is.Not.EqualTo(PLATFORM_BLUE));

      double worn = LightnessOf(accent);
      Color[] lighter =
      {
        (Color)LookUp(probe, "SystemAccentColorLight1")!,
        (Color)LookUp(probe, "SystemAccentColorLight2")!,
        (Color)LookUp(probe, "SystemAccentColorLight3")!
      };
      Color[] darker =
      {
        (Color)LookUp(probe, "SystemAccentColorDark1")!,
        (Color)LookUp(probe, "SystemAccentColorDark2")!,
        (Color)LookUp(probe, "SystemAccentColorDark3")!
      };

      for (int step = 0; step < lighter.Length; step++)
      {
        Assert.That(LightnessOf(lighter[step]), Is.GreaterThan(worn).Or.EqualTo(worn),
          "step " + (step + 1) + " above the accent has to be lighter, not the same colour");
        Assert.That(LightnessOf(darker[step]), Is.LessThan(worn).Or.EqualTo(worn),
          "step " + (step + 1) + " below the accent has to be darker, not the same colour");
        if (step > 0)
        {
          Assert.That(LightnessOf(lighter[step]), Is.GreaterThanOrEqualTo(LightnessOf(lighter[step - 1])));
          Assert.That(LightnessOf(darker[step]), Is.LessThanOrEqualTo(LightnessOf(darker[step - 1])));
        }
      }

      // A step is the same colour stronger or weaker, never a different one: turquoise stays turquoise, which
      // for this accent means green and blue hold their order and red stays the weakest channel. A step that has
      // been pushed off the end of the scale is black or white, and black has no hue to keep.
      foreach (Color step in lighter.Concat(darker))
      {
        bool hasAHue = Math.Max(step.R, Math.Max(step.G, step.B)) - Math.Min(step.R, Math.Min(step.G, step.B)) > 0;
        if (!hasAHue)
          continue;

        Assert.That(step.G, Is.GreaterThan(step.R), "a step off turquoise is still turquoise");
        Assert.That(step.B, Is.GreaterThan(step.R), "a step off turquoise is still turquoise");
      }
    }

    [AvaloniaTest]
    public void TheAccentIsStampedOnTheApplicationItselfAndSaidAgainAfterAFaceChange()
    {
      Application app = Application.Current!;
      Wear("nereus");
      Color accent = FrontendTheme.ColorOf("nereus", "AccentBrush");

      // Not merely in a dictionary merged into the application: the theme the controls come with answers its
      // accent from the operating system and writes that onto the application itself, which outranks every
      // dictionary merged underneath. A skin that only merges therefore loses the question to whatever colour
      // the machine's desktop happens to wear.
      Assert.That(app.Resources["SystemAccentColor"], Is.EqualTo(accent),
          "the application itself has to answer its accent with the skin's colour");
      Assert.That(app.Resources["SystemAccentColorDark3"], Is.Not.Null,
          "and the whole ramp, not just the accent");

      // The theme writes its own accent again whenever the light-or-dark answer changes, and would then have
      // the last word. So the skin says its colour again at exactly that moment.
      app.Resources["SystemAccentColor"] = PLATFORM_BLUE;
      ThemeVariant was = app.RequestedThemeVariant;
      app.RequestedThemeVariant = ThemeVariant.Light;
      try
      {
        Assert.That(app.Resources["SystemAccentColor"], Is.EqualTo(accent),
            "a late write by the theme must not leave the platform's colour standing");
      }
      finally
      {
        app.RequestedThemeVariant = was;
      }
    }

    [AvaloniaTest]
    public void EveryFacePaintsItsStandingRowItsOwnAccent()
    {
      foreach (string face in FrontendTheme.Names)
      {
        Wear(face);
        Color painted = PaintedRow(out _);

        Assert.That(painted, Is.EqualTo(FrontendTheme.ColorOf(face, "AccentBrush")),
            face + " has to paint a selected row with the accent it names, not with a stock blue");
      }
    }

    [AvaloniaTest]
    public void TheColourRowMovesTheRowAListStandsOn()
    {
      Application app = Application.Current!;
      Wear("nereus");

      string file = Path.Combine(_ownSettingsDir, "skin-nereus.json");
      SkinSettingsViewModel screen = new SkinSettingsViewModel(app, file);
      SkinSettingOption colour = screen.Options.First(o => o.Label == "Skin colour");

      Assert.That(colour.ValueText, Is.EqualTo("Turquoise"));
      Assert.That(PaintedRow(out _), Is.EqualTo(FrontendTheme.ColorOf("nereus", "AccentBrush")));

      // The viewer's press: the row walks to the next family, the file is written, and the television follows
      // without a restart - which is the whole point of asking the question on the couch.
      colour.Press();
      Assert.That(colour.ValueText, Is.EqualTo("Yellow"));
      NereusSkinSettings saved = NereusSkinSettings.Load(file, out string? read);
      Assert.That(read, Is.Null, read ?? "");
      Assert.That(saved.UseYellow, Is.True);

      (string name, string light, string medium, string dark) = saved.ChosenColour;
      Assert.That(name, Is.EqualTo("Yellow"));
      Assert.That(PaintedRow(out _), Is.EqualTo(Color.Parse(light)),
          "picking a family has to repaint the row a list stands on, not only the parts the skin draws itself");
    }
  }
}
