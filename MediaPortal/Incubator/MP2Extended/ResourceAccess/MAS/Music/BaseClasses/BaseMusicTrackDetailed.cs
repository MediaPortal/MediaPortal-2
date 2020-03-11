#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MP2Extended.MAS.Music;
using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music.BaseClasses
{
  class BaseMusicTrackDetailed : BaseMusicTrackBasic
  {
    internal static WebMusicTrackDetailed MusicTrackDetailed(MediaItem item)
    {
      WebMusicTrackBasic webTrackBasic = MusicTrackBasic(item);

      WebMusicTrackDetailed webTrackDetailed = new WebMusicTrackDetailed
      {
        Title = webTrackBasic.Title,
        Id = webTrackBasic.Id,
        Type = webTrackBasic.Type,
        Path = webTrackBasic.Path,
        Year = webTrackBasic.Year,
        Duration = webTrackBasic.Duration,
        DateAdded = webTrackBasic.DateAdded,
        Rating = webTrackBasic.Rating,
        Artwork = webTrackBasic.Artwork,
        Album = webTrackBasic.Album,
        DiscNumber = webTrackBasic.DiscNumber,
        TrackNumber = webTrackBasic.TrackNumber,
        AlbumArtist = webTrackBasic.AlbumArtist,
        AlbumArtistId = webTrackBasic.AlbumArtistId,
        AlbumId = webTrackBasic.AlbumId,
        Artist = webTrackBasic.Artist,
        ArtistId = webTrackBasic.ArtistId,
        Genres = webTrackBasic.Genres
      };

      return webTrackDetailed;
    }

    internal static void AssignArtists(IOwinContext context, IEnumerable<WebMusicTrackDetailed> tracks)
    {
      // assign artists
      var searchFilter = new MediaItemIdFilter(tracks.SelectMany(t => t.ArtistId).Concat(tracks.Select(t => t.AlbumArtistId)).Select(s => Guid.Parse(s)).Distinct());
      var artists = MediaLibraryAccess.Search(context, BaseMusicArtistBasic.BasicNecessaryMIATypeIds, BaseMusicArtistBasic.BasicOptionalMIATypeIds, searchFilter);
      foreach (var track in tracks)
      {
        var albumArtist = artists.FirstOrDefault(a => a.MediaItemId.ToString() == track.AlbumArtistId);
        if (albumArtist != null)
          track.AlbumArtistObject = BaseMusicArtistBasic.MusicArtistBasic(albumArtist);
        track.Artists = new List<WebMusicArtistBasic>();
        foreach (var artistsId in track.ArtistId)
        {
          var artist = artists.FirstOrDefault(a => a.MediaItemId.ToString() == artistsId);
          if (artist != null)
            track.Artists.Add(BaseMusicArtistBasic.MusicArtistBasic(artist));
        }
      }
    }
  }
}
