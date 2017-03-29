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
  class SeriesEpisodeRelationshipExtractor : ISeriesRelationshipExtractor, IRelationshipRoleExtractor
  {
    private static readonly Guid[] ROLE_ASPECTS = { SeriesAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { EpisodeAspect.ASPECT_ID };

    public bool BuildRelationship
    {
      //We don't want to build series -> episode relation because there already is a episode -> series relation
      get { return false; }
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
      get { return EpisodeAspect.ROLE_EPISODE; }
    }

    public Guid[] LinkedRoleAspects
    {
      get { return LINKED_ROLE_ASPECTS; }
    }

    public Guid[] MatchAspects
    {
      get { return EpisodeInfo.EQUALITY_ASPECTS; }
    }

    public IFilter GetSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      return GetEpisodeSearchFilter(extractedAspects);
    }

    public bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, bool importOnly, out IList<RelationshipItem> extractedLinkedAspects)
    {
      extractedLinkedAspects = null;

      if (importOnly)
        return false;

      if (SeriesMetadataExtractor.OnlyLocalMedia)
        return false;

      SeriesInfo seriesInfo = new SeriesInfo();
      if (!seriesInfo.FromMetadata(aspects))
        return false;

      if (CheckCacheContains(seriesInfo))
        return false;

      if (!SeriesMetadataExtractor.SkipOnlineSearches)
        OnlineMatcherService.Instance.UpdateSeries(seriesInfo, true, false);

      if (seriesInfo.Episodes.Count == 0)
        return false;

      if (BaseInfo.CountRelationships(aspects, LinkedRole) < seriesInfo.Episodes.Count)
        seriesInfo.HasChanged = true; //Force save for new episodes
      else
        return false;

      if (!seriesInfo.HasChanged)
        return false;

      AddToCheckCache(seriesInfo);

      extractedLinkedAspects = new List<RelationshipItem>();
      for (int i = 0; i < seriesInfo.Episodes.Count; i++)
      {
        EpisodeInfo episodeInfo = seriesInfo.Episodes[i];
        episodeInfo.SeriesNameId = seriesInfo.NameId;

        IDictionary<Guid, IList<MediaItemAspect>> episodeAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
        episodeInfo.SetMetadata(episodeAspects);
        MediaItemAspect.SetAttribute(episodeAspects, MediaAspect.ATTR_ISVIRTUAL, true);

        if (episodeAspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
          extractedLinkedAspects.Add(new RelationshipItem(episodeAspects, Guid.Empty));
      }
      return extractedLinkedAspects.Count > 0;
    }

    public bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      if (!existingAspects.ContainsKey(EpisodeAspect.ASPECT_ID))
        return false;

      EpisodeInfo linkedEpisode = new EpisodeInfo();
      if (!linkedEpisode.FromMetadata(extractedAspects))
        return false;

      EpisodeInfo existingEpisode = new EpisodeInfo();
      if (!existingEpisode.FromMetadata(existingAspects))
        return false;

      return linkedEpisode.Equals(existingEpisode);
    }

    public bool TryGetRelationshipIndex(IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> linkedAspects, out int index)
    {
      index = -1;

      SingleMediaItemAspect linkedAspect;
      if (!MediaItemAspect.TryGetAspect(linkedAspects, EpisodeAspect.Metadata, out linkedAspect))
        return false;

      int? season = linkedAspect.GetAttributeValue<int?>(EpisodeAspect.ATTR_SEASON);
      if (!season.HasValue)
        return false;

      IEnumerable<int> episodes = linkedAspect.GetCollectionAttribute<int>(EpisodeAspect.ATTR_EPISODE);
      List<int> episodeList = new List<int>(episodes);

      index = season.Value * 1000 + episodeList.First();
      return index >= 0;
    }

    public void CacheExtractedItem(Guid extractedItemId, IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
