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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Common.MediaManagement.MLQueries;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor
{
  class EpisodeSeasonRelationshipExtractor : ISeriesRelationshipExtractor, IRelationshipRoleExtractor
  {
    private static readonly Guid[] ROLE_ASPECTS = { EpisodeAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { SeasonAspect.ASPECT_ID };

    public bool BuildRelationship
    {
      get { return true; }
    }

    public Guid Role
    {
      get { return EpisodeAspect.ROLE_EPISODE; }
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

    public Guid[] MatchAspects
    {
      get { return SeasonInfo.EQUALITY_ASPECTS; }
    }

    public IFilter GetSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      return GetSeasonSearchFilter(extractedAspects);
    }

    public bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, out IDictionary<IDictionary<Guid, IList<MediaItemAspect>>, Guid> extractedLinkedAspects, bool importOnly)
    {
      extractedLinkedAspects = null;

      EpisodeInfo episodeInfo = new EpisodeInfo();
      if (!episodeInfo.FromMetadata(aspects))
        return false;

      if (CheckCacheContains(episodeInfo))
        return false;

      SeasonInfo cachedSeason;
      Guid seasonId;
      SeasonInfo seasonInfo = episodeInfo.CloneBasicInstance<SeasonInfo>();
      if (TryGetInfoFromCache(seasonInfo, out cachedSeason, out seasonId))
        seasonInfo = cachedSeason;
      else if (!SeriesMetadataExtractor.SkipOnlineSearches)
        OnlineMatcherService.Instance.UpdateSeason(seasonInfo, importOnly);

      if (seasonInfo.SeriesName.IsEmpty)
        return false;

      if (!BaseInfo.HasRelationship(aspects, LinkedRole))
        seasonInfo.HasChanged = true; //Force save if no relationship exists

      if (!seasonInfo.HasChanged && !importOnly)
        return false;

      AddToCheckCache(episodeInfo);

      extractedLinkedAspects = new Dictionary<IDictionary<Guid, IList<MediaItemAspect>>, Guid>();
      IDictionary<Guid, IList<MediaItemAspect>> seasonAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      seasonInfo.SetMetadata(seasonAspects);

      bool episodeVirtual = true;
      if (MediaItemAspect.TryGetAttribute(aspects, MediaAspect.ATTR_ISVIRTUAL, false, out episodeVirtual))
      {
        MediaItemAspect.SetAttribute(seasonAspects, MediaAspect.ATTR_ISVIRTUAL, episodeVirtual);
      }

      if (!seasonAspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
        return false;

      if (seasonId != Guid.Empty)
        extractedLinkedAspects.Add(seasonAspects, seasonId);
      else
        extractedLinkedAspects.Add(seasonAspects, Guid.Empty);
      return true;
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

      return linkedSeason.SeasonNumber == existingSeason.SeasonNumber;
    }

    public bool TryGetRelationshipIndex(IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> linkedAspects, out int index)
    {
      index = -1;

      SingleMediaItemAspect aspect;
      if (!MediaItemAspect.TryGetAspect(aspects, EpisodeAspect.Metadata, out aspect))
        return false;

      IEnumerable<int> indexes = aspect.GetCollectionAttribute<int>(EpisodeAspect.ATTR_EPISODE);
      if (indexes == null)
        return false;

      IList<int> episodeNums = indexes.ToList();
      if (episodeNums.Count == 0)
        return false;

      index = episodeNums.First();
      return index > 0;
    }

    public void CacheExtractedItem(Guid extractedItemId, IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      SeasonInfo season = new SeasonInfo();
      season.FromMetadata(extractedAspects);
      AddToCache(extractedItemId, season, false);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
