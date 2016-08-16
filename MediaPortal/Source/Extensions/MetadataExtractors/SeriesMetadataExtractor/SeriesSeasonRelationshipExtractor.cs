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
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor
{
  class SeriesSeasonRelationshipExtractor : IRelationshipRoleExtractor
  {
    private static readonly Guid[] ROLE_ASPECTS = { SeriesAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { SeasonAspect.ASPECT_ID };
    private CheckedItemCache<SeriesInfo> _checkCache = new CheckedItemCache<SeriesInfo>(SeriesMetadataExtractor.MINIMUM_HOUR_AGE_BEFORE_UPDATE);

    public bool BuildRelationship
    {
      get { return true; }
    }

    public Guid Role
    {
      get { return SeriesAspect.ROLE_SERIES; }
    }

    public Guid[] RoleAspects
    {
      get { return ROLE_ASPECTS; }
    }

    public Guid LinkedRole
    {
      get { return SeasonAspect.ROLE_SEASON; }
    }

    public Guid[] LinkedRoleAspects
    {
      get { return LINKED_ROLE_ASPECTS; }
    }

    public bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, out ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedLinkedAspects, bool forceQuickMode)
    {
      extractedLinkedAspects = null;

      SeriesInfo seriesInfo = new SeriesInfo();
      if (!seriesInfo.FromMetadata(aspects))
        return false;

      if (_checkCache.IsItemChecked(seriesInfo))
        return false;

      OnlineMatcherService.UpdateSeries(seriesInfo, false, forceQuickMode);

      if (seriesInfo.Seasons.Count == 0)
        return false;

      if (!seriesInfo.HasChanged)
        return false;

      extractedLinkedAspects = new List<IDictionary<Guid, IList<MediaItemAspect>>>();

      foreach (SeasonInfo season in seriesInfo.Seasons)
      {
        IDictionary<Guid, IList<MediaItemAspect>> seasonAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
        season.SetMetadata(seasonAspects);

        if (seasonAspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
          extractedLinkedAspects.Add(seasonAspects);
      }
      return extractedLinkedAspects.Count > 0;
    }

    public bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      if (!existingAspects.ContainsKey(SeasonAspect.ASPECT_ID))
        return false;

      SeasonInfo linkedSeason = new SeasonInfo();
      if (!linkedSeason.FromMetadata(extractedAspects))
        return false;

      SeasonInfo existingSeason = new SeasonInfo();
      if (!existingSeason.FromMetadata(existingAspects))
        return false;

      return linkedSeason.Equals(existingSeason);
    }

    public bool TryGetRelationshipIndex(IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> linkedAspects, out int index)
    {
      return MediaItemAspect.TryGetAttribute(linkedAspects, SeasonAspect.ATTR_SEASON, out index);
    }
  }
}
