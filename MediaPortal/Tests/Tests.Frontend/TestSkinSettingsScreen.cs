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
using Avalonia;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// The skin settings screen: the switches it shows, the ones it must not show, and what a press leaves
  /// behind in the skin's file.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The section is the reference's own <c>/Appearance/SkinSettings</c> area, and its one promise is to fill
  /// itself from the worn skin and only from it - so the WMC test counts its rows at the reference's eleven
  /// and looks for the absence of every Nereus-only row, and the front end's own face is required to show
  /// nothing rather than to borrow a skin's switches.
  /// </para>
  /// <para>
  /// Every test names its own file, so no test reads or writes what a real viewer on this machine set.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestSkinSettingsScreen
  {
    private string _previousDir = "";
    private string _ownDir = "";

    [SetUp]
    public void IsolateTheSkinSettingsFolder()
    {
      _previousDir = Environment.GetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR") ?? "";
      _ownDir = Path.Combine(Path.GetTempPath(), "mp2-skin-screen-" + Guid.NewGuid().ToString("N"));
      Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR", _ownDir);
    }

    [TearDown]
    public void PutTheMachineBack()
    {
      Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR",
          _previousDir.Length == 0 ? null : _previousDir);

      Application? app = Application.Current;
      if (app != null)
        FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);
    }

    private static Application RequireApplication()
    {
      Application? app = Application.Current;
      Assert.That(app, Is.Not.Null, "the screen needs the application to know what skin it wears");
      return app!;
    }

    private static SkinSettingOption Row(SkinSettingsViewModel vm, string label)
    {
      SkinSettingOption? row = vm.Options.FirstOrDefault(o => o.Label == label);
      Assert.That(row, Is.Not.Null, "no row called '" + label + "' on the screen");
      return row!;
    }

    private static string FileIn(string dir, string skin)
    {
      Directory.CreateDirectory(dir);
      return Path.Combine(dir, "skin-" + skin + ".json");
    }

    [AvaloniaTest]
    public void NereusShowsItsSwitchesAndAPressSaves()
    {
      Application app = RequireApplication();
      FrontendTheme.Apply(app, "nereus", out _);

      string file = FileIn(_ownDir, "nereus");
      SkinSettingsViewModel vm = new SkinSettingsViewModel(app, file);

      Assert.That(vm.SkinName, Is.EqualTo("nereus"));
      Assert.That(Row(vm, "Fanart behind the media").ValueText, Is.EqualTo("On"),
          "the screen opens on the reference's default, not on a blank");
      Assert.That(Row(vm, "Skin colour").ValueText, Is.EqualTo("Turquoise"));

      vm.Selected = Row(vm, "Fanart behind the media");
      vm.PressSelected();
      Assert.That(Row(vm, "Fanart behind the media").ValueText, Is.EqualTo("Off"));
      Assert.That(Row(vm, "Fanart behind the media").IsChecked, Is.False,
          "the row's own checkbox has to show the answer it just took");
      Assert.That(vm.Status, Does.Contain("Fanart behind the media is now Off"));

      NereusSkinSettings saved = NereusSkinSettings.Load(file, out _);
      Assert.That(saved.EnableFanart, Is.False, "the press has to reach the file, not just the screen");

      // The checkbox path is the mouse's whole interaction: writing the checkbox IS the change and the save.
      Row(vm, "Show time").IsChecked = false;
      Assert.That(vm.Status, Does.Contain("Show time is now Off"));
      saved = NereusSkinSettings.Load(file, out _);
      Assert.That(saved.ShowTime, Is.False, "the checkbox has to reach the file on its own, no press needed");

      // The colour row is one question, so a press walks to the next family and takes the eight bools with it.
      vm.Selected = Row(vm, "Skin colour");
      vm.PressSelected();
      Assert.That(Row(vm, "Skin colour").ValueText, Is.EqualTo("Yellow"));

      saved = NereusSkinSettings.Load(file, out _);
      Assert.That(saved.UseYellow, Is.True);
      Assert.That(saved.UseTorquoise, Is.False);
      Assert.That(new[] { saved.UseYellow, saved.UseOrange, saved.UseRed, saved.UsePurple,
              saved.UseGreen, saved.UseBlue, saved.UseGrey }.Count(v => v), Is.EqualTo(1),
          "one family at a time, the way the reference's dialog wore them");
    }

    [AvaloniaTest]
    public void WmcShowsExactlyItsThirteenRows()
    {
      Application app = RequireApplication();
      FrontendTheme.Apply(app, "wmc", out _);

      SkinSettingsViewModel vm = new SkinSettingsViewModel(app, FileIn(_ownDir, "wmc"));

      Assert.That(vm.Options.Count, Is.EqualTo(13),
          "WMC's settings class holds its eleven switches plus the two formats the reference's skin base "
          + "spoke; the screen may show neither more nor fewer");
      Assert.That(vm.Options.Any(o => o.Label == "Home content"), Is.True);
      Assert.That(Row(vm, "Time format").IsText, Is.True, "a format is typed, as its row always was");
      Assert.That(Row(vm, "Date format").Words, Is.EqualTo("D"));

      foreach (string foreign in new[] { "Skin colour", "Header transparency", "Show time", "Help texts",
              "Auto scrolling", "Grid details", "Touch display", "Fanart blur" })
      {
        Assert.That(vm.Options.Any(o => o.Label == foreign), Is.False,
            "'" + foreign + "' is Nereus's switch; on WMC the screen would be lying");
      }
    }

    [AvaloniaTest]
    public void TheOwnFaceShipsNoSwitches()
    {
      Application app = RequireApplication();
      FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);

      SkinSettingsViewModel vm = new SkinSettingsViewModel(app, Path.Combine(_ownDir, "skin-default.json"));

      Assert.That(vm.Options.Count, Is.Zero, "the front end's own face has no skin settings to show");
      Assert.That(vm.Subtitle, Does.Contain("own face"));
      Assert.That(File.Exists(Path.Combine(_ownDir, "skin-default.json")), Is.False,
          "a screen with nothing to say writes nothing");
    }

    /// <summary>
    /// The two format rows are the only ones on this screen that are written rather than worked: typing into
    /// one is the change and the save, and pressing the row - which is what a remote does to a highlighted
    /// row - has to leave it alone rather than walk a sentence somewhere.
    /// </summary>
    [AvaloniaTest]
    public void AFormatIsTypedIntoTheSkinAndNothingElseHappensToIt()
    {
      Application app = RequireApplication();
      FrontendTheme.Apply(app, "nereus", out _);
      string file = FileIn(_ownDir, "nereus");

      SkinSettingsViewModel vm = new SkinSettingsViewModel(app, file);
      SkinSettingOption clock = Row(vm, "Time format");

      Assert.That(clock.Words, Is.EqualTo("t"), "the screen opens on the reference's own format");
      Assert.That(clock.IsText, Is.True);
      Assert.That(clock.IsToggle, Is.False);
      Assert.That(clock.ShowsButton, Is.False, "a typed row wears a box, not a button walking nowhere");

      vm.Selected = clock;
      vm.PressSelected();
      Assert.That(clock.Words, Is.EqualTo("t"), "pressing a written row cannot advance it anywhere");
      Assert.That(vm.Status, Is.Empty.Or.EqualTo(""), "and says nothing about a change it did not make");

      clock.Words = "  HH:mm  ";
      Assert.That(vm.Status, Does.Contain("Time format is now HH:mm"));

      NereusSkinSettings saved = NereusSkinSettings.Load(file, out _);
      Assert.That(saved.TimeFormat, Is.EqualTo("HH:mm"),
          "the spaces around what somebody typed are not part of the answer");
      Assert.That(saved.DateFormat, Is.EqualTo("D"), "the day was not sent and stands as it was");

      // A format the machine cannot write with is the viewer's own mistake to make; the head line falls back
      // rather than going blank, and the screen keeps what was written so it can be seen and corrected.
      clock.Words = "%";
      saved = NereusSkinSettings.Load(file, out _);
      Assert.That(saved.TimeFormat, Is.EqualTo("%"), "the screen stores what was typed");
      Assert.That(SkinSettingsBase.Written(saved.TimeFormat, SkinSettingsBase.DEFAULT_TIME_FORMAT),
          Is.EqualTo("t"), "but the clock reads its own words through the fallback");
    }

    [AvaloniaTest]
    public void APressReopensInTheNextScreen()
    {
      Application app = RequireApplication();
      FrontendTheme.Apply(app, "nereus", out _);
      string file = FileIn(_ownDir, "nereus");

      SkinSettingsViewModel first = new SkinSettingsViewModel(app, file);
      first.Selected = Row(first, "Movie grid");
      first.PressSelected();
      Assert.That(Row(first, "Movie grid").ValueText, Is.EqualTo("Banner"));

      SkinSettingsViewModel second = new SkinSettingsViewModel(app, file);
      Assert.That(Row(second, "Movie grid").ValueText, Is.EqualTo("Banner"),
          "a choice made once has to greet the viewer the next time, too");

      // And the step rows walk their range and wrap: 0.85 -> 0.9 -> 0.95 -> 1 -> 0.
      SkinSettingOption opacity = Row(second, "Fanart overlay opacity");
      foreach (string expected in new[] { "0.9", "0.95", "1", "0" })
      {
        second.Selected = opacity;
        second.PressSelected();
        Assert.That(opacity.ValueText, Is.EqualTo(expected));
      }
    }

    /// <summary>
    /// The blur row offers six answers - plain and five turns - wraps back to plain after its highest, and
    /// leaves the turn it ended on in the skin's file like every other row does.
    /// </summary>
    [AvaloniaTest]
    public void TheBlurDialWalksItsTurnsAndWrapsBackToPlain()
    {
      Application app = RequireApplication();
      FrontendTheme.Apply(app, "nereus", out _);
      string file = FileIn(_ownDir, "nereus");

      SkinSettingsViewModel vm = new SkinSettingsViewModel(app, file);
      SkinSettingOption blur = Row(vm, "Fanart blur");

      Assert.That(blur.ValueText, Is.EqualTo("0"),
          "the screen greets a viewer who never touched the dial with the library's own picture");

      foreach (string expected in new[] { "1", "2", "3", "4", "5", "0" })
      {
        vm.Selected = blur;
        vm.PressSelected();
        Assert.That(blur.ValueText, Is.EqualTo(expected), "the dial does not walk its six answers");
      }

      Assert.That(NereusSkinSettings.Load(file, out _).FanartBlur, Is.Zero,
          "the wrap back to plain has to reach the file, not only the screen");
    }
  }
}