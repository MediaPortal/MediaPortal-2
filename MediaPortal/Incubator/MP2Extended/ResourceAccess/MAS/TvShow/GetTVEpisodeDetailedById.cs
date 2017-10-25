using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow.BaseClasses;
using System;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  internal class GetTVEpisodeDetailedById : BaseEpisodeDetailed
  {
    public WebTVEpisodeDetailed Process(Guid id)
    {
      MediaItem item = GetMediaItems.GetMediaItemById(id, DetailedNecessaryMIATypeIds, DetailedOptionalMIATypeIds);

      if (item == null)
        throw new BadRequestException(String.Format("GetTvEpisodeBasicById: No MediaItem found with id: {0}", id));

      WebTVEpisodeDetailed webTvEpisodeDetailed = EpisodeDetailed(item);

      return webTvEpisodeDetailed;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
