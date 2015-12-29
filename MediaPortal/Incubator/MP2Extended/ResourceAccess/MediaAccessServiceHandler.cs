using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.BaseClasses;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.FileSystem;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Filter;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.General;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Movie;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Picture;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Playlist;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess
{
  [ApiHandlerDescription(FriendlyName = "Media Access Service", Summary = "The Media Access Service allows you to access Tv Shows, Movies, Pictures and Music.")]
  internal class MediaAccessServiceHandler : BaseJsonHeader, IRequestModuleHandler
  {
    private readonly Dictionary<string, IRequestMicroModuleHandler> _requestModuleHandlers = new Dictionary<string, IRequestMicroModuleHandler>
    {
      // General
      { "GetExternalMediaInfo", new GetExternalMediaInfo() },
      { "GetFileInfo", new GetFileInfo() },
      { "GetLocalDiskInformation", new GetLocalDiskInformation() },
      { "GetMediaItem", new GetMediaItem() },
      { "GetServiceDescription", new GetServiceDescription() },
      { "TestConnection", new TestConnection() },
      // Filter
      { "CreateFilterString", new CreateFilterString() },
      { "GetFilterOperators", new GetFilterOperators() },
      { "GetFilterValues", new GetFilterValues() },
      { "GetFilterValuesByRange", new GetFilterValuesByRange() },
      { "GetFilterValuesCount", new GetFilterValuesCount() },
      // Movie
      { "GetMovieActorCount", new GetMovieActorCount() },
      { "GetMovieActors", new GetMovieActors() },
      { "GetMovieCount", new GetMovieCount() },
      { "GetMovieDetailedById", new GetMovieDetailedById() },
      { "GetMovieGenres", new GetMovieGenres() },
      { "GetMoviesBasic", new GetMoviesBasic() },
      { "GetMoviesBasicByRange", new GetMoviesBasicByRange() },
      { "GetMoviesDetailedByRange", new GetMoviesDetailedByRange() },
      // Music
      { "GetMusicAlbumBasicById", new GetMusicAlbumBasicById() },
      { "GetMusicArtistCount", new GetMusicArtistCount() },
      { "GetMusicArtistsBasic", new GetMusicArtistsBasic() },
      { "GetMusicArtistsBasicByRange", new GetMusicArtistsBasicByRange() },
      { "GetMusicGenres", new GetMusicGenres() },
      { "GetMusicTrackBasicById", new GetMusicTrackBasicById() },
      { "GetMusicTracksBasicForAlbum", new GetMusicTracksBasicForAlbum() },
      // OnlineVideos
      { "GetOnlineVideosCategoryVideos", new GetOnlineVideosCategoryVideos() },
      { "GetOnlineVideosGlobalSites", new GetOnlineVideosGlobalSites() },
      { "GetOnlineVideosSiteCategories", new GetOnlineVideosSiteCategories() },
      { "GetOnlineVideosSites", new GetOnlineVideosSites() },
      { "GetOnlineVideosSiteSettings", new GetOnlineVideosSiteSettings() },
      { "GetOnlineVideosSubCategories", new GetOnlineVideosSubCategories() },
      { "GetOnlineVideosVideoUrls", new GetOnlineVideosVideoUrls() },
      { "SetOnlineVideosSiteSetting", new SetOnlineVideosSiteSetting() },
      // Playlist
      { "AddPlaylistItem", new AddPlaylistItem() },
      { "AddPlaylistItems", new GetMusicTracksBasicForAlbum() },
      { "CreatePlaylist", new CreatePlaylist() },
      { "DeletePlaylist", new DeletePlaylist() },
      { "GetPlaylists", new GetPlaylists() },
      { "ClearAndAddPlaylistItems", new ClearAndAddPlaylistItems() },
      // Picture
      { "GetPictureCategories", new GetPictureCategories() },
      { "GetPictureCount", new GetPictureCount() },
      { "GetPictureDetailedById", new GetPictureDetailedById() },
      { "GetPicturesBasic", new GetPicturesBasic() },
      { "GetPicturesBasicByCategory", new GetPicturesBasicByCategory() },
      { "GetPicturesDetailed", new GetPicturesDetailed() },
      { "GetPictureSubCategories", new GetPictureSubCategories() },
      // TvShow
      { "GetTVEpisodeBasicById", new GetTVEpisodeBasicById() },
      { "GetTVEpisodeCount", new GetTVEpisodeCount() },
      { "GetTVEpisodeCountForSeason", new GetTVEpisodeCountForSeason() },
      { "GetTVEpisodeCountForTVShow", new GetTVEpisodeCountForTVShow() },
      { "GetTVEpisodeDetailedById", new GetTVEpisodeDetailedById() },
      { "GetTVEpisodesBasic", new GetTVEpisodesBasic() },
      { "GetTVEpisodesBasicForSeason", new GetTVEpisodesBasicForSeason() },
      { "GetTVEpisodesDetailedByRange", new GetTVEpisodesDetailedByRange() },
      { "GetTVEpisodesDetailedForSeason", new GetTVEpisodesDetailedForSeason() },
      { "GetTVSeasonCountForTVShow", new GetTVSeasonCountForTVShow() },
      { "GetTVSeasonsBasicForTVShow", new GetTVSeasonsBasicForTVShow() },
      { "GetTVSeasonsDetailedForTVShow", new GetTVSeasonsDetailedForTVShow() },
      { "GetTVShowCount", new GetTVShowCount() },
      { "GetTVShowDetailedById", new GetTVShowDetailedById() },
      { "GetTVShowGenres", new GetTVShowGenres() },
      { "GetTVShowsBasic", new GetTVShowsBasic() },
      { "GetTVShowsBasicByRange", new GetTVShowsBasicByRange() },
      // FileSystem
      { "GetFileSystemDriveBasicById", new GetFileSystemDriveBasicById() },
      { "GetFileSystemDriveCount", new GetFileSystemDriveCount() },
      { "GetFileSystemDrives", new GetFileSystemDrives() },
      { "GetFileSystemDrivesByRange", new GetFileSystemDrivesByRange() },
      { "GetFileSystemFileBasicById", new GetFileSystemFileBasicById() },
      { "GetFileSystemFiles", new GetFileSystemFiles() },
      { "GetFileSystemFilesAndFolders", new GetFileSystemFilesAndFolders() },
      { "GetFileSystemFilesAndFoldersByRange", new GetFileSystemFilesAndFoldersByRange() },
      { "GetFileSystemFilesAndFoldersCount", new GetFileSystemFilesAndFoldersCount() },
      { "GetFileSystemFilesByRange", new GetFileSystemFilesByRange() },
      { "GetFileSystemFilesCount", new GetFileSystemFilesCount() },
      { "GetFileSystemFolderBasicById", new GetFileSystemFolderBasicById() },
      { "GetFileSystemFolders", new GetFileSystemFolders() },
      { "GetFileSystemFoldersByRange", new GetFileSystemFoldersByRange() },
      { "GetFileSystemFoldersCount", new GetFileSystemFoldersCount() },
    };

    public bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
    {
      string[] uriParts = request.Uri.AbsolutePath.Split('/');
      string action = uriParts.Last();

      Logger.Info("MAS: AbsolutePath: {0}, uriParts.Length: {1}, Lastpart: {2}", request.Uri.AbsolutePath, uriParts.Length, action);

      // pass on to the micro processors
      IRequestMicroModuleHandler requestModuleHandler;
      dynamic returnValue = null;
      if (_requestModuleHandlers.TryGetValue(action, out requestModuleHandler))
        returnValue = requestModuleHandler.Process(request, session);

      if (returnValue == null)
      {
        Logger.Warn("MAS: Micromodule not found: {0}", action);
        throw new BadRequestException(String.Format("MAS: Micromodule not found: {0}", action));
      }

      // TODO: remove in final version
      Logger.Debug("MAS response: {0}", returnValue);

      byte[] output = ResourceAccessUtils.GetBytesFromDynamic(returnValue);

      // Send the response
      SendHeader(response, output.Length);

      response.SendBody(output);

      return true;
    }

    public Dictionary<string, object> GetRequestMicroModuleHandlers()
    {
      return _requestModuleHandlers.ToDictionary<KeyValuePair<string, IRequestMicroModuleHandler>, string, object>(module => module.Key, module => module.Value);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}