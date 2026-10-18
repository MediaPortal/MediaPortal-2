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
using System.ComponentModel;
using System.IO;

using MediaPortal.Frontend.Player;
using MediaPortal.Frontend.ViewModels;

using NUnit.Framework;

namespace Tests.Frontend
{
  /// <summary>
  /// The screen that plays something, measured against a fake player.
  /// </summary>
  /// <remarks>
  /// <para>
  /// No libVLC and no display are needed for any of this, which is the point: the questions a viewer actually
  /// sees answered - what does the screen say while nothing is loaded, does a refusal keep the engine's own
  /// sentence, does the bar follow the media or the clock, does an event from the engine's thread reach the
  /// screen on the screen's thread - are all answerable in a container.
  /// </para>
  /// <para>
  /// The marshaller handed to the view model holds work instead of running it (<see cref="Pump"/> runs it), so a
  /// test can tell the difference between "the view model changed a property" and "the view model asked the
  /// screen's thread to change it". The first of those is the crash the project has already had once.
  /// </remarks>
  [TestFixture]
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
      "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
      Justification = "TearDown disposes the screen, and the screen owns the player and releases it. A Dispose on the fixture itself would put a second owner on the same two objects without releasing anything sooner.")]
  public class TestNowPlayingScreen
  {
    private FakePlayer _player = null!;
    private NowPlayingViewModel _screen = null!;
    private readonly Queue<Action> _queued = new Queue<Action>();
    private int _changeCount;

    [SetUp]
    public void SetUp()
    {
      _player = new FakePlayer();
      _screen = new NowPlayingViewModel(_player, _queued.Enqueue);
      _screen.Changed += delegate { _changeCount++; };
      Pump();
    }

    [TearDown]
    public void TearDown()
    {
      // The screen owns the player and releases it with itself, which is what a closed screen does on a real
      // machine too; disposing the player separately would double-count the calls a test is counting.
      _screen.Dispose();
    }

    /// <summary>Runs everything the engine's events handed to the screen's thread.</summary>
    private void Pump()
    {
      while (_queued.Count > 0)
        _queued.Dequeue()();
    }

    private MediaLocation SomeFile(string name)
    {
      return MediaLocation.FromFile(Path.Combine(Path.GetTempPath(), "mp2-screen", name));
    }

    // ---- what the screen says before anything happens ----------------------------------------------

    [Test]
    public void AScreenWithNothingLoadedSaysThereIsNothing()
    {
      Assert.That(_screen.HasMedia, Is.False);
      Assert.That(_screen.Title, Is.EqualTo("Nothing playing"));
      Assert.That(_screen.TransportLabel, Is.EqualTo("Nothing to play"));
      Assert.That(_screen.PositionText, Does.Contain("--:--"));
      Assert.That(_screen.Progress, Is.EqualTo(0));
    }

    [Test]
    public void AScreenWithoutAPlayerSaysThereIsNone()
    {
      NowPlayingViewModel screen = new NowPlayingViewModel(null);
      Assert.That(screen.Play(SomeFile("film.mkv")), Is.False);
      Assert.That(screen.StatusLine, Does.Contain("no player"));
      Assert.That(screen.AttachVideo(new IntPtr(12345)), Is.False);
      Assert.That(screen.SetVolume(40), Is.False);
      screen.Dispose();
    }

    [Test]
    public void APropertyThatMovedAnnouncesItselfSoThatAScreenRedrawsIt()
    {
      // XAML does not poll. It subscribes to INotifyPropertyChanged, so a view model that only raises its own
      // event looks perfectly alive in a test like this one - every assertion reads the properties directly -
      // while showing a viewer a screen that never changes. The notification the bindings use is therefore
      // asserted here rather than assumed.
      var seen = new List<string?>();
      _screen.PropertyChanged += delegate (object? sender, PropertyChangedEventArgs args)
      {
        seen.Add(args.PropertyName);
      };

      Assert.That(_screen.Play(SomeFile("announced.mkv")), Is.True, _screen.StatusLine);
      Assert.That(seen, Is.Not.Empty, "the bindings were told that something is playing");
      Assert.That(seen, Has.All.Null,
          "one notification naming no property means \"read all of them again\", which is what this view model promises");

      int beforeTheEngineSpoke = seen.Count;
      _player.RaisePosition(30_000, 60_000);
      Pump();
      Assert.That(seen.Count, Is.GreaterThan(beforeTheEngineSpoke), "and again when the engine moved the clock");
    }

    // ---- opening something ---------------------------------------------------------------------------

    [Test]
    public void OpeningAMediaPutsItsNameOnTheScreenAndStartsTheClock()
    {
      Assert.That(_screen.Play(SomeFile("the-big-film.mkv")), Is.True, _screen.StatusLine);
      Pump();

      Assert.That(_screen.HasMedia, Is.True);
      Assert.That(_screen.Title, Is.EqualTo("the-big-film.mkv"), "a title strip that shows the whole path is unreadable on a TV");
      Assert.That(_player.Opened.Count, Is.EqualTo(1));
      Assert.That(_screen.StateText, Is.EqualTo("Playing"));
      Assert.That(_screen.TransportLabel, Is.EqualTo("Pause"));
    }

    [Test]
    public void ARefusalKeepsTheEnginesOwnSentence()
    {
      _player.AcceptsOpen = false;
      _player.RefuseWith = "libVLC would not accept a file with no reader for its container.";

      Assert.That(_screen.Play(SomeFile("broken.xyz")), Is.False);
      Pump();

      Assert.That(_screen.StatusLine, Does.Contain("no reader for its container"),
          "the engine's reason is the only thing a viewer can act on, so it must survive to the screen");
      Assert.That(_screen.HasMedia, Is.False);
      Assert.That(_player.Opened.Count, Is.EqualTo(1), "the attempt still happened; only the outcome was refused");
    }

    [Test]
    public void TheEngineIsAskedForThePictureInTheWindowItWasGiven()
    {
      Assert.That(_screen.AttachVideo(new IntPtr(42)), Is.True);
      Assert.That(_player.Calls, Does.Contain("AttachTo:42"));

      // 0 means "no window", which the player answers honestly rather than pretending to draw.
      Assert.That(_screen.AttachVideo(IntPtr.Zero), Is.False);
      Pump();
      Assert.That(_screen.StatusLine, Is.Not.Empty);
    }

    // ---- position, which is what a viewer watches -----------------------------------------------------

    [Test]
    public void PositionEventsMoveTheBarAndTheText()
    {
      _screen.Play(SomeFile("film.mkv"));
      Pump();

      _player.RaisePosition(30_000, 60_000);
      Pump();

      Assert.That(_screen.PositionText, Is.EqualTo("0:30 / 1:00"));
      Assert.That(_screen.Progress, Is.EqualTo(0.5).Within(0.0001));
    }

    [Test]
    public void AStreamWithNoLengthShowsDashesAndNoBar()
    {
      _screen.Play(MediaLocation.FromUrl("http://example.org/live.ts"));
      Pump();

      _player.RaisePosition(125_000, 0);
      Pump();

      Assert.That(_screen.PositionText, Is.EqualTo("2:05 / --:--"));
      Assert.That(_screen.Progress, Is.EqualTo(0),
          "a live stream is not at 0 percent of anything, and a bar at 0 percent is a lie about progress");
    }

    [Test]
    public void TheBarNeverRunsPastTheEnd()
    {
      _screen.Play(SomeFile("film.mkv"));
      Pump();

      _player.RaisePosition(90_000, 60_000);
      Pump();

      Assert.That(_screen.Progress, Is.EqualTo(1).Within(0.0001),
          "engines report a position past the last frame while they wind down; a bar that overflows reads as a bug");
    }

    [Test]
    public void LongMediasAreSpelledWithTheHours()
    {
      Assert.That(NowPlayingViewModel.Format(0), Is.EqualTo("0:00"));
      Assert.That(NowPlayingViewModel.Format(65_000), Is.EqualTo("1:05"));
      Assert.That(NowPlayingViewModel.Format(3_723_000), Is.EqualTo("1:02:03"));
      Assert.That(NowPlayingViewModel.Format(-1), Is.EqualTo("--:--"));
    }

    // ---- the transport keys ----------------------------------------------------------------------------

    [Test]
    public void PauseAndResumeAreTheOneButtonTheRemoteHas()
    {
      _screen.Play(SomeFile("film.mkv"));
      Pump();

      Assert.That(_screen.TogglePause(), Is.True);
      Pump();
      Assert.That(_screen.StateText, Is.EqualTo("Paused"));
      Assert.That(_screen.TransportLabel, Is.EqualTo("Resume"));

      Assert.That(_screen.TogglePause(), Is.True);
      Pump();
      Assert.That(_screen.StateText, Is.EqualTo("Playing"));
      Assert.That(_screen.TransportLabel, Is.EqualTo("Pause"));
    }

    [Test]
    public void APauseTheEngineRefusesLeavesTheReasonOnTheScreen()
    {
      _screen.Play(SomeFile("film.mkv"));
      Pump();
      _player.PausesWhenAsked = false;

      Assert.That(_screen.TogglePause(), Is.False);
      Pump();
      Assert.That(_screen.StatusLine, Does.Contain("cannot be paused"));
      Assert.That(_screen.StateText, Is.EqualTo("Playing"), "the screen must not claim a pause the engine refused");
    }

    [Test]
    public void PauseKeysDoNothingWhenNothingIsLoaded()
    {
      Assert.That(_screen.TogglePause(), Is.False);
      Assert.That(_screen.Stop(), Is.False);
      Assert.That(_screen.Skip(30_000), Is.False);
      Assert.That(_player.Calls, Has.None.Contains("Pause"));
      Assert.That(_player.Calls, Has.None.Contains("Stop"));
    }

    [Test]
    public void SeekingStaysInsideTheMedia()
    {
      _screen.Play(SomeFile("film.mkv"));
      Pump();
      _player.LengthInMs = 60_000;
      _player.TimeInMs = 50_000;

      Assert.That(_screen.Skip(30_000), Is.True);
      Assert.That(_player.TimeInMs, Is.EqualTo(60_000), "one minute is the end; the seek must not ask for more");

      Assert.That(_screen.Skip(-120_000), Is.True);
      Assert.That(_player.TimeInMs, Is.EqualTo(0));
    }

    [Test]
    public void SeekingInAStreamWithoutPositionIsRefusedWithAReason()
    {
      _screen.Play(MediaLocation.FromUrl("http://example.org/live.ts"));
      Pump();
      _player.LengthInMs = 0;

      Assert.That(_screen.Skip(60_000), Is.False);
      Pump();
      Assert.That(_screen.StatusLine, Does.Contain("no position"));
    }

    [Test]
    public void VolumeKeysClampAndMuteStandsOnItsOwn()
    {
      Assert.That(_screen.SetVolume(140), Is.True);
      Assert.That(_screen.Volume, Is.EqualTo(100));
      Assert.That(_player.Volume, Is.EqualTo(100));

      Assert.That(_screen.SetVolume(-20), Is.True);
      Assert.That(_screen.Volume, Is.EqualTo(0));

      Assert.That(_screen.SetVolume(40), Is.True);
      Assert.That(_screen.ToggleMute(), Is.True);
      Pump();
      Assert.That(_screen.Muted, Is.True);
      Assert.That(_screen.Volume, Is.EqualTo(40), "muting is not setting the volume to zero, and unmute must not lose it");
      Assert.That(_player.Muted, Is.True);
      Assert.That(_player.Volume, Is.EqualTo(40));
    }

    [Test]
    public void TheScreenStartsWhereThePlayerAlreadyIs()
    {
      FakePlayer loud = new FakePlayer();
      loud.Volume = 35;
      loud.Muted = true;
      NowPlayingViewModel screen = new NowPlayingViewModel(loud, _queued.Enqueue);
      Assert.That(screen.Volume, Is.EqualTo(35));
      Assert.That(screen.Muted, Is.True);
      screen.Dispose();
    }

    // ---- how things end -------------------------------------------------------------------------------

    [Test]
    public void RunningOutSaysFinished()
    {
      _screen.Play(SomeFile("film.mkv"));
      Pump();

      _player.RaiseEndOfMedia();
      Pump();

      Assert.That(_screen.StateText, Is.EqualTo("Finished"));
      Assert.That(_screen.IsPlaying, Is.False);
      Assert.That(_screen.StatusLine, Is.EqualTo("Finished"));
    }

    [Test]
    public void StoppingIsNotRunningOut()
    {
      _screen.Play(SomeFile("film.mkv"));
      Pump();

      Assert.That(_screen.Stop(), Is.True);
      Pump();

      Assert.That(_screen.StateText, Is.EqualTo("Stopped"));
      Assert.That(_screen.StatusLine, Is.Not.EqualTo("Finished"));
      Assert.That(_screen.HasMedia, Is.False, "a stopped player holds no media, so the transport goes back to idle");
    }

    [Test]
    public void AFailurePutsTheEnginesSentenceOnTheScreen()
    {
      _screen.Play(SomeFile("film.mkv"));
      Pump();

      _player.RaiseFailure("the audio device is busy");
      Pump();

      Assert.That(_screen.StateText, Is.EqualTo("Failed"));
      Assert.That(_screen.StatusLine, Is.EqualTo("the audio device is busy"));
      Assert.That(_screen.IsPlaying, Is.False);
    }

    // ---- the thread the events come from ---------------------------------------------------------------

    [Test]
    public void AnEventFromTheEngineThreadWaitsForTheScreen()
    {
      _screen.Play(SomeFile("film.mkv"));
      Pump();
      Assert.That(_screen.StateText, Is.EqualTo("Playing"));

      _changeCount = 0;
      _player.RaiseStateFromAnotherThread(PlayerState.Paused);

      Assert.That(_screen.StateText, Is.EqualTo("Playing"),
          "the view model touched its own property on the engine's thread; anything bound to it would be updated from the wrong thread");
      Assert.That(_changeCount, Is.EqualTo(0), "and it announced a change nobody had run yet");

      Pump();
      Assert.That(_screen.StateText, Is.EqualTo("Paused"));
      Assert.That(_changeCount, Is.EqualTo(1));
    }

    [Test]
    public void AClosedScreenStopsListeningToThePlayer()
    {
      _screen.Play(SomeFile("film.mkv"));
      Pump();

      _screen.Dispose();
      _changeCount = 0;
      _queued.Clear();
      _player.RaisePosition(45_000, 60_000);

      Assert.That(_changeCount, Is.EqualTo(0));
      Assert.That(_queued.Count, Is.EqualTo(0), "a disposed screen must not even be asked to schedule work");
    }

    [Test]
    public void AClosedScreenRefusesRatherThanThrowing()
    {
      _screen.Dispose();
      Assert.That(_screen.Play(SomeFile("film.mkv")), Is.False);
      Assert.That(_screen.StatusLine, Does.Contain("closed"));
    }
  }
}
