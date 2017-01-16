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

using System;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Common.MediaManagement.MLQueries;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor
{
  class SeasonSeriesRelationshipExtractor : ISeriesRelationshipExtractor, IRelationshipRoleExtractor
  {
    private static readonly Guid[] ROLE_ASPECTS = { SeasonAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { SeriesAspect.ASPECT_ID };

    public bool BuildRelationship
    {
      get { return true; }
    }

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

    public Guid[] MatchAspects
    {
      get { return SeriesInfo.EQUALITY_ASPECTS; }
    }

    public IFilter GetSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      return GetSeriesSearchFilter(extractedAspects);
    }

    public bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, out IDictionary<IDictionary<Guid, IList<MediaItemAspect>>, Guid> extractedLinkedAspects, bool importOnly)
    {
      extractedLinkedAspects = null;

      SeasonInfo seasonInfo = new SeasonInfo();
      if (!seasonInfo.FromMetadata(aspects))
        return false;

      if (CheckCacheContains(seasonInfo))
        return false;
      
      SeriesInfo cachedSeries;
      Guid seriesId;
      SeriesInfo seriesInfo = seasonInfo.CloneBasicInstance<SeriesInfo>();
      if (TryGetInfoFromCache(seriesInfo, out cachedSeries, out seriesId))
        seriesInfo = cachedSeries;
      else if (!SeriesMetadataExtractor.SkipOnlineSearches)
        OnlineMatcherService.Instance.UpdateSeries(seriesInfo, false, importOnly);

      if (!BaseInfo.HasRelationship(aspects, LinkedRole))
        seriesInfo.HasChanged = true; //Force save if no relationship exists

      if (!seriesInfo.HasChanged && !importOnly)
        return false;

      AddToCheckCache(seasonInfo);

      extractedLinkedAspects = new Dictionary<IDictionary<Guid, IList<MediaItemAspect>>, Guid>();
      IDictionary<Guid, IList<MediaItemAspect>> seriesAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      seriesInfo.SetMetadata(seriesAspects);

      if (aspects.ContainsKey(EpisodeAspect.ASPECT_ID))
      {
        bool episodeVirtual = true;
        if (MediaItemAspect.TryGetAttribute(aspects, MediaAspect.ATTR_ISVIRTUAL, false, out episodeVirtual))
        {
          MediaItemAspect.SetAttribute(seriesAspects, MediaAspect.ATTR_ISVIRTUAL, episodeVirtual);
        }
      }

      if (!seriesAspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
        return false;

      if (seriesId != Guid.Empty)
        extractedLinkedAspects.Add(seriesAspects, seriesId);
      else
        extractedLinkedAspects.Add(seriesAspects, Guid.Empty);
      return true;
    }

    public bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      return existingAspects.ContainsKey(SeriesAspect.ASPECT_ID);
    }

    public bool TryGetRelationshipIndex(IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> linkedAspects, out int index)
    {
      return MediaItemAspect.TryGetAttribute(aspects, SeasonAspect.ATTR_SEASON, out index);
    }

    public void CacheExtractedItem(Guid extractedItemId, IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      SeriesInfo series = new SeriesInfo();
      series.FromMetadata(extractedAspects);
      AddToCache(extractedItemId, series, false);
    }
  }
}
