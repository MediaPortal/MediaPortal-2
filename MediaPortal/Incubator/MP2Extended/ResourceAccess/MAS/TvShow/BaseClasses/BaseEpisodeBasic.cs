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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using MediaPortal.Utilities;
using MP2Extended.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow.BaseClasses
{
  class BaseEpisodeBasic
  {
    internal static ISet<Guid> BasicNecessaryMIATypeIds = new HashSet<Guid>
    {
      MediaAspect.ASPECT_ID,
      ImporterAspect.ASPECT_ID,
      ProviderResourceAspect.ASPECT_ID,
      EpisodeAspect.ASPECT_ID
    };

    internal static ISet<Guid> BasicOptionalMIATypeIds = new HashSet<Guid>
    {
      RelationshipAspect.ASPECT_ID,
      ExternalIdentifierAspect.ASPECT_ID
    };

    internal static WebTVEpisodeBasic EpisodeBasic(MediaItem item, Guid? showId = null, Guid? seasonId = null)
    {
      MediaItemAspect episodeAspect = item.GetAspect(EpisodeAspect.Metadata);
      MediaItemAspect importerAspect = item.GetAspect(ImporterAspect.Metadata);
      MediaItemAspect mediaAspect = item.GetAspect(MediaAspect.Metadata);

      IEnumerable<int> episodeNumbers = episodeAspect.GetCollectionAttribute<int>(EpisodeAspect.ATTR_EPISODE);
      int episodeNumber = episodeNumbers != null ? episodeNumbers.FirstOrDefault() : 0;

      GetParentIds(item, ref showId, ref seasonId);

      WebTVEpisodeBasic webTvEpisodeBasic = new WebTVEpisodeBasic
      {
        Title = episodeAspect.GetAttributeValue<string>(EpisodeAspect.ATTR_EPISODE_NAME),
        EpisodeNumber = episodeNumber,
        Id = item.MediaItemId.ToString(),
        ShowId = showId.HasValue ? showId.Value.ToString() : null,
        SeasonId = seasonId.HasValue ? seasonId.Value.ToString() : null,
        Type = WebMediaType.TVEpisode,
        Path = ResourceAccessUtils.GetPaths(item),
        Watched = Convert.ToInt32(item.UserData.FirstOrDefault(d => d.Key == UserDataKeysKnown.KEY_PLAY_PERCENTAGE).Value ?? "0") >= 100,
        DateAdded = importerAspect.GetAttributeValue<DateTime>(ImporterAspect.ATTR_DATEADDED),
        SeasonNumber = episodeAspect.GetAttributeValue<int>(EpisodeAspect.ATTR_SEASON),
        FirstAired = mediaAspect.GetAttributeValue<DateTime>(MediaAspect.ATTR_RECORDINGTIME),
        Rating = Convert.ToSingle(episodeAspect.GetAttributeValue<double>(EpisodeAspect.ATTR_TOTAL_RATING)),
        Artwork = ResourceAccessUtils.GetWebArtwork(item),
      };
      
      string TvDbId;
      MediaItemAspect.TryGetExternalAttribute(item.Aspects, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, out TvDbId);
      if (TvDbId != null)
        webTvEpisodeBasic.ExternalId.Add(new WebExternalId { Site = "TVDB", Id = TvDbId });
      string ImdbId;
      MediaItemAspect.TryGetExternalAttribute(item.Aspects, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, out ImdbId);
      if (ImdbId != null)
        webTvEpisodeBasic.ExternalId.Add(new WebExternalId { Site = "IMDB", Id = ImdbId });
      
      return webTvEpisodeBasic;
    }

    protected static void GetParentIds(MediaItem item, ref Guid? showId, ref Guid? seasonId)
    {
      if (showId.HasValue && seasonId.HasValue)
        return;

      IList<MediaItemAspect> relationships;
      if (!item.Aspects.TryGetValue(RelationshipAspect.ASPECT_ID, out relationships))
        return;

      if (!showId.HasValue)
        showId = relationships.Where(ra =>
          ra.GetAttributeValue<Guid>(RelationshipAspect.ATTR_ROLE) == EpisodeAspect.ROLE_EPISODE &&
          ra.GetAttributeValue<Guid>(RelationshipAspect.ATTR_LINKED_ROLE) == SeriesAspect.ROLE_SERIES)
          .Select(ra => ra.GetAttributeValue<Guid?>(RelationshipAspect.ATTR_LINKED_ID))
          .FirstOrDefault();

      if (!seasonId.HasValue)
        seasonId = relationships.Where(ra =>
          ra.GetAttributeValue<Guid>(RelationshipAspect.ATTR_ROLE) == EpisodeAspect.ROLE_EPISODE &&
          ra.GetAttributeValue<Guid>(RelationshipAspect.ATTR_LINKED_ROLE) == SeasonAspect.ROLE_SEASON)
          .Select(ra => ra.GetAttributeValue<Guid?>(RelationshipAspect.ATTR_LINKED_ID))
          .FirstOrDefault();
    }
  }
}
