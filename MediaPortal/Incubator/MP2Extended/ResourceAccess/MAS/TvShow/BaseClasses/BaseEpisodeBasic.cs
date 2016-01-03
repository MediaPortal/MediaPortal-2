using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using MediaPortal.Utilities;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common.MediaManagement.Helpers;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow.BaseClasses
{
  class BaseEpisodeBasic
  {
    internal WebTVEpisodeBasic EpisodeBasic(MediaItem item, MediaItem showItem = null)
    {
      MediaItemAspect episodeAspect = MediaItemAspect.GetAspect(item.Aspects, EpisodeAspect.Metadata);
      ResourcePath path = ResourcePath.Deserialize((string)MediaItemAspect.GetAspect(item.Aspects, ProviderResourceAspect.Metadata)[ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH]);

      if (showItem == null)
        showItem = GetMediaItems.GetMediaItemByName((string)episodeAspect[EpisodeAspect.ATTR_SERIESNAME], null);

      WebTVEpisodeBasic webTvEpisodeBasic = new WebTVEpisodeBasic
      {
        IsProtected = false, //??
        Rating = episodeAspect[EpisodeAspect.ATTR_TOTAL_RATING] == null ? 0 : Convert.ToSingle((double)episodeAspect[EpisodeAspect.ATTR_TOTAL_RATING]),
        SeasonNumber = (int)episodeAspect[EpisodeAspect.ATTR_SEASON],
        Type = WebMediaType.TVEpisode,
        Watched = ((int)(MediaItemAspect.GetAspect(item.Aspects, MediaAspect.Metadata)[MediaAspect.ATTR_PLAYCOUNT] ?? 0) > 0),
        Path = new List<string> { (path != null && path.PathSegments.Count > 0) ? StringUtils.RemovePrefixIfPresent(path.LastPathSegment.Path, "/") : string.Empty },
        //Artwork = ,
        DateAdded = (DateTime)MediaItemAspect.GetAspect(item.Aspects, ImporterAspect.Metadata)[ImporterAspect.ATTR_DATEADDED],
        Id = item.MediaItemId.ToString(),
        PID = 0,
        Title = (string)episodeAspect[EpisodeAspect.ATTR_EPISODENAME],
      };
      var episodeNumber = ((HashSet<object>)MediaItemAspect.GetAspect(item.Aspects, EpisodeAspect.Metadata)[EpisodeAspect.ATTR_EPISODE]).Cast<int>().ToList();
      webTvEpisodeBasic.EpisodeNumber = episodeNumber[0];
      string TvDbId;
      MediaItemAspect.TryGetExternalAttribute(item.Aspects, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, out TvDbId);
      if (TvDbId != null)
      {
        webTvEpisodeBasic.ExternalId.Add(new WebExternalId
        {
          Site = "TVDB",
          Id = TvDbId
        });
      }
      string ImdbId;
      MediaItemAspect.TryGetExternalAttribute(item.Aspects, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, out ImdbId);
      if (ImdbId != null)
      {
        webTvEpisodeBasic.ExternalId.Add(new WebExternalId
        {
          Site = "IMDB",
          Id = ImdbId
        });
      }

      var firstAired = episodeAspect[EpisodeAspect.ATTR_FIRSTAIRED];
      if (firstAired != null)
        webTvEpisodeBasic.FirstAired = (DateTime)firstAired;

      // season
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(SeasonAspect.ASPECT_ID);

      IFilter seasonFilter = new RelationshipFilter(item.MediaItemId, EpisodeAspect.ROLE_EPISODE, SeasonAspect.ROLE_SEASON);
      MediaItemQuery seasonQuery = new MediaItemQuery(necessaryMIATypes, null, seasonFilter);
      List<MediaItem> season = ServiceRegistration.Get<IMediaLibrary>().Search(seasonQuery, false).ToList();

      if (season.Count > 0)
      {
        // show
        necessaryMIATypes = new HashSet<Guid>();
        necessaryMIATypes.Add(SeriesAspect.ASPECT_ID);

        IFilter showFilter = new RelationshipFilter(season[0].MediaItemId, SeasonAspect.ROLE_SEASON, SeriesAspect.ROLE_SERIES);
        MediaItemQuery showQuery = new MediaItemQuery(necessaryMIATypes, null, showFilter);
        List<MediaItem> show = ServiceRegistration.Get<IMediaLibrary>().Search(showQuery, false).ToList();

        if (season.Count > 0 && show.Count > 0)
        {
          webTvEpisodeBasic.ShowId = show[0].MediaItemId.ToString();
          webTvEpisodeBasic.SeasonId = season[0].MediaItemId.ToString();
        }
      }


      return webTvEpisodeBasic;
    }
  }
}
