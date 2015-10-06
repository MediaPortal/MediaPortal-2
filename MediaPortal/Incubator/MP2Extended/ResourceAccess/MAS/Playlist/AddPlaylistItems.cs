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
  class AddPlaylistItems : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
    {
      HttpParam httpParam = request.Param;
      string playlistId = httpParam["playlistId"].Value;
      string itemIds = httpParam["id"].Value;
      string position = httpParam["position"].Value;
      if (playlistId == null)
        throw new BadRequestException("AddPlaylistItems: playlistId is null");
      if (itemIds == null)
        throw new BadRequestException("AddPlaylistItems: id is null");

      Guid playlistGuid;
      if (!Guid.TryParse(playlistId, out playlistGuid))
        throw new BadRequestException(String.Format("AddPlaylistItem: Couldn't parse playlistId: {0}", playlistId));

      // separte the ids
      string[] itemIdsArray = itemIds.Split('|');

      if (itemIdsArray.Length == 0)
        throw new BadRequestException(String.Format("AddPlaylistItems: id array is empty - itemIds: {0}", itemIds));

      int positionInt = -1;
      if (!int.TryParse(position, out positionInt))
        Logger.Warn("AddPlaylistItem: Couldn't parse position to int: {0}", position);

      // get the playlist
      PlaylistRawData playlistRawData = ServiceRegistration.Get<IMediaLibrary>().ExportPlaylist(playlistGuid);
      
      // insert the data

      foreach (var itemId in itemIdsArray)
      {
        Guid mediaItemGuid;
        if (!Guid.TryParse(itemId, out mediaItemGuid))
          throw new BadRequestException(String.Format("AddPlaylistItem: Couldn't parse id: {0}", playlistId));

        if (positionInt > -1 && positionInt < playlistRawData.MediaItemIds.Count)
        {
          playlistRawData.MediaItemIds.Insert(positionInt, mediaItemGuid); // List{0,1,2} -Insert@index:1Value:5-> List{0,5,1,2}
          positionInt++;
        }
        else
          playlistRawData.MediaItemIds.Add(mediaItemGuid);
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
