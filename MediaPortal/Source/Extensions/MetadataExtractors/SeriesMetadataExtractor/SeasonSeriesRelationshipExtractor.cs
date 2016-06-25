#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor
{
  class SeasonSeriesRelationshipExtractor : SeriesBaseTryExtractRelationships, IRelationshipRoleExtractor
  {
    private static readonly Guid[] ROLE_ASPECTS = { SeasonAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { SeriesAspect.ASPECT_ID };

    public Guid Role
    {
      get { return SeasonAspect.ROLE_SEASON; }
    }

    public Guid[] RoleAspects
    {
      get { return ROLE_ASPECTS; }
    }

    public Guid LinkedRole
    {
      get { return SeriesAspect.ROLE_SERIES; }
    }

    public Guid[] LinkedRoleAspects
    {
      get { return LINKED_ROLE_ASPECTS; }
    }

    public string ExternalIdType
    {
      get
      {
        return ExternalIdentifierAspect.TYPE_SERIES;
      }
    }

    // TryExtractRelationships is in SeriesBaseTryExtractRelationships

    public bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      return existingAspects.ContainsKey(SeriesAspect.ASPECT_ID);
    }

    public bool TryGetRelationshipIndex(IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> linkedAspects, out int index)
    {
      return MediaItemAspect.TryGetAttribute(aspects, SeasonAspect.ATTR_SEASON, out index);
    }
  }
}
