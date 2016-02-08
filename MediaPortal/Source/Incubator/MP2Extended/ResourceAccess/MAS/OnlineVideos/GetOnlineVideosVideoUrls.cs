using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.Utils;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.OnlineVideos
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(List<WebOnlineVideosVideo>), Summary = "This function returns a list of available Urls for a given Video.")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  internal class GetOnlineVideosVideoUrls
  {
    public List<string> Process(string id)
    {
      if (id == null)
        throw new BadRequestException("GetOnlineVideosVideoUrls: id is null");

      string siteName;
      string categoryRecursiveName;
      string videoUrl;
      OnlineVideosIdGenerator.DecodeVideoId(id, out siteName, out categoryRecursiveName, out videoUrl);

      return MP2Extended.OnlineVideosManager.GetVideoUrls(siteName, categoryRecursiveName, videoUrl);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}