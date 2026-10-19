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
using NUnit.Framework;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// The settings a skin keeps: the reference's properties, the reference's defaults, per viewer.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Every default asserted here was read off <c>Source\UI\Skins\Nereus\Settings\NereusSkinSettings.cs</c>
  /// and <c>Source\UI\Skins\WMC\Settings\WMCSkinSettings.cs</c>; a mismatch means the port changed what a
  /// viewer of the original would have found switched on, which is the one thing these classes may not do.
  /// The round-trip test is the promise that a choice made in phase C2 will survive to the next screen.
  /// </para>
  /// <para>
  /// Every test names its own file or its own <c>MP2_SKIN_SETTINGS_DIR</c>, so the suite never reads or
  /// writes what a real viewer on this machine set.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestSkinSettings
  {
    private string _previousDir = "";

    [SetUp]
    public void IsolateTheSkinFolder()
    {
      _previousDir = Environment.GetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR") ?? "";
      Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR",
          Path.Combine(Path.GetTempPath(), "mp2-skin-settings-" + Guid.NewGuid().ToString("N")));
    }

    [TearDown]
    public void PutTheMachineBack()
    {
      Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR",
          _previousDir.Length == 0 ? null : _previousDir);
    }

    [Test]
    public void NereusDefaultsAreTheReferencesDefaults()
    {
      NereusSkinSettings s = new NereusSkinSettings();

      Assert.That(s.SkinName, Is.EqualTo("nereus"));
      Assert.That(s.EnableFanart, Is.True);
      Assert.That(s.FanartOverlayOpacity, Is.EqualTo(0.85));
      Assert.That(s.EnableTouchDisplay, Is.False);
      Assert.That(s.EnableCircleMenuImages, Is.False);
      Assert.That(s.EnableMenuSelection, Is.False);
      Assert.That(s.EnableGridDetails, Is.True);
      Assert.That(s.EnableCoverDetails, Is.True);
      Assert.That(s.EnableListWatchedFlags, Is.True);
      Assert.That(s.EnableGridWatchedFlags, Is.True);
      Assert.That(s.EnableCoverWatchedFlags, Is.True);
      Assert.That(s.MovieGridViewType, Is.EqualTo(GridViewType.Poster));
      Assert.That(s.SeriesGridViewType, Is.EqualTo(GridViewType.Poster));
      Assert.That(s.SeasonGridViewType, Is.EqualTo(GridViewType.Poster));
      Assert.That(s.EnableMediaItemDetailsView, Is.True,
          "this front end wears its details by default; the reference's off-by-default belonged to a skin "
          + "where details replaced playing, not to a panel beside the shelf");
      Assert.That(s.EnableAgeCertificationLogos, Is.True);
      Assert.That(s.EnableAutoScrolling, Is.True);
      Assert.That(s.AutoScrollSpeed, Is.EqualTo(20));
      Assert.That(s.AutoScrollDelay, Is.EqualTo(2));
      Assert.That(s.EnableLoopScrolling, Is.True);
      Assert.That(s.UseTransparency, Is.True);
      Assert.That(s.UseNoColor, Is.False);
      Assert.That(s.UseWhiteColor, Is.False);
      Assert.That(s.UseFocusColor, Is.True);
      Assert.That(s.ShowTime, Is.True);
      Assert.That(s.ShowDate, Is.True);
      Assert.That(s.ShowTemperature, Is.True);
      Assert.That(s.ShowWeatherCondition, Is.True);
      Assert.That(s.DialogBackgroundOpacity, Is.EqualTo(0.85));
      Assert.That(s.UseRoundedDialogCorners, Is.False);
      Assert.That(s.EnableHelpTexts, Is.True);
      Assert.That(s.LastSelectedHomeMenuActionId, Is.Null);
      Assert.That(s.HomeMenuActionMediaLists, Is.Empty);
    }

    [Test]
    public void NereusShipsTurquoiseAndThirteenHomeTiles()
    {
      NereusSkinSettings s = new NereusSkinSettings();

      // Exactly one colour is on - the reference's colour dialog guarantees it, and its shipped file says
      // turquoise. If a second one were on, two dialogs would both claim to be worn.
      Assert.That(s.UseTorquoise, Is.True);
      Assert.That(new[] { s.UseNoColor, s.UseWhiteColor, s.UseYellow, s.UseOrange, s.UseRed, s.UsePurple,
          s.UseGreen, s.UseBlue, s.UseGrey }, Is.All.EqualTo(false));

      // The reference's thirteen default tiles, in its order: Movies second, the app launcher last.
      Assert.That(s.HomeMenuActionIds.Length, Is.EqualTo(13));
      Assert.That(s.HomeMenuActionIds[1], Is.EqualTo(new Guid("80d2e2cc-baaa-4750-807b-f37714153751")),
          "Movies, the second tile, exactly where the reference's file has it");
      Assert.That(s.HomeMenuActionIds[12], Is.EqualTo(new Guid("873EB147-C998-4632-8F86-D5E24062BE2E")),
          "and the App Launcher last");

      // An emptied list answers with the shipped one, as the reference's getter did.
      s.HomeMenuActionIds = Array.Empty<Guid>();
      Assert.That(s.HomeMenuActionIds.Length, Is.EqualTo(13));
    }

    [Test]
    public void WmcShipsExactlyItsElevenAndNoColours()
    {
      WMCSkinSettings s = new WMCSkinSettings();

      Assert.That(s.SkinName, Is.EqualTo("wmc"));
      Assert.That(s.EnableFanart, Is.True);
      Assert.That(s.FanartOverlayOpacity, Is.EqualTo(1.0), "WMC overlaid its fanart plain, not at 0.85");
      Assert.That(s.EnableListWatchedFlags, Is.True);
      Assert.That(s.EnableGridWatchedFlags, Is.True);
      Assert.That(s.EnableCoverWatchedFlags, Is.True);
      Assert.That(s.MovieGridViewType, Is.EqualTo(GridViewType.Poster));
      Assert.That(s.SeriesGridViewType, Is.EqualTo(GridViewType.Poster));
      Assert.That(s.SeasonGridViewType, Is.EqualTo(GridViewType.Poster));
      Assert.That(s.EnableHomeContent, Is.True);
      Assert.That(s.EnableMediaItemDetailsView, Is.True, "worn by default on every skin, as above");
      Assert.That(s.EnableAgeCertificationLogos, Is.True);

      // The absence is a fact: Nereus's colour and header switches must not appear on WMC just because a
      // base class made them convenient.
      Assert.That(typeof(WMCSkinSettings).GetProperty("UseTorquoise"), Is.Null);
      Assert.That(typeof(WMCSkinSettings).GetProperty("ShowTime"), Is.Null);
      Assert.That(typeof(WMCSkinSettings).GetProperty("EnableHomeContent"), Is.Not.Null);
      Assert.That(typeof(NereusSkinSettings).GetProperty("EnableHomeContent"), Is.Null,
          "and home content is WMC's question, not Nereus'");
    }

    [Test]
    public void AChoiceSurvivesItsFileAndComesBackNextScreen()
    {
      string file = Path.Combine(Path.GetTempPath(), "mp2-skin-settings-" + Guid.NewGuid().ToString("N") + ".json");

      try
      {
        NereusSkinSettings s = new NereusSkinSettings();
        s.UseTorquoise = false;
        s.UseRed = true;
        s.FanartOverlayOpacity = 0.5;
        s.MovieGridViewType = GridViewType.Banner;
        s.HomeMenuActionIds = new[] { new Guid("80d2e2cc-baaa-4750-807b-f37714153751") };
        string says = s.Save(file, out bool saved);
        Assert.That(saved, Is.True, says);

        NereusSkinSettings back = NereusSkinSettings.Load(file, out string? problem);
        Assert.That(problem, Is.Null, problem ?? "the file reads back cleanly");
        Assert.That(back.UseTorquoise, Is.False);
        Assert.That(back.UseRed, Is.True);
        Assert.That(back.FanartOverlayOpacity, Is.EqualTo(0.5));
        Assert.That(back.MovieGridViewType, Is.EqualTo(GridViewType.Banner));
        Assert.That(back.HomeMenuActionIds.Length, Is.EqualTo(1));
        Assert.That(back.EnableHelpTexts, Is.True, "what was never touched keeps its shipped answer");
      }
      finally
      {
        File.Delete(file);
      }
    }

    [Test]
    public void EachSkinHasItsOwnFileAndNobodyGetsInventedOptions()
    {
      Assert.That(Path.GetFileName(SkinSettingsBase.FileFor("nereus")), Is.EqualTo("skin-nereus.json"));
      Assert.That(Path.GetFileName(SkinSettingsBase.FileFor("WMC")), Is.EqualTo("skin-wmc.json"),
          "the name is answered as the skin is worn by, whatever way it was typed");

      string dir = Environment.GetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR")!;
      Assert.That(Path.GetDirectoryName(SkinSettingsBase.FileFor("nereus")), Is.EqualTo(dir),
          "the override folder is what tests and gates write to");

      SkinSettingsBase? nereus = SkinSettingsBase.LoadFor("nereus", null, out string? p1);
      Assert.That(p1, Is.Null, p1 ?? "a never-written file is not a complaint");
      Assert.That(nereus, Is.InstanceOf<NereusSkinSettings>());

      SkinSettingsBase? wmc = SkinSettingsBase.LoadFor("wmc", null, out string? p2);
      Assert.That(wmc, Is.InstanceOf<WMCSkinSettings>());

      Assert.That(SkinSettingsBase.LoadFor("graveyard", null, out string? p3), Is.Null);
      Assert.That(p3, Does.Contain("graveyard"), "a skin without settings says so instead of inventing some");
    }

    [Test]
    public void ABrokenFileSaysSoAndTheSkinStandsInItsShippedSettings()
    {
      string file = Path.Combine(Path.GetTempPath(), "mp2-skin-settings-" + Guid.NewGuid().ToString("N") + ".json");
      File.WriteAllText(file, "this is not json {{{");

      try
      {
        WMCSkinSettings s = WMCSkinSettings.Load(file, out string? problem);
        Assert.That(problem, Is.Not.Null.And.Contains(file),
            "an unreadable settings file is a fact the application reports, not one it swallows");
        Assert.That(s.EnableHomeContent, Is.True, "and the shipped defaults answer in the meantime");
      }
      finally
      {
        File.Delete(file);
      }
    }

    [Test]
    public void SavingThroughTheBaseCarriesTheSkinsOwnProperties()
    {
      string file = Path.Combine(Path.GetTempPath(), "mp2-skin-settings-" + Guid.NewGuid().ToString("N") + ".json");

      try
      {
        // The reference's save path: whoever holds the settings may hold them as the base - a save must
        // still write the concrete skin's own switches, or a C2 dialog would silently lose its work.
        SkinSettingsBase settings = new NereusSkinSettings { UseGreen = true };
        Assert.That(settings.Save(file, out bool saved), Does.Contain("Saved"));
        Assert.That(saved, Is.True);

        NereusSkinSettings back = NereusSkinSettings.Load(file, out _);
        Assert.That(back.UseGreen, Is.True, "the green survived a save through the base class");
      }
      finally
      {
        File.Delete(file);
      }
    }

    /// <summary>
    /// WMC's one and only switch: master_homemenu hung its entire HomeContentGrid on
    /// <c>EnableHomeContent</c>, so switching it off leaves the menu standing alone - the way the reference
    /// left it. Nereus never had such a switch, and a face without one keeps its content.
    /// </summary>
    [Test]
    public void WmcsOneSwitchCommandsTheWholeContentColumn()
    {
      Assert.That(HomeViewModel.HomeContentShownFor(new WMCSkinSettings()), Is.True,
          "the shipped answer shows the panels WMC was built on");
      Assert.That(HomeViewModel.HomeContentShownFor(new WMCSkinSettings { EnableHomeContent = false }), Is.False,
          "and switching them off hides all of them, not one panel less");
      Assert.That(HomeViewModel.HomeContentShownFor(new NereusSkinSettings()), Is.True,
          "Nereus has no such switch and keeps its content");
      Assert.That(HomeViewModel.HomeContentShownFor(null), Is.True,
          "a face that ships no settings is not an answer of off");
    }
  }
}
