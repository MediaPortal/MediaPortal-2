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
using MediaPortal.Common.UserManagement;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Utilities;

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
      optionalMIATypes.Add(ImageAspect.ASPECT_ID);

      IFilter searchFilter = new MediaItemIdFilter(id);
      IList<MediaItem> items = await SearchAsync(necessaryMIATypes, optionalMIATypes, searchFilter, 1);

      return items[0];
    }

    internal static async Task<MediaItem> GetMediaItemByNameAsync(string name)
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
      optionalMIATypes.Add(ImageAspect.ASPECT_ID);

      IFilter searchFilter = new RelationalFilter(MediaAspect.ATTR_TITLE, RelationalOperator.EQ, name);
      IList<MediaItem> items = await SearchAsync(necessaryMIATypes, optionalMIATypes, searchFilter, 1);

      return items[0];
    }

    internal static async Task<MediaItem> GetMediaItemByMovieNameAsync(string movieName)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(VideoAspect.ASPECT_ID);
      necessaryMIATypes.Add(MovieAspect.ASPECT_ID);

      ISet<Guid> optionalMIATypes = new HashSet<Guid>();

      IFilter searchFilter = new RelationalFilter(MovieAspect.ATTR_MOVIE_NAME, RelationalOperator.EQ, movieName);
      IList<MediaItem> items = await SearchAsync(necessaryMIATypes, optionalMIATypes, searchFilter, 1);

      return items[0];
    }

    internal static async Task<MediaItem> GetMediaItemBySeriesNameAsync(string seriesName)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(SeriesAspect.ASPECT_ID);

      ISet<Guid> optionalMIATypes = new HashSet<Guid>();

      IFilter searchFilter = new RelationalFilter(SeriesAspect.ATTR_SERIES_NAME, RelationalOperator.EQ, seriesName);
      IList<MediaItem> items = await SearchAsync(necessaryMIATypes, optionalMIATypes, searchFilter, 1);

      return items[0];
    }

    internal static Task<IList<MediaItem>> GetMediaItemsByEpisodeAsync(Guid seriesId, int seasonNo, int episodeNo)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(EpisodeAspect.ASPECT_ID);

      ISet<Guid> optionalMIATypes = new HashSet<Guid>();

      IFilter filter = new RelationalFilter(EpisodeAspect.ATTR_SEASON, RelationalOperator.EQ, seasonNo);
      filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, new InFilter(EpisodeAspect.ATTR_EPISODE, new object[] { episodeNo }));
      return GetMediaItemsByGroupAsync(EpisodeAspect.ROLE_EPISODE, SeriesAspect.ROLE_SERIES, seriesId, necessaryMIATypes, optionalMIATypes, filter);
    }

    internal static Task<IList<MediaItem>> GetMediaItemsBySeriesAsync(Guid seriesId)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(EpisodeAspect.ASPECT_ID);

      ISet<Guid> optionalMIATypes = new HashSet<Guid>();

      return GetMediaItemsByGroupAsync(EpisodeAspect.ROLE_EPISODE, SeriesAspect.ROLE_SERIES, seriesId, necessaryMIATypes, optionalMIATypes);
    }

    internal static Task<IList<MediaItem>> GetMediaItemsByGroupAsync(Guid itemRole, Guid groupRole, Guid groupId, ISet<Guid> necessaryMIATypes, ISet<Guid> optionalMIATypes, IFilter filter = null)
    {
      if (filter != null)
        filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, new RelationshipFilter(itemRole, groupRole, groupId));
      else
        filter = new RelationshipFilter(itemRole, groupRole, groupId);
      return SearchAsync(necessaryMIATypes, optionalMIATypes, filter);
    }

    private static async Task<IList<MediaItem>> SearchAsync(ISet<Guid> necessaryMIATypes, ISet<Guid> optionalMIATypes, IFilter filter, uint? limit = null)
    {
      var scm = ServiceRegistration.Get<IServerConnectionManager>();
      var library = scm.ContentDirectory;
      var userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement.CurrentUser != null)
      {
        ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
        IDictionary<Guid, Share> serverShares = new Dictionary<Guid, Share>();
        ICollection<Share> shares = await library.GetSharesAsync(systemResolver.LocalSystemId, SharesFilter.All);
        var userFilter = userProfileDataManagement.CurrentUser.GetUserFilter(necessaryMIATypes, shares);
        filter = filter == null ? userFilter : userFilter != null ? BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, userFilter) : filter;

        MediaItemQuery searchQuery = new MediaItemQuery(necessaryMIATypes, optionalMIATypes, filter) { Limit = limit };
        return await library.SearchAsync(searchQuery, false, userProfileDataManagement.CurrentUser.ProfileId, false);
      }

      return new List<MediaItem>();
    }

    internal static async Task<IEnumerable<MediaItem>> LoadPlayListsAsync(Guid playlistId)
    {
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      Guid[] necessaryMIATypes = new Guid[]
      {
              ProviderResourceAspect.ASPECT_ID,
              MediaAspect.ASPECT_ID,
      };
      Guid[] optionalMIATypes = new Guid[]
      {
              AudioAspect.ASPECT_ID,
              VideoAspect.ASPECT_ID,
              ImageAspect.ASPECT_ID,
      };

      PlaylistRawData playlistData = await cd.ExportPlaylistAsync(playlistId);
      List<MediaItem> items = new List<MediaItem>();
      foreach (var cluster in CollectionUtils.Cluster(playlistData.MediaItemIds, 1000))
      {
        items.AddRange(await cd.LoadCustomPlaylistAsync(cluster, necessaryMIATypes, optionalMIATypes));
      }
      return items;
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
      optionalMIATypes.Add(ImageAspect.ASPECT_ID);

      IFilter searchFilter = new MediaItemIdFilter(mediaItemGuid);
      MediaItemQuery searchQuery = new MediaItemQuery(necessaryMIATypes, optionalMIATypes, searchFilter) { Limit = 1 };
      IList<MediaItem> items = await ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory.SearchAsync(searchQuery, false, null, false);

      if (items.Count == 0)
      {
        ServiceRegistration.Get<ILogger>().Info("PlayFile: No media item found");
        return;
      }

      await PlayItemsModel.PlayItem(items[0]);

      if (startPos > 0)
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
