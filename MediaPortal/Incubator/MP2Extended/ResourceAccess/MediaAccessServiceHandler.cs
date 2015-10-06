using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.General;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Movie;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Playlist;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow;
using GetTVShowGenres = MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow.GetTVShowGenres;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess
{
  class MediaAccessServiceHandler : IRequestModuleHandler
  {
    private readonly Dictionary<string, IRequestMicroModuleHandler> _requestModuleHandlers = new Dictionary<string, IRequestMicroModuleHandler>
    {
      // General
      { "GetExternalMediaInfo", new GetExternalMediaInfo()},
      { "GetMediaItem", new GetMediaItem()},
      { "GetServiceDescription", new GetServiceDescription()},
      { "TestConnection", new TestConnection()},
      // Movie
      { "GetMovieDetailedById", new GetMovieDetailedById()},
      { "GetMovieGenres", new GetMovieGenres()},
      { "GetMoviesBasic", new GetMoviesBasic()},
      { "GetMoviesDetailedByRange", new GetMoviesDetailedByRange()},
      // Music
      { "GetMusicAlbumBasicById", new GetMusicAlbumBasicById()},
      { "GetMusicGenres", new GetMusicGenres()},
      { "GetMusicTrackBasicById", new GetMusicTrackBasicById()},
      { "GetMusicTracksBasicForAlbum", new GetMusicTracksBasicForAlbum()},
      // Playlist
      { "AddPlaylistItem", new AddPlaylistItem()},
      { "GetMusicTracksBasicForAlbum", new GetMusicTracksBasicForAlbum()},
      { "CreatePlaylist", new CreatePlaylist()},
      { "DeletePlaylist", new DeletePlaylist()},
      { "GetPlaylists", new GetPlaylists()},
      // TvShow
      { "GetTVEpisodeBasicById", new GetTVEpisodeBasicById()},
      { "GetTVEpisodeCount", new GetTVEpisodeCount()},
      { "GetTVEpisodeCountForSeason", new GetTVEpisodeCountForSeason()},
      { "GetTVEpisodeCountForTVShow", new GetTVEpisodeCountForTVShow()},
      { "GetTVEpisodeDetailedById", new GetTVEpisodeDetailedById()},
      { "GetTVEpisodesBasic", new GetTVEpisodesBasic()},
      { "GetTVEpisodesBasicForSeason", new GetTVEpisodesBasicForSeason()},
      { "GetTVEpisodesDetailedByRange", new GetTVEpisodesDetailedByRange()},
      { "GetTVEpisodesDetailedForSeason", new GetTVEpisodesDetailedForSeason()},
      { "GetTVSeasonCountForTVShow", new GetTVSeasonCountForTVShow()},
      { "GetTVSeasonsBasicForTVShow", new GetTVSeasonsBasicForTVShow()},
      { "GetTVSeasonsDetailedForTVShow", new GetTVSeasonsDetailedForTVShow()},
      { "GetTVShowCount", new GetTVShowCount()},
      { "GetTVShowDetailedById", new GetTVShowDetailedById()},
      { "GetTVShowGenres", new GetTVShowGenres()},
      { "GetTVShowsBasic", new GetTVShowsBasic()},
      { "GetTVShowsBasicByRange", new GetTVShowsBasicByRange()},
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
        returnValue = requestModuleHandler.Process(request);

      if (returnValue == null)
      {
        Logger.Warn("MAS: Micromodule not found: {0}", action);
        throw new BadRequestException(String.Format("MAS: Micromodule not found: {0}", action));
      }

      byte[] output = ResourceAccessUtils.GetBytesFromDynamic(returnValue);

      // Send the response
      response.Status = HttpStatusCode.OK;
      response.ContentType = "text/html";
      response.ContentLength = output.Length;
      response.SendHeaders();

      response.SendBody(output);

      return true;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
