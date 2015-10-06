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
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Playlist
{
  class GetPlaylists : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
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
