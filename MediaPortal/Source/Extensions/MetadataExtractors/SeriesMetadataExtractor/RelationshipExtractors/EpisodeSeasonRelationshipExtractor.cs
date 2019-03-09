#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor
{
  class EpisodeSeasonRelationshipExtractor : IRelationshipRoleExtractor
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
      SingleMediaItemAspect seasonAspect;
      if (!MediaItemAspect.TryGetAspect(extractedAspects, SeasonAspect.Metadata, out seasonAspect))
        return null;

      IFilter seasonFilter = RelationshipExtractorUtils.CreateExternalItemFilter(extractedAspects, ExternalIdentifierAspect.TYPE_SEASON);
      IFilter seriesFilter = RelationshipExtractorUtils.CreateExternalItemFilter(extractedAspects, ExternalIdentifierAspect.TYPE_SERIES);
      if (seriesFilter == null)
        return seasonFilter;

      int? seasonNumber = seasonAspect.GetAttributeValue<int?>(SeasonAspect.ATTR_SEASON);
      IFilter seasonNumberFilter = seasonNumber.HasValue ?
        new RelationalFilter(SeasonAspect.ATTR_SEASON, RelationalOperator.EQ, seasonNumber.Value) : null;

      seriesFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, seriesFilter, seasonNumberFilter);

      return BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, seasonFilter, seriesFilter);
    }

    public ICollection<string> GetExternalIdentifiers(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      SingleMediaItemAspect seasonAspect;
      if (!MediaItemAspect.TryGetAspect(extractedAspects, SeasonAspect.Metadata, out seasonAspect))
        return new List<string>();

      ICollection<string> seasonIdentifiers = RelationshipExtractorUtils.CreateExternalItemIdentifiers(extractedAspects, ExternalIdentifierAspect.TYPE_SEASON);
      ICollection<string> seriesIdentifiers = RelationshipExtractorUtils.CreateExternalItemIdentifiers(extractedAspects, ExternalIdentifierAspect.TYPE_SERIES);
      if (seriesIdentifiers == null || seriesIdentifiers.Count == 0)
        return seasonIdentifiers;

      int? seasonNumber = seasonAspect.GetAttributeValue<int?>(SeasonAspect.ATTR_SEASON);
      string seasonSuffix = seasonNumber.HasValue ? "S" + seasonNumber.Value : string.Empty;

      foreach (string seriesIdentifier in seriesIdentifiers)
        seasonIdentifiers.Add(seriesIdentifier + seasonSuffix);

      return seasonIdentifiers;
    }

    public async Task<bool> TryExtractRelationshipsAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> aspects, IList<IDictionary<Guid, IList<MediaItemAspect>>> extractedLinkedAspects)
    {
      EpisodeInfo episodeInfo = new EpisodeInfo();
      if (!episodeInfo.FromMetadata(aspects))
        return false;

      SeasonInfo seasonInfo = episodeInfo.CloneBasicInstance<SeasonInfo>();

      if (!SeriesMetadataExtractor.SkipOnlineSearches)
        await OnlineMatcherService.Instance.UpdateSeasonAsync(seasonInfo).ConfigureAwait(false);

      if (seasonInfo.SeriesName.IsEmpty)
        return false;
      
      IDictionary<Guid, IList<MediaItemAspect>> seasonAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      seasonInfo.SetMetadata(seasonAspects);

      bool episodeVirtual = true;
      if (MediaItemAspect.TryGetAttribute(aspects, MediaAspect.ATTR_ISVIRTUAL, false, out episodeVirtual))
        MediaItemAspect.SetAttribute(seasonAspects, MediaAspect.ATTR_ISVIRTUAL, episodeVirtual);

      if (!seasonAspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
        return false;

      extractedLinkedAspects.Add(seasonAspects);
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
      List<int> episodeNums = new SafeList<int>(indexes);
      if (episodeNums.Count == 0)
        return false;

      index = episodeNums.First();
      return index > 0;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
