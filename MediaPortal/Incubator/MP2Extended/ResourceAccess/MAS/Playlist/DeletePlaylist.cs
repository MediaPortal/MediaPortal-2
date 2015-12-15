using System;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Playlist
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "playlistId", Type = typeof(string), Nullable = false)]
  internal class DeletePlaylist : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string playlistId = httpParam["playlistId"].Value;
      if (playlistId == null)
        throw new BadRequestException("DeletePlaylist: playlistId is null");

      Guid playlistIdGuid;
      if (!Guid.TryParse(playlistId, out playlistIdGuid))
        throw new BadRequestException(String.Format("DeletePlaylist: Couldn't parse playlistId: {0}", playlistId));

      bool result = ServiceRegistration.Get<IMediaLibrary>().DeletePlaylist(playlistIdGuid);

      return new WebBoolResult { Result = result };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}