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
    public void TheShippedAnswersAreTheReferenceOnesAndStartFull()
    {
      PlaybackSettings settings = PlaybackSettings.Load(_file, out string? problem);

      Assert.That(problem, Is.Null, "a file that was never written is not a problem");
      Assert.That(settings.InstantSkipPercent, Is.EqualTo(20),
          "the reference's instant skip is twenty per cent of the title, and ships that answer");
      Assert.That(settings.InstantSkipSeconds, Is.EqualTo(5));
      Assert.That(settings.PreferInstantSkipSeconds, Is.False, "the reference prefers the percentage");
      Assert.That(settings.VolumeStep, Is.EqualTo(5));
      Assert.That(settings.InitialVolume, Is.EqualTo(100));
      Assert.That(settings.SubtitlesEnabled, Is.True);
      Assert.That(settings.AudioLanguage, Is.Empty);
      Assert.That(settings.SlideshowSeconds, Is.EqualTo(7));
      Assert.That(settings.WatchedMarkPercentage, Is.EqualTo(90),
          "the reference counts ninety per cent as watched, and ships that answer");
    }

    [Test]
    public void AnswersWrittenComeBackAndNonsenseIsClamped()
    {
      PlaybackSettings settings = PlaybackSettings.Load(_file, out _);
      settings.InstantSkipPercent = 50;
      settings.InstantSkipSeconds = 15;
      settings.PreferInstantSkipSeconds = true;
      settings.InitialVolume = 40;
      settings.AudioLanguage = "eng";
      settings.Save(_file, out bool saved);
      Assert.That(saved, Is.True);

      PlaybackSettings again = PlaybackSettings.Load(_file, out _);
      Assert.That(again.InstantSkipPercent, Is.EqualTo(50));
      Assert.That(again.InstantSkipSeconds, Is.EqualTo(15));
      Assert.That(again.PreferInstantSkipSeconds, Is.True);
      Assert.That(again.InitialVolume, Is.EqualTo(40));
      Assert.That(again.AudioLanguage, Is.EqualTo("eng"));

      File.WriteAllText(_file, "{\"instantSkipPercent\": 99999, \"instantSkipSeconds\": 0, " +
          "\"initialVolume\": -9, \"watchedMarkPercentage\": 400}");
      PlaybackSettings edited = PlaybackSettings.Load(_file, out _);
      Assert.That(edited.InstantSkipPercent, Is.EqualTo(20), "a skip of a thousand titles is not an answer");
      Assert.That(edited.InstantSkipSeconds, Is.EqualTo(5), "a skip of no time at all is not an answer either");
      Assert.That(edited.InitialVolume, Is.EqualTo(100), "negative sound does not exist");
      Assert.That(edited.WatchedMarkPercentage, Is.EqualTo(90),
          "four hundred per cent of a film is not a thing, and the reference's ninety stands");

      // A whole per cent inside the stretch is an answer somebody can mean - including nought, which the
      // reference's own number selector allowed.
      File.WriteAllText(_file, "{\"watchedMarkPercentage\": 0}");
      Assert.That(PlaybackSettings.Load(_file, out _).WatchedMarkPercentage, Is.Zero);
    }

    [Test]
    public void TheSkipLadderAndItsWaitAreTheOnesTheReferenceShips()
    {
      PlaybackSettings settings = PlaybackSettings.Load(_file, out _);
      Assert.That(settings.SkipStepList, Is.EqualTo("15,30,60,180,300,600,900,1800,3600,7200"),
          "the reference's escalating ladder, rung for rung");
      Assert.That(settings.SkipStepTimeoutSeconds, Is.EqualTo(1.5),
          "the reference waits a and a half seconds before it commits the jump");

      settings.SkipStepList = "10,20,40";
      settings.SkipStepTimeoutSeconds = 3;
      settings.Save(_file, out bool saved);
      Assert.That(saved, Is.True);

      PlaybackSettings again = PlaybackSettings.Load(_file, out _);
      Assert.That(again.SkipStepList, Is.EqualTo("10,20,40"));
      Assert.That(again.SkipStepTimeoutSeconds, Is.EqualTo(3));

      // A wait nobody could press against, and a list with nothing climbable in it, are not answers.
      File.WriteAllText(_file, "{\"skipStepTimeoutSeconds\": 0.01, \"skipStepList\": \"  \"}");
      PlaybackSettings edited = PlaybackSettings.Load(_file, out _);
      Assert.That(edited.SkipStepTimeoutSeconds, Is.EqualTo(1.5));
      Assert.That(edited.SkipStepList, Is.EqualTo(PlaybackSettings.DEFAULT_SKIP_STEP_LIST));
    }

    [Test]
    public void TheWatchedMarkRowWalksTheLadderAndWritesEachRung()
    {
      PlaybackSettingsViewModel screen = new PlaybackSettingsViewModel(_file);
      SkinSettingOption row = null;
      foreach (SkinSettingOption option in screen.Options)
        if (option.Label == "Watched at")
          row = option;
      Assert.That(row, Is.Not.Null, "the reference's watched-mark setting deserves its row");

      Assert.That(row.ValueText, Is.EqualTo("90 %"));
      row.Press();
      Assert.That(row.ValueText, Is.EqualTo("100 %"));
      row.Press();
      Assert.That(row.ValueText, Is.EqualTo("50 %"), "the row is a round trip, not a one-way door");
      Assert.That(PlaybackSettings.Load(_file, out _).WatchedMarkPercentage, Is.EqualTo(50),
          "every stop on the row is written where it is reached");
    }

    [Test]
    public void TheKenBurnsEffectIsOnBecauseTheReferenceShippedItOnAndSavesWhenTurnedOff()
    {
      Assert.That(PlaybackSettings.Load(_file, out _).KenBurnsEffect, Is.True,
          "the reference shipped the moving pictures on, and so does this");

      PlaybackSettingsViewModel screen = new PlaybackSettingsViewModel(_file);
      SkinSettingOption row = null;
      foreach (SkinSettingOption option in screen.Options)
        if (option.Label == "Ken Burns effect")
          row = option;
      Assert.That(row, Is.Not.Null);

      row.Press();
      Assert.That(PlaybackSettings.Load(_file, out _).KenBurnsEffect, Is.False,
          "a checkbox on a settings screen that does not save is a decoration");
    }

    [Test]
    public void TheFinishedFilmClosesItsScreenBecauseTheReferenceShippedItThatWay()
    {
      Assert.That(PlaybackSettings.Load(_file, out _).ClosePlayerWhenFinished, Is.True,
          "MediaModelSettings.DEFAULT_CLOSE_PLAYER_WHEN_FINISHED is true, and so this answer ships true");

      PlaybackSettingsViewModel screen = new PlaybackSettingsViewModel(_file);
      SkinSettingOption row = null;
      foreach (SkinSettingOption option in screen.Options)
        if (option.Label == "Close when finished")
          row = option;
      Assert.That(row, Is.Not.Null);

      row.Press();
      Assert.That(PlaybackSettings.Load(_file, out _).ClosePlayerWhenFinished, Is.False,
          "turned off, the finished film stays on its screen until the viewer says otherwise");
    }

    [Test]
    public void ThePlayerKeysReadTheViewersJump()
    {
      PlaybackSettings settings = new PlaybackSettings { InstantSkipPercent = 50, VolumeStep = 2 };
      FakePlayer player = new FakePlayer();
      using NowPlayingViewModel nowPlaying = new NowPlayingViewModel(player);
      nowPlaying.Preferences = settings;

      // The fake's title is a minute long, so half of it is thirty seconds - the reference takes the
      // percentage of the whole title, not of what is left.
      Assert.That(nowPlaying.InstantSkipMs(1), Is.EqualTo(30_000));
      Assert.That(nowPlaying.InstantSkipMs(-1), Is.EqualTo(-30_000));
      Assert.That(nowPlaying.VolumeStep, Is.EqualTo(2));

      PlaybackSettings seconds = new PlaybackSettings { PreferInstantSkipSeconds = true, InstantSkipSeconds = 15 };
      nowPlaying.Preferences = seconds;
      Assert.That(nowPlaying.InstantSkipMs(1), Is.EqualTo(15_000),
          "when seconds are preferred, the length of the title stops mattering");
      Assert.That(nowPlaying.InstantSkipCaption, Is.EqualTo("15 s"));
      nowPlaying.Preferences = settings;
      Assert.That(nowPlaying.InstantSkipCaption, Is.EqualTo("50 %"));

      using NowPlayingViewModel bare = new NowPlayingViewModel(player);
      Assert.That(bare.InstantSkipMs(1), Is.EqualTo(12_000),
          "without an answer of their own, the shipped twenty per cent of the minute stands");
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
