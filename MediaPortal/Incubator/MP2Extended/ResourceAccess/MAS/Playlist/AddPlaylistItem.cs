using System;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Playlist
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "playlistId", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "position", Type = typeof(int), Nullable = true)]
  internal class AddPlaylistItem : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string playlistId = httpParam["playlistId"].Value;
      string itemId = httpParam["id"].Value;
      string position = httpParam["position"].Value;
      if (playlistId == null)
        throw new BadRequestException("AddPlaylistItem: playlistId is null");
      if (itemId == null)
        throw new BadRequestException("AddPlaylistItem: id is null");

      Guid playlistGuid;
      if (!Guid.TryParse(playlistId, out playlistGuid))
        throw new BadRequestException(String.Format("AddPlaylistItem: Couldn't parse playlistId: {0}", playlistId));

      Guid mediaItemGuid;
      if (!Guid.TryParse(itemId, out mediaItemGuid))
        throw new BadRequestException(String.Format("AddPlaylistItem: Couldn't parse id: {0}", playlistId));

      int positionInt = -1;
      if (!int.TryParse(position, out positionInt))
        Logger.Warn("AddPlaylistItem: Couldn't parse position to int: {0}", position);

      // get the playlist
      PlaylistRawData playlistRawData = ServiceRegistration.Get<IMediaLibrary>().ExportPlaylist(playlistGuid);

      // insert the data
      if (positionInt > -1 && positionInt < playlistRawData.MediaItemIds.Count)
        playlistRawData.MediaItemIds.Insert(positionInt, mediaItemGuid); // List{0,1,2} -Insert@index:1Value:5-> List{0,5,1,2}
      else
        playlistRawData.MediaItemIds.Add(mediaItemGuid);

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