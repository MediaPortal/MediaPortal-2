using System.Collections.Generic;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS.Playlist;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Playlist
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(List<WebPlaylist>), Summary = "")]
  internal class GetPlaylists : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      ICollection<PlaylistInformationData> playlists = ServiceRegistration.Get<IMediaLibrary>().GetPlaylists();

      List<WebPlaylist> output = new List<WebPlaylist>();

      foreach (var playlist in playlists)
      {
        WebPlaylist webPlaylist = new WebPlaylist
        {
          ItemCount = playlist.NumItems,
          Type = WebMediaType.Playlist,
          Id = playlist.PlaylistId.ToString(),
          PID = 0,
          Title = playlist.Name
        };
        //webPlaylist.Artwork;
        //webPlaylist.DateAdded;
        //webPlaylist.Path;

        output.Add(webPlaylist);
      }

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}