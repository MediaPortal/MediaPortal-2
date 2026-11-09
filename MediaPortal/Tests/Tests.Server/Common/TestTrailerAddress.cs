#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
    http://www.team-mediaportal.com

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

#endregion

using MediaPortal.Common.MediaManagement;
using NUnit.Framework;

namespace Tests.Server.Common
{
  /// <summary>
  /// Checks which trailer addresses are called dead, that is standing at a site that answers no more.
  /// </summary>
  /// <remarks>
  /// The answers here decide what an import stores and what goes out of a library, so both halves matter: a
  /// dead link that is kept is a trailer that cannot be played, and a living link that is thrown away is a
  /// trailer the title really has.
  /// </remarks>
  [TestFixture]
  public class TestTrailerAddress
  {
    [Test]
    public void TestIsDeadSiteRecognizesAppleTrailerAddresses()
    {
      // The shape the tagging tools wrote for years, and the older one before it.
      Assert.IsTrue(TrailerAddress.IsDeadSite(
          "https://movietrailers.apple.com/movies/paramount/top-gun-maverick/top-gun-maverick-trailer-1_h720p.mov"));
      Assert.IsTrue(TrailerAddress.IsDeadSite(
          "http://trailers.apple.com/trailers/fox/avatar/trailer-hd_480p.mov"));
      // Without a path at all, and in capitals, since a tagging tool writes what its own source gave it.
      Assert.IsTrue(TrailerAddress.IsDeadSite("https://apple.com"));
      Assert.IsTrue(TrailerAddress.IsDeadSite(
          "HTTPS://MOVietrailers.Apple.com/movies/x/movie-hd.mov"));
    }

    [Test]
    public void TestIsDeadSiteRecognizesAnAppleAddressHiddenBehindAPlugin()
    {
      // A trailer bought from a trailer site carries the real address as the last part of the plugin's own
      // path and percent encoded, and the site's own name stands in that path as a whole part.
      Assert.IsTrue(TrailerAddress.IsDeadSite(
          "plugin://plugin.video.hdtrailers_net/video/apple.com/"
          + "https%3A%2F%2Fmovietrailers.apple.com%2Fmovies%2Ffox%2Favatar%2Favatar-trailer-de_h720p.mov"));
      Assert.IsTrue(TrailerAddress.IsDeadSite(
          "plugin://plugin.video.hdtrailers_net/video/apple.com/avatar/trailer.mov"));
    }

    [Test]
    public void TestIsDeadSiteLeavesALivingTrailerAlone()
    {
      // The video site links a library is full of, in both shapes the tagging tools write.
      Assert.IsFalse(TrailerAddress.IsDeadSite("plugin://plugin.video.youtube/play/?video_id=VWqJifMMgZE"));
      Assert.IsFalse(TrailerAddress.IsDeadSite("plugin://plugin.video.youtube/?action=play_video&videoid=VWqJifMMgZE"));
      Assert.IsFalse(TrailerAddress.IsDeadSite("https://www.youtube.com/watch?v=VWqJifMMgZE&hd=1"));
      // A trailer standing in the film's own folder, which is what the import finds next to the film.
      Assert.IsFalse(TrailerAddress.IsDeadSite("file://192.168.0.10/filme/Alien/trailer.mp4"));
      Assert.IsFalse(TrailerAddress.IsDeadSite("file://localhost/D:/Filme/Alien/Alien-trailer.mov"));
      // Nothing at all.
      Assert.IsFalse(TrailerAddress.IsDeadSite(null));
      Assert.IsFalse(TrailerAddress.IsDeadSite(""));
      Assert.IsFalse(TrailerAddress.IsDeadSite("   "));
    }

    [Test]
    public void TestIsDeadSiteDoesNotTakeAHostThatOnlyLooksLikeOne()
    {
      // The character in front of the host has to be a separator, so a longer name ending in it is not meant.
      Assert.IsFalse(TrailerAddress.IsDeadSite("https://pineapple.com/trailers/movie-1080p.mov"));
      Assert.IsFalse(TrailerAddress.IsDeadSite("plugin://plugin.video.pineapple_net/video/pineapple.com/movie.mov"));
      // A file named after the site is not a trailer standing at it, either.
      Assert.IsFalse(TrailerAddress.IsDeadSite("file://server/share/apple.com.mkv"));
    }

    [Test]
    public void TestDeadSiteHostsAreWhatTheAddressIsJudgedBy()
    {
      // The library asks the database about the hosts in one stroke, so the list it uses has to be the list the
      // address is judged by. A host named here has to be caught by the address test, too.
      foreach (string host in TrailerAddress.DeadSiteHosts)
        Assert.IsTrue(TrailerAddress.IsDeadSite("https://trailers." + host + "/x/movie.mov"), host);
    }
  }
}
