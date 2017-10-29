using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using MediaPortal.Utilities;
using MP2Extended.Extensions;
using MP2Extended.ResourceAccess;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow.BaseClasses
{
  class BaseEpisodeBasic
  {
    internal ISet<Guid> BasicNecessaryMIATypeIds = new HashSet<Guid>
    {
      MediaAspect.ASPECT_ID,
      ImporterAspect.ASPECT_ID,
      ProviderResourceAspect.ASPECT_ID,
      EpisodeAspect.ASPECT_ID
    };

    internal ISet<Guid> BasicOptionalMIATypeIds = new HashSet<Guid>
    {
      RelationshipAspect.ASPECT_ID,
      ExternalIdentifierAspect.ASPECT_ID
    };

    internal WebTVEpisodeBasic EpisodeBasic(MediaItem item, Guid? showId = null, Guid? seasonId = null)
    {
      MediaItemAspect episodeAspect = item.GetAspect(EpisodeAspect.Metadata);
      MediaItemAspect importerAspect = item.GetAspect(ImporterAspect.Metadata);
      MediaItemAspect mediaAspect = item.GetAspect(MediaAspect.Metadata);

      ResourcePath resourcePath = ResourcePath.Deserialize(item.PrimaryProviderResourcePath());
      string path = resourcePath.PathSegments.Count > 0 ? StringUtils.RemovePrefixIfPresent(resourcePath.LastPathSegment.Path, "/") : string.Empty;

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
        Path = new List<string> { path },
        Watched = mediaAspect.GetAttributeValue<int>(MediaAspect.ATTR_PLAYCOUNT) > 0,
        DateAdded = importerAspect.GetAttributeValue<DateTime>(ImporterAspect.ATTR_DATEADDED),
        SeasonNumber = episodeAspect.GetAttributeValue<int>(EpisodeAspect.ATTR_SEASON),
        FirstAired = mediaAspect.GetAttributeValue<DateTime>(MediaAspect.ATTR_RECORDINGTIME),
        Rating = Convert.ToSingle(episodeAspect.GetAttributeValue<double>(EpisodeAspect.ATTR_TOTAL_RATING)),
        Artwork = GetFanart.GetArtwork(item.MediaItemId, WebMediaType.TVEpisode)
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

    protected void GetParentIds(MediaItem item, ref Guid? showId, ref Guid? seasonId)
    {
      if (showId.HasValue && seasonId.HasValue)
        return;

      IList<MediaItemAspect> relationships;
      if (!item.Aspects.TryGetValue(RelationshipAspect.ASPECT_ID, out relationships))
        return;

      if (!showId.HasValue)
        showId = relationships.GetLinkedIdOrDefault(EpisodeAspect.ROLE_EPISODE, SeriesAspect.ROLE_SERIES);

      if (!seasonId.HasValue)
        seasonId = relationships.GetLinkedIdOrDefault(EpisodeAspect.ROLE_EPISODE, SeasonAspect.ROLE_SEASON);
    }
  }
}
