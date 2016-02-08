using System;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.General;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Playlist
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "playlistName", Type = typeof(string), Nullable = false)]
  internal class CreatePlaylist
  {
    public WebStringResult Process(string playlistName)
    {
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