using System;
using System.Collections.Generic;
using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images.BaseClasses;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images
{
  // TODO: implement offset
  internal class GetArtwork : IStreamRequestMicroModuleHandler
  {
    public byte[] Process(IHttpRequest request)
    {
      HttpParam httpParam = request.Param;
      string id = httpParam["id"].Value;
      string artworktype = httpParam["artworktype"].Value;
      string mediatype = httpParam["mediatype"].Value;

      bool isSeason = false;
      string showId = string.Empty;
      string seasonId = string.Empty;

      if (id == null)
        throw new BadRequestException("GetArtworkResized: id is null");
      if (artworktype == null)
        throw new BadRequestException("GetArtworkResized: artworktype is null");
      if (mediatype == null)
        throw new BadRequestException("GetArtworkResized: mediatype is null");

      // if teh Id contains a ':' it is a season
      if (id.Contains(":"))
        isSeason = true;

      IList<FanArtImage> fanart = BaseGetArtwork.GetFanArtImages(artworktype, mediatype, id, showId, seasonId, isSeason);

      // get a random FanArt from the List
      Random rnd = new Random();
      int r = rnd.Next(fanart.Count);
      return fanart[r].BinaryData;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}