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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor
{
  class SeriesEpisodeRelationshipExtractor : IRelationshipRoleExtractor
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
      return SeriesRelationshipExtractor.GetEpisodeSearchFilter(extractedAspects);
    }

    public ICollection<string> GetExternalIdentifiers(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      SingleMediaItemAspect episodeAspect;
      if (!MediaItemAspect.TryGetAspect(extractedAspects, EpisodeAspect.Metadata, out episodeAspect))
        return new List<string>();

      ICollection<string> episodeIdentifiers = RelationshipExtractorUtils.CreateExternalItemIdentifiers(extractedAspects, ExternalIdentifierAspect.TYPE_EPISODE);
      ICollection<string> seriesIdentifiers = RelationshipExtractorUtils.CreateExternalItemIdentifiers(extractedAspects, ExternalIdentifierAspect.TYPE_SERIES);
      if (seriesIdentifiers == null || seriesIdentifiers.Count == 0)
        return episodeIdentifiers;

      int? seasonNumber = episodeAspect.GetAttributeValue<int?>(EpisodeAspect.ATTR_SEASON);
      string seasonEpisodeSuffix = seasonNumber.HasValue ? "S" + seasonNumber.Value : string.Empty;

      IEnumerable<int> episodeNumbers = episodeAspect.GetCollectionAttribute<int>(EpisodeAspect.ATTR_EPISODE);
      if (episodeNumbers != null && episodeNumbers.Any())
        seasonEpisodeSuffix += "E" + episodeNumbers.First();

      foreach (string seriesIdentifier in seriesIdentifiers)
        episodeIdentifiers.Add(seriesIdentifier + seasonEpisodeSuffix);

      return episodeIdentifiers;
    }

    public Task<bool> TryExtractRelationshipsAsync(IDictionary<Guid, IList<MediaItemAspect>> aspects, IList<IDictionary<Guid, IList<MediaItemAspect>>> extractedLinkedAspects)
    {
      if (SeriesMetadataExtractor.OnlyLocalMedia)
        return Task.FromResult(false);

      SeriesInfo seriesInfo = new SeriesInfo();
      if (!seriesInfo.FromMetadata(aspects))
        return Task.FromResult(false);

      if (!SeriesMetadataExtractor.SkipOnlineSearches)
        OnlineMatcherService.Instance.UpdateSeries(seriesInfo, true, false);

      if (seriesInfo.Episodes.Count == 0)
        return Task.FromResult(false);

      if (BaseInfo.CountRelationships(aspects, LinkedRole) < seriesInfo.Episodes.Count)
        seriesInfo.HasChanged = true; //Force save for new episodes
      else
        return Task.FromResult(false);

      if (!seriesInfo.HasChanged)
        return Task.FromResult(false);
      
      for (int i = 0; i < seriesInfo.Episodes.Count; i++)
      {
        EpisodeInfo episodeInfo = seriesInfo.Episodes[i];
        episodeInfo.SeriesNameId = seriesInfo.NameId;

        IDictionary<Guid, IList<MediaItemAspect>> episodeAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
        episodeInfo.SetMetadata(episodeAspects);
        MediaItemAspect.SetAttribute(episodeAspects, MediaAspect.ATTR_ISVIRTUAL, true);

        if (episodeAspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
          extractedLinkedAspects.Add(episodeAspects);
      }
      return Task.FromResult(extractedLinkedAspects.Count > 0);
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
      List<int> episodeList = new SafeList<int>(episodes);

      index = season.Value * 1000 + episodeList.First();
      return index >= 0;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
