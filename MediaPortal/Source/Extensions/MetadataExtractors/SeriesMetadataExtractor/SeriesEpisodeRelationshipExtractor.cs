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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Extensions.OnlineLibraries.Matchers;
using MediaPortal.Common.MediaManagement.Helpers;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor
{
  class SeriesEpisodeRelationshipExtractor : IRelationshipRoleExtractor
  {
    private static readonly Guid[] ROLE_ASPECTS = { SeriesAspect.ASPECT_ID };
    private static readonly Guid[] LINKED_ROLE_ASPECTS = { EpisodeAspect.ASPECT_ID };

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

    public string ExternalIdType
    {
      get
      {
        return ExternalIdentifierAspect.TYPE_EPISODE;
      }
    }

    public bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, out ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedLinkedAspects, bool forceQuickMode)
    {
      extractedLinkedAspects = null;

      SeriesInfo seriesInfo = new SeriesInfo();
      if (!seriesInfo.FromMetadata(aspects))
        return false;

      SeriesTheMovieDbMatcher.Instance.UpdateSeries(seriesInfo, forceQuickMode);
      SeriesTvMazeMatcher.Instance.UpdateSeries(seriesInfo, forceQuickMode);
      SeriesTvDbMatcher.Instance.UpdateSeries(seriesInfo, forceQuickMode);
      SeriesOmDbMatcher.Instance.UpdateSeries(seriesInfo, forceQuickMode);
      SeriesFanArtTvMatcher.Instance.UpdateSeries(seriesInfo, forceQuickMode);

      if (seriesInfo.Episodes.Count == 0)
        return false;

      extractedLinkedAspects = new List<IDictionary<Guid, IList<MediaItemAspect>>>();

      foreach (EpisodeInfo episode in seriesInfo.Episodes)
      {
        IDictionary<Guid, IList<MediaItemAspect>> episodeAspects = new Dictionary<Guid, IList<MediaItemAspect>>();
        episode.SetMetadata(episodeAspects);

        if (episodeAspects.ContainsKey(ExternalIdentifierAspect.ASPECT_ID))
          extractedLinkedAspects.Add(episodeAspects);
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

      IEnumerable<object> episodes = linkedAspect.GetCollectionAttribute<object>(EpisodeAspect.ATTR_EPISODE);
      List<int> episodeList = new List<int>(episodes.Cast<int>());

      index = season.Value * 100 + episodeList.First();
      return index >= 0;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
