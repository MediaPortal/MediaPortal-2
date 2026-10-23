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
  /// Links a track to the artists its tag names in the performer frames.
  /// </summary>
  internal class TrackArtistRelationshipExtractor : TrackPersonsRelationshipExtractorBase
  {
    public TrackArtistRelationshipExtractor()
      : base(PersonAspect.ROLE_ARTIST, AudioAspect.ATTR_ARTISTS, PersonAspect.OCCUPATION_ARTIST, false)
    {
    }

    protected override IList<PersonInfo> GetPersons(IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      TrackInfo trackInfo = new TrackInfo();
      if (!trackInfo.FromMetadata(aspects))
        return null;
      return trackInfo.Artists;
    }
  }
}
