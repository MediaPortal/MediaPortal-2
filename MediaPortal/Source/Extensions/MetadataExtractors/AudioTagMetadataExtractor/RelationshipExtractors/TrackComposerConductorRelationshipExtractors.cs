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
  /// Links a track to the composers its tag names.
  /// </summary>
  internal class TrackComposerRelationshipExtractor : TrackPersonsRelationshipExtractorBase
  {
    public TrackComposerRelationshipExtractor()
      : base(PersonAspect.ROLE_COMPOSER, AudioAspect.ATTR_COMPOSERS, PersonAspect.OCCUPATION_COMPOSER, false)
    {
    }

    protected override IList<PersonInfo> GetPersons(IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      TrackInfo trackInfo = new TrackInfo();
      if (!trackInfo.FromMetadata(aspects))
        return null;
      return trackInfo.Composers;
    }
  }

  /// <summary>
  /// Links a track to the conductor its tag names.
  /// </summary>
  internal class TrackConductorRelationshipExtractor : TrackPersonsRelationshipExtractorBase
  {
    public TrackConductorRelationshipExtractor()
      : base(PersonAspect.ROLE_CONDUCTOR, AudioAspect.ATTR_CONDUCTERS, PersonAspect.OCCUPATION_CONDUCTOR, false)
    {
    }

    protected override IList<PersonInfo> GetPersons(IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      TrackInfo trackInfo = new TrackInfo();
      if (!trackInfo.FromMetadata(aspects))
        return null;
      return trackInfo.Conductors;
    }
  }
}
