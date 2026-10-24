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
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// Nereus' two switches about the home menu - EnableMenuSelection and its touch twin EnableTouchDisplay -
  /// decide whether the pane beside the menu follows the walking highlight or waits for the tile to be
  /// pressed, and every press is remembered in LastSelectedHomeMenuActionId so tomorrow's home opens on the
  /// shelf the viewer last stood on. That is HomeMenuModel's answer in the reference; these tests are the
  /// port's word that it kept it.
  /// </summary>
  [TestFixture]
  public class TestHomeSelectionDial
  {
    private string _previousDir = "";

    [SetUp]
    public void IsolateTheSkinSettingsFile()
    {
      _previousDir = Environment.GetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR") ?? "";
      Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR",
          Path.Combine(Path.GetTempPath(), "mp2-selection-" + Guid.NewGuid().ToString("N")));
    }

    [TearDown]
    public void PutTheSkinSettingsFileBack()
    {
      Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR", _previousDir.Length == 0 ? null : _previousDir);

      // The face the tests put on comes off again, so no other test wakes up wearing Nereus.
      Application? app = Application.Current;
      if (app != null)
        FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);
    }

    [AvaloniaTest]
    public void ThePaneWaitsForAPressWhenNereusIsToldToWait()
    {
      Application app = RequireApplication();
      FrontendTheme.Apply(app, "nereus", out _);
      new NereusSkinSettings { EnableMenuSelection = true }.Save(null, out _);

      HomeViewModel home = new HomeViewModel();
      MainMenuEntry first = NthEntryWithAnAction(home, 0);
      MainMenuEntry second = NthEntryWithAnAction(home, 1);

      Assert.That(home.PaneEntry, Is.SameAs(home.SelectedEntry),
          "the restored startup selection fills content immediately, as in the reference");
      home.SelectedEntry = first;
      Assert.That(home.PaneEntry, Is.SameAs(first),
          "the initial restored tile already owns the content pane");
      Assert.That(HomeViewModel.WaitsForAPress, Is.True,
          "and the same switch tells the mouse that a click first selects - touch behaviour");

      home.CommitEntry();
      Assert.That(home.PaneEntry, Is.SameAs(first), "pressing the initial tile keeps its content");

      home.SelectedEntry = second;
      Assert.That(home.PaneEntry, Is.SameAs(first),
          "and walking away from the pressed tile leaves the pressed tile's lists where they are");

      Assert.That(NereusSkinSettings.Load(null, out _).LastSelectedHomeMenuActionId, Is.EqualTo(first.HomeImageId),
          "walking past does not count as having stood there - only the press is remembered");

      home.CommitEntry();
      Assert.That(home.PaneEntry, Is.SameAs(second), "the second press moves the pane");
      Assert.That(NereusSkinSettings.Load(null, out _).LastSelectedHomeMenuActionId, Is.EqualTo(second.HomeImageId),
          "and is the answer tomorrow's home starts from");
    }

    [AvaloniaTest]
    public async Task ThePaneFollowsTheWalkAndRemembersEveryStopByDefault()
    {
      Application app = RequireApplication();
      FrontendTheme.Apply(app, "nereus", out _);

      HomeViewModel home = new HomeViewModel();
      MainMenuEntry first = NthEntryWithAnAction(home, 0);
      MainMenuEntry second = NthEntryWithAnAction(home, 1);

      home.SelectedEntry = first;
      await WaitForPaneAsync(home, first);
      Assert.That(home.PaneEntry, Is.SameAs(first), "with both switches off the pane follows the walk");
      Assert.That(home.ContentRows.Count, Is.GreaterThan(0), "and fills itself as it follows");
      Assert.That(HomeViewModel.WaitsForAPress, Is.False,
          "and the desktop answer holds for the mouse too: one click works the tile");
      Assert.That(NereusSkinSettings.Load(null, out _).LastSelectedHomeMenuActionId, Is.EqualTo(first.HomeImageId),
          "and every stop is the reference's selection, so every stop is remembered");

      home.SelectedEntry = second;
      await WaitForPaneAsync(home, second);
      Assert.That(home.PaneEntry, Is.SameAs(second), "and keeps following every stop of the walk");
      Assert.That(NereusSkinSettings.Load(null, out _).LastSelectedHomeMenuActionId, Is.EqualTo(second.HomeImageId));
    }

    /// <summary>
    /// The way a wall goes down belongs to the step onto the next tile, not to the answer that comes after the
    /// settling: measured on the shown screen, a wall that was only taken down by that answer stood untouched for
    /// a full settling after the viewer had decided, and its successor arrived 220 ms later still.
    /// </summary>
    [AvaloniaTest]
    public async Task TheStepOntoANewTileSendsTheStandingWallDown()
    {
      Application app = RequireApplication();
      FrontendTheme.Apply(app, "nereus", out _);

      HomeViewModel home = new HomeViewModel();
      Assert.That(home.IsNereusHome, Is.True,
          "the wall and its way down are Nereus' arrangement, so this screen has to be wearing Nereus");
      Assert.That(HomeViewModel.WaitsForAPress, Is.False,
          "and the pane has to follow the walk, or a step onto a tile is not yet a step for the wall");

      // Two tiles the screen is not standing on already: a home remembers where its viewer last pressed, and
      // stepping onto the tile it woke up on is not a step - nothing changes and no wall has any reason to move.
      MainMenuEntry? first = null;
      MainMenuEntry? second = null;
      foreach (MainMenuEntry entry in home.DisplayMenu)
      {
        if (entry.Mp2Action == null || ReferenceEquals(entry, home.SelectedEntry))
          continue;
        if (first == null)
          first = entry;
        else
        {
          second = entry;
          break;
        }
      }

      Assert.That(first, Is.Not.Null, "the menu has to offer two tiles besides the one it stands on");
      Assert.That(second, Is.Not.Null, "the menu has to offer two tiles besides the one it stands on");

      int sent = 0;
      home.WallLeaving = () => sent++;

      home.SelectedEntry = first;
      Assert.That(sent, Is.EqualTo(1), "the step asks for the way down itself, before the settling has run");
      Assert.That(home.PaneEntry, Is.Not.SameAs(first),
          "and that is before anything of the successor has been read, which is the point of asking so early");

      home.SelectedEntry = second;
      Assert.That(sent, Is.EqualTo(2),
          "a step whose predecessor is cancelled still sends its wall down - the viewer saw this step too");

      // And the reading is seen through to its end before the screen is left, so that the next screen does not
      // inherit a half-finished wall of this one.
      await WaitForPaneAsync(home, second!);
    }

    /// <summary>
    /// A tile picked with the mouse is the reference's explicitly chosen tile, which it answered at once
    /// (HomeMenuModel.OnSelectedItemChanged); only the walk that might still be moving waits for its settling.
    /// Measured on the shown screen: a walked switch stands 515-594 ms after the step, a picked one 234-265 ms
    /// after the click, which is the way down and nothing else.
    /// </summary>
    [AvaloniaTest]
    public async Task APickedTileAnswersBeforeTheSettlingHasRun()
    {
      Application app = RequireApplication();
      FrontendTheme.Apply(app, "nereus", out _);

      HomeViewModel home = new HomeViewModel();
      Assert.That(HomeViewModel.WaitsForAPress, Is.False,
          "the pane has to follow the walk, or there is no settling for a pick to cut short");

      MainMenuEntry? picked = null;
      foreach (MainMenuEntry entry in home.DisplayMenu)
      {
        if (entry.Mp2Action == null || ReferenceEquals(entry, home.SelectedEntry))
          continue;
        picked = entry;
        break;
      }

      Assert.That(picked, Is.Not.Null, "the menu has to offer a tile the screen is not standing on");

      home.SelectedEntry = picked;
      home.ChooseEntryNow();

      // Well inside the settling a walk would wait out, so this can only be the pick having answered.
      DateTime deadline = DateTime.UtcNow.AddMilliseconds(250);
      while (!ReferenceEquals(home.PaneEntry, picked) && DateTime.UtcNow < deadline)
        await Task.Delay(10);

      Assert.That(home.PaneEntry, Is.SameAs(picked),
          "a picked tile is answered at once, not when the settling happens to run out");

      // The settling that was interrupted must not have the last word: it was cancelled, and a wall that arrived
      // after the answer the viewer took would be a wall they never asked for.
      await Task.Delay(700);
      Assert.That(home.PaneEntry, Is.SameAs(picked), "and the cancelled settling does not answer a second time");
      await WaitForPaneAsync(home, picked!);
    }

    private static async Task WaitForPaneAsync(HomeViewModel home, MainMenuEntry expected)
    {
      DateTime deadline = DateTime.UtcNow.AddSeconds(3);
      while (!ReferenceEquals(home.PaneEntry, expected) && DateTime.UtcNow < deadline)
        await Task.Delay(20);

      Assert.That(home.PaneEntry, Is.SameAs(expected), "the debounced selection eventually loads its content");
    }

    [AvaloniaTest]
    public async Task TomorrowsHomeOpensOnTheTileTheViewerLastPressed()
    {
      Application app = RequireApplication();
      FrontendTheme.Apply(app, "nereus", out _);

      HomeViewModel today = new HomeViewModel();
      MainMenuEntry pressed = NthEntryWithAnAction(today, 2);
      today.SelectedEntry = pressed;
      today.CommitEntry();
      await WaitForPaneAsync(today, pressed);
      Assert.That(today.PaneEntry, Is.SameAs(pressed));

      // A fresh screen on the same machine - the way the menu stands when the viewer sits down again. The
      // reference restored the saved action rather than starting at the top of the list.
      HomeViewModel tomorrow = new HomeViewModel();
      Assert.That(tomorrow.SelectedEntry?.Mp2Action, Is.EqualTo(pressed.Mp2Action),
          "the remembered tile is where the new screen starts");

      // And with the wait-for-a-press switch on, the restored selection must still fill the pane: the pane
      // waits for presses, and the remembered tile was pressed yesterday - which is exactly the reference's
      // restore path, an explicit selection rather than a walk onto it.
      NereusSkinSettings waitingSettings = NereusSkinSettings.Load(null, out _);
      waitingSettings.EnableMenuSelection = true;
      waitingSettings.Save(null, out _);
      HomeViewModel waiting = new HomeViewModel();
      Assert.That(waiting.SelectedEntry?.Mp2Action, Is.EqualTo(pressed.Mp2Action));
      Assert.That(waiting.PaneEntry?.Mp2Action, Is.EqualTo(pressed.Mp2Action),
          "even a pane that waits for a press starts filled from the remembered tile");
    }

    [AvaloniaTest]
    public void TheCircleSwitchMovesTheTilesIntoTheirRoundFolder()
    {
      // The reference drew Nereus' home tiles from HomeMenu or HomeMenuRound through one style, and the
      // style's question was EnableCircleMenuImages. Both folders ship; the switch alone answers which.
      Application app = RequireApplication();
      FrontendTheme.Apply(app, "nereus", out _);
      Assert.That(HomeViewModel.HomeTileFolder, Is.EqualTo("HomeMenu"),
          "the shipped answer is the square folder");

      NereusSkinSettings round = new NereusSkinSettings();
      round.EnableCircleMenuImages = true;
      round.Save(null, out _);
      Assert.That(HomeViewModel.HomeTileFolder, Is.EqualTo("HomeMenuRound"),
          "the circle switch sends the tiles to the round folder the skin ships beside the square one");

      MainMenuEntry entry = NthEntryWithAnAction(new HomeViewModel(), 0);
      Assert.That(entry.HomeImagePath, Is.EqualTo("HomeMenuRound/" + entry.HomeImageId),
          "and the tile asks for its picture by the full name the folder and its Id make");

      FrontendTheme.Apply(app, "wmc", out _);
      Assert.That(HomeViewModel.HomeTileFolder, Is.EqualTo("HomeMenu"),
          "WMC's tiles are spelled by WMC's template and no circle switch of another face reaches them");
    }

    private static Application RequireApplication()
    {
      Application? app = Application.Current;
      Assert.That(app, Is.Not.Null, "these tests need the application object, so they have to be AvaloniaTest tests");
      return app!;
    }

    private static MainMenuEntry NthEntryWithAnAction(HomeViewModel home, int nth)
    {
      int seen = 0;
      foreach (MainMenuEntry entry in home.DisplayMenu)
      {
        if (entry.Mp2Action == null)
          continue;
        if (seen++ == nth)
          return entry;
      }

      Assert.Fail("the home menu needs several entries with reference actions for these tests");
      throw new InvalidOperationException();
    }
  }
}
