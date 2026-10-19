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
using MediaPortal.Frontend.Player;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// What the viewer told the player, and whether the player listens. The answers live in playback.json
  /// beside the other settings files; the player reads the reach of its skip and volume keys, the volume
  /// a media starts at, and - once a media has said which tracks it has - the languages to reach for.
  /// A preference is honoured only where a track says that language: these tests hold the player to that
  /// promise from both sides.
  /// </summary>
  [TestFixture]
  public class TestPlaybackSettings
  {
    private string _file = "";

    [SetUp]
    public void AFileOfItsOwn()
    {
      _file = Path.Combine(Path.GetTempPath(), "mp2-playback-" + Guid.NewGuid().ToString("N") + ".json");
    }

    [TearDown]
    public void TakeTheFileAway()
    {
      try { File.Delete(_file); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    [Test]
    public void TheShippedAnswersSkipThirtyAndStartFull()
    {
      PlaybackSettings settings = PlaybackSettings.Load(_file, out string? problem);

      Assert.That(problem, Is.Null, "a file that was never written is not a problem");
      Assert.That(settings.SkipStepSeconds, Is.EqualTo(30));
      Assert.That(settings.VolumeStep, Is.EqualTo(5));
      Assert.That(settings.InitialVolume, Is.EqualTo(100));
      Assert.That(settings.SubtitlesEnabled, Is.True);
      Assert.That(settings.AudioLanguage, Is.Empty);
      Assert.That(settings.SlideshowSeconds, Is.EqualTo(7));
    }

    [Test]
    public void AnswersWrittenComeBackAndNonsenseIsClamped()
    {
      PlaybackSettings settings = PlaybackSettings.Load(_file, out _);
      settings.SkipStepSeconds = 120;
      settings.InitialVolume = 40;
      settings.AudioLanguage = "eng";
      settings.Save(_file, out bool saved);
      Assert.That(saved, Is.True);

      PlaybackSettings again = PlaybackSettings.Load(_file, out _);
      Assert.That(again.SkipStepSeconds, Is.EqualTo(120));
      Assert.That(again.InitialVolume, Is.EqualTo(40));
      Assert.That(again.AudioLanguage, Is.EqualTo("eng"));

      File.WriteAllText(_file, "{\"skipStepSeconds\": 99999, \"initialVolume\": -9}");
      PlaybackSettings edited = PlaybackSettings.Load(_file, out _);
      Assert.That(edited.SkipStepSeconds, Is.EqualTo(30), "a skip of a thousand hours is not an answer");
      Assert.That(edited.InitialVolume, Is.EqualTo(100), "negative sound does not exist");
    }

    [Test]
    public void ThePlayerKeysReadTheViewersReach()
    {
      PlaybackSettings settings = new PlaybackSettings { SkipStepSeconds = 90, VolumeStep = 2 };
      using NowPlayingViewModel nowPlaying = new NowPlayingViewModel(new FakePlayer());
      nowPlaying.Preferences = settings;

      Assert.That(nowPlaying.SkipStepMs, Is.EqualTo(90_000));
      Assert.That(nowPlaying.VolumeStep, Is.EqualTo(2));

      using NowPlayingViewModel bare = new NowPlayingViewModel(new FakePlayer());
      Assert.That(bare.SkipStepMs, Is.EqualTo(30_000), "without an answer of their own, the shipped reach stands");
      Assert.That(bare.VolumeStep, Is.EqualTo(5));
    }

    [Test]
    public void AStartedMediaBeginsAtTheAnsweredVolume()
    {
      FakePlayer player = new FakePlayer();
      using NowPlayingViewModel nowPlaying = new NowPlayingViewModel(player);
      nowPlaying.Preferences = new PlaybackSettings { InitialVolume = 40 };

      Assert.That(nowPlaying.Play(MediaLocation.FromFile("d:\\media\\the.film.mkv")), Is.True);
      Assert.That(player.Volume, Is.EqualTo(40), "the first frame is already as loud as the answer says");
    }

    [Test]
    public void TheAnsweredLanguageReachesForItsTrackOnceTheMediaSaysItHasOne()
    {
      FakePlayer player = new FakePlayer
      {
        Audio = new[]
        {
          new PlaybackTrack(TrackKind.Audio, 1, "Stereo", "deu"),
          new PlaybackTrack(TrackKind.Audio, 2, "Surround", "eng")
        },
        Subtitles = new[]
        {
          new PlaybackTrack(TrackKind.Subtitle, 10, "English", "eng"),
          new PlaybackTrack(TrackKind.Subtitle, 11, "Deutsch", "deu")
        },
        CurrentAudioId = 1,
        CurrentSubtitleId = -1
      };

      using NowPlayingViewModel nowPlaying = new NowPlayingViewModel(player);
      nowPlaying.Preferences = new PlaybackSettings { AudioLanguage = "eng", SubtitleLanguage = "eng" };
      Assert.That(nowPlaying.Play(MediaLocation.FromFile("d:\\media\\the.film.mkv")), Is.True);
      nowPlaying.ReadTracks();

      Assert.That(player.CurrentAudioId, Is.EqualTo(2), "the track that says English gets the audio");
      Assert.That(player.CurrentSubtitleId, Is.EqualTo(10), "and the subtitle that says English gets the words");
    }

    [Test]
    public void AFileWithoutTheLanguageIsNotArguedWith()
    {
      FakePlayer player = new FakePlayer
      {
        Audio = new[] { new PlaybackTrack(TrackKind.Audio, 1, "Stereo", "deu") },
        CurrentAudioId = 1
      };

      using NowPlayingViewModel nowPlaying = new NowPlayingViewModel(player);
      nowPlaying.Preferences = new PlaybackSettings { AudioLanguage = "pol" };
      nowPlaying.Play(MediaLocation.FromFile("d:\\media\\the.film.mkv"));
      nowPlaying.ReadTracks();

      Assert.That(player.CurrentAudioId, Is.EqualTo(1), "nobody speaks Polish here, and the media's choice stands");
      Assert.That(player.Calls, Does.Not.Contain("SelectTrack:1"), "and no switching was even tried");
    }

    [Test]
    public void SubtitlesSwitchedOffStayOffEvenWhenTheMediaPickedThem()
    {
      FakePlayer player = new FakePlayer
      {
        Subtitles = new[] { new PlaybackTrack(TrackKind.Subtitle, 10, "English", "eng") },
        CurrentSubtitleId = 10
      };

      using NowPlayingViewModel nowPlaying = new NowPlayingViewModel(player);
      nowPlaying.Preferences = new PlaybackSettings { SubtitlesEnabled = false };
      nowPlaying.Play(MediaLocation.FromFile("d:\\media\\the.film.mkv"));
      nowPlaying.ReadTracks();

      Assert.That(player.CurrentSubtitleId, Is.EqualTo(-1), "the viewer said no, and no is not a preference");
    }

    [Test]
    public void TheAnswerIsAppliedOncePerMediaNotOncePerSecond()
    {
      FakePlayer player = new FakePlayer
      {
        Audio = new[]
        {
          new PlaybackTrack(TrackKind.Audio, 1, "Stereo", "deu"),
          new PlaybackTrack(TrackKind.Audio, 2, "Surround", "eng")
        },
        CurrentAudioId = 2
      };

      using NowPlayingViewModel nowPlaying = new NowPlayingViewModel(player);
      nowPlaying.Preferences = new PlaybackSettings { AudioLanguage = "eng" };
      nowPlaying.Play(MediaLocation.FromFile("d:\\media\\the.film.mkv"));
      nowPlaying.ReadTracks();

      // The viewer pressed the track button themselves, against the answered language; a second ReadTracks
      // - a chapter change, a screen coming back - must not undo what their own press said.
      player.CurrentAudioId = 1;
      nowPlaying.ReadTracks();
      Assert.That(player.CurrentAudioId, Is.EqualTo(1), "the viewer's own press outvotes the old answer");
    }
  }
}
