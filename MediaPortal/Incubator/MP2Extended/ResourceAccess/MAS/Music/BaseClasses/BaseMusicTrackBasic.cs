#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Common;
using MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.Music;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music.BaseClasses
{
  class BaseMusicTrackBasic
  {
    internal static ISet<Guid> BasicNecessaryMIATypeIds = new HashSet<Guid>
    {
      MediaAspect.ASPECT_ID,
      ImporterAspect.ASPECT_ID,
      ProviderResourceAspect.ASPECT_ID,
      AudioAspect.ASPECT_ID
    };

    internal static ISet<Guid> BasicOptionalMIATypeIds = new HashSet<Guid>
    {
      GenreAspect.ASPECT_ID,
      RelationshipAspect.ASPECT_ID
    };

    internal static WebMusicTrackBasic MusicTrackBasic(MediaItem item)
    {
      MediaItemAspect audioAspect = item.GetAspect(AudioAspect.Metadata);
      MediaItemAspect mediaAspect = item.GetAspect(MediaAspect.Metadata);
      MediaItemAspect importerAspect = item.GetAspect(ImporterAspect.Metadata);

      WebMusicTrackBasic webTrackBasic = new WebMusicTrackBasic
      {
        Title = audioAspect.GetAttributeValue<string>(AudioAspect.ATTR_TRACKNAME),
        Id = item.MediaItemId.ToString(),
        Type = WebMediaType.MusicTrack,
        Path = ResourceAccessUtils.GetPaths(item),
        Year = mediaAspect.GetAttributeValue<DateTime>(MediaAspect.ATTR_RECORDINGTIME).Year,
        Duration = (int)audioAspect.GetAttributeValue<long>(AudioAspect.ATTR_DURATION),
        DateAdded = importerAspect.GetAttributeValue<DateTime>(ImporterAspect.ATTR_DATEADDED),
        Rating = Convert.ToSingle(audioAspect.GetAttributeValue<double>(AudioAspect.ATTR_TOTAL_RATING)),
        Artwork = ResourceAccessUtils.GetWebArtwork(item),
        Album = audioAspect.GetAttributeValue<string>(AudioAspect.ATTR_ALBUM),
        DiscNumber = audioAspect.GetAttributeValue<int>(AudioAspect.ATTR_DISCID),
        TrackNumber = audioAspect.GetAttributeValue<int>(AudioAspect.ATTR_TRACK),
      };

      IEnumerable<string> aspectArtists = audioAspect.GetCollectionAttribute<string>(AudioAspect.ATTR_ARTISTS);
      if (aspectArtists != null)
        webTrackBasic.Artist = aspectArtists.Distinct().ToList();

      aspectArtists = audioAspect.GetCollectionAttribute<string>(AudioAspect.ATTR_ALBUMARTISTS);
      if (aspectArtists != null)
        webTrackBasic.AlbumArtist = aspectArtists.FirstOrDefault();

      if (MediaItemAspect.TryGetAspects(item.Aspects, RelationshipAspect.Metadata, out var relationAspects))
      {
        webTrackBasic.ArtistId = relationAspects.Where(r => (Guid?)r[RelationshipAspect.ATTR_LINKED_ROLE] == PersonAspect.ROLE_ARTIST).Select(r => r[RelationshipAspect.ATTR_LINKED_ID].ToString()).ToList();
        webTrackBasic.AlbumId = relationAspects.Where(r => (Guid?)r[RelationshipAspect.ATTR_LINKED_ROLE] == AudioAlbumAspect.ROLE_ALBUM).Select(r => r[RelationshipAspect.ATTR_LINKED_ID].ToString()).FirstOrDefault();
        webTrackBasic.AlbumArtistId = relationAspects.Where(r => (Guid?)r[RelationshipAspect.ATTR_LINKED_ROLE] == PersonAspect.ROLE_ALBUMARTIST).Select(r => r[RelationshipAspect.ATTR_LINKED_ID].ToString()).FirstOrDefault();
      }

      IList<MediaItemAspect> genres;
      if (item.Aspects.TryGetValue(GenreAspect.ASPECT_ID, out genres))
        webTrackBasic.Genres = genres.Select(g => g.GetAttributeValue<string>(GenreAspect.ATTR_GENRE)).ToList();

      return webTrackBasic;
    }
  }
}
