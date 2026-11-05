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
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using MediaPortal.Frontend.Player;
using MediaPortal.Frontend.Services;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using NUnit.Framework;

namespace Tests.Frontend
{
  /// <summary>
  /// The way out of the machine, measured against the reference's <c>SystemStateMenu</c> plugin: the rows
  /// <c>CreateDefaultShutdownMenu</c> hung on the dialog, in its order and spelling; the sleep timer that
  /// waits on the wall clock and then does the way out it was promised; and the sleep timer dialog's
  /// arithmetic (<c>SleepTimerModel</c>) - five-minute steps, a ceiling of three hundred minutes, one
  /// minute's mercy on the end of the standing title, and a remembered way out.
  /// </summary>
  /// <remarks>
  /// <para>
  /// No test here waits a real second: the timer's clock and the tick's route to the screens are seams on
  /// purpose, so a whole hour of waiting is one line about the clock. And no test needs a television: the
  /// menu and the dialog are models, and the models are where every promise of this feature is kept or
  /// broken.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestShutdownMenu
  {
    // ---- the menu itself -------------------------------------------------------------------------------

    [Test]
    public void TheMenuHangsTheReferencesWaysOutInItsOrderAndSpelling()
    {
      CloseViewModel menu = new CloseViewModel(null, null, Full);

      Assert.That(menu.Heading, Is.EqualTo("Shutdown menu"));
      Assert.That(menu.Choices.Select(c => c.Name), Is.EqualTo(new[]
      {
          "Suspend", "Hibernate", "Shutdown", "Setup sleep timer", "Restart Windows",
          "Close MediaPortal", "Restart MediaPortal"
      }), "the reference's default menu, in its order, in its words");
      Assert.That(menu.Choices.Select(c => c.Action), Is.EqualTo(new[]
      {
          ShutdownAction.Suspend, ShutdownAction.Hibernate, ShutdownAction.Shutdown,
          ShutdownAction.SleepTimer, ShutdownAction.Restart, ShutdownAction.CloseMP,
          ShutdownAction.RestartMP
      }));

      // The faces are the skin's own shapes now, named per line; both ways back wear the one fresh
      // face, and the sleep timer's line wears the clock in either of its states.
      Assert.That(menu.Choices.Select(c => c.Icon), Is.EqualTo(new[]
      {
          "PowerMenu/suspend", "PowerMenu/hibernate", "PowerMenu/shutdown", "PowerMenu/timer",
          "PowerMenu/refresh", "PowerMenu/close", "PowerMenu/refresh"
      }));
    }

    [Test]
    public void APlatformThatCannotAnswerALeavesItsLinesOffTheMenu()
    {
      // A Linux desktop: nothing hands it the machine's power, so those lines stay hung nowhere; the
      // application's own ways and the timer stand.
      CloseViewModel linux = new CloseViewModel(null, null,
          new SystemStateSupport(false, false, false));
      Assert.That(linux.Choices.Select(c => c.Action), Is.EqualTo(new[]
      {
          ShutdownAction.SleepTimer, ShutdownAction.CloseMP, ShutdownAction.RestartMP
      }));

      // A phone: the operating system owns the machine's power and the application's life both. A menu
      // that can promise nothing says nothing rather than lying seven times.
      CloseViewModel phone = new CloseViewModel(null, null,
          new SystemStateSupport(false, false, false, Application: false));
      Assert.That(phone.Choices, Is.Empty);
    }

    [Test]
    public void PressingALineDoesItsWayOutThroughTheApplication()
    {
      List<ShutdownAction> done = new List<ShutdownAction>();
      CloseViewModel menu = new CloseViewModel(null, done.Add, Full);

      menu.Choose(menu.Choices.First(c => c.Action == ShutdownAction.Shutdown));
      menu.Choose(menu.Choices.First(c => c.Action == ShutdownAction.CloseMP));

      Assert.That(done, Is.EqualTo(new[] { ShutdownAction.Shutdown, ShutdownAction.CloseMP }),
          "a row that is pressed must hand its way out to the application, which owns leaving");
    }

    [Test]
    public void TheSleepTimerRowOpensTheDialogWhileNothingWaitsAndStopsTheWaitWhenOneStands()
    {
      SleepTimerService timer = new SleepTimerService();
      int opened = 0;
      CloseViewModel menu = new CloseViewModel(timer, null, Full,
          delegate { opened++; });

      menu.Choose(SleepTimerRow(menu));
      Assert.That(opened, Is.EqualTo(1), "while nothing waits, the reference's row opened the timer's dialog");

      timer.Start(30, ShutdownAction.Shutdown);
      Assert.That(SleepTimerRow(menu).Name, Is.EqualTo("Stop sleep timer"),
          "the row IS the stop while a wait stands, and says so");
      Assert.That(SleepTimerRow(menu).Icon, Is.EqualTo("PowerMenu/timer"),
          "the row wears the clock in either of its states");
      Assert.That(menu.HasNotice, Is.True);
      Assert.That(menu.Notice, Is.EqualTo("Shutdown in 30 minutes"));

      menu.Choose(SleepTimerRow(menu));
      Assert.That(timer.IsActive, Is.False, "pressing the stop row must break the standing order");
      Assert.That(opened, Is.EqualTo(1), "the stop row must not open the dialog on the side");
      Assert.That(SleepTimerRow(menu).Name, Is.EqualTo("Setup sleep timer"));
      Assert.That(menu.HasNotice, Is.False);
    }

    // ---- the timer itself -------------------------------------------------------------------------------

    [Test]
    public void TheWaitCountsDownOnTheClockAndDoesTheWayOutItWasPromisedOnce()
    {
      DateTime now = new DateTime(2025, 3, 12, 22, 0, 0);
      List<ShutdownAction> done = new List<ShutdownAction>();
      SleepTimerService timer = new SleepTimerService(done.Add) { Clock = delegate { return now; } };

      timer.Start(12, ShutdownAction.Hibernate);
      Assert.That(timer.IsActive, Is.True);
      Assert.That(timer.NoticeText, Is.EqualTo("Hibernate in 12 minutes"),
          "the reference spelled the standing order out under its list");

      now = now.AddMinutes(11);
      timer.Refresh();
      Assert.That(timer.RemainingMinutes, Is.EqualTo(1));
      Assert.That(timer.NoticeText, Is.EqualTo("Hibernate in less than one minute"));

      now = now.AddMinutes(1);
      timer.Refresh();
      Assert.That(timer.IsActive, Is.False, "a wait that has run out is not standing any more");
      Assert.That(done, Is.EqualTo(new[] { ShutdownAction.Hibernate }),
          "the reference ran the way out only after stopping itself");

      now = now.AddMinutes(5);
      timer.Refresh();
      Assert.That(done.Count, Is.EqualTo(1), "one order fires its way out once, never twice");
    }

    [Test]
    public void TwoWaitsNeverStandAndBreakingOneTwiceChangesNothing()
    {
      SleepTimerService timer = new SleepTimerService();

      timer.Start(20, ShutdownAction.Shutdown);
      timer.Start(5, ShutdownAction.Restart);
      Assert.That(timer.InitialMinutes, Is.EqualTo(20),
          "the reference never ran two timers: a second Start answers with the one that stands");

      timer.Stop();
      timer.Stop();
      Assert.That(timer.IsActive, Is.False);
      Assert.That(timer.NoticeText, Is.Empty);
    }

    [Test]
    public void TheWaitRefusesNumbersOutsideTheReferencesRange()
    {
      SleepTimerService timer = new SleepTimerService();
      Assert.That(() => timer.Start(0, ShutdownAction.Shutdown),
          Throws.TypeOf<ArgumentOutOfRangeException>());
      Assert.That(() => timer.Start(SleepTimerService.MaxMinutes + 1, ShutdownAction.Shutdown),
          Throws.TypeOf<ArgumentOutOfRangeException>());
      Assert.That(timer.IsActive, Is.False);
    }

    // ---- the timer's dialog -------------------------------------------------------------------------------

    [Test]
    public void TheDialogStepsFiveMinutesAtATimeAndStopsAtTheReferencesBorders()
    {
      SleepTimerViewModel dialog = Dialog(out _);
      Assert.That(dialog.TimeText, Is.EqualTo("30"), "a fresh dialog waits the reference's half hour");

      dialog.AddTime();
      Assert.That(dialog.TimeText, Is.EqualTo("35"));
      dialog.SubTime();
      dialog.SubTime();
      Assert.That(dialog.TimeText, Is.EqualTo("25"), "five minutes is the reference's step, both ways");

      // A typed number that does not sit on the ladder still steps onto the ladder.
      dialog.TimeText = "31";
      dialog.AddTime();
      Assert.That(dialog.TimeText, Is.EqualTo("35"));
      dialog.TimeText = "31";
      dialog.SubTime();
      Assert.That(dialog.TimeText, Is.EqualTo("30"));

      dialog.TimeText = SleepTimerService.MaxMinutes.ToString();
      Assert.That(dialog.AddEnabled, Is.False, "the ceiling is the reference's three hundred minutes");
      dialog.TimeText = "0";
      Assert.That(dialog.SubEnabled, Is.False);
      Assert.That(dialog.ActivateEnabled, Is.False, "a wait of nought minutes is no wait");
    }

    [Test]
    public void AFieldThatIsNotAMinuteNumberSaysSoAndKillsTheWideButton()
    {
      SleepTimerViewModel dialog = Dialog(out _);

      dialog.TimeText = "zwanzig";
      Assert.That(dialog.ErrorText, Is.EqualTo("Not a number of minutes."));
      Assert.That(dialog.ActivateEnabled, Is.False);

      dialog.TimeText = "20";
      Assert.That(dialog.ErrorText, Is.Empty);
      Assert.That(dialog.ActivateEnabled, Is.True);
    }

    [Test]
    public void TheWideButtonStartsTheWaitStaysOpenAndBecomesTheStop()
    {
      SleepTimerService timer = new SleepTimerService();
      SleepTimerViewModel dialog = Dialog(timer);

      dialog.TimeText = "45";
      dialog.Activate();
      Assert.That(timer.IsActive, Is.True);
      Assert.That(timer.InitialMinutes, Is.EqualTo(45));
      Assert.That(dialog.StartButtonText, Is.EqualTo("Stop"),
          "the reference's dialog stayed open after starting and its button turned into the stop");
      Assert.That(dialog.EditingEnabled, Is.False, "typing is dead while a wait runs");
      Assert.That(dialog.ActionSelectEnabled, Is.False);

      dialog.Activate();
      Assert.That(timer.IsActive, Is.False);
      Assert.That(dialog.StartButtonText, Is.EqualTo("Start"));
    }

    [Test]
    public void TheActionButtonCyclesTheWaysOutAndTheLastChoiceIsRemembered()
    {
      SleepTimerService.LastAction = ShutdownAction.Shutdown;
      SleepTimerViewModel dialog = Dialog(out _);
      Assert.That(dialog.ActionName, Is.EqualTo("Shutdown"), "the dialog starts from the remembered way out");

      dialog.SelectAction();
      Assert.That(dialog.ActionName, Is.EqualTo("Restart Windows"));
      dialog.SelectAction();
      Assert.That(dialog.ActionName, Is.EqualTo("Close MediaPortal"),
          "the reference cycled its one action button through the machine's ways out");
      Assert.That(SleepTimerService.LastAction, Is.EqualTo(ShutdownAction.CloseMP));

      SleepTimerViewModel again = Dialog(out _);
      Assert.That(again.ActionName, Is.EqualTo("Close MediaPortal"),
          "the reference remembered the last way out as the next dialog's first");
    }

    [Test]
    public void TheNumberCanComeFromTheStandingTitlePlusOneMinuteOfMercy()
    {
      FakePlayer player = new FakePlayer();
      using NowPlayingViewModel playback = new NowPlayingViewModel(player);
      SleepTimerViewModel dialog = Dialog(new SleepTimerService(), playback);

      Assert.That(dialog.MediaItemEnabled, Is.False, "nothing stands, so the line cannot answer");

      Assert.That(playback.Play(SomeFile("the-long-movie.mkv")), Is.True, playback.StatusLine);
      player.RaisePosition((long)TimeSpan.FromMinutes(30).TotalMilliseconds,
          (long)TimeSpan.FromMinutes(90).TotalMilliseconds);
      Assert.That(dialog.MediaItemEnabled, Is.True);

      dialog.FromMediaItem();
      Assert.That(dialog.TimeText, Is.EqualTo("61"),
          "the rest of the title plus one minute, so the wait cannot beat the end credits");
    }

    // ---- helpers -------------------------------------------------------------------------------------------

    /// <summary>
    /// The faces the shutdown menu's rows name are the skin's own shapes: a shape takes the row's white
    /// and the row's state, which is why the reference's pictures were replaced by them and why a row
    /// that cannot light its face cannot say which line the viewer stands on.
    /// </summary>
    [AvaloniaTest]
    public void TheShutdownRowsNameFacesTheWornSkinCarriesAsShapes()
    {
      string before = SkinEngine.SkinsFolder;
      try
      {
        SkinEngine.SkinsFolder = Path.Combine(FrontendSource(), "Skins");
        Application app = Application.Current!;
        FrontendTheme.Apply(app, "nereus", out _);
        Assert.That(SkinEngine.Apply(app, "nereus", out string? problem), Is.True, problem ?? "");

        WornSkin? worn = SkinEngine.Worn(app);
        Assert.That(worn, Is.Not.Null, "the shipped skin came off the disk and onto the application");

        foreach (string face in new[]
        {
            "PowerMenu/suspend", "PowerMenu/hibernate", "PowerMenu/shutdown", "PowerMenu/timer",
            "PowerMenu/refresh", "PowerMenu/close"
        })
        {
          Assert.That(worn!.Vectors.Has(face), Is.True,
              "a shutdown row names '" + face + "' and the skin carries no icon of that name");
          Assert.That(worn.Vectors.Load(face), Is.InstanceOf<Geometry>(),
              "'" + face + "' is on disk and did not read as a shape");
        }
      }
      finally
      {
        SkinEngine.SkinsFolder = before;
      }
    }

    // ---- the viewer's own menu (the reference's dialogSystemStateConfiguration) ----------------------

    [Test]
    public void AMissingFileAnswersTheReferencesDefaultMenu()
    {
      string file = MenuFile();
      ShutdownMenuSettings settings = ShutdownMenuSettings.Load(file, out string? problem);

      Assert.That(problem, Is.Null);
      Assert.That(settings.Items.Select(i => i.Action), Is.EqualTo(new[]
      {
          ShutdownAction.Suspend, ShutdownAction.Hibernate, ShutdownAction.Shutdown,
          ShutdownAction.SleepTimer, ShutdownAction.Restart, ShutdownAction.CloseMP,
          ShutdownAction.RestartMP
      }));
      Assert.That(settings.Items.All(i => i.Visible), Is.True,
          "the reference's CreateDefaultShutdownMenu showed all seven lines it knew");
      Assert.That(settings.MaxSleepTimeout, Is.EqualTo(300));
    }

    [Test]
    public void TheOrderAndTheHidingSurviveAFile()
    {
      string file = MenuFile();
      ShutdownMenuSettings settings = new ShutdownMenuSettings();
      settings.Items.Reverse();
      settings.Items.First().Visible = false;
      settings.MaxSleepTimeout = 45;

      Assert.That(settings.Save(file, out bool saved), Does.StartWith("Saved in"));
      Assert.That(saved, Is.True);

      ShutdownMenuSettings again = ShutdownMenuSettings.Load(file, out string? problem);
      Assert.That(problem, Is.Null);
      Assert.That(again.Items.Select(i => i.Action), Is.EqualTo(settings.Items.Select(i => i.Action)));
      Assert.That(again.Items.Select(i => i.Visible), Is.EqualTo(settings.Items.Select(i => i.Visible)));
      Assert.That(again.MaxSleepTimeout, Is.EqualTo(45));
      File.Delete(file);
    }

    [Test]
    public void AFileThatKnowsFewerLinesLosesNoWayOut()
    {
      // The reference's GetShutdownActionsFromSettings hung every action it had never seen in the file
      // onto the end of the operator's order. A file that names two lines keeps its order and its
      // hiding; the five it forgot stand behind them, as defaults.
      string file = MenuFile();
      File.WriteAllText(file, @"{
        // two lines and a comment, because the reader may have them
        ""items"": [ { ""action"": ""Restart"", ""visible"": true },
                     { ""action"": ""Shutdown"", ""visible"": false } ],
        ""maxSleepTimeout"": 45 }");

      ShutdownMenuSettings settings = ShutdownMenuSettings.Load(file, out string? problem);
      Assert.That(problem, Is.Null);
      Assert.That(settings.Items.Select(i => i.Action).Take(2),
          Is.EqualTo(new[] { ShutdownAction.Restart, ShutdownAction.Shutdown }));
      Assert.That(settings.Items.Count, Is.EqualTo(7));
      Assert.That(settings.Items[0].Visible, Is.True);
      Assert.That(settings.Items[1].Visible, Is.False);
      Assert.That(settings.MaxSleepTimeout, Is.EqualTo(45));
      File.Delete(file);
    }

    [Test]
    public void TheMenuHangsTheViewersOrderAndHidesHisHiddenLines()
    {
      ShutdownMenuSettings settings = new ShutdownMenuSettings();
      settings.Items = new List<ShutdownMenuItem>
      {
          new ShutdownMenuItem { Action = ShutdownAction.RestartMP },
          new ShutdownMenuItem { Action = ShutdownAction.Suspend, Visible = false },
          new ShutdownMenuItem { Action = ShutdownAction.Shutdown }
      };

      CloseViewModel menu = new CloseViewModel(null, null, Full, null, delegate { return settings; });

      // The two shown lines keep their order, the hidden one stays off, and the four lines the file
      // forgot are still hung behind them - a way out nobody offered an answer for is not a way out
      // to lose.
      Assert.That(menu.Choices.Select(c => c.Action), Is.EqualTo(new[]
      {
          ShutdownAction.RestartMP, ShutdownAction.Shutdown, ShutdownAction.Hibernate,
          ShutdownAction.SleepTimer, ShutdownAction.Restart, ShutdownAction.CloseMP
      }));
      Assert.That(menu.Choices.Select(c => c.Action), Has.No.Member(ShutdownAction.Suspend));
    }

    [Test]
    public void AVisibleLineOnAPlatformThatRefusesStillStaysOff()
    {
      // Settings say what the viewer wants; the platform says what it can do; the answer is both. On a
      // platform that hands out no power and no leaving, every visible line still stays off.
      CloseViewModel refusing = new CloseViewModel(null, null,
          new SystemStateSupport(false, false, false, false), null,
          delegate { return new ShutdownMenuSettings(); });
      Assert.That(refusing.Choices, Is.Empty);

      // And on a machine without the platform's power but with its own two ways - every application on
      // any desktop - only the application's lines and the honest timer stand, however visible the
      // machine's lines are in the file.
      CloseViewModel appOnly = new CloseViewModel(null, null,
          new SystemStateSupport(false, false, false), null,
          delegate { return new ShutdownMenuSettings(); });
      Assert.That(appOnly.Choices.Select(c => c.Action), Is.EqualTo(new[]
      {
          ShutdownAction.SleepTimer, ShutdownAction.CloseMP, ShutdownAction.RestartMP
      }));
    }

    [Test]
    public void TheEditorTogglesMovesAndSavesEachAnswerItself()
    {
      string file = MenuFile();
      ShutdownMenuEditorViewModel editor = new ShutdownMenuEditorViewModel(file);

      Assert.That(editor.Rows.Count, Is.EqualTo(7));
      Assert.That(editor.MaxMinutesText, Is.EqualTo("300"));

      editor.SelectedRow = editor.Rows[0];
      Assert.That(editor.SelectedRow!.Action, Is.EqualTo(ShutdownAction.Suspend));
      editor.MoveDownCommand.Execute(null);
      Assert.That(editor.Status, Does.StartWith("Saved in"));

      ShutdownMenuSettings moved = ShutdownMenuSettings.Load(file, out _);
      Assert.That(moved.Items.Select(i => i.Action).Take(2),
          Is.EqualTo(new[] { ShutdownAction.Hibernate, ShutdownAction.Suspend }));

      // The moved row kept the selection - hiding it is the next press, and the next save.
      editor.ToggleRowCommand.Execute(null);
      Assert.That(editor.SelectedRow!.IsShown, Is.False);
      Assert.That(editor.SelectedRow.ShownWord, Is.EqualTo("hidden"));

      ShutdownMenuSettings hidden = ShutdownMenuSettings.Load(file, out _);
      Assert.That(hidden.Items[1].Action, Is.EqualTo(ShutdownAction.Suspend));
      Assert.That(hidden.Items[1].Visible, Is.False);

      editor.MaxMinutesText = "45";
      editor.SaveMaximumCommand.Execute(null);
      Assert.That(ShutdownMenuSettings.Load(file, out _).MaxSleepTimeout, Is.EqualTo(45));

      editor.MaxMinutesText = "nonsense";
      editor.SaveMaximumCommand.Execute(null);
      Assert.That(editor.MaxMinutesText, Is.EqualTo("300"),
          "nonsense answers itself to the reference's default rather than writing a broken file");
      Assert.That(ShutdownMenuSettings.Load(file, out _).MaxSleepTimeout, Is.EqualTo(300));
      File.Delete(file);
    }

    [Test]
    public void TheSleepTimerObeyesTheCeilingFromTheSettings()
    {
      SleepTimerService timer = new SleepTimerService();
      Assert.That(timer.MaxMinutesAllowed, Is.EqualTo(SleepTimerService.MaxMinutes),
          "an untouched file leaves the reference's three hundred");

      // The application sets the ceiling from the settings file when the menu opens (CloseFrame); from
      // there on the timer and its dialog both stop at it.
      timer.MaxMinutesAllowed = 30;
      timer.Start(30, ShutdownAction.Shutdown);
      Assert.That(timer.IsActive, Is.True);
      timer.Stop();
      Assert.That(() => timer.Start(31, ShutdownAction.Shutdown),
          Throws.TypeOf<ArgumentOutOfRangeException>());

      SleepTimerViewModel dialog = Dialog(timer);
      dialog.TimeText = "30";
      Assert.That(dialog.AddEnabled, Is.False, "the up stepper dies at the ceiling the settings set");
      Assert.That(dialog.ActivateEnabled, Is.True);
    }

    private static string MenuFile()
    {
      return Path.Combine(Path.GetTempPath(), "mp2-shutdown",
          "menu-" + Guid.NewGuid().ToString("N") + ".json");
    }

    private static string FrontendSource()
    {
      AssemblyMetadataAttribute? value = typeof(TestShutdownMenu).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => a.Key == "FrontendSource");
      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project has to hand FrontendSource over for this to be checkable");
      return Path.GetFullPath(value!.Value!);
    }

    private static readonly SystemStateSupport Full = new SystemStateSupport(true, true, true);

    private static ShutdownChoice SleepTimerRow(CloseViewModel menu)
    {
      return menu.Choices.First(c => c.Action == ShutdownAction.SleepTimer);
    }

    private static SleepTimerViewModel Dialog(SleepTimerService? timer,
        NowPlayingViewModel? playback = null)
    {
      return new SleepTimerViewModel(timer ?? new SleepTimerService(), playback, Full);
    }

    private static SleepTimerViewModel Dialog(out SleepTimerService timer,
        NowPlayingViewModel? playback = null)
    {
      timer = new SleepTimerService();
      return new SleepTimerViewModel(timer, playback, Full);
    }

    private static MediaLocation SomeFile(string name)
    {
      return MediaLocation.FromFile(Path.Combine(Path.GetTempPath(), "mp2-shutdown", name));
    }
  }
}
