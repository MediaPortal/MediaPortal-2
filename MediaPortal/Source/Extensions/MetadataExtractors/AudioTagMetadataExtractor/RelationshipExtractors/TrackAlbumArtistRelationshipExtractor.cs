#region Copyright (C) 2007-2025 Team MediaPortal

/*
    Copyright (C) 2007-2025 Team MediaPortal
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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;

namespace MediaPortal.Extensions.MetadataExtractors.AudioTagMetadataExtractor
{
  /// <summary>
  /// Links a track to the artists of the album its tag names.
  /// </summary>
  /// <remarks>
  /// The tag extractor already falls back to the track artists when a file carries no album artist
  /// frame, so a library whose files were ripped without that frame still gets an artist for every
  /// album.
  /// </remarks>
  internal class TrackAlbumArtistRelationshipExtractor : TrackPersonsRelationshipExtractorBase
  {
    public TrackAlbumArtistRelationshipExtractor()
      : base(PersonAspect.ROLE_ALBUMARTIST, AudioAspect.ATTR_ALBUMARTISTS, PersonAspect.OCCUPATION_ARTIST, true)
    {
    }

    protected override IList<PersonInfo> GetPersons(IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      TrackInfo trackInfo = new TrackInfo();
      if (!trackInfo.FromMetadata(aspects))
        return null;
      return trackInfo.AlbumArtists;
    }
  }
}
