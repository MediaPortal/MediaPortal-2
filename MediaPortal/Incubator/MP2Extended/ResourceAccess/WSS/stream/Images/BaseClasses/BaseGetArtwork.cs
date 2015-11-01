using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images.BaseClasses
{
  internal class BaseGetArtwork
  {
    internal const string NO_FANART_IMAGE_NAME = "B1D44E89-1EAC-4765-B9E9-EF4BBE75C774";
    
    private static readonly Dictionary<WebMediaType, FanArtConstants.FanArtMediaType> _fanArtMediaTypeMapping = new Dictionary<WebMediaType, FanArtConstants.FanArtMediaType>
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
      { WebMediaType.Recording, FanArtConstants.FanArtMediaType.Undefined },
    };

    private static readonly Dictionary<WebFileType, FanArtConstants.FanArtType> _fanArtTypeMapping = new Dictionary<WebFileType, FanArtConstants.FanArtType>
    {
      { WebFileType.Backdrop, FanArtConstants.FanArtType.FanArt },
      { WebFileType.Banner, FanArtConstants.FanArtType.Banner },
      { WebFileType.Content, FanArtConstants.FanArtType.Thumbnail },
      { WebFileType.Cover, FanArtConstants.FanArtType.Poster },
      { WebFileType.Logo, FanArtConstants.FanArtType.FanArt }, // ??
      { WebFileType.Poster, FanArtConstants.FanArtType.Poster },
    };


    internal void MapTypes(string artworktype, string mediatype, out FanArtConstants.FanArtType fanartType, out FanArtConstants.FanArtMediaType fanArtMediaType)
    {
      WebFileType webFileType = (WebFileType)JsonConvert.DeserializeObject(artworktype, typeof(WebFileType));
      WebMediaType webMediaType = (WebMediaType)JsonConvert.DeserializeObject(mediatype, typeof(WebMediaType));

      // Map the Fanart Type
      if (!_fanArtTypeMapping.TryGetValue(webFileType, out fanartType))
        fanartType = FanArtConstants.FanArtType.Undefined;


      // Map the Fanart MediaType
      if (!_fanArtMediaTypeMapping.TryGetValue(webMediaType, out fanArtMediaType))
        fanArtMediaType = FanArtConstants.FanArtMediaType.Undefined;
    }

    internal IList<FanArtImage> GetFanArtImages(string id, string showId, string seasonId, bool isSeason, bool isTvRadio, bool isRecording, FanArtConstants.FanArtType fanartType, FanArtConstants.FanArtMediaType fanArtMediaType)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);

      ISet<Guid> optionalMIATypes = new HashSet<Guid>();
      optionalMIATypes.Add(VideoAspect.ASPECT_ID);
      optionalMIATypes.Add(MovieAspect.ASPECT_ID);
      optionalMIATypes.Add(SeriesAspect.ASPECT_ID);
      optionalMIATypes.Add(AudioAspect.ASPECT_ID);
      optionalMIATypes.Add(ImageAspect.ASPECT_ID);

      MediaItem item = null;
      if (isSeason)
      {
        string[] ids = id.Split(':');
        if (ids.Length < 2)
          throw new BadRequestException(String.Format("GetArtworkResized: not enough ids: {0}", ids.Length));

        showId = ids[0];
        seasonId = ids[1];

        item = GetMediaItems.GetMediaItemById(showId, necessaryMIATypes, optionalMIATypes);
      }
      else if (!isTvRadio)
        item = GetMediaItems.GetMediaItemById(id, necessaryMIATypes, optionalMIATypes);

      if (item == null && !isTvRadio)
        throw new BadRequestException(String.Format("GetArtworkResized: No MediaItem found with id: {0}", id));

      string name;
      if (isTvRadio)
      {
        name = id;
        if (ServiceRegistration.IsRegistered<ITvProvider>())
        {
          IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;
          IChannel channel;
          int idInt = int.Parse(id);
          if (channelAndGroupInfo.GetChannel(idInt, out channel))
            name = channel.Name;
        }
      }else if (isRecording)
      {
        name = id;
      }else
      {
        name = (string)item.Aspects[MediaAspect.ASPECT_ID][MediaAspect.ATTR_TITLE];
        // Tv Episode
        if (item.Aspects.ContainsKey(SeriesAspect.ASPECT_ID))
        {
          //name = (string)item.Aspects[SeriesAspect.ASPECT_ID][SeriesAspect.ATTR_EPISODENAME];
          name = item.MediaItemId.ToString();
          fanArtMediaType = FanArtConstants.FanArtMediaType.Undefined;
          fanartType = FanArtConstants.FanArtType.Thumbnail;
        }

        if (isSeason)
        {
          name = String.Format("{0} S{1}", (string)item.Aspects[MediaAspect.ASPECT_ID][MediaAspect.ATTR_TITLE], seasonId);
          fanartType = FanArtConstants.FanArtType.Poster;
        }
      }

      IList<FanArtImage> fanart = ServiceRegistration.Get<IFanArtService>().GetFanArt(fanArtMediaType, fanartType, name, 0, 0, false);

      if (fanart == null || fanart.Count == 0)
      {
        Logger.Debug("BaseGetArtwork: no fanart found - fanArtMediaType: {0}, fanartType: {1}, name: {2}", Enum.GetName(typeof(FanArtConstants.FanArtMediaType), fanArtMediaType), Enum.GetName(typeof(FanArtConstants.FanArtType), fanartType), name);
        // We return a transparent image instead of throwing an exception
        Bitmap newImage = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
        Graphics graphic = Graphics.FromImage(newImage);
        graphic.Clear(Color.Transparent);
        MemoryStream ms = new MemoryStream();
        newImage.Save(ms, ImageFormat.Png);
        return new List<FanArtImage>
        {
          new FanArtImage
          {
            Name = NO_FANART_IMAGE_NAME,
            BinaryData = ms.ToArray()
          }
        };
      }

      return fanart;
    }

    internal Guid IntToGuid(int value)
    {
      byte[] bytes = new byte[16];
      BitConverter.GetBytes(value).CopyTo(bytes, 0);
      return new Guid(bytes);
    }

    internal Guid StringToGuid(string value)
    {
      using (MD5 md5 = MD5.Create())
      {
        byte[] hash = md5.ComputeHash(Encoding.Default.GetBytes(value));
        return new Guid(hash);
      }
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}