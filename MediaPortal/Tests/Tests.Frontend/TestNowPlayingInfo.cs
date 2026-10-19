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
  /// What the player's info panel says, and how often it asks the library to say it.
  /// </summary>
  /// <remarks>
  /// The reference skins kept a media info screen over the picture for exactly this sentence, and it read the
  /// library rather than the file name. These tests hold the playback model to the same rules: one read per
  /// title, an honest sentence when there is nothing to read, and no lie carried over from the previous film.
  /// </remarks>
  [TestFixture]
  public class TestNowPlayingInfo
  {
    private static NowPlayingViewModel Player()
    {
      Mock<IVideoPlayer> player = new Mock<IVideoPlayer>();
      player.Setup(p => p.Open(It.IsAny<MediaLocation>())).Returns(true);
      return new NowPlayingViewModel(player.Object);
    }

    private static MediaItemDetails Film(Guid id)
    {
      return new MediaItemDetails(id, "The Film", null, 1999, new DateTime(1999, 3, 5),
          new List<string> { "Drama", "Crime" }, 120, "A film about film.", null, null, "FSK 16",
          null, null, 8.4, null, null, null, null, null, null, "d:\\media\\the.film.mkv", false, null);
    }

    [Test]
    public async Task TheInfoSaysWhatTheLibraryKnows()
    {
      NowPlayingViewModel nowPlaying = Player();
      Guid id = Guid.NewGuid();
      nowPlaying.DetailsReader = delegate (Guid asked) { return Task.FromResult(Film(asked)); };
      nowPlaying.Play(MediaLocation.FromFile("d:\\media\\the.film.mkv"), id, 0);

      await nowPlaying.ReadMediaInfoAsync();

      Assert.That(nowPlaying.MediaInfoText, Does.Contain("The Film (1999)"));
      Assert.That(nowPlaying.MediaInfoText, Does.Contain("Drama, Crime"));
      Assert.That(nowPlaying.MediaInfoText, Does.Contain("first aired 5 Mar 1999"));
      Assert.That(nowPlaying.MediaInfoText, Does.Contain("rated FSK 16"));
      Assert.That(nowPlaying.MediaInfoText, Does.Contain("d:\\media\\the.film.mkv"));
      Assert.That(nowPlaying.MediaInfoText, Does.Contain("A film about film."));
    }

    [Test]
    public async Task OneTitleIsAskedOnce()
    {
      NowPlayingViewModel nowPlaying = Player();
      Guid id = Guid.NewGuid();
      int reads = 0;
      nowPlaying.DetailsReader = delegate (Guid asked)
      {
        reads++;
        return Task.FromResult(Film(asked));
      };
      nowPlaying.Play(MediaLocation.FromFile("d:\\media\\the.film.mkv"), id, 0);

      await nowPlaying.ReadMediaInfoAsync();
      await nowPlaying.ReadMediaInfoAsync();

      Assert.That(reads, Is.EqualTo(1), "the library does not rewrite a film's plot while it is watched");
    }

    [Test]
    public async Task AFileThatBelongsToNoLibraryIsNotAsked()
    {
      NowPlayingViewModel nowPlaying = Player();
      int reads = 0;
      nowPlaying.DetailsReader = delegate (Guid asked)
      {
        reads++;
        return Task.FromResult(Film(asked));
      };
      nowPlaying.Play(MediaLocation.FromFile("d:\\media\\the.film.mkv"));

      await nowPlaying.ReadMediaInfoAsync();

      Assert.That(reads, Is.EqualTo(0), "a question with no subject is not asked");
      Assert.That(nowPlaying.MediaInfoText, Does.Contain("no library"));
    }

    [Test]
    public async Task TheNextFilmDoesNotWearTheOnesBeforePlot()
    {
      NowPlayingViewModel nowPlaying = Player();
      Guid first = Guid.NewGuid();
      Guid second = Guid.NewGuid();
      nowPlaying.DetailsReader = delegate (Guid asked)
      {
        return Task.FromResult(asked == first ? Film(asked) : Film(asked) with { Title = "The Other" });
      };
      nowPlaying.Play(MediaLocation.FromFile("d:\\media\\the.film.mkv"), first, 0);
      await nowPlaying.ReadMediaInfoAsync();
      Assert.That(nowPlaying.MediaInfoText, Does.Contain("The Film"));

      nowPlaying.Play(MediaLocation.FromFile("d:\\media\\the.other.mkv"), second, 0);

      Assert.That(nowPlaying.MediaInfoText, Does.Not.Contain("A film about film."),
          "an old film's plot does not outlive its film");
      Assert.That(nowPlaying.MediaInfoText, Does.Contain("not answered"),
          "the new title says it is waiting rather than quoting the old one");
    }

    [Test]
    public async Task TheCoverArrivesOnceAndGoesWithTheTitle()
    {
      NowPlayingViewModel nowPlaying = Player();
      Guid first = Guid.NewGuid();
      Guid second = Guid.NewGuid();
      int reads = 0;
      nowPlaying.CoverReader = delegate (Guid asked)
      {
        reads++;
        return Task.FromResult<byte[]?>(new byte[] { 1, 2, 3 });
      };
      nowPlaying.Play(MediaLocation.FromFile("d:\\media\\the.film.mkv"), first, 0);

      await nowPlaying.ReadCoverAsync();
      await nowPlaying.ReadCoverAsync();

      Assert.That(nowPlaying.Cover, Is.Not.Null);
      Assert.That(reads, Is.EqualTo(1), "the same title is asked once");

      nowPlaying.Play(MediaLocation.FromFile("d:\\media\\the.other.mkv"), second, 0);
      Assert.That(nowPlaying.Cover, Is.Null, "the next title does not wear this one's cover");
    }

    [Test]
    public async Task AFileThatBelongsToNoLibraryGetsNoCover()
    {
      NowPlayingViewModel nowPlaying = Player();
      int reads = 0;
      nowPlaying.CoverReader = delegate (Guid asked)
      {
        reads++;
        return Task.FromResult<byte[]?>(new byte[] { 1 });
      };
      nowPlaying.Play(MediaLocation.FromFile("d:\\media\\the.film.mkv"));

      await nowPlaying.ReadCoverAsync();

      Assert.That(reads, Is.EqualTo(0));
      Assert.That(nowPlaying.Cover, Is.Null);
    }
  }
}
