using System;
using System.Collections.Generic;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Playlist
{
  // this function ignores the porsiton argument, the documentation is not very clear
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "playlistId", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "position", Type = typeof(int), Nullable = true)]
  internal class ClearAndAddPlaylistItems
  {
    public WebBoolResult Process(Guid playlistId, WebMediaType type, int? position, List<Guid> ids)
    {
      
      if (ids.Count == 0)
        throw new BadRequestException(String.Format("AddPlaylistItems: id array is empty - itemIds: {0}", ids));

     // get the playlist
      PlaylistRawData playlistRawData = ServiceRegistration.Get<IMediaLibrary>().ExportPlaylist(playlistId);

      // clear the data
      playlistRawData.MediaItemIds.Clear();

      // insert the data

      foreach (var itemId in ids)
      {
       playlistRawData.MediaItemIds.Add(itemId);
      }

      // save playlist
      ServiceRegistration.Get<IMediaLibrary>().SavePlaylist(playlistRawData);


      return new WebBoolResult { Result = true };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}