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
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Player;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// Which of the two arrangements the playback screen wears, and what a song is therefore asked of the library.
  /// </summary>
  /// <remarks>
  /// The reference's audio screen (screens/FullScreenContentAudio.xaml) drew another screen for a song than for a
  /// film: no track pickers, the carrier turning behind the cover, and the file's own make told in plaques. All of
  /// that hangs on one answer - is this music - and the answer is taken from two places only: the shelf the title
  /// was pressed in, and the library's statement that the file carries sound of its own. Both are held here,
  /// together with what a screen wearing the audio arrangement may not do: offer a list of tracks to a song, or ask
  /// a film's library for a record it has nowhere to draw.
  /// </remarks>
  [TestFixture]
  public class TestNowPlayingWearsAudio
  {
    private static NowPlayingViewModel Player()
    {
      Mock<IVideoPlayer> player = new Mock<IVideoPlayer>();
      player.Setup(p => p.Open(It.IsAny<MediaLocation>())).Returns(true);
      return new NowPlayingViewModel(player.Object);
    }

    /// <summary>A song as the library files it: tagged, rated, and FLAC on two channels.</summary>
    private static MediaItemDetails Song(Guid id)
    {
      return new MediaItemDetails(id, "Nightglass", null, 2019, null,
          new List<string> { "Synthpop" }, 4, "Recorded in one night.", null, null, null,
          null, null, 7.6, null, null, null, null, null, null, "d:\\music\\nightglass.flac", false, null,
          AudioEncoding: "flac", AudioChannels: 2, AudioBitRate: 771, AudioSampleRate: 44100,
          Artist: "Marta Kessler", Album: "Nightglass", AlbumArtist: "Marta Kessler");
    }

    /// <summary>A film: no audio aspect, so the library answers nothing about its sound.</summary>
    private static MediaItemDetails Film(Guid id)
    {
      return new MediaItemDetails(id, "The Film", null, 1999, new DateTime(1999, 3, 5),
          new List<string> { "Drama" }, 120, "A film about film.", null, null, "FSK 16",
          null, null, 8.4, null, null, null, null, null, null, "d:\\media\\the.film.mkv", false, null);
    }

    [Test]
    public void ASongPressedInTheMusicShelfWearsTheAudioArrangement()
    {
      // The shelf knows before anything is read over the network, so the arrangement is right from the first frame
      // and not a heartbeat later.
      // Said after the title went into the player, which is the order the application speaks it in
      // (App.axaml.cs:2303-2306): a new title is asked from scratch, so the player forgets the shelf of the one
      // before and the caller says the new one.
      NowPlayingViewModel nowPlaying = Player();
      nowPlaying.Play(MediaLocation.FromFile("d:\\music\\nightglass.flac"), Guid.NewGuid(), 0);
      nowPlaying.PlayingAspect = "AudioItem";

      Assert.That(nowPlaying.IsAudio, Is.True, "a title pressed in the music shelf is music");
      Assert.That(nowPlaying.ShowsTrackLists, Is.False,
          "a song is one sound, so the two buttons that open a list of tracks stay away");
      Assert.That(nowPlaying.ShowsClearArt, Is.False,
          "and the lettering by the transport stays away too, since the cover stands there instead");
    }

    [Test]
    public void AFilmPressedInItsShelfKeepsItsTrackPickers()
    {
      NowPlayingViewModel nowPlaying = Player();
      Guid id = Guid.NewGuid();
      nowPlaying.DetailsReader = delegate (Guid asked) { return Task.FromResult(Film(asked)); };
      nowPlaying.Play(MediaLocation.FromFile("d:\\media\\the.film.mkv"), id, 0);
      nowPlaying.PlayingAspect = "MovieItem";

      Assert.That(nowPlaying.IsAudio, Is.False, "a film is not music");
      Assert.That(nowPlaying.ShowsTrackLists, Is.True,
          "and a film keeps the two buttons that open its lists of sound and subtitle tracks");
    }

    [Test]
    public void NothingPlayingOffersNoTrackLists()
    {
      // A picker over no film at all promises a list nobody can fill, which is why the buttons followed the media
      // before they followed the medium. Saying "not a song" is therefore not enough to show them.
      NowPlayingViewModel nowPlaying = Player();

      Assert.That(nowPlaying.HasMedia, Is.False);
      Assert.That(nowPlaying.ShowsTrackLists, Is.False, "with nothing playing no track list is offered");
    }

    [Test]
    public async Task AFileTheLibraryCallsAudioWearsTheAudioArrangementToo()
    {
      // A title opened without a shelf - a file on the command line, a press from the home strip - names no shelf,
      // and then the library's own word about the file decides. A film carries no audio aspect and is answered
      // null, so it keeps the film's arrangement.
      NowPlayingViewModel nowPlaying = Player();
      Guid id = Guid.NewGuid();
      nowPlaying.DetailsReader = delegate (Guid asked) { return Task.FromResult(Song(asked)); };
      nowPlaying.Play(MediaLocation.FromFile("d:\\music\\nightglass.flac"), id, 0);
      Assert.That(nowPlaying.IsAudio, Is.False, "nobody has said what this file is yet");

      await nowPlaying.ReadMediaInfoAsync();

      Assert.That(nowPlaying.IsAudio, Is.True, "the library says the file carries sound of its own");
      Assert.That(nowPlaying.ArtistName, Is.EqualTo("Marta Kessler"), "and who sings it comes with that answer");
    }

    [Test]
    public async Task ARecordIsAskedForASongAndNotForAFilm()
    {
      // The carrier is drawn behind the cover on the audio arrangement and nowhere on the film's, so a film's
      // screen asks for a picture it has no place for. One read per title either way, as with the cover.
      int reads = 0;
      NowPlayingViewModel film = Player();
      film.DiscReader = delegate (Guid asked)
      {
        reads++;
        return Task.FromResult<byte[]?>(new byte[] { 1, 2, 3 });
      };
      film.Play(MediaLocation.FromFile("d:\\media\\the.film.mkv"), Guid.NewGuid(), 0);

      await film.ReadDiscAsync();

      Assert.That(reads, Is.EqualTo(0), "a film is not asked for a record");
      Assert.That(film.Disc, Is.Null);

      NowPlayingViewModel song = Player();
      Guid id = Guid.NewGuid();
      song.DiscReader = delegate (Guid asked)
      {
        reads++;
        return Task.FromResult<byte[]?>(new byte[] { 1, 2, 3 });
      };
      song.Play(MediaLocation.FromFile("d:\\music\\nightglass.flac"), id, 0);

      // The order the application speaks it in, and the reason the record is asked at all: the title going into the
      // player forgets the shelf, and the shelf being named is what starts the question.
      song.PlayingAspect = "AudioItem";

      await song.ReadDiscAsync();
      await song.ReadDiscAsync();

      Assert.That(reads, Is.EqualTo(1), "a song is asked once for its record");
      Assert.That(song.Disc, Is.Not.Null);
      Assert.That(song.ShowsDisc, Is.False, "and it is only drawn while the engine says it is playing");
    }

    [Test]
    public async Task TheStarsLightTheRatingAndNeverLessThanAQuarter()
    {
      // Seven point six of ten is three and four fifths stars, and the star a rating never reached still keeps a
      // quarter of its light - the browser's panel does it this way (MediaBrowserViewModel.cs:4113-4129) and two
      // screens of one library must not light one title two different ways.
      NowPlayingViewModel nowPlaying = Player();
      Guid id = Guid.NewGuid();
      nowPlaying.DetailsReader = delegate (Guid asked) { return Task.FromResult(Song(asked)); };
      nowPlaying.Play(MediaLocation.FromFile("d:\\music\\nightglass.flac"), id, 0);
      nowPlaying.PlayingAspect = "AudioItem";

      await nowPlaying.ReadMediaInfoAsync();

      Assert.That(nowPlaying.ShowsRating, Is.True);
      Assert.That(nowPlaying.Stars[0], Is.EqualTo(1.0));
      Assert.That(nowPlaying.Stars[2], Is.EqualTo(1.0));
      Assert.That(nowPlaying.Stars[3], Is.EqualTo(0.85).Within(0.0001), "the star it reached four fifths of");
      Assert.That(nowPlaying.Stars[4], Is.EqualTo(0.25), "and the one it never reached keeps its quarter");
      Assert.That(nowPlaying.RatingWords, Is.EqualTo("(7.6)"));
    }

    [Test]
    public async Task ThePlaquesSayWhatTheFileIsMadeOfAndStayAwayFromAFilm()
    {
      NowPlayingViewModel song = Player();
      Guid id = Guid.NewGuid();
      song.DetailsReader = delegate (Guid asked) { return Task.FromResult(Song(asked)); };
      song.Play(MediaLocation.FromFile("d:\\music\\nightglass.flac"), id, 0);
      song.PlayingAspect = "AudioItem";

      await song.ReadMediaInfoAsync();

      Assert.That(song.ShowsAudioPlaques, Is.True, "a FLAC on two channels has something to say");
      Assert.That(song.AudioLine.ShowsCodec, Is.True);
      Assert.That(song.AudioLine.ShowsRates, Is.True);

      NowPlayingViewModel film = Player();
      Guid filmId = Guid.NewGuid();
      film.DetailsReader = delegate (Guid asked) { return Task.FromResult(Film(filmId)); };
      film.Play(MediaLocation.FromFile("d:\\media\\the.film.mkv"), filmId, 0);
      await film.ReadMediaInfoAsync();

      Assert.That(film.ShowsAudioPlaques, Is.False,
          "a film's file is not described by the plaques of a song's screen");
    }

    /// <summary>
    /// A library that keeps nothing about the title is asked once, not once per answer. The screen asks its four
    /// questions again on every change it hears from the player - that is how it is written, so that a picture
    /// arriving late is painted - so the brake is the record each question keeps of the title it was answered for.
    /// A 'no picture' that left that record open would be asked again by its own answer, which in front of a server
    /// is one request per answer for as long as the screen stays open. Nothing about this is hypothetical: a track
    /// of a running library answers 'nothing' for its disc art, and most libraries keep no lettering either.
    /// </summary>
    [Test]
    public async Task ATitleTheLibraryKeepsNoPicturesForIsAskedOnce()
    {
      NowPlayingViewModel nowPlaying = Player();
      Guid id = Guid.NewGuid();
      int covers = 0;
      int discs = 0;
      int fanarts = 0;
      nowPlaying.CoverReader = delegate (Guid asked)
      {
        covers++;
        return Task.FromResult<byte[]?>(null);
      };
      nowPlaying.DiscReader = delegate (Guid asked)
      {
        discs++;
        return Task.FromResult<byte[]?>(null);
      };
      nowPlaying.FanartReader = delegate (Guid asked)
      {
        fanarts++;
        return Task.FromResult<byte[]?>(null);
      };
      nowPlaying.Play(MediaLocation.FromFile("d:\\music\\nightglass.flac"), id, 0);
      nowPlaying.PlayingAspect = "AudioItem";

      // Three rounds, because an open screen asks again whenever the player says anything at all.
      for (int round = 0; round < 3; round++)
      {
        await nowPlaying.ReadCoverAsync();
        await nowPlaying.ReadDiscAsync();
        await nowPlaying.ReadFanartAsync();
      }

      Assert.That(covers, Is.EqualTo(1), "a cover the library does not have is not asked for again");
      Assert.That(discs, Is.EqualTo(1), "no disc art is the ordinary answer, and an ordinary answer is believed");
      Assert.That(fanarts, Is.EqualTo(1), "a stage picture the library does not have is not asked for again");
    }

    /// <summary>
    /// A question that never got through stays open, where a question that was answered with nothing is closed: a
    /// server that was not up when the song was pressed is asked again once it is, and a screen that decided a
    /// server's refusal meant 'no pictures' would draw a song without its cover for the rest of the evening.
    /// </summary>
    [Test]
    public async Task APictureThatCouldNotBeAskedForIsAskedAgain()
    {
      NowPlayingViewModel nowPlaying = Player();
      Guid id = Guid.NewGuid();
      int reads = 0;
      nowPlaying.CoverReader = delegate (Guid asked)
      {
        reads++;
        throw new InvalidOperationException("there is no server to ask");
      };
      nowPlaying.Play(MediaLocation.FromFile("d:\\music\\nightglass.flac"), id, 0);
      nowPlaying.PlayingAspect = "AudioItem";

      await nowPlaying.ReadCoverAsync();
      await nowPlaying.ReadCoverAsync();

      Assert.That(reads, Is.EqualTo(2), "a refused question is asked again, since nothing was answered");
      Assert.That(nowPlaying.Cover, Is.Null);
    }
  }
}
