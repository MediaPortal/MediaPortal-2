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

    // ---- the tracks a film carries --------------------------------------------------------------------

    [Test]
    public void AFilmThatStartedNamesItsTracksAndSaysWhichOneIsInUse()
    {
      _player.Audio = new PlaybackTrack[]
      {
          new PlaybackTrack(TrackKind.Audio, 1, "Language (eng)", "eng"),
          new PlaybackTrack(TrackKind.Audio, 2, "Language (ger)", "ger")
      };
      _player.CurrentAudioId = 2;

      _screen.Play(SomeFile("film.mkv"));
      Pump();

      Assert.That(_screen.AudioTracks.Count, Is.EqualTo(2));
      Assert.That(_screen.HasTrackChoice, Is.True);
      Assert.That(_screen.AudioTrackLine, Is.EqualTo("Sound: ger - Language (ger)"),
          "the track the engine chose is the one on screen, not the first of the list");
    }

    [Test]
    public void SubtitlesCanBeSwitchedOffFromTheList()
    {
      _player.Subtitles = new PlaybackTrack[]
      {
          new PlaybackTrack(TrackKind.Subtitle, 1, "Language (eng)", "eng")
      };
      _player.CurrentSubtitleId = 1;

      _screen.Play(SomeFile("film.mkv"));
      Pump();

      // The off entry is in the list, so the way back is where a viewer looks for it.
      Assert.That(_screen.SubtitleTracks.Count, Is.EqualTo(2));
      Assert.That(_screen.SubtitleTracks[0].Id, Is.EqualTo(-1));
      Assert.That(_screen.SubtitleTrackLine, Is.EqualTo("Subtitles: eng - Language (eng)"));

      Assert.That(_screen.NextSubtitleTrack(), Is.True);
      Pump();
      Assert.That(_player.CurrentSubtitleId, Is.EqualTo(-1), "the engine got the off id");
      Assert.That(_screen.SubtitleTrackLine, Is.EqualTo("Subtitles: no subtitles"));

      Assert.That(_screen.NextSubtitleTrack(), Is.True);
      Pump();
      Assert.That(_player.CurrentSubtitleId, Is.EqualTo(1), "and the next press is back on the only subtitle");
    }

    [Test]
    public void SoundCyclesThroughTheTracksAndWrapsAround()
    {
      _player.Audio = new PlaybackTrack[]
      {
          new PlaybackTrack(TrackKind.Audio, 1, "Language (eng)", "eng"),
          new PlaybackTrack(TrackKind.Audio, 2, "Language (ger)", "ger"),
          new PlaybackTrack(TrackKind.Audio, 3, "Director's commentary")
      };
      _player.CurrentAudioId = 1;

      _screen.Play(SomeFile("film.mkv"));
      Pump();

      Assert.That(_screen.NextAudioTrack(), Is.True);
      Assert.That(_screen.AudioTrackLine, Does.Contain("ger"));
      Assert.That(_screen.NextAudioTrack(), Is.True);
      Assert.That(_screen.AudioTrackLine, Does.Contain("Director's commentary"),
          "a track with no language on it is named the way the engine named it");
      Assert.That(_screen.NextAudioTrack(), Is.True);
      Assert.That(_screen.AudioTrackLine, Does.Contain("eng"), "and the fourth press is back at the first");
    }

    [Test]
    public void ATrackThePlayerRefusesLeavesItsReasonOnTheScreen()
    {
      _player.Audio = new PlaybackTrack[]
      {
          new PlaybackTrack(TrackKind.Audio, 1, "Language (eng)", "eng"),
          new PlaybackTrack(TrackKind.Audio, 2, "Language (ger)", "ger")
      };
      _screen.Play(SomeFile("film.mkv"));
      Pump();

      _player.RefusesTrackSelection = true;
      Assert.That(_screen.NextAudioTrack(), Is.False);
      Pump();
      Assert.That(_screen.StatusLine, Does.Contain("would not switch"));
      Assert.That(_screen.AudioTrackLine, Does.Contain("none"),
          "and the line still says what is really playing, not what was asked for");
    }

    [Test]
    public void TheNextFilmIsAskedForItsOwnTracks()
    {
      _player.Audio = new PlaybackTrack[]
      {
          new PlaybackTrack(TrackKind.Audio, 1, "Language (eng)", "eng"),
          new PlaybackTrack(TrackKind.Audio, 2, "Language (ger)", "ger")
      };
      _screen.Play(SomeFile("first.mkv"));
      Pump();
      Assert.That(_screen.AudioTracks.Count, Is.EqualTo(2));

      // A single-track film: the picker has nothing to choose, and must not offer the previous film's tracks.
      _player.Audio = new PlaybackTrack[] { new PlaybackTrack(TrackKind.Audio, 1, "Language (eng)", "eng") };
      _player.CurrentAudioId = 1;
      _screen.Play(SomeFile("second.mkv"));
      Pump();

      Assert.That(_screen.AudioTracks.Count, Is.EqualTo(1));
      Assert.That(_screen.HasTrackChoice, Is.False, "one sound track is not a choice");

      // Once per media and not once per tick: the position events arrive several a second, and a screen that
      // asked the engine for its tracks along with them would be spending every one of them on a list nobody
      // is looking at. Two films, two questions.
      _player.RaisePosition(10_000, 60_000);
      _player.RaisePosition(11_000, 60_000);
      Pump();
      Assert.That(_player.Calls, Has.Exactly(2).EqualTo("Tracks:Audio"));
    }

    [Test]
    public void TrackKeysDoNothingWhenNothingIsLoaded()
    {
      Assert.That(_screen.NextAudioTrack(), Is.False);
      Assert.That(_screen.NextSubtitleTrack(), Is.False);
      Assert.That(_screen.StatusLine, Does.Contain("nothing is playing"));
      Assert.That(_screen.HasTrackChoice, Is.False);
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

    #region What a viewing leaves behind

    private static readonly Guid TITLE_ID = new Guid("66666666-6666-6666-6666-666666666666");

    private readonly List<PlaybackReportedEventArgs> _reports = new List<PlaybackReportedEventArgs>();

    /// <summary>
    /// Listens once, the way the application does. Every test about reporting starts with this, because a screen
    /// nobody is listening to keeps its opinions to itself - which is a behaviour of its own, and is tested apart.
    /// </summary>
    private void Listen()
    {
      _reports.Clear();
      _screen.PlaybackReported += delegate (object? sender, PlaybackReportedEventArgs args) { _reports.Add(args); };
    }

    [Test]
    public void AFilmWatchedToTheEndIsReportedAsWatched()
    {
      Listen();
      _screen.Play(SomeFile("Alien Earth.mkv"), TITLE_ID);
      Pump();
      _player.RaisePosition(59_000, 60_000);
      Pump();

      _player.RaiseEndOfMedia();
      Pump();

      Assert.That(_reports, Has.Count.EqualTo(1));
      Assert.That(_reports[0].MediaItemId, Is.EqualTo(TITLE_ID));
      Assert.That(_reports[0].Percentage, Is.EqualTo(100),
          "the engine said the media ended, which is the only thing that says a film was finished");
    }

    [Test]
    public void StoppingHalfwayReportsHowFarTheViewerGot()
    {
      Listen();
      _screen.Play(SomeFile("film.mkv"), TITLE_ID);
      Pump();
      _player.RaisePosition(30_000, 60_000);
      Pump();

      Assert.That(_screen.Stop(), Is.True);
      Pump();

      Assert.That(_reports, Has.Count.EqualTo(1));
      Assert.That(_reports[0].Percentage, Is.EqualTo(50));
    }

    [Test]
    public void HowFarSomebodyGotIsMeasuredBeforeTheEngineForgetsTheFilm()
    {
      // Not a trick of the fake: closing a media really does take its length away, so a screen that works the
      // percentage out after the stop is over is left dividing by nothing and reports nothing at all.
      _player.ForgetsLengthOnStop = true;
      Listen();
      _screen.Play(SomeFile("film.mkv"), TITLE_ID);
      Pump();
      _player.RaisePosition(45_000, 60_000);
      Pump();

      _screen.Stop();
      Pump();

      Assert.That(_player.LengthInMs, Is.EqualTo(0), "the engine really did forget");
      Assert.That(_reports, Has.Count.EqualTo(1));
      Assert.That(_reports[0].Percentage, Is.EqualTo(75));
    }

    [Test]
    public void StoppingABarFromTheEndCountsAsWatchedBecauseTheReferenceSaysSo()
    {
      // The position the engine reports can reach the length itself - a paused last frame, a seek to the end -
      // and the measured percentage caps at ninety-nine rather than a hundred it did not earn. The reference's
      // own rule then decides what the library hears: a viewing past the watched mark counts as watched, and
      // watched travels as the hundred the library door understands (PlayerContextManager.NotifyPlayback).
      Listen();
      _screen.Play(SomeFile("film.mkv"), TITLE_ID);
      Pump();
      _player.RaisePosition(60_000, 60_000);
      Pump();

      _screen.Stop();
      Pump();

      Assert.That(_reports, Has.Count.EqualTo(1));
      Assert.That(_reports[0].Percentage, Is.EqualTo(100),
          "past the reference's ninety, a stopped film is a watched film");
    }

    [Test]
    public void TheWatchedMarkIsTheViewersOwnAnswer()
    {
      // The reference's setting (PlayerManagerSettings.WatchedPlayPercentage) walked nought to a hundred; a
      // hundred there means "watched means finished", and then a stop at ninety-nine really is a film left.
      _screen.Preferences = new MediaPortal.Frontend.Settings.PlaybackSettings { WatchedMarkPercentage = 100 };
      Listen();
      _screen.Play(SomeFile("film.mkv"), TITLE_ID);
      Pump();
      _player.RaisePosition(59_400, 60_000);
      Pump();

      _screen.Stop();
      Pump();

      Assert.That(_reports, Has.Count.EqualTo(1));
      Assert.That(_reports[0].Percentage, Is.EqualTo(99),
          "asked to count only finished films, a bar ninety-nine per cent full is not one");
    }

    [Test]
    public void AFilmNobodyGotIntoIsNotReportedAtAll()
    {
      Listen();
      _screen.Play(SomeFile("film.mkv"), TITLE_ID);
      Pump();
      _player.RaisePosition(500, 60_000);
      Pump();

      _screen.Stop();
      Pump();

      Assert.That(_reports, Is.Empty,
          "thirty seconds of studio logos is not a fact about a film, and writing it down would make a library"
          + " that says a title was never watched impossible to answer");
    }

    [Test]
    public void AFilePlayedWithoutALibraryTitleIsNotReported()
    {
      Listen();
      _screen.Play(SomeFile("something-on-a-usb-stick.mkv"));
      Pump();
      _player.RaisePosition(40_000, 60_000);
      Pump();

      _player.RaiseEndOfMedia();
      Pump();

      Assert.That(_screen.MediaItemId, Is.Null);
      Assert.That(_reports, Is.Empty, "there is nothing to tell the server about a file it has never seen");
    }

    [Test]
    public void TheFurthestPointCountsRatherThanTheLastOne()
    {
      Listen();
      _screen.Play(SomeFile("film.mkv"), TITLE_ID);
      Pump();
      _player.RaisePosition(50_000, 60_000);
      Pump();

      // The thing people do at the end of a film: wind back a minute to show somebody what they missed.
      Assert.That(_screen.Skip(-20_000), Is.True);
      _player.RaisePosition(30_000, 60_000);
      Pump();

      _screen.Stop();
      Pump();

      Assert.That(_reports, Has.Count.EqualTo(1));
      Assert.That(_reports[0].Percentage, Is.EqualTo(83),
          "they saw five sixths of it, whatever position the screen stopped on");
    }

    [Test]
    public void TheNextFilmIsReportedForItselfAndNotForTheOneBefore()
    {
      Listen();
      Guid first = Guid.NewGuid();
      Guid second = Guid.NewGuid();

      _screen.Play(SomeFile("first.mkv"), first);
      Pump();
      _player.RaisePosition(30_000, 60_000);
      Pump();

      _screen.Play(SomeFile("second.mkv"), second);
      Pump();

      Assert.That(_reports, Has.Count.EqualTo(1), "the first film got its one report when it was replaced");
      Assert.That(_reports[0].MediaItemId, Is.EqualTo(first));
      Assert.That(_reports[0].Percentage, Is.EqualTo(50));

      _player.RaiseEndOfMedia();
      Pump();

      Assert.That(_reports, Has.Count.EqualTo(2));
      Assert.That(_reports[1].MediaItemId, Is.EqualTo(second), "the second film is not credited to the first");
      Assert.That(_reports[1].Percentage, Is.EqualTo(100));
    }

    [Test]
    public void FinishingAFilmAndThenPressingStopSaysItOnce()
    {
      Listen();
      _screen.Play(SomeFile("film.mkv"), TITLE_ID);
      Pump();

      _player.RaiseEndOfMedia();
      Pump();
      _screen.Stop();
      Pump();

      Assert.That(_reports, Has.Count.EqualTo(1),
          "a play count that climbs because a viewer pressed Stop after the credits is a wrong play count");
    }

    [Test]
    public void AScreenNobodyListensToKeepsItsReportsToItself()
    {
      // No Listen(): a screen on a machine where nothing subscribed. The film still ends and the screen still says
      // so, but no report is invented for nobody. The list is emptied by hand because this fixture is one object
      // for every test in it, so whatever an earlier test collected would otherwise still be in there.
      _reports.Clear();
      _screen.Play(SomeFile("film.mkv"), TITLE_ID);
      Pump();

      _player.RaiseEndOfMedia();
      Pump();

      Assert.That(_screen.StateText, Is.EqualTo("Finished"));
      Assert.That(_reports, Is.Empty);
    }

    #endregion

    #region starting where the viewer left it

    [Test]
    public void ATitleLeftHalfwayStartsWhereItsViewerStoppedIt()
    {
      // The figure is the server's, arrived with the title (see TestMediaBrowser), and it is a percentage rather
      // than a time so that it still means the same thing when the file behind a title is replaced. What is
      // asserted here is the other half: that the screen turns it into a seek instead of politely starting at the
      // beginning and leaving the viewer to rewind by hand.
      Assert.That(_screen.Play(SomeFile("resumed.mkv"), TITLE_ID, 42), Is.True, _screen.StatusLine);

      _player.RaisePosition(1_000, 100_000);
      Pump();

      Assert.That(_player.TimeInMs, Is.EqualTo(42_000),
          "42% of a hundred seconds is forty-two seconds, which is where this viewer stopped it");
      Assert.That(_screen.StatusLine, Does.Contain("Resumed at 42%"),
          "a film that starts minutes in looks like a broken player unless the screen says it was on purpose");
    }

    [Test]
    public void TheResumeIsOneSeekAndNotAFightWithTheViewer()
    {
      Assert.That(_screen.Play(SomeFile("resumed-again.mkv"), TITLE_ID, 42), Is.True, _screen.StatusLine);

      _player.RaisePosition(1_000, 200_000);
      Pump();
      Assert.That(_player.TimeInMs, Is.EqualTo(84_000));

      // The next events report the position the seek produced. If the screen sought on every one of them, the
      // viewer could never rewind: every skip back would be dragged to 42% again.
      _player.RaisePosition(84_000, 200_000);
      Pump();
      _player.RaisePosition(20_000, 200_000);
      Pump();

      Assert.That(_player.TimeInMs, Is.EqualTo(20_000),
          "a position the viewer moved to by hand stays where they moved it");
    }

    [Test]
    public void TheResumeWaitsForAnEngineThatDoesNotYetKnowHowLongTheMediaIs()
    {
      // An engine reports a length only once the media is prepared, and a fraction of nought is nought: a seek
      // issued against a length of nought would land at the beginning and consider the resume done.
      Assert.That(_screen.Play(SomeFile("slow-open.mkv"), TITLE_ID, 25), Is.True, _screen.StatusLine);

      _player.RaisePosition(0, 0);
      Pump();
      Assert.That(_player.TimeInMs, Is.EqualTo(0), "nothing to take a fraction of yet, so nothing is sought");

      _player.RaisePosition(0, 200_000);
      Pump();
      Assert.That(_player.TimeInMs, Is.EqualTo(50_000),
          "and the resume is still waiting when the length arrives, not spent on the first event");
    }

    [Test]
    public void ATitleNobodyStoppedOrEverybodyFinishedStartsAtTheBeginning()
    {
      // Nought is "no position" - what a title the viewer never opened carries, and what the server answers when
      // it was asked by nobody. A hundred is a film that is over. Neither is a place to seek to.
      Assert.That(_screen.Play(SomeFile("new.mkv"), TITLE_ID, 0), Is.True, _screen.StatusLine);
      _player.RaisePosition(0, 100_000);
      Pump();
      Assert.That(_player.TimeInMs, Is.EqualTo(0));
      Assert.That(_screen.StatusLine, Does.Not.Contain("Resumed"));

      Assert.That(_screen.Play(SomeFile("finished.mkv"), TITLE_ID, 100), Is.True, _screen.StatusLine);
      _player.RaisePosition(0, 100_000);
      Pump();
      Assert.That(_player.TimeInMs, Is.EqualTo(0),
          "a finished film plays from the start again rather than a second before the credits");
    }

    #endregion
  }
}
