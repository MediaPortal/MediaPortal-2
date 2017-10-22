using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using MediaPortal.Backend.MediaLibrary;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MP2Extended.Extensions;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(Guid), Nullable = false)]
  internal class GetTVEpisodeCountForTVShow
  {
    public WebIntResult Process(Guid id)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(SeriesAspect.ASPECT_ID);

      // this is the MediaItem from the TvShow
      MediaItem item = GetMediaItems.GetMediaItemById(id, necessaryMIATypes);
      if (item == null)
        throw new BadRequestException(String.Format("GetTVEpisodeCountForTvShow: No MediaItem found with id: {0}", id));

      int count = item.GetAspect(SeriesAspect.Metadata).GetAttributeValue<int>(SeriesAspect.ATTR_AVAILABLE_EPISODES);

      WebIntResult webIntResult = new WebIntResult { Result = count };

      return webIntResult;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
