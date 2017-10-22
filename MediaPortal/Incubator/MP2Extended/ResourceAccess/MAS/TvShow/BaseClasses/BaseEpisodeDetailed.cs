using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using System;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow.BaseClasses
{
  class BaseEpisodeDetailed : BaseEpisodeBasic
  {
    internal ISet<Guid> DetailedNecessaryMIATypeIds = new HashSet<Guid>
    {
      MediaAspect.ASPECT_ID,
      ImporterAspect.ASPECT_ID,
      ProviderResourceAspect.ASPECT_ID,
      EpisodeAspect.ASPECT_ID,
      VideoAspect.ASPECT_ID
    };

    internal ISet<Guid> DetailedOptionalMIATypeIds = new HashSet<Guid>
    {
      RelationshipAspect.ASPECT_ID,
      ExternalIdentifierAspect.ASPECT_ID
    };

    internal WebTVEpisodeDetailed EpisodeDetailed(MediaItem item, Guid? showId = null, Guid? seasonId = null)
    {
      WebTVEpisodeBasic episodeBasic = EpisodeBasic(item, showId, seasonId);
      MediaItemAspect episodeAspect = MediaItemAspect.GetAspect(item.Aspects, EpisodeAspect.Metadata);
      MediaItemAspect videoAspect = MediaItemAspect.GetAspect(item.Aspects, VideoAspect.Metadata);

      var writers = videoAspect.GetCollectionAttribute<string>(VideoAspect.ATTR_WRITERS)?.Distinct().ToList() ?? new List<string>();
      var directors = videoAspect.GetCollectionAttribute<string>(VideoAspect.ATTR_DIRECTORS)?.Distinct().ToList() ?? new List<string>();

      WebTVEpisodeDetailed webTvEpisodeDetailed = new WebTVEpisodeDetailed
      {
        EpisodeNumber = episodeBasic.EpisodeNumber,
        ExternalId = episodeBasic.ExternalId,
        FirstAired = episodeBasic.FirstAired,
        IsProtected = episodeBasic.IsProtected,
        Rating = episodeBasic.Rating,
        SeasonNumber = episodeBasic.SeasonNumber,
        ShowId = episodeBasic.ShowId,
        SeasonId = episodeBasic.SeasonId,
        Type = episodeBasic.Type,
        Watched = episodeBasic.Watched,
        Path = episodeBasic.Path,
        DateAdded = episodeBasic.DateAdded,
        Id = episodeBasic.Id,
        PID = episodeBasic.PID,
        Title = episodeBasic.Title,
        Artwork = episodeBasic.Artwork,
        Show = episodeAspect.GetAttributeValue<string>(EpisodeAspect.ATTR_SERIES_NAME),
        Summary = videoAspect.GetAttributeValue<string>(VideoAspect.ATTR_STORYPLOT),
        Writers = writers,
        Directors = directors
      };

      return webTvEpisodeDetailed;
    }
  }
}
