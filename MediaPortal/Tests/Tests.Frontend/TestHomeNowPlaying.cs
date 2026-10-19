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

using System.Linq;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// Which home tile wears the "this shelf is playing" mark, and which wears nothing.
  /// </summary>
  /// <remarks>
  /// Both rescued skins carried a picture named exactly <c>IsPlaying</c> for this sentence, and the reference was
  /// strict about where it hung: what plays in a series was never said on the films' tile. These tests are that
  /// strictness written down - one mark, on the shelf the viewer walked in from, and off again in silence.
  /// </remarks>
  [TestFixture]
  public class TestHomeNowPlaying
  {
    private static HomeViewModel Home()
    {
      // The menu before any server has answered is the menu the marks go on: an entry exists whether or not this
      // build can fill it, and a film playing proves nothing about whether the server counts its shelf.
      return new HomeViewModel();
    }

    private static MainMenuEntry Tile(HomeViewModel home, string mp2Action)
    {
      return home.Menu.Single(entry => entry.Mp2Action == mp2Action);
    }

    [Test]
    public void AFilmMarksItsOwnTileAndNobodyElses()
    {
      HomeViewModel home = Home();

      home.ShowPlaying("MovieItem");

      Assert.That(Tile(home, "Home->Movies").IsPlaying, Is.True);
      Assert.That(Tile(home, "Home->Series").IsPlaying, Is.False, "a film is not a series");
      Assert.That(Tile(home, "Home->Audio").IsPlaying, Is.False, "a film is not music");
    }

    [Test]
    public void AnEpisodeMarksTheSeriesTileNotTheFilms()
    {
      HomeViewModel home = Home();

      // Pressing an episode is using the series' shelf - the reference filed episodes inside series, and a mark
      // on the films' tile for an episode would be the same lie the old "continue playing" strip told.
      home.ShowPlaying("EpisodeItem");

      Assert.That(Tile(home, "Home->Series").IsPlaying, Is.True);
      Assert.That(Tile(home, "Home->Movies").IsPlaying, Is.False);
    }

    [Test]
    public void ATrackMarksTheAudioTile()
    {
      HomeViewModel home = Home();

      home.ShowPlaying("AudioItem");

      // The audio tile is filled by albums when the importer knew about them and by tracks when it did not;
      // either way it is the one shelf the party and the album read from.
      Assert.That(Tile(home, "Home->Audio").IsPlaying, Is.True);
      Assert.That(Tile(home, "Home->Videos").IsPlaying, Is.False);
    }

    [Test]
    public void SilenceTakesEveryMarkOffAgain()
    {
      HomeViewModel home = Home();
      home.ShowPlaying("MovieItem");

      home.ShowPlaying(null);

      Assert.That(home.Menu.Any(entry => entry.IsPlaying), Is.False,
          "a stopped player leaves no tile claiming a film");
    }

    [Test]
    public void AMenuRebuiltUnderARunningFilmKeepsTheMark()
    {
      HomeViewModel home = Home();
      home.ShowPlaying("MovieItem");

      // Pointing the screen at a server rebuilds the whole menu from the new reading; the film has not stopped,
      // so the tile's mark may not quietly fall off with the old entries.
      home.UseServer(new ServerApiClient(new System.Uri("http://127.0.0.1:1")));

      Assert.That(Tile(home, "Home->Movies").IsPlaying, Is.True);
    }
  }
}
