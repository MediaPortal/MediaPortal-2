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
using MediaPortal.Plugins.MP2Extended.MAS.Music;
using MP2Extended.Extensions;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music.BaseClasses
{
  class BaseMusicAlbumBasic
  {
    internal static ISet<Guid> BasicNecessaryMIATypeIds = new HashSet<Guid>
    {
      MediaAspect.ASPECT_ID,
      ImporterAspect.ASPECT_ID,
      ProviderResourceAspect.ASPECT_ID,
      AudioAlbumAspect.ASPECT_ID
    };

    internal static ISet<Guid> BasicOptionalMIATypeIds = new HashSet<Guid>
    {
      ExternalIdentifierAspect.ASPECT_ID,
      GenreAspect.ASPECT_ID,
      RelationshipAspect.ASPECT_ID
    };

    internal static WebMusicAlbumBasic MusicAlbumBasic(MediaItem item)
    {
      MediaItemAspect importerAspect = item.GetAspect(ImporterAspect.Metadata);
      MediaItemAspect mediaAspect = item.GetAspect(MediaAspect.Metadata);
      MediaItemAspect albumAspect = item.GetAspect(AudioAlbumAspect.Metadata);

      var artists = albumAspect.GetCollectionAttribute<string>(AudioAlbumAspect.ATTR_ARTISTS);

      IList<MultipleMediaItemAspect> genres;
      if (!MediaItemAspect.TryGetAspects(item.Aspects, GenreAspect.Metadata, out genres))
        genres = new List<MultipleMediaItemAspect>();

      var artistIds = new HashSet<string>();
      if (MediaItemAspect.TryGetAspects(item.Aspects, RelationshipAspect.Metadata, out var relationAspects))
        artistIds = new HashSet<string>(relationAspects.Where(r => (Guid?)r[RelationshipAspect.ATTR_LINKED_ROLE] == PersonAspect.ROLE_ALBUMARTIST).Select(r => r[RelationshipAspect.ATTR_LINKED_ID].ToString()));

      return new WebMusicAlbumBasic
      {
        Id = item.MediaItemId.ToString(),
        Artists = artists?.ToList(),
        ArtistsId = artistIds?.ToList(),
        AlbumArtist = artists?.FirstOrDefault()?.ToString(),
        AlbumArtistId = artistIds?.FirstOrDefault()?.ToString(),
        Genres = genres?.Select(a => a.GetAttributeValue<string>(GenreAspect.ATTR_GENRE)).ToList(),
        Title = albumAspect.GetAttributeValue<string>(AudioAlbumAspect.ATTR_ALBUM),
        Year = mediaAspect.GetAttributeValue<DateTime>(MediaAspect.ATTR_RECORDINGTIME).Year,
        DateAdded = importerAspect.GetAttributeValue<DateTime>(ImporterAspect.ATTR_DATEADDED),
        Rating = Convert.ToSingle(albumAspect.GetAttributeValue<double>(AudioAlbumAspect.ATTR_TOTAL_RATING)),
        Artwork = ResourceAccessUtils.GetWebArtwork(item),
        //Composer = composers.Cast<string>().ToList()
      };
    }
  }
}
