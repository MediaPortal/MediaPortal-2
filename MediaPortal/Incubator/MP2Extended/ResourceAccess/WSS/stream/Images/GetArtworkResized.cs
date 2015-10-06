using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Services.ThumbnailGenerator;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.WSS;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images
{
  class GetArtworkResized : IStreamRequestMicroModuleHandler
  {
    readonly Dictionary<WebMediaType, FanArtConstants.FanArtMediaType> _fanArtMediaTypeMapping = new Dictionary<WebMediaType, FanArtConstants.FanArtMediaType>
    {
      { WebMediaType.Movie, FanArtConstants.FanArtMediaType.Movie },
      { WebMediaType.TVEpisode, FanArtConstants.FanArtMediaType.Episode },
      { WebMediaType.TVSeason, FanArtConstants.FanArtMediaType.SeriesSeason },
      { WebMediaType.TVShow, FanArtConstants.FanArtMediaType.Series },
      { WebMediaType.MusicTrack, FanArtConstants.FanArtMediaType.Audio },
      { WebMediaType.MusicAlbum, FanArtConstants.FanArtMediaType.Album },
      { WebMediaType.MusicArtist, FanArtConstants.FanArtMediaType.Artist },
      { WebMediaType.Picture, FanArtConstants.FanArtMediaType.Image },
      { WebMediaType.TV, FanArtConstants.FanArtMediaType.ChannelTv },
      { WebMediaType.Radio, FanArtConstants.FanArtMediaType.ChannelRadio },
    };

    readonly Dictionary<WebFileType, FanArtConstants.FanArtType> _fanArtTypeMapping = new Dictionary<WebFileType, FanArtConstants.FanArtType>
    {
      { WebFileType.Backdrop, FanArtConstants.FanArtType.ClearArt },
      { WebFileType.Banner, FanArtConstants.FanArtType.Banner },
      { WebFileType.Content, FanArtConstants.FanArtType.Thumbnail },
      { WebFileType.Cover, FanArtConstants.FanArtType.FanArt },
      { WebFileType.Logo, FanArtConstants.FanArtType.FanArt },  // ??
      { WebFileType.Poster, FanArtConstants.FanArtType.Poster },
    };

    
    public byte[] Process(IHttpRequest request)
    {
      HttpParam httpParam = request.Param;
      string id = httpParam["id"].Value;
      string artworktype = httpParam["artworktype"].Value;
      string mediatype = httpParam["mediatype"].Value;
      string maxWidth = httpParam["maxWidth"].Value;
      string maxHeight = httpParam["maxHeight"].Value;
      string borders = httpParam["borders"].Value;

      bool isSeason = false;
      string showId;
      string seasonId = string.Empty;

      // if teh Id contains a ':' it is a season
      if (id.Contains(":"))
        isSeason = true;

      if (id == null)
        throw new BadRequestException("GetArtworkResized: id is null");
      if (artworktype == null)
        throw new BadRequestException("GetArtworkResized: artworktype is null");
      if (mediatype == null)
        throw new BadRequestException("GetArtworkResized: mediatype is null");
      if (maxWidth == null)
        throw new BadRequestException("GetArtworkResized: maxWidth is null");
      if (maxHeight == null)
        throw new BadRequestException("GetArtworkResized: maxHeight is null");

      WebFileType webFileType = (WebFileType)JsonConvert.DeserializeObject(artworktype, typeof(WebFileType));
      WebMediaType webMediaType = (WebMediaType)JsonConvert.DeserializeObject(mediatype, typeof(WebMediaType));

      int maxWidthInt;
      if (!Int32.TryParse(maxWidth, out maxWidthInt))
      {
        throw new BadRequestException(String.Format("GetArtworkResized: Couldn't convert maxWidth to int: {0}", maxWidth));
      }

      int maxHeightInt;
      if (!Int32.TryParse(maxHeight, out maxHeightInt))
      {
        throw new BadRequestException(String.Format("GetArtworkResized: Couldn't convert maxHeight to int: {0}", maxHeight));
      }

      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);

      ISet<Guid> optionalMIATypes = new HashSet<Guid>();
      optionalMIATypes.Add(VideoAspect.ASPECT_ID);
      optionalMIATypes.Add(MovieAspect.ASPECT_ID);
      optionalMIATypes.Add(SeriesAspect.ASPECT_ID);
      optionalMIATypes.Add(AudioAspect.ASPECT_ID);
      optionalMIATypes.Add(ImageAspect.ASPECT_ID);

      MediaItem item;
      if (isSeason)
      {
        string[] ids = id.Split(':');
        if (ids.Length < 2)
          throw new BadRequestException(String.Format("GetTVEpisodeCountForSeason: not enough ids: {0}", ids.Length));

        showId = ids[0];
        seasonId = ids[1];

        item = GetMediaItems.GetMediaItemById(showId, necessaryMIATypes, optionalMIATypes);
      }
      else
        item = GetMediaItems.GetMediaItemById(id, necessaryMIATypes, optionalMIATypes);

      if (item == null)
        throw new BadRequestException(String.Format("GetArtworkResized: No MediaItem found with id: {0}", httpParam["id"].Value));

      // Map the Fanart Type
      FanArtConstants.FanArtType fanartType;
      if (!_fanArtTypeMapping.TryGetValue(webFileType, out fanartType))
        fanartType = FanArtConstants.FanArtType.Undefined;
      

      // Map the Fanart MediaType
      FanArtConstants.FanArtMediaType fanArtMediaType;
      if (!_fanArtMediaTypeMapping.TryGetValue(webMediaType, out fanArtMediaType))
        fanArtMediaType = FanArtConstants.FanArtMediaType.Undefined;

      string name = (string)item.Aspects[MediaAspect.ASPECT_ID][MediaAspect.ATTR_TITLE];
      // Tv Episode
      if (item.Aspects.ContainsKey(SeriesAspect.ASPECT_ID))
      {
        name = (string)item.Aspects[SeriesAspect.ASPECT_ID][SeriesAspect.ATTR_EPISODENAME];
        fanArtMediaType = FanArtConstants.FanArtMediaType.Undefined;
        fanartType = FanArtConstants.FanArtType.Thumbnail;
      }

      if (isSeason)
        name = String.Format("{0} S{1}", (string)item.Aspects[MediaAspect.ASPECT_ID][MediaAspect.ATTR_TITLE], seasonId);

      IList<FanArtImage> fanart = ServiceRegistration.Get<IFanArtService>().GetFanArt(fanArtMediaType, fanartType, name, 0, 0, true);

      if (fanart == null || fanart.Count == 0)
      {
        Logger.Warn("GetArtworkResized: no fanart found - fanArtMediaType: {0}, fanartType: {1}, name: {2}", Enum.GetName(typeof(FanArtConstants.FanArtMediaType), fanArtMediaType), Enum.GetName(typeof(FanArtConstants.FanArtType), fanartType), name);
        throw new BadRequestException("GetArtworkResized: no fanart found");
      }

      // get a random FanArt from the List
      Random rnd = new Random();
      int r = rnd.Next(fanart.Count);
      return Plugins.MP2Extended.WSS.Images.ResizeImage(fanart[r].BinaryData,maxWidthInt, maxHeightInt, borders);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
