#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

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
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Common.FanArt;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.Owin;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.BaseClasses;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images.BaseClasses
{
  internal class BaseGetArtwork : BaseSendData
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
      { WebFileType.Cover, FanArtTypes.Cover },
      { WebFileType.Logo, FanArtTypes.Logo },
      { WebFileType.Poster, FanArtTypes.Poster },
      { WebFileType.ClearArt, FanArtTypes.ClearArt },
      { WebFileType.DiscArt, FanArtTypes.DiscArt },
    };


    internal static void MapTypes(WebFileType webFileType, WebMediaType webMediaType, out string fanartType, out string fanArtMediaType)
    {
      // Map the Fanart Type
      if (!_fanArtTypeMapping.TryGetValue(webFileType, out fanartType))
        fanartType = FanArtTypes.Undefined;

      // Map the Fanart MediaType
      if (!_fanArtMediaTypeMapping.TryGetValue(webMediaType, out fanArtMediaType))
        fanArtMediaType = FanArtMediaTypes.Undefined;
    }

    internal static IList<FanArtImage> GetFanArtImages(IOwinContext context, string id, bool isTvRadio, bool isRecording, string fanartType, string fanArtMediaType)
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
        item = MediaLibraryAccess.GetMediaItemById(context, id, necessaryMIATypes, optionalMIATypes);

      if (item == null && !isTvRadio)
        throw new BadRequestException(String.Format("GetFanArtImages: No MediaItem found with id: {0}", id));

      string name = id;
      if (isTvRadio)
      {
        IChannelAndGroupInfoAsync channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>(false) as IChannelAndGroupInfoAsync;
        if (channelAndGroupInfo != null)
        {
          int idInt = int.Parse(id);
          var channel = channelAndGroupInfo.GetChannelAsync(idInt).Result;
          if (channel.Success)
            name = channel.Result.Name;
          else
            throw new BadRequestException(String.Format("GetFanArtImages: No Channel found with id: {0}", id));
        }
      }

      IList<FanArtImage> fanart = ServiceRegistration.Get<IFanArtService>().GetFanArt(fanArtMediaType, fanartType, name, 0, 0, false);
      if (fanart == null || fanart.Count == 0)
      {
        Logger.Debug("GetFanArtImages: no fanart found - fanArtMediaType: {0}, fanartType: {1}, name: {2}", fanArtMediaType, fanartType, name);
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

    internal static Guid IntToGuid(int value)
    {
      byte[] bytes = new byte[16];
      BitConverter.GetBytes(value).CopyTo(bytes, 0);
      return new Guid(bytes);
    }

    internal static Guid StringToGuid(string value)
    {
      byte[] bytes = ResourceAccessUtils.GetBytes(value);
      return new Guid(bytes);
    }

    //public static HttpResponseMessage ImageFile(byte[] bytes)
    //{
    //  HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
    //  result.Content = new ByteArrayContent(bytes);
    //  result.Content.Headers.ContentType = new MediaTypeHeaderValue("image/*");
    //  return result;
    //}

    public static Stream ImageFile(byte[] bytes)
    {
      MemoryStream mem = new MemoryStream(bytes);
      mem.Position = 0;
      return mem;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
