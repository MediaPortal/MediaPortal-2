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
using System.IO;
using MediaPortal.Utilities.DB;
using System.Linq;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.Aspects;

namespace MediaPortal.Plugins.WifiRemote
{
  internal class Helper
  {
    internal static bool IsNowPlaying()
    {
      bool isPlaying = false;
      if (ServiceRegistration.Get<IPlayerManager>().NumActiveSlots > 0)
        isPlaying = ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext?.CurrentPlayer != null;
        //isPlaying = ServiceRegistration.Get<IPlayerContextManager>().PrimaryPlayerContext.PlaybackState == PlaybackState.Playing;
      return isPlaying;
    }

    #region Media Items

    internal static async Task<MediaItem> GetMediaItemByIdAsync(Guid? userId, Guid id)
    {
      IFilter searchFilter = new MediaItemIdFilter(id);
      IList<MediaItem> items = await GetMediaItemsAsync(userId, searchFilter, 1);
      return items.FirstOrDefault();
    }

    internal static async Task<MediaItem> GetMediaItemByFileNameAsync(Guid? userId, string path)
    {
      string fileName = Path.GetFileName(path);
      IFilter searchFilter = new LikeFilter(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, "%" + SqlUtils.LikeEscape(fileName, '\\'), '\\', false);
      IList<MediaItem> items = await GetMediaItemsAsync(userId, searchFilter, 1);
      return items.FirstOrDefault();
    }

    private static Task<IList<MediaItem>> GetMediaItemsAsync(Guid? userId, IFilter filter, uint? limit = null, uint? offset = null, IList<ISortInformation> sort = null)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);

      ISet<Guid> optionalMIATypes = new HashSet<Guid>();
      optionalMIATypes.Add(AudioAspect.ASPECT_ID);
      optionalMIATypes.Add(VideoAspect.ASPECT_ID);
      optionalMIATypes.Add(MovieAspect.ASPECT_ID);
      optionalMIATypes.Add(EpisodeAspect.ASPECT_ID);
      optionalMIATypes.Add(ImageAspect.ASPECT_ID);
      optionalMIATypes.Add(RelationshipAspect.ASPECT_ID);
      optionalMIATypes.Add(GenreAspect.ASPECT_ID);

      return SearchAsync(userId, necessaryMIATypes, optionalMIATypes, filter, limit, offset, sort);
    }

    #endregion

    #region Movies

    internal static async Task<MediaItem> GetMovieByMovieNameAsync(Guid? userId, string movieName)
    {
      IFilter searchFilter = new RelationalFilter(MovieAspect.ATTR_MOVIE_NAME, RelationalOperator.EQ, movieName);
      IList<MediaItem> items = await GetMoviesAsync(userId, searchFilter, 1);
      return items.FirstOrDefault();
    }

    internal static Task<IList<MediaItem>> GetMoviesByMovieSearchAsync(Guid? userId, string movieSearch, uint? limit = null, uint? offset = null)
    {
      IFilter searchFilter = null;
      if (!string.IsNullOrEmpty(movieSearch))
        searchFilter = new LikeFilter(MovieAspect.ATTR_MOVIE_NAME, SqlUtils.LikeEscape(movieSearch, '\\') + "%", '\\', false);

      List<ISortInformation> sort = new List<ISortInformation>();
      sort.Add(new AttributeSortInformation(MovieAspect.ATTR_MOVIE_NAME, SortDirection.Ascending));

      return GetMoviesAsync(userId, searchFilter, limit, offset, sort);
    }

    private static Task<IList<MediaItem>> GetMoviesAsync(Guid? userId, IFilter filter = null, uint? limit = null, uint? offset = null, IList<ISortInformation> sort = null)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(VideoAspect.ASPECT_ID);
      necessaryMIATypes.Add(MovieAspect.ASPECT_ID);

      ISet<Guid> optionalMIATypes = new HashSet<Guid>();
      optionalMIATypes.Add(GenreAspect.ASPECT_ID);
      optionalMIATypes.Add(VideoStreamAspect.ASPECT_ID);

      return SearchAsync(userId, necessaryMIATypes, optionalMIATypes, filter, limit, offset, sort);
    }

    #endregion

    #region Videos

    internal static async Task<MediaItem> GetVideoByVideoNameAsync(Guid? userId, string videoName)
    {
      IFilter searchFilter = new RelationalFilter(MediaAspect.ATTR_TITLE, RelationalOperator.EQ, videoName);
      IList<MediaItem> items = await GetVideosAsync(userId, searchFilter, 1);
      return items.FirstOrDefault();
    }

    internal static Task<IList<MediaItem>> GetVideosByVideoSearchAsync(Guid? userId, string videoSearch, uint? limit = null, uint? offset = null)
    {
      IFilter searchFilter = null;
      if (!string.IsNullOrEmpty(videoSearch))
        searchFilter = new LikeFilter(MediaAspect.ATTR_TITLE, SqlUtils.LikeEscape(videoSearch, '\\') + "%", '\\', false);

      List<ISortInformation> sort = new List<ISortInformation>();
      sort.Add(new AttributeSortInformation(MediaAspect.ATTR_TITLE, SortDirection.Ascending));

      return GetVideosAsync(userId, searchFilter, limit, offset, sort);
    }

    private static Task<IList<MediaItem>> GetVideosAsync(Guid? userId, IFilter filter = null, uint? limit = null, uint? offset = null, IList<ISortInformation> sort = null)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(VideoAspect.ASPECT_ID);

      ISet<Guid> optionalMIATypes = new HashSet<Guid>();
      optionalMIATypes.Add(GenreAspect.ASPECT_ID);
      optionalMIATypes.Add(VideoStreamAspect.ASPECT_ID);

      return SearchAsync(userId, necessaryMIATypes, optionalMIATypes, filter, limit, offset, sort);
    }

    #endregion

    #region Recordings

    internal static async Task<MediaItem> GetRecordingByRecordingNameAsync(Guid? userId, string recordingName)
    {
      IFilter searchFilter = new RelationalFilter(MediaAspect.ATTR_TITLE, RelationalOperator.EQ, recordingName);
      IList<MediaItem> items = await GetRecordingsAsync(userId, searchFilter, 1);
      return items.FirstOrDefault();
    }

    internal static Task<IList<MediaItem>> GetRecordingsByRecordingSearchAsync(Guid? userId, string recordingSearch, uint? limit = null, uint? offset = null)
    {
      IFilter searchFilter = null;
      if (!string.IsNullOrEmpty(recordingSearch))
        searchFilter = new LikeFilter(MediaAspect.ATTR_TITLE, SqlUtils.LikeEscape(recordingSearch, '\\') + "%", '\\', false);

      List<ISortInformation> sort = new List<ISortInformation>();
      sort.Add(new AttributeSortInformation(MediaAspect.ATTR_TITLE, SortDirection.Ascending));

      return GetRecordingsAsync(userId, searchFilter, limit, offset, sort);
    }

    private static Task<IList<MediaItem>> GetRecordingsAsync(Guid? userId, IFilter filter = null, uint? limit = null, uint? offset = null, IList<ISortInformation> sort = null)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(VideoAspect.ASPECT_ID);
      necessaryMIATypes.Add(RecordingAspect.ASPECT_ID);

      ISet<Guid> optionalMIATypes = new HashSet<Guid>();
      optionalMIATypes.Add(GenreAspect.ASPECT_ID);
      optionalMIATypes.Add(VideoStreamAspect.ASPECT_ID);

      return SearchAsync(userId, necessaryMIATypes, optionalMIATypes, filter, limit, offset, sort);
    }

    #endregion

    #region Images

    internal static Task<IList<MediaItem>> GetImagesByImageSearchAsync(Guid? userId, string imageSearch, uint? limit = null, uint? offset = null)
    {
      IFilter searchFilter = null;
      if (!string.IsNullOrEmpty(imageSearch))
        searchFilter = new LikeFilter(MediaAspect.ATTR_TITLE, SqlUtils.LikeEscape(imageSearch, '\\') + "%", '\\', false);

      List<ISortInformation> sort = new List<ISortInformation>();
      sort.Add(new AttributeSortInformation(MediaAspect.ATTR_TITLE, SortDirection.Ascending));

      return GetImagesAsync(userId, searchFilter, limit, offset, sort);
    }

    private static Task<IList<MediaItem>> GetImagesAsync(Guid? userId, IFilter filter = null, uint? limit = null, uint? offset = null, IList<ISortInformation> sort = null)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImageAspect.ASPECT_ID);

      ISet<Guid> optionalMIATypes = new HashSet<Guid>();

      return SearchAsync(userId, necessaryMIATypes, optionalMIATypes, filter, limit, offset, sort);
    }

    #endregion

    #region Series

    internal static async Task<MediaItem> GetSeriesBySeriesNameAsync(Guid? userId, string seriesName)
    {
      IFilter searchFilter = new RelationalFilter(SeriesAspect.ATTR_SERIES_NAME, RelationalOperator.EQ, seriesName);
      IList<MediaItem> items = await GetSeriesAsync(userId, searchFilter, 1);
      return items.FirstOrDefault();
    }

    internal static Task<IList<MediaItem>> GetSeriesBySeriesSearchAsync(Guid? userId, string seriesSearch, uint? limit = null, uint? offset = null)
    {
      IFilter searchFilter = null;
      if (!string.IsNullOrEmpty(seriesSearch))
        searchFilter = new LikeFilter(SeriesAspect.ATTR_SERIES_NAME, SqlUtils.LikeEscape(seriesSearch, '\\') + "%", '\\', false);

      List<ISortInformation> sort = new List<ISortInformation>();
      sort.Add(new AttributeSortInformation(SeriesAspect.ATTR_SERIES_NAME, SortDirection.Ascending));

      return GetSeriesAsync(userId, searchFilter, limit, offset, sort);
    }

    private static Task<IList<MediaItem>> GetSeriesAsync(Guid? userId, IFilter filter = null, uint? limit = null, uint? offset = null, IList<ISortInformation> sort = null)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(SeriesAspect.ASPECT_ID);

      ISet<Guid> optionalMIATypes = new HashSet<Guid>();
      optionalMIATypes.Add(GenreAspect.ASPECT_ID);

      return SearchAsync(userId, necessaryMIATypes, optionalMIATypes, filter, limit, offset, sort);
    }

    internal static Task<IList<MediaItem>> GetSeasonsBySeriesIdAsync(Guid? userId, Guid seriesId)
    {
      List<ISortInformation> sort = new List<ISortInformation>();
      sort.Add(new AttributeSortInformation(SeasonAspect.ATTR_SEASON, SortDirection.Ascending));

      return GetSeasonsAsync(userId, seriesId, sort: sort);
    }

    private static Task<IList<MediaItem>> GetSeasonsAsync(Guid? userId, Guid seriesId, IFilter filter = null, IList<ISortInformation> sort = null)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(SeasonAspect.ASPECT_ID);

      ISet<Guid> optionalMIATypes = new HashSet<Guid>();
      optionalMIATypes.Add(RelationshipAspect.ASPECT_ID);

      return SearchByGroupAsync(userId, SeasonAspect.ROLE_SEASON, SeriesAspect.ROLE_SERIES, seriesId, necessaryMIATypes, optionalMIATypes, filter, sort: sort);
    }

    internal static async Task<MediaItem> GetEpisodeBySeriesEpisodeAsync(Guid? userId, Guid seriesId, int seasonNo, int episodeNo)
    {
      IFilter filter = new RelationalFilter(EpisodeAspect.ATTR_SEASON, RelationalOperator.EQ, seasonNo);
      filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, new InFilter(EpisodeAspect.ATTR_EPISODE, new object[] { episodeNo }));
      var items = await GetEpisodesAsync(userId, seriesId, filter);
      return items.FirstOrDefault();
    }

    internal static Task<IList<MediaItem>> GetEpisodesBySeriesIdAsync(Guid? userId, Guid seriesId)
    {
      List<ISortInformation> sort = new List<ISortInformation>();
      sort.Add(new AttributeSortInformation(EpisodeAspect.ATTR_SEASON, SortDirection.Ascending));
      sort.Add(new AttributeSortInformation(EpisodeAspect.ATTR_EPISODE, SortDirection.Ascending));

      return GetEpisodesAsync(userId, seriesId, sort: sort);
    }

    internal static Task<IList<MediaItem>> GetEpisodesBySeriesSeasonAsync(Guid? userId, Guid seriesId, int seasonNo)
    {
      IFilter filter = new RelationalFilter(EpisodeAspect.ATTR_SEASON, RelationalOperator.EQ, seasonNo);
      List<ISortInformation> sort = new List<ISortInformation>();
      sort.Add(new AttributeSortInformation(EpisodeAspect.ATTR_EPISODE, SortDirection.Ascending));

      return GetEpisodesAsync(userId, seriesId, filter, sort);
    }

    private static Task<IList<MediaItem>> GetEpisodesAsync(Guid? userId, Guid seriesId, IFilter filter = null, IList<ISortInformation> sort = null)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(EpisodeAspect.ASPECT_ID);

      ISet<Guid> optionalMIATypes = new HashSet<Guid>();
      optionalMIATypes.Add(RelationshipAspect.ASPECT_ID);
      optionalMIATypes.Add(VideoStreamAspect.ASPECT_ID);

      return SearchByGroupAsync(userId, EpisodeAspect.ROLE_EPISODE, SeriesAspect.ROLE_SERIES, seriesId, necessaryMIATypes, optionalMIATypes, filter, sort: sort);
    }

    #endregion

    #region Music

    internal static async Task<MediaItem> GetAlbumByAlbumNameAsync(Guid? userId, string albumName)
    {
      IFilter searchFilter = new RelationalFilter(AudioAlbumAspect.ATTR_ALBUM, RelationalOperator.EQ, albumName);
      IList<MediaItem> items = await GetAlbumsAsync(userId, searchFilter, 1);
      return items.FirstOrDefault();
    }

    internal static Task<IList<MediaItem>> GetAlbumsByAlbumSearchAsync(Guid? userId, string albumSearch, uint? limit = null, uint? offset = null)
    {
      IFilter searchFilter = null;
      if (!string.IsNullOrEmpty(albumSearch))
        searchFilter = new LikeFilter(AudioAlbumAspect.ATTR_ALBUM, SqlUtils.LikeEscape(albumSearch, '\\') + "%", '\\', false);

      List<ISortInformation> sort = new List<ISortInformation>();
      sort.Add(new AttributeSortInformation(AudioAlbumAspect.ATTR_ALBUM, SortDirection.Ascending));

      return GetAlbumsAsync(userId, searchFilter, limit, offset, sort);
    }

    private static Task<IList<MediaItem>> GetAlbumsAsync(Guid? userId, IFilter filter = null, uint? limit = null, uint? offset = null, IList<ISortInformation> sort = null)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(AudioAlbumAspect.ASPECT_ID);

      ISet<Guid> optionalMIATypes = new HashSet<Guid>();
      optionalMIATypes.Add(GenreAspect.ASPECT_ID);

      return SearchAsync(userId, necessaryMIATypes, optionalMIATypes, filter, limit, offset, sort);
    }

    internal static Task<IList<MediaItem>> GetTracksByTrackSearchAsync(Guid? userId, string trackSearch, uint? limit = null, uint? offset = null)
    {
      IFilter searchFilter = null;
      if (!string.IsNullOrEmpty(trackSearch))
        searchFilter = new LikeFilter(AudioAspect.ATTR_TRACKNAME, SqlUtils.LikeEscape(trackSearch, '\\') + "%", '\\', false);

      List<ISortInformation> sort = new List<ISortInformation>();
      sort.Add(new AttributeSortInformation(AudioAspect.ATTR_TRACKNAME, SortDirection.Ascending));

      return GetAlbumsAsync(userId, searchFilter, limit, offset, sort);
    }

    internal static Task<IList<MediaItem>> GetTrackByAlbumTrackAsync(Guid? userId, Guid albumId, int discNo, int trackNo)
    {
      IFilter filter = new RelationalFilter(AudioAspect.ATTR_DISCID, RelationalOperator.EQ, discNo);
      filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, new RelationalFilter(AudioAspect.ATTR_TRACK, RelationalOperator.EQ, trackNo));
      return GetTracksAsync(userId, albumId, filter);
    }

    internal static Task<IList<MediaItem>> GetTracksByAlbumIdAsync(Guid? userId, Guid albumId)
    {
      List<ISortInformation> sort = new List<ISortInformation>();
      sort.Add(new AttributeSortInformation(AudioAspect.ATTR_DISCID, SortDirection.Ascending));
      sort.Add(new AttributeSortInformation(AudioAspect.ATTR_TRACK, SortDirection.Ascending));

      return GetTracksAsync(userId, albumId, sort: sort);
    }

    private static Task<IList<MediaItem>> GetTracksAsync(Guid? userId, Guid albumId, IFilter filter = null, IList<ISortInformation> sort = null)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(AudioAspect.ASPECT_ID);

      ISet<Guid> optionalMIATypes = new HashSet<Guid>();
      optionalMIATypes.Add(GenreAspect.ASPECT_ID);
      optionalMIATypes.Add(RelationshipAspect.ASPECT_ID);

      return SearchByGroupAsync(userId, AudioAspect.ROLE_TRACK, AudioAlbumAspect.ROLE_ALBUM, albumId, necessaryMIATypes, optionalMIATypes, filter, sort: sort);
    }

    #endregion

    #region Search

    private static Task<IList<MediaItem>> SearchByGroupAsync(Guid? userId, Guid itemRole, Guid groupRole, Guid groupId, ISet<Guid> necessaryMIATypes, ISet<Guid> optionalMIATypes, IFilter filter = null, uint? limit = null, uint? offset = null, IList<ISortInformation> sort = null)
    {
      if (filter != null)
        filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, new RelationshipFilter(itemRole, groupRole, groupId));
      else
        filter = new RelationshipFilter(itemRole, groupRole, groupId);
      return SearchAsync(userId, necessaryMIATypes, optionalMIATypes, filter, limit, offset, sort);
    }

    private static async Task<IList<MediaItem>> SearchAsync(Guid? userId, ISet<Guid> necessaryMIATypes, ISet<Guid> optionalMIATypes, IFilter filter, uint? limit = null, uint? offset = null, IList<ISortInformation> sort = null)
    {
      var scm = ServiceRegistration.Get<IServerConnectionManager>();
      var library = scm.ContentDirectory;
      var userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      UserProfile user = null;
      if (userId == null)
        user = userProfileDataManagement?.CurrentUser;
      else
        user = (await userProfileDataManagement?.UserProfileDataManagement.GetProfileAsync(userId.Value))?.Result;
      if (user != null)
      {
        ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
        IDictionary<Guid, Share> serverShares = new Dictionary<Guid, Share>();
        var userFilter = user.GetUserFilter(necessaryMIATypes);
        filter = filter == null ? userFilter : userFilter != null ? BooleanCombinationFilter.CombineFilters(BooleanOperator.And, filter, userFilter) : filter;

        MediaItemQuery searchQuery = new MediaItemQuery(necessaryMIATypes, optionalMIATypes, filter)
        {
          Limit = limit,
          Offset = offset,
          SortInformation = sort
        };
        return await library.SearchAsync(searchQuery, false, user.ProfileId, false);
      }

      return new List<MediaItem>();
    }

    #endregion

    #region Playlists

    internal static async Task<(string Name, IEnumerable<MediaItem> Items)> LoadPlayListAsync(Guid playlistId)
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
              MovieAspect.ASPECT_ID,
              EpisodeAspect.ASPECT_ID,
              GenreAspect.ASPECT_ID,
              VideoStreamAspect.ASPECT_ID,
      };

      PlaylistRawData playlistData = await cd.ExportPlaylistAsync(playlistId);
      List<MediaItem> items = new List<MediaItem>();
      foreach (var cluster in CollectionUtils.Cluster(playlistData.MediaItemIds, 1000))
      {
        items.AddRange(await cd.LoadCustomPlaylistAsync(cluster, necessaryMIATypes, optionalMIATypes));
      }
      return (playlistData.Name, items);
    }

    internal static Task SavePlayListAsync(Guid playlistId, string name, string type, IEnumerable<Guid> items)
    {
      PlaylistRawData data = new PlaylistRawData(playlistId, name, type, items);
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      return cd.SavePlaylistAsync(data);
    }

    #endregion

    #region Playback

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
        ServiceRegistration.Get<ILogger>().Info("WifiRemote: PlayFile: No media item found");
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

    #endregion

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

    internal static string GetImageBaseURL(IChannel channel, string fanartType = null, string imageType = null)
    {
      string mediaType = fanartType ?? FanArtMediaTypes.ChannelTv;
      string fanartImageType = imageType ?? FanArtTypes.Banner;

      // Using MP2's FanArtService provides access to all kind of resources, thumbnails from ML and also local fanart from filesystem
      string url = string.Format("{0}/FanartService?mediatype={1}&fanarttype={2}&name={3}&width={4}&height={5}",
        GetBaseResourceURL(), mediaType, fanartImageType, channel.Name,
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
      Common.Services.ResourceAccess.Settings.ServerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<Common.Services.ResourceAccess.Settings.ServerSettings>();
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
