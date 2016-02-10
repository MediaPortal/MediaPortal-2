using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images.BaseClasses
{
  internal class BaseGetArtwork
  {
    internal const string NO_FANART_IMAGE_NAME = "B1D44E89-1EAC-4765-B9E9-EF4BBE75C774";
    
    private static readonly Dictionary<WebMediaType, string> _fanArtMediaTypeMapping = new Dictionary<WebMediaType, string>
    {
      { WebMediaType.Movie, FanArtMediaTypes.Movie },
      { WebMediaType.TVEpisode, FanArtMediaTypes.Episode },
      { WebMediaType.TVSeason, FanArtMediaTypes.SeriesSeason },
      { WebMediaType.TVShow, FanArtMediaTypes.Series },
      { WebMediaType.MusicTrack, FanArtMediaTypes.Audio },
      { WebMediaType.MusicAlbum, FanArtMediaTypes.Album },
      { WebMediaType.MusicArtist, FanArtMediaTypes.Artist },
      { WebMediaType.Picture, FanArtMediaTypes.Image },
      { WebMediaType.TV, FanArtMediaTypes.ChannelTv },
      { WebMediaType.Radio, FanArtMediaTypes.ChannelRadio },
      { WebMediaType.Recording, FanArtMediaTypes.Undefined },
    };

    private static readonly Dictionary<WebFileType, string> _fanArtTypeMapping = new Dictionary<WebFileType, string>
    {
      { WebFileType.Backdrop, FanArtTypes.FanArt },
      { WebFileType.Banner, FanArtTypes.Banner },
      { WebFileType.Content, FanArtTypes.Thumbnail },
      { WebFileType.Cover, FanArtTypes.Poster },
      { WebFileType.Logo, FanArtTypes.FanArt }, // ??
      { WebFileType.Poster, FanArtTypes.Poster },
    };


    internal void MapTypes(WebFileType webFileType, WebMediaType webMediaType, out string fanartType, out string fanArtMediaType)
    {
      // Map the Fanart Type
      if (!_fanArtTypeMapping.TryGetValue(webFileType, out fanartType))
        fanartType = FanArtTypes.Undefined;


      // Map the Fanart MediaType
      if (!_fanArtMediaTypeMapping.TryGetValue(webMediaType, out fanArtMediaType))
        fanArtMediaType = FanArtMediaTypes.Undefined;
    }

    internal IList<FanArtImage> GetFanArtImages(string id, bool isTvRadio, bool isRecording, string fanartType, string fanArtMediaType)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);

      ISet<Guid> optionalMIATypes = new HashSet<Guid>();
      optionalMIATypes.Add(VideoAspect.ASPECT_ID);
      optionalMIATypes.Add(MovieAspect.ASPECT_ID);
      optionalMIATypes.Add(SeriesAspect.ASPECT_ID);
      optionalMIATypes.Add(SeasonAspect.ASPECT_ID);
      optionalMIATypes.Add(EpisodeAspect.ASPECT_ID);
      optionalMIATypes.Add(AudioAspect.ASPECT_ID);
      optionalMIATypes.Add(ImageAspect.ASPECT_ID);

      MediaItem item = null;
      if (!isTvRadio)
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
        name = (string)MediaItemAspect.GetAspect(item.Aspects, MediaAspect.Metadata)[MediaAspect.ATTR_TITLE];
        // Tv Episode
        if (item.Aspects.ContainsKey(EpisodeAspect.ASPECT_ID))
        {
          //name = (string)item.Aspects[SeriesAspect.ASPECT_ID][SeriesAspect.ATTR_EPISODENAME];
          name = item.MediaItemId.ToString();
          fanArtMediaType = FanArtMediaTypes.Undefined;
          fanartType = FanArtTypes.Thumbnail;
        }
        else
        {
          name = id;
        }

        /*if (isSeason)
        {
          name = String.Format("{0} S{1}", (string)MediaItemAspect.GetAspect(item.Aspects, MediaAspect.Metadata)[MediaAspect.ATTR_TITLE], seasonId);
          fanartType = FanArtTypes.Poster;
        }*/
      }

      IList<FanArtImage> fanart = ServiceRegistration.Get<IFanArtService>().GetFanArt(fanArtMediaType, fanartType, name, 0, 0, false);

      if (fanart == null || fanart.Count == 0)
      {
        Logger.Debug("BaseGetArtwork: no fanart found - fanArtMediaType: {0}, fanartType: {1}, name: {2}", fanArtMediaType, fanartType, name);
        // We return a transparent image instead of throwing an exception
        Bitmap newImage = new Bitmap(1, 1, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
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