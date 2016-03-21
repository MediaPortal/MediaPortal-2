#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.MediaServer.DLNA;
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Utilities.Network;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.Plugins.Transcoding.Interfaces;
using MediaPortal.Plugins.Transcoding.Interfaces.Helpers;
using MediaPortal.Plugins.Transcoding.Interfaces.Transcoding;

namespace MediaPortal.Plugins.MediaServer.ResourceAccess
{
  public static class DlnaResourceAccessUtils
  {
    /// <summary>
    /// Base HTTP path for resource access, e.g. "/GetDlnaResource".
    /// </summary>
    public const string RESOURCE_ACCESS_PATH = "/GetDlnaResource";

    /// <summary>
    /// Argument name for the resource path argument, e.g. "MediaItem".
    /// </summary>
    public const string RESOURCE_PATH_ARGUMENT_NAME = "ResourcePath";


    public const string SYNTAX = RESOURCE_ACCESS_PATH + "/[media item guid]";

    public static string GetResourceUrl(string mediaItem)
    {
      return RESOURCE_ACCESS_PATH + "/" + mediaItem;
    }

    public static bool ParseMediaItem(Uri resourceUri, out Guid mediaItemGuid)
    {
      mediaItemGuid = Guid.Empty;
      try
      {
        var r = Regex.Match(resourceUri.PathAndQuery, RESOURCE_ACCESS_PATH + @"\/([\w-]*)\/?");
        var mediaItem = r.Groups[1].Value;
        if (mediaItem.Contains("."))
        {
          mediaItem = mediaItem.Substring(0, mediaItem.IndexOf("."));
        }
        if (Guid.TryParse(mediaItem, out mediaItemGuid))
        {
          return true;
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("ParseMediaItem: Failed with input url {0}", e, resourceUri.OriginalString);
      }

      return false;
    }

    public static bool ParseRadioChannel(Uri resourceUri, out int radioChannel)
    {
      radioChannel = 0;
      try
      {

        var r = Regex.Match(resourceUri.PathAndQuery, RESOURCE_ACCESS_PATH + @"\/5244494F-0000-0000-0000-([\w-]*)\/?");
        var channel = r.Groups[1].Value;
        if (int.TryParse(channel, out radioChannel))
        {
          return true;
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("ParseRadioChannel: Failed with input url {0}", e, resourceUri.OriginalString);
      }
      return false;
    }

    public static bool ParseTVChannel(Uri resourceUri, out int tvChannel)
    {
      tvChannel = 0;
      try
      {
        var r = Regex.Match(resourceUri.PathAndQuery, RESOURCE_ACCESS_PATH + @"\/54560000-0000-0000-0000-([\w-]*)\/?");
        var channel = r.Groups[1].Value;
        if (int.TryParse(channel, out tvChannel))
        {
          return true;
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("ParseTVChannel: Failed with input url {0}", e, resourceUri.OriginalString);
      }
      return false;
    }

    public static bool UseSoftCodedSubtitle(EndPointSettings client, out SubtitleCodec targetCodec, out string targetMime)
    {
      targetCodec = SubtitleCodec.Unknown;
      targetMime = "text/plain";
      if (client.Profile.MediaTranscoding.SubtitleSettings.SubtitleMode == SubtitleSupport.SoftCoded)
      {
        targetCodec = client.Profile.MediaTranscoding.SubtitleSettings.SubtitlesSupported[0].Format;
        if (string.IsNullOrEmpty(client.Profile.MediaTranscoding.SubtitleSettings.SubtitlesSupported[0].Mime) == false)
          targetMime = client.Profile.MediaTranscoding.SubtitleSettings.SubtitlesSupported[0].Mime;
        else
          targetMime = Subtitles.GetSubtitleMime(targetCodec);
        return true;
      }
      return false;
    }

    private static IPAddress GetLocalIp()
    {
      bool useIPv4 = true;
      bool useIPv6 = false;
      ServerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ServerSettings>();
      if (settings.UseIPv4) useIPv4 = true;
      if (settings.UseIPv6) useIPv6 = true;

      var host = Dns.GetHostEntry(Dns.GetHostName());
      IPAddress ip6 = null;
      foreach (var ip in host.AddressList)
      {
        if (IPAddress.IsLoopback(ip) == true)
        {
          continue;
        }
        if (useIPv4)
        {
          if (ip.AddressFamily == AddressFamily.InterNetwork)
          {
            return ip;
          }
        }
        if (useIPv6)
        {
          if (ip.AddressFamily == AddressFamily.InterNetworkV6)
          {
            ip6 = ip;
          }
        }
      }
      if (ip6 != null)
      {
        return ip6;
      }
      return null;
    }

    public static bool IsSoftCodedSubtitleAvailable(DlnaMediaItem dlnaItem, EndPointSettings client)
    {
      if (client.Profile.MediaTranscoding.SubtitleSettings.SubtitleMode != SubtitleSupport.SoftCoded)
      {
        return false;
      }
      if (dlnaItem.IsTranscoded && dlnaItem.IsVideo)
      {
        VideoTranscoding video = (VideoTranscoding)dlnaItem.TranscodingParameter;
        if (Subtitles.IsSubtitleAvailable(video)) return true;
      }
      else if (dlnaItem.IsVideo)
      {
        VideoTranscoding subtitle = (VideoTranscoding)dlnaItem.SubtitleTranscodingParameter;
        if (Subtitles.IsSubtitleAvailable(subtitle)) return true;
      }
      return false;
    }

    public static string GetThumbnailBaseURL(MediaItem item, EndPointSettings client)
    {
      //bool useFanart = false;
      //if (item.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
      //{
      //  useFanart = true;
      //}
      //else if (item.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
      //{
      //  useFanart = true;
      //}

      string url;
      bool useFanart = true;
      if (useFanart)
      {
        string mediaType = FanArtMediaTypes.Undefined;
        if (item.Aspects.ContainsKey(ImageAspect.ASPECT_ID)) mediaType = FanArtMediaTypes.Image;
        else if (item.Aspects.ContainsKey(MovieAspect.ASPECT_ID)) mediaType = FanArtMediaTypes.Movie;
        else if (item.Aspects.ContainsKey(SeriesAspect.ASPECT_ID)) mediaType = FanArtMediaTypes.Series;
        else if (item.Aspects.ContainsKey(AudioAspect.ASPECT_ID)) mediaType = FanArtMediaTypes.Audio;
        else if (item.Aspects.ContainsKey(AudioAlbumAspect.ASPECT_ID)) mediaType = FanArtMediaTypes.Album;
        else if (item.Aspects.ContainsKey(EpisodeAspect.ASPECT_ID)) mediaType = FanArtMediaTypes.Episode;
        else if (item.Aspects.ContainsKey(SeasonAspect.ASPECT_ID)) mediaType = FanArtMediaTypes.SeriesSeason;

        // Using MP2's FanArtService provides access to all kind of resources, thumbnails from ML and also local fanart from filesystem
        url = string.Format("{0}/FanartService?mediatype={1}&fanarttype={2}&name={3}&width={4}&height={5}",
          GetBaseResourceURL(), mediaType, FanArtTypes.Thumbnail, item.MediaItemId, 
          client.Profile.Settings.Thumbnails.MaxWidth, client.Profile.Settings.Thumbnails.MaxHeight);
      }
      else
      {
        // Using MP2's thumbnails
        url = string.Format("{0}{1}?aspect={2}&width={3}&height={4}",
          GetBaseResourceURL(), GetResourceUrl(item.MediaItemId.ToString()), "THUMBNAIL", 
          client.Profile.Settings.Thumbnails.MaxWidth, client.Profile.Settings.Thumbnails.MaxHeight);
      }
      return url;
    }

    public static string GetChannelLogoBaseURL(string channelName, EndPointSettings client, bool isTV)
    {
      string mediaType = isTV ? FanArtMediaTypes.ChannelTv : FanArtMediaTypes.ChannelRadio;
      string url = string.Format("{0}/FanartService?mediatype={1}&fanarttype={2}l&name={3}&width={4}&height={5}",
          GetBaseResourceURL(), mediaType, FanArtTypes.Thumbnail, WebUtility.UrlEncode(channelName),
          client.Profile.Settings.Thumbnails.MaxWidth, client.Profile.Settings.Thumbnails.MaxHeight);
      return url;
    }

    public static string GetSubtitleBaseURL(MediaItem item, EndPointSettings client, out string subMime, out string subExtension)
    {
      SubtitleCodec codec = SubtitleCodec.Unknown;
      subMime = null;
      subExtension = null;

      if (UseSoftCodedSubtitle(client, out codec, out subMime) == true)
      {
        subExtension = "srt";
        string subType = codec.ToString();
        switch (codec)
        {
          case SubtitleCodec.Ass:
            subExtension = "ass";
            break;
          case SubtitleCodec.Ssa:
            subExtension = "ssa";
            break;
          case SubtitleCodec.Smi:
            subExtension = "smi";
            break;
          case SubtitleCodec.Srt:
            subExtension = "srt";
            break;
          case SubtitleCodec.MicroDvd:
            subExtension = "sub";
            break;
          case SubtitleCodec.SubView:
            subExtension = "sub";
            break;
          case SubtitleCodec.WebVtt:
            subExtension = "vtt";
            break;
        }

        return string.Format(GetBaseResourceURL()
                    + GetResourceUrl(item.MediaItemId.ToString())
                    + "?aspect=SUBTITLE&type={0}&file=subtitle.{1}", subType, subExtension);
      }
      return null;
    }

    public static string GetBaseResourceURL()
    {
      var rs = ServiceRegistration.Get<IResourceServer>();
      return "http://" + NetworkUtils.IPAddrToString(GetLocalIp()) + ":" + rs.GetPortForIP(GetLocalIp());
    }
  }
}
