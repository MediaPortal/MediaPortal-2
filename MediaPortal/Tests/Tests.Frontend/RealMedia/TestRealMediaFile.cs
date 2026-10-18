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
using System.Globalization;
using System.IO;
using System.Threading;
using MediaPortal.Frontend.Player;
using NUnit.Framework;

namespace Tests.Frontend.RealMedia
{
  /// <summary>
  /// The player in front of a file that a person actually watches: a feature-length video with a real codec,
  /// real audio and embedded subtitles, none of which fits in a repository.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This fixture is <b>not compiled</b> unless the build is asked for it with <c>-p:RealMediaTests=true</c>, and
  /// that is deliberate. The alternative - a test that skips when the file is absent - looks honest and is not:
  /// a suite whose only video-codec test reports "skipped" will keep reporting green forever without decoding a
  /// single frame. Compiling this file at all means somebody named a file, so every missing input here is a
  /// failure and nothing here is a skip.
  /// </para>
  /// <para>
  /// The file comes from <c>MP2_TEST_MEDIA</c>. <see cref="TestPlayerEngine"/> covers the state machine and the
  /// clock with a WAV file it generates itself, which is the part that must run everywhere; what this file adds
  /// is the part that cannot be generated: HEVC decoding, a duration in hours, several tracks, and seeking across
  /// keyframes. <c>scripts\ci\test-player-file.ps1</c> is the intended way to run it - it asks <c>ffprobe</c> what
  /// the container holds and passes that along, so what the player reports is checked against an instrument that
  /// is not the player.
  /// </para>
  /// <para>
  /// What is configured: <c>MP2_TEST_MEDIA</c> (the file, required), <c>MP2_TEST_MEDIA_LENGTH_MS</c> (the
  /// duration the container states), <c>MP2_TEST_MEDIA_AUDIO_TRACKS</c> and <c>MP2_TEST_MEDIA_SUBTITLE_TRACKS</c>
  /// (how many the container holds), <c>MP2_TEST_MEDIA_HAS_VIDEO</c> (default true, "0" for audio-only),
  /// <c>MP2_TEST_MEDIA_SECONDS</c> (how long to watch, default 6) and <c>MP2_TEST_MEDIA_SHOW</c> ("1" plays with
  /// a real video output instead of the dummy one, for a person to look at).
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestRealMediaFile
  {
    private LibVlcPlayer _player = null!;
    private string _path = "";
    private long _expectedLengthMs;
    private int _expectedAudioTracks;
    private int _expectedSubtitleTracks;
    private bool _expectsVideo;
    private int _expectedVideoWidth;
    private int _expectedVideoHeight;
    private int _watchMilliseconds;

    [OneTimeSetUp]
    public void FindTheFile()
    {
      _path = (Environment.GetEnvironmentVariable("MP2_TEST_MEDIA") ?? string.Empty).Trim().Trim('"');
      Assert.That(_path, Is.Not.Empty,
          "this fixture was compiled on purpose, so it needs MP2_TEST_MEDIA to point at a media file");
      Assert.That(File.Exists(_path), Is.True, "there is no file at MP2_TEST_MEDIA: " + _path);

      _expectedLengthMs = ReadNumber("MP2_TEST_MEDIA_LENGTH_MS", 0);
      _expectedAudioTracks = (int)ReadNumber("MP2_TEST_MEDIA_AUDIO_TRACKS", 1);
      _expectedSubtitleTracks = (int)ReadNumber("MP2_TEST_MEDIA_SUBTITLE_TRACKS", 0);
      _expectsVideo = Environment.GetEnvironmentVariable("MP2_TEST_MEDIA_HAS_VIDEO") != "0";
      _watchMilliseconds = (int)ReadNumber("MP2_TEST_MEDIA_SECONDS", 6) * 1000;

      // The size the container says the film is, as WxH. Optional, but when it is there it is worth more than the
      // frame counter: a decoder hands over a frame rounded up to its own block size, and only the container's
      // number says which part of that is the film.
      string video = (Environment.GetEnvironmentVariable("MP2_TEST_MEDIA_VIDEO") ?? string.Empty).Trim();
      int separator = video.IndexOf('x');
      if (separator > 0)
      {
        int.TryParse(video.Substring(0, separator), out _expectedVideoWidth);
        int.TryParse(video.Substring(separator + 1), out _expectedVideoHeight);
      }

      TestContext.Progress.WriteLine("  media:    " + Path.GetFileName(_path));
      TestContext.Progress.WriteLine("  bytes:    " + new FileInfo(_path).Length.ToString("N0", CultureInfo.InvariantCulture));
      TestContext.Progress.WriteLine("  expected: " + DescribeExpectations());
    }

    [SetUp]
    public void BuildTheEngine()
    {
      // Headless by default, because that is the mode a container and a set-top box share. MP2_TEST_MEDIA_SHOW=1
      // turns the dummy outputs off, which is how a person watches the same file the assertions are running
      // against and judges the picture - the one thing no assertion here can do.
      bool show = Environment.GetEnvironmentVariable("MP2_TEST_MEDIA_SHOW") == "1";
      if (!LibVlcPlayer.TryCreate(out LibVlcPlayer? player, out string reason,
                                  new PlayerOptions { Headless = !show }))
      {
        // The one skip in this file, and it is the same honest skip TestPlayerEngine uses: a host without the
        // native library cannot be made to have it, and failing here would blame the code for the machine.
        Assert.Ignore("needs libVLC, which this host cannot provide: " + reason);
      }
      _player = player!;
      TestContext.Progress.WriteLine("  engine:   " + _player.EngineDescription);
    }

    [TearDown]
    public void PutTheEngineAway()
    {
      _player?.Dispose();
      _player = null!;
    }

    [Test]
    public void ARealFileOpensAndPlays()
    {
      Assert.That(_player.Open(MediaLocation.FromFile(_path)), Is.True,
          _player.LastFailure ?? "(the engine gave no reason)");
      WaitFor(() => _player.State == PlayerState.Playing, 30_000, "a real file never started playing");
    }

    [Test]
    public void ThePlayerReportsTheLengthTheContainerStates()
    {
      OpenAndPlay();

      Assert.That(_player.LengthInMs, Is.GreaterThan(0),
          "the engine does not know how long the file is, so a progress bar and a resume position are impossible");

      if (_expectedLengthMs <= 0)
      {
        TestContext.Progress.WriteLine("  length:   " + Format(_player.LengthInMs)
            + " (nothing stated to compare it with)");
        return;
      }

      // ffprobe and libVLC read the same container, so a difference of more than a second and a half means one of
      // them is reading a header field the other ignores. A resume position saved against the wrong number would
      // put a viewer at the wrong minute of their film.
      long difference = Math.Abs(_player.LengthInMs - _expectedLengthMs);
      Assert.That(difference, Is.LessThan(1500),
          $"the container says {_expectedLengthMs} ms and the player says {_player.LengthInMs} ms: "
          + $"{difference} ms apart");
      TestContext.Progress.WriteLine("  length:   player " + Format(_player.LengthInMs) + ", container "
          + Format(_expectedLengthMs));
    }

    /// <summary>
    /// The assertion no generated file could ever support: frames of a real video codec came out of the decoder.
    /// </summary>
    [Test]
    public void TheVideoIsReallyDecoded()
    {
      OpenAndPlay();
      Thread.Sleep(_watchMilliseconds);
      PlayerStatistics counted = _player.Statistics;
      TestContext.Progress.WriteLine("  decoded:  " + counted);

      Assert.That(counted.DecodedAudioFrames, Is.GreaterThan(0),
          "nothing was decoded from the audio of a file that has audio");

      if (!_expectsVideo)
      {
        Assert.That(counted.DecodedVideoFrames, Is.Zero, "an audio-only file should not decode video");
        return;
      }

      Assert.That(counted.DecodedVideoFrames, Is.GreaterThan(0),
          "the player says it is playing but no video frame was decoded. That is the state where a viewer hears "
          + "the film and sees a black screen, and it is invisible in every other property of the seam");

      // Some dropped frames are normal. Dropping more than it shows is not "a slow machine" - it is a machine
      // that will never show this film.
      Assert.That(counted.LostVideoFrames, Is.LessThan(counted.DecodedVideoFrames),
          "the engine dropped more frames than it decoded: " + counted);
    }

    /// <summary>
    /// The road a picture takes to a screen: libVLC decodes, hands the result into memory this codebase owns, and
    /// somebody downstream can copy it out - and keeps doing so after the collector has been through.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This needs a player that was not built headless. A dummy video output and "hand me the frames yourself" are
    /// two answers to one question, and the engine refuses the combination loudly rather than playing a film whose
    /// picture never arrives; asking for the pictures is itself the memory video output, which needs no display on
    /// any platform.
    /// </para>
    /// <para>
    /// The collector is invoked on purpose in the middle of it. The door from native code into managed code is a
    /// handful of callbacks this object hands over once, and if nothing rooted them the collector would take them
    /// away between one frame and the next - a failure that shows up minutes into a film on a machine under memory
    /// pressure, which is to say never in a test and always in a living room.
    /// </para>
    /// <para>
    /// The order of the two <c>using</c> statements below is part of the test and should not be tidied away: the
    /// sink is disposed <i>first</i>, while the engine is still playing, because that is the order a front end
    /// produces when a window closes. It used to take the process down with an access violation inside libVLC -
    /// the sink unpinned buffers the decoder still held, and a lock callback with no sink to take the picture
    /// answered the engine with nothing, which the memory video output does not tolerate. Both halves of that are
    /// fixed in MediaPortal.Frontend.Player, and this is the test that keeps them fixed.
    /// </para>
    /// </remarks>
    [Test]
    public void ThePictureArrivesAtTheSinksDoorAndSurvivesAGarbageCollection()
    {
      Assert.That(_expectsVideo, Is.True,
          "the gate says this file holds no video, so there is no picture to ask an engine for");

      Assert.That(LibVlcPlayer.TryCreate(out LibVlcPlayer? painter, out string reason,
                                         new PlayerOptions { Headless = false }),
                  Is.True, "no second engine to ask for pictures: " + reason);

      using (painter)
      using (VideoFrameSink frames = new VideoFrameSink())
      {
        Assert.That(painter!.AttachTo(frames), Is.True,
            painter.LastFailure ?? "the player refused to hand its pictures over, and said nothing about why");

        Assert.That(painter.Open(MediaLocation.FromFile(_path)), Is.True, painter.LastFailure);
        painter.Play();

        Assert.That(Wait(delegate { return frames.FramesArrived > 0; }, 20_000), Is.True,
            "the film played for twenty seconds and not one picture reached the sink");
        // A second of playback, so the number printed below is a rate somebody can recognise rather than the
        // single frame the wait above happened to wake up on.
        Thread.Sleep(1000);
        TestContext.Progress.WriteLine("  pictures: " + frames.FramesArrived + " of " + frames.PictureWidth
            + "x" + frames.PictureHeight + " (buffers " + frames.Width + "x" + frames.Height + ")");

        Assert.That(frames.PictureWidth, Is.GreaterThan(0));
        Assert.That(frames.PictureHeight, Is.GreaterThan(0));

        if (_expectedVideoWidth > 0 && _expectedVideoHeight > 0)
        {
          Assert.That(frames.PictureWidth, Is.EqualTo(_expectedVideoWidth),
              "the picture a screen would draw is " + frames.PictureWidth + " wide, and the container says the "
              + "film is " + _expectedVideoWidth);
          Assert.That(frames.PictureHeight, Is.EqualTo(_expectedVideoHeight),
              "the picture a screen would draw is " + frames.PictureHeight + " lines high while the container "
              + "says " + _expectedVideoHeight + ", which means the lines the codec rounded its height up to are "
              + "being drawn: the film is stretched by "
              + (frames.PictureHeight * 100 / _expectedVideoHeight - 100) + " percent");
        }

        byte[] copied = new byte[frames.PictureWidth * 4 * frames.PictureHeight];
        Assert.That(frames.TryCopyLatest(copied, frames.PictureWidth * 4, frames.PictureWidth,
                                         frames.PictureHeight),
                    Is.True, "a picture was announced and then could not be copied by a screen");

        long before = frames.FramesArrived;
        for (int i = 0; i < 10; i++)
        {
          GC.Collect();
          GC.WaitForPendingFinalizers();
          GC.Collect();
        }

        Assert.That(Wait(delegate { return frames.FramesArrived > before; }, 20_000), Is.True,
            "after a full collection no further picture arrived, which means the callbacks from native code into "
            + "this assembly were not rooted anywhere and the collector has taken the door away");
      }
    }

    [Test]
    public void TheCountersOnlyGrowWhileAFilePlays()
    {
      OpenAndPlay();
      PlayerStatistics first = _player.Statistics;
      Thread.Sleep(Math.Max(1000, _watchMilliseconds / 2));
      PlayerStatistics then = _player.Statistics;
      TestContext.Progress.WriteLine($"  counters: {first} -> {then}");

      Assert.That(then.DecodedVideoFrames, Is.GreaterThanOrEqualTo(first.DecodedVideoFrames),
          $"the video counter went backwards: {first} then {then}");
      Assert.That(then.DecodedAudioFrames, Is.GreaterThan(first.DecodedAudioFrames),
          $"the audio counter did not move while playing: {first} then {then}");
    }

    [Test]
    public void AFileWithSeveralTracksListsThemAndTakesBeingSwitched()
    {
      OpenAndPlay();

      IReadOnlyList<PlaybackTrack> audio = _player.Tracks(TrackKind.Audio);
      IReadOnlyList<PlaybackTrack> subtitles = _player.Tracks(TrackKind.Subtitle);
      foreach (PlaybackTrack track in audio)
        TestContext.Progress.WriteLine("  audio:    " + track);
      foreach (PlaybackTrack track in subtitles)
        TestContext.Progress.WriteLine("  subtitle: " + track);

      Assert.That(audio.Count, Is.GreaterThanOrEqualTo(_expectedAudioTracks),
          $"the container holds {_expectedAudioTracks} audio track(s), the player found {audio.Count}");
      Assert.That(subtitles.Count, Is.GreaterThanOrEqualTo(_expectedSubtitleTracks),
          $"the container holds {_expectedSubtitleTracks} subtitle track(s), the player found {subtitles.Count}");

      if (subtitles.Count == 0)
        return;

      Assert.That(_player.SelectTrack(subtitles[0]), Is.True,
          _player.LastFailure ?? "libVLC refused to select the subtitle track it had just listed");
      Assert.That(_player.SelectTrack(new PlaybackTrack(TrackKind.Subtitle, -1)), Is.True,
          "switching subtitles off is the same call with id -1, and a viewer has to be able to do it");
    }

    [Test]
    public void SeekingIntoTheMiddleOfAFileLandsThere()
    {
      OpenAndPlay();
      long target = _player.LengthInMs / 3;
      _player.TimeInMs = target;

      // A seek lands on a keyframe rather than on the millisecond that was asked for, and how far off that is
      // depends on how the file was encoded - a minute is normal for a long-GOP broadcast encode. What must not
      // happen is a player that reports a seek and stays at the beginning.
      long tolerance = Math.Min(120_000, Math.Max(20_000, _player.LengthInMs / 20));
      Assert.That(Wait(() => Math.Abs(_player.TimeInMs - target) <= tolerance, 30_000), Is.True,
          $"asked for {Format(target)} and is at {Format(_player.TimeInMs)} after 30 s");

      Assert.That(_player.TimeInMs, Is.GreaterThan(_player.LengthInMs / 10),
          "the position barely moved from the start, so the seek did not happen");
      TestContext.Progress.WriteLine("  seek:     asked " + Format(target) + ", arrived at "
          + Format(_player.TimeInMs));

      Assert.That(Wait(() => _player.State == PlayerState.Playing, 15_000), Is.True,
          "a seek is not a pause; whoever pressed the remote expects the film to carry on");
    }

    [Test]
    public void PausingAndResumingADeeplyBufferedFileWorks()
    {
      OpenAndPlay();
      Thread.Sleep(1500);

      Assert.That(_player.Pause(), Is.True, _player.LastFailure ?? "(no reason)");
      Assert.That(Wait(() => _player.State == PlayerState.Paused, 10_000), Is.True,
          "the file is playing but did not pause");
      long at = _player.TimeInMs;
      Thread.Sleep(1000);
      Assert.That(_player.TimeInMs, Is.EqualTo(at), "a paused file kept advancing");

      Assert.That(_player.Play(), Is.True, _player.LastFailure ?? "(no reason)");
      Assert.That(Wait(() => _player.TimeInMs > at, 20_000), Is.True,
          "the file was resumed and never moved again");
    }

    [Test]
    public void StoppingARealFileLeavesThePlayerUsableForTheNextOne()
    {
      OpenAndPlay();
      Assert.That(_player.Stop(), Is.True, _player.LastFailure ?? "(no reason)");
      Assert.That(Wait(() => _player.State == PlayerState.Stopped, 10_000), Is.True,
          "stop was accepted and the state is " + _player.State);
      Assert.That(_player.Statistics.NothingCounted, Is.True,
          "the counters belong to the media that is open, not to the one that was last: " + _player.Statistics);

      Assert.That(_player.Open(MediaLocation.FromFile(_path)), Is.True,
          _player.LastFailure ?? "the same file could not be opened twice on one player");
      Assert.That(Wait(() => _player.State == PlayerState.Playing, 30_000), Is.True,
          "the second open was accepted but nothing played");
    }

    // ---- helpers -------------------------------------------------------------------------------------

    private void OpenAndPlay()
    {
      Assert.That(_player.Open(MediaLocation.FromFile(_path)), Is.True,
          _player.LastFailure ?? "(the engine gave no reason)");
      WaitFor(() => _player.State == PlayerState.Playing, 30_000, "the file never started playing");
      WaitFor(() => _player.LengthInMs > 0, 15_000, "the file is playing and its length is still unknown");
    }

    private void WaitFor(Func<bool> condition, int milliseconds, string because)
    {
      if (Wait(condition, milliseconds))
        return;
      // Assert at the end rather than returning a bool, so the reason reaches the report - a helper that returns
      // false leaves the outer assert printing its own message without the state that produced it.
      Assert.That(condition(), Is.True,
          because + $" (state {_player.State}, failure: {_player.LastFailure ?? "none"})");
    }

    private static bool Wait(Func<bool> condition, int milliseconds)
    {
      Stopwatch watch = Stopwatch.StartNew();
      while (watch.ElapsedMilliseconds < milliseconds)
      {
        if (condition())
          return true;
        Thread.Sleep(50);
      }
      return false;
    }

    private static long ReadNumber(string variable, long whenAbsent)
    {
      string? raw = Environment.GetEnvironmentVariable(variable);
      return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed)
          ? parsed
          : whenAbsent;
    }

    private string DescribeExpectations()
    {
      return (_expectedLengthMs > 0 ? Format(_expectedLengthMs) : "length unstated") + ", "
             + _expectedAudioTracks + " audio track(s), " + _expectedSubtitleTracks + " subtitle track(s), "
             + (_expectsVideo ? "video expected" : "no video track") + ", watching "
             + (_watchMilliseconds / 1000) + " s";
    }

    private static string Format(long milliseconds)
    {
      return TimeSpan.FromMilliseconds(milliseconds).ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture);
    }
  }
}
