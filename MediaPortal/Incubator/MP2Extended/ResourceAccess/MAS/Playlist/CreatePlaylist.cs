using System;
using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.General;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Playlist
{
  internal class CreatePlaylist : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
    {
      HttpParam httpParam = request.Param;
      string playlistName = httpParam["playlistName"].Value;
      if (playlistName == null)
        throw new BadRequestException("CreatePlaylist: playlistName is null");

      Guid playListGuid = Guid.NewGuid();

      PlaylistRawData playlistRawData = new PlaylistRawData(playListGuid, playlistName, String.Empty);

      ServiceRegistration.Get<IMediaLibrary>().SavePlaylist(playlistRawData);


      return new WebStringResult { Result = playListGuid.ToString() };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}