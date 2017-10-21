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
using MP2Extended.Extensions;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow.BaseClasses
{

  // TODO: Add more detailes
  class BaseTvShowBasic
  {
    internal WebTVShowBasic TVShowBasic(MediaItem item, MediaItem showItem = null)
    {
      var seriesAspect = item.GetAspect(SeriesAspect.Metadata);
      var importerAspect = item.GetAspect(ImporterAspect.Metadata);

      ISet<Guid> necessaryMIATypespisodes = new HashSet<Guid>();
      necessaryMIATypespisodes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypespisodes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypespisodes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypespisodes.Add(EpisodeAspect.ASPECT_ID);

      IFilter searchFilter = new RelationshipFilter(item.MediaItemId, SeriesAspect.ROLE_SERIES, EpisodeAspect.ROLE_EPISODE);
      MediaItemQuery searchQuery = new MediaItemQuery(necessaryMIATypespisodes, null, searchFilter);

      IList<MediaItem> episodes = ServiceRegistration.Get<IMediaLibrary>().Search(searchQuery, false, null, false);
      var episodesInThisShowUnwatched = episodes.ToList().FindAll(x => x.GetAspect(MediaAspect.Metadata)[MediaAspect.ATTR_PLAYCOUNT] == null || (int)x.GetAspect(MediaAspect.Metadata)[MediaAspect.ATTR_PLAYCOUNT] == 0);


      return new WebTVShowBasic()
      {
        Id = item.MediaItemId.ToString(),
        Title = (string)seriesAspect[SeriesAspect.ATTR_SERIES_NAME],
        DateAdded = (DateTime)importerAspect[ImporterAspect.ATTR_DATEADDED],
        EpisodeCount = episodes.Count,
        UnwatchedEpisodeCount = episodesInThisShowUnwatched.Count
      };
    }
  }
}
