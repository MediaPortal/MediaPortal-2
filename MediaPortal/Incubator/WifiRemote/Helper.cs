#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using System.Net.Sockets;
using System.Net;
using MediaPortal.Common.Settings;

namespace MediaPortal.Plugins.WifiRemote
{
  internal class Helper
  {
    internal static bool IsNowPlaying()
    {
      bool isPlaying = false;
      if (ServiceRegistration.Get<IPlayerManager>().NumActiveSlots > 0)
        isPlaying = ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.CurrentPlayer != null;
        //isPlaying = ServiceRegistration.Get<IPlayerContextManager>().PrimaryPlayerContext.PlaybackState == PlaybackState.Playing;
      return isPlaying;
    }

    internal static async Task<MediaItem> GetMediaItemByIdAsync(Guid id)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);

      ISet<Guid> optionalMIATypes = new HashSet<Guid>();
      optionalMIATypes.Add(AudioAspect.ASPECT_ID);
      optionalMIATypes.Add(VideoAspect.ASPECT_ID);
      optionalMIATypes.Add(MovieAspect.ASPECT_ID);
      optionalMIATypes.Add(SeriesAspect.ASPECT_ID);


      IFilter searchFilter = new MediaItemIdFilter(id);
      MediaItemQuery searchQuery = new MediaItemQuery(necessaryMIATypes, optionalMIATypes, searchFilter) { Limit = 1 };
      IList<MediaItem> items = await ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory.SearchAsync(searchQuery, false, null, false);

      return items[0];
    }

    internal static async Task PlayMediaItemAsync(Guid mediaItemGuid, int startPos)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);

      ISet<Guid> optionalMIATypes = new HashSet<Guid>();
      optionalMIATypes.Add(AudioAspect.ASPECT_ID);
      optionalMIATypes.Add(VideoAspect.ASPECT_ID);
      optionalMIATypes.Add(MovieAspect.ASPECT_ID);
      optionalMIATypes.Add(SeriesAspect.ASPECT_ID);


      IFilter searchFilter = new MediaItemIdFilter(mediaItemGuid);
      MediaItemQuery searchQuery = new MediaItemQuery(necessaryMIATypes, optionalMIATypes, searchFilter) { Limit = 1 };
      IList<MediaItem> items = await ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory.SearchAsync(searchQuery, false, null, false);

      if (items.Count == 0)
      {
        ServiceRegistration.Get<ILogger>().Info("PlayFile: No media item found");
        return;
      }

      await PlayItemsModel.PlayItem(items[0]);

      SetPosition(startPos, true);
    }

    /// <summary>
    /// Set the player position to the given absolute percentage 
    /// </summary>
    /// <param name="position">position in %</param>
    /// <param name="absolute">absolute or relative to current position</param>
    internal static void SetPositionPercent(int position, bool absolute)
    {
      IPlayer player = ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.CurrentPlayer;
      IMediaPlaybackControl mediaPlaybackControl = player as IMediaPlaybackControl;
      if (mediaPlaybackControl != null)
      {
        if (absolute)
        {
          TimeSpan finalPosition = TimeSpan.FromSeconds(mediaPlaybackControl.Duration.TotalSeconds * ((float)position / 100));
          mediaPlaybackControl.CurrentTime = finalPosition;
        }
        else
        {
          TimeSpan finalPosition = TimeSpan.FromSeconds(mediaPlaybackControl.CurrentTime.TotalSeconds + (mediaPlaybackControl.Duration.TotalSeconds * ((float)position / 100)));
          mediaPlaybackControl.CurrentTime = finalPosition;
        }
      }
    }

    /// <summary>
    /// Set the player position to the given absolute time (in s)
    /// </summary>
    /// <param name="position">position in s</param>
    /// <param name="absolute">absolute or relative to current position</param>
    internal static void SetPosition(double position, bool absolute)
    {
      IPlayer player = ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.CurrentPlayer;
      IMediaPlaybackControl mediaPlaybackControl = player as IMediaPlaybackControl;
      if (mediaPlaybackControl != null)
      {
        if (absolute)
        {
          TimeSpan finalPosition = TimeSpan.FromSeconds(position);
          mediaPlaybackControl.CurrentTime = finalPosition;
        }
        else
        {
          TimeSpan finalPosition = TimeSpan.FromSeconds(mediaPlaybackControl.CurrentTime.TotalSeconds + position);
          mediaPlaybackControl.CurrentTime = finalPosition;
        }
      }
    }

    internal static string GetImageBaseURL(MediaItem item, string fanartType = null, string imageType = null)
    {
      string mediaType = fanartType ?? FanArtMediaTypes.Undefined;
      if (string.IsNullOrEmpty(fanartType))
      {
        if (item.Aspects.ContainsKey(ImageAspect.ASPECT_ID)) mediaType = FanArtMediaTypes.Image;
        else if (item.Aspects.ContainsKey(MovieAspect.ASPECT_ID)) mediaType = FanArtMediaTypes.Movie;
        else if (item.Aspects.ContainsKey(MovieCollectionAspect.ASPECT_ID)) mediaType = FanArtMediaTypes.MovieCollection;
        else if (item.Aspects.ContainsKey(SeriesAspect.ASPECT_ID)) mediaType = FanArtMediaTypes.Series;
        else if (item.Aspects.ContainsKey(SeasonAspect.ASPECT_ID)) mediaType = FanArtMediaTypes.SeriesSeason;
        else if (item.Aspects.ContainsKey(AudioAspect.ASPECT_ID)) mediaType = FanArtMediaTypes.Audio;
        else if (item.Aspects.ContainsKey(AudioAlbumAspect.ASPECT_ID)) mediaType = FanArtMediaTypes.Album;
        else if (item.Aspects.ContainsKey(EpisodeAspect.ASPECT_ID)) mediaType = FanArtMediaTypes.Episode;
        else if (item.Aspects.ContainsKey(CharacterAspect.ASPECT_ID)) mediaType = FanArtMediaTypes.Character;
      }
      string fanartImageType = imageType ?? FanArtTypes.Thumbnail;

      // Using MP2's FanArtService provides access to all kind of resources, thumbnails from ML and also local fanart from filesystem
      string url = string.Format("{0}/FanartService?mediatype={1}&fanarttype={2}&name={3}&width={4}&height={5}",
        GetBaseResourceURL(), mediaType, fanartImageType, item.MediaItemId,
        320, 480);
      return url;
    }

    internal static string GetBaseResourceURL()
    {
      var rs = ServiceRegistration.Get<IResourceServer>();
      return rs.GetServiceUrl(GetLocalIp());
    }

    internal static IPAddress GetLocalIp()
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
  }
}
