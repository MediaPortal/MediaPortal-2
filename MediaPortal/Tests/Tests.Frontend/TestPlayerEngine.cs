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
using System.Diagnostics;
using System.Threading;
using MediaPortal.Frontend.Player;
using NUnit.Framework;

namespace Tests.Frontend
{
  /// <summary>
  /// The libVLC engine, driven for real.
  /// </summary>
  /// <remarks>
  /// <para>
  /// These tests do not mock the player - a mocked player tests the mock. Each one builds a real engine, opens a
  /// real file that <see cref="PlayerFixture"/> generated, and watches the state machine and the timings the
  /// engine reports. <c>Headless</c> asks libVLC for its dummy video and audio outputs, so the whole decoding and
  /// clocking half runs on a host with no screen and no sound card, which is what a container and a headless
  /// set-top box have in common.
  /// </para>
  /// <para>
  /// On a machine without libVLC every test here reports as <i>skipped</i> with the engine's own sentence
  /// explaining it, rather than failing. A failure would be a lie about the code; a silent pass would be a lie
  /// about the coverage; the named skip is the only honest third option, and <c>tests.ps1</c> prints the reason so
  /// that a shrinking executed count cannot be mistaken for a shorter suite.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestPlayerEngine
  {
    private LibVlcPlayer? _player;
    private string _reason = "(no engine built yet)";

    [SetUp]
    public void SetUp()
    {
      PlayerOptions options = new PlayerOptions { Headless = true };
      if (!LibVlcPlayer.TryCreate(out _player, out _reason, options))
      {
        _player = null;
        Assert.Ignore("needs libVLC, which this host cannot provide: " + _reason);
      }
      TestContext.Progress.WriteLine("  engine: " + _player!.EngineDescription);
    }

    [TearDown]
    public void TearDown()
    {
      _player?.Dispose();
      _player?.Dispose();  // twice, because a second Dispose must be a no-op rather than a crash
      _player = null;
    }

    // ---- the engine that is loaded -------------------------------------------------------------------

    [Test]
    public void TheEngineNamesTheNativeLibraryItLoaded()
    {
      string description = _player!.EngineDescription;
      Assert.That(description, Does.StartWith("libVLC "));

      // "libVLC 3.0.20 Vetinari" - the version and the release name, straight out of the native library. The
      // number is what matters here: this binding drives the 3.x entry points.
      string version = description.Substring("libVLC ".Length).Split(' ')[0];
      Assert.That(Version.TryParse(version, out Version? parsed), Is.True,
          "the version comes from libVLC itself, so it must parse: " + description);
      Assert.That(parsed!.Major, Is.EqualTo(3),
          "this binding drives libVLC 3.x; a different major version means a different set of entry points");
    }

    [Test]
    public void APlayerStartsIdleWithNothingLoaded()
    {
      Assert.That(_player!.State, Is.EqualTo(PlayerState.Idle));
      Assert.That(_player.Current, Is.Null);
      Assert.That(_player.LastFailure, Is.Null);
      Assert.That(_player.LengthInMs, Is.Zero);
    }

    // ---- playing a file ------------------------------------------------------------------------------

    [Test]
    public void AGeneratedFilePlaysAndIsAsLongAsItsHeaderSays()
    {
      string path = PlayerFixture.WriteWav(2, "length-two-seconds");
      Assert.That(_player!.Open(MediaLocation.FromFile(path)), Is.True, _player.LastFailure ?? "(no reason given)");

      WaitForState(PlayerState.Playing, 15000, "the file never started playing");
      Assert.That(_player.Current?.EngineString, Is.EqualTo(path));

      // The header says 2 s at 8 kHz/16-bit mono. That the engine reports it means the demuxer ran, the format
      // was parsed, and the length reached IVideoPlayer in milliseconds rather than as an internal unit.
      Assert.That(_player.LengthInMs, Is.EqualTo(2000).Within(60),
          "libVLC reported a length that is not the file's own");
    }

    [Test]
    public void TheClockRunsWhilePlaying()
    {
      string path = PlayerFixture.WriteWav(10, "clock-ten-seconds");
      _player!.Open(MediaLocation.FromFile(path));
      WaitForState(PlayerState.Playing, 15000, _player.LastFailure ?? "(no reason)");

      long first = _player.TimeInMs;
      Thread.Sleep(700);
      long later = _player.TimeInMs;
      Assert.That(later, Is.GreaterThan(first),
          "the position did not move in 700 ms, so the clock is not running");
      Assert.That(later, Is.LessThanOrEqualTo(_player.LengthInMs),
          "the position cannot pass the end of the media");
    }

    [Test]
    public void PositionEventsArriveWhilePlaying()
    {
      List<PlaybackPositionEventArgs> events = new List<PlaybackPositionEventArgs>();
      _player!.PositionChanged += delegate (object? sender, PlaybackPositionEventArgs args)
      {
        lock (events)
          events.Add(args);
      };

      string path = PlayerFixture.WriteWav(10, "position-events");
      _player.Open(MediaLocation.FromFile(path));
      WaitForState(PlayerState.Playing, 15000, _player.LastFailure ?? "(no reason)");

      // Waited for rather than slept through, because a headless run is not paced to wall clock (see
      // PlayerOptions.Headless): the growth of the list is the only deadline here worth trusting.
      AssertThatWithin(8000, delegate
      {
        lock (events)
          return events.Count >= 2;
      }, "no position events arrived while playing, so a screen would have to poll the engine");

      PlaybackPositionEventArgs[] seen;
      lock (events)
        seen = events.ToArray();

      Assert.That(seen[seen.Length - 1].LengthInMs, Is.EqualTo(10000).Within(60),
          "the length in the event is the media's own length, not a number the player made up");
      Assert.That(seen[seen.Length - 1].TimeInMs, Is.GreaterThanOrEqualTo(seen[0].TimeInMs),
          "the positions a screen is told must not run backwards");

      for (int i = 0; i < seen.Length; i++)
      {
        Assert.That(seen[i].TimeInMs, Is.GreaterThanOrEqualTo(0),
            "the seam promised never to hand out libVLC's -1 for unknown");
        Assert.That(seen[i].Fraction, Is.InRange(-1d, 1d),
            "a progress value outside the bar is a layout bug waiting to happen");
        if (i > 0)
        {
          Assert.That(seen[i].TimeInMs == seen[i - 1].TimeInMs &&
                      seen[i].LengthInMs == seen[i - 1].LengthInMs, Is.False,
                      "the same position was published twice; a screen rebuilding its layout per event would " +
                      "spend its frame budget on a bar that did not move");
        }
      }

      TestContext.Progress.WriteLine($"  position events: {seen.Length}, last {seen[seen.Length - 1].TimeInMs} ms");
    }

    [Test]
    public void SeekingToTheEndBringsTheEndNearer()
    {
      string path = PlayerFixture.WriteWav(10, "seek-ten-seconds");
      _player!.Open(MediaLocation.FromFile(path));
      WaitForState(PlayerState.Playing, 15000, _player.LastFailure ?? "(no reason)");

      // The measurable half of a seek: after jumping to one second before the end, the media must run out. A
      // position read straight back would only prove a setter stored a number.
      _player.TimeInMs = _player.LengthInMs - 1000;
      WaitForState(PlayerState.Ended, 20000, "the media did not end after seeking near its end");
    }

    [Test]
    public void RunningOutIsReportedAsAnEndAndAnnouncedOnce()
    {
      string path = PlayerFixture.WriteWav(2, "ends-soon");
      int endOfMedia = 0;
      _player!.EndOfMedia += (s, e) => Interlocked.Increment(ref endOfMedia);

      _player.Open(MediaLocation.FromFile(path));
      WaitForState(PlayerState.Ended, 20000, "the file ran out but this is not reported as an end");
      Assert.That(Volatile.Read(ref endOfMedia), Is.EqualTo(1),
          "the end of media is what a playlist acts on; it must fire exactly once");
    }

    [Test]
    public void StoppingIsNotTheSameAsRunningOut()
    {
      string path = PlayerFixture.WriteWav(10, "stopped-early");
      int endOfMedia = 0;
      _player!.EndOfMedia += (s, e) => Interlocked.Increment(ref endOfMedia);

      _player.Open(MediaLocation.FromFile(path));
      WaitForState(PlayerState.Playing, 15000, _player.LastFailure ?? "(no reason)");
      Assert.That(_player.Stop(), Is.True);

      Assert.That(_player.State, Is.EqualTo(PlayerState.Stopped));
      Assert.That(Volatile.Read(ref endOfMedia), Is.Zero,
          "a stop the user caused must not look like the end of the media, or a playlist loops the same item");
      Assert.That(_player.LastFailure, Is.Null);
    }

    [Test]
    public void PausingHoldsAndPlayResumes()
    {
      string path = PlayerFixture.WriteWav(10, "pause-and-play");
      _player!.Open(MediaLocation.FromFile(path));
      WaitForState(PlayerState.Playing, 15000, _player.LastFailure ?? "(no reason)");

      Assert.That(_player.Pause(), Is.True);
      WaitForState(PlayerState.Paused, 5000, "state is " + _player.State);

      Thread.Sleep(300);
      Assert.That(_player.State, Is.EqualTo(PlayerState.Paused), "a paused player must stay paused");

      Assert.That(_player.Play(), Is.True, _player.LastFailure ?? "(no reason)");
      WaitForState(PlayerState.Playing, 5000, "state is " + _player.State);
    }

    [Test]
    public void OnePlayerPlaysSeveralItemsInSequence()
    {
      string first = PlayerFixture.WriteWav(2, "sequence-one");
      string second = PlayerFixture.WriteWav(3, "sequence-two");

      _player!.Open(MediaLocation.FromFile(first));
      WaitForState(PlayerState.Playing, 15000, "the first item in the sequence never started playing");
      Assert.That(_player.LengthInMs, Is.EqualTo(2000).Within(60));

      // This is the path that leaks or deadlocks when Media objects are not released: the old tree's players were
      // rebuilt per item, and a screen that plays an album must be able to reuse one engine. Note that the state
      // may well stay Playing across the switch, which is why the wait is on the *new length* and not on a state
      // change - the length is the thing that can only belong to the second item.
      _player.Open(MediaLocation.FromFile(second));
      AssertThatWithin(15000, () => _player.LengthInMs == 3000,
          "the second item never reported its length; got " + _player.LengthInMs);
      Assert.That(_player.Current?.EngineString, Is.EqualTo(second));
      Assert.That(_player.State, Is.EqualTo(PlayerState.Playing).Or.EqualTo(PlayerState.Ended),
          "a headless engine can run a short file out before this is read, but it must not be broken: " +
          _player.LastFailure);
    }

    // ---- failures, which are the part a TV shows ------------------------------------------------------

    [Test]
    public void AMissingFileFailsWithASentenceAndNoCrash()
    {
      string path = PlayerFixture.MissingFile();
      _player!.Open(MediaLocation.FromFile(path));

      // libVLC accepts the path and then fails a moment later, so both answers are correct as long as one of them
      // arrives; what is not correct is silence, a crash, or a state that stays "opening" forever.
      Stopwatch watch = Stopwatch.StartNew();
      while (watch.ElapsedMilliseconds < 15000)
      {
        if (_player.State == PlayerState.Failed || _player.LastFailure != null)
          break;
        Thread.Sleep(50);
      }

      Assert.That(_player.State, Is.EqualTo(PlayerState.Failed).Or.EqualTo(PlayerState.Stopped),
          "a file that does not exist left the player in " + _player.State);
      Assert.That(_player.LastFailure, Is.Not.Null.And.Not.Empty, "no reason was recorded");
      TestContext.Progress.WriteLine("  refusal: " + _player.LastFailure);
    }

    [Test]
    public void PausingAnIdlePlayerIsRefusedWithAReason()
    {
      Assert.That(_player!.Pause(), Is.False);
      Assert.That(_player.LastFailure, Is.Not.Null.And.Contains("idle"));
    }

    [Test]
    public void VolumeIsClampedRatherThanRefused()
    {
      _player!.Volume = 500;
      Assert.That(_player.Volume, Is.EqualTo(100), "a remote's volume-up at maximum must not wrap or explode");
      _player.Volume = -20;
      Assert.That(_player.Volume, Is.Zero);

      _player.Muted = true;
      Assert.That(_player.Muted, Is.True);
      Assert.That(_player.Volume, Is.EqualTo(0), "muted must not have rewritten the volume");
      _player.Muted = false;
      Assert.That(_player.Muted, Is.False);
    }

    [Test]
    public void TracksAreListedWithoutTheEnginesOffEntry()
    {
      string path = PlayerFixture.WriteWav(4, "one-audio-track");
      _player!.Open(MediaLocation.FromFile(path));
      WaitForState(PlayerState.Playing, 15000, _player.LastFailure ?? "(no reason)");

      // Wait for the engine to have parsed the tracks: they appear after playback starts, not before.
      for (int attempt = 0; attempt < 40 && _player.Tracks(TrackKind.Audio).Count == 0; attempt++)
        Thread.Sleep(100);

      foreach (PlaybackTrack track in _player.Tracks(TrackKind.Audio))
      {
        Assert.That(track.Kind, Is.EqualTo(TrackKind.Audio));
        Assert.That(track.Id, Is.GreaterThanOrEqualTo(0),
            "the engine's own -1 means 'off' and must not be offered as a choice");
        Assert.That(track.Label, Is.Not.Empty);
      }
      TestContext.Progress.WriteLine($"  audio tracks: {_player.Tracks(TrackKind.Audio).Count}");
    }

    [Test]
    public void AVideoWindowIsOnlyClaimedOnAPlatformThatCanDrawIntoOne()
    {
      bool accepted = _player!.AttachTo(0);
      if (OperatingSystem.IsWindows())
      {
        Assert.That(accepted, Is.True, _player.LastFailure ?? "(no reason)");
      }
      else
      {
        Assert.That(accepted, Is.False,
            "this build claims no window support on this platform, so it must not accept a handle");
        Assert.That(_player.LastFailure, Is.Not.Null.And.Not.Empty);
      }
    }

    [Test]
    public void TwoPlayersCanExistAtTheSameTime()
    {
      // The old tree had one global player, which is why a second screen could not preview while something played.
      // Nothing in this design requires that, and one test proves it.
      Assert.That(LibVlcPlayer.TryCreate(out LibVlcPlayer? second, out string reason,
          new PlayerOptions { Headless = true }), Is.True, reason);
      try
      {
        string path = PlayerFixture.WriteWav(3, "second-player");
        second!.Open(MediaLocation.FromFile(path));

        AssertThatWithin(15000, () => second.State == PlayerState.Playing,
            "the second player did not start; state is " + second.State);
        Assert.That(second.LengthInMs, Is.EqualTo(3000).Within(60));
        Assert.That(_player!.State, Is.EqualTo(PlayerState.Idle), "the second player must not touch the first");
      }
      finally
      {
        second?.Dispose();
      }
    }

    [Test]
    public void ADisposedPlayerRefusesWorkInsteadOfThrowing()
    {
      Assert.That(LibVlcPlayer.TryCreate(out LibVlcPlayer? created, out string reason,
              new PlayerOptions { Headless = true }), Is.True, reason);
      // TryCreate answers with a refusal instead of throwing, so the compiler cannot know that a true answer
      // means a player; one null-forgiving operator says that louder than a redundant null assert.
      LibVlcPlayer player = created!;
      player.Dispose();

      Assert.That(player.Open(MediaLocation.FromFile(PlayerFixture.WriteWav(1, "after-dispose"))), Is.False);
      Assert.That(player.LastFailure, Is.Not.Null.And.Contains("disposed"));
      Assert.That(player.State, Is.EqualTo(PlayerState.Idle));
    }

    // ---- helpers ---------------------------------------------------------------------------------------

    /// <summary>Wait for a state, without assuming the engine reports every intermediate one.</summary>
    private bool WaitForState(PlayerState wanted, int milliseconds, string because)
    {
      return AssertThatWithin(milliseconds, () => _player?.State == wanted,
          because + $" (state is {_player?.State}, failure: {_player?.LastFailure ?? "none"})");
    }

    private static bool AssertThatWithin(int milliseconds, Func<bool> condition, string because)
    {
      Stopwatch watch = Stopwatch.StartNew();
      while (watch.ElapsedMilliseconds < milliseconds)
      {
        if (condition())
          return true;
        Thread.Sleep(25);
      }
      // The reason goes into the assert that ends the test. An outer Assert.That(...) around a helper that
      // already asserts would print its message only if the helper *returned*, which it does not - the first run
      // of this file lost its diagnosis exactly that way.
      Assert.That(condition(), Is.True, because);
      return true;
    }
  }
}
