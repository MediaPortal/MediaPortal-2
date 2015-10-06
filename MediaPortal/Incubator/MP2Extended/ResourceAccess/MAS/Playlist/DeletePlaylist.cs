using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS;
using MediaPortal.Plugins.MP2Extended.MAS.Playlist;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.General;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Playlist
{
  class DeletePlaylist : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
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
