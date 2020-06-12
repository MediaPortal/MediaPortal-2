using Emulators.Common.Games;
using Emulators.Common.Matchers;
using Emulators.Common.NameProcessing;
using Emulators.Common.WebRequests;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Emulators.Common.MobyGames
{
  class MobyGamesWrapper : BaseMatcher<GameMatch<string>, string, string, string>, IOnlineMatcher
  {
    #region Logger
    protected static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
    #endregion

    protected static readonly Guid MATCHER_ID = new Guid("DB465C66-A213-4EA3-8C80-DFD948524D18");
    protected static readonly string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\MobyGames\");
    protected const string COVERS_DIRECTORY = "Covers";
    protected const string COVERS_FRONT = "front";
    protected const string COVERS_BACK = "back";
    protected const int MAX_SEARCH_DISTANCE = 2;
    protected const string BASE_URL = "http://www.mobygames.com";
    protected const string SEARCH_PATH = "search/quick?q={0}&p={1}&search=Go&sFilter=1&sG=on";
    protected const string GET_PATH = "game";
    protected const string COVER_PATH = "cover-art";
    protected static readonly string _matchesSettingsFile = Path.Combine(CACHE_PATH, "Matches.xml");

    protected HtmlDownloader _downloader = new HtmlDownloader() { Encoding = Encoding.UTF8 };

    public Guid MatcherId
    {
      get { return MATCHER_ID; }
    }

    protected override string MatchesSettingsFile
    {
      get { return _matchesSettingsFile; }
    }

    public async Task<bool> FindAndUpdateGameAsync(GameInfo gameInfo)
    {
      if (!await InitAsync().ConfigureAwait(false))
        return false;

      MobyGamesResult result;
      if (TryGetFromStorage(gameInfo.GameName, gameInfo.Platform, out result))
      {
        Logger.Debug("MobyGames: Retrieved from cache: '{0}' - '{1}'", gameInfo.GameName, gameInfo.Platform);
        UpdateGameInfo(gameInfo, result);
        return true;
      }

      List<SearchResult> results;
      if (!Search(gameInfo.GameName, gameInfo.Platform, out results))
      {
        Logger.Debug("MobyGames: No results found for '{0}' - '{1}'", gameInfo.GameName, gameInfo.Platform);
        return false;
      }

      Logger.Debug("MobyGames: Found {0} search results for '{1}' - '{2}'", results.Count, gameInfo.GameName, gameInfo.Platform);
      results = results.FindAll(r => r.Title == gameInfo.GameName || NameProcessor.GetLevenshteinDistance(r.Title, gameInfo.GameName) <= MAX_SEARCH_DISTANCE);
      if (results.Count == 0 || !Get(results[0].Id, out result))
      {
        Logger.Debug("MobyGames: No close match found for: '{0}' - '{1}'", gameInfo.GameName, gameInfo.Platform);
        return false;
      }

      Logger.Debug("MobyGames: Matched '{0}' to '{1}' - '{2}'", results[0].Title, gameInfo.GameName, gameInfo.Platform);
      AddToStorage(gameInfo.GameName, gameInfo.Platform, result.Id);
      UpdateGameInfo(gameInfo, result);
      return true;
    }

    public bool TryGetImagePath(string id, ImageType imageType, out string path)
    {
      path = null;
      if (string.IsNullOrEmpty(id))
        return false;

      switch (imageType)
      {
        case ImageType.FrontCover:
          path = Path.Combine(CACHE_PATH, ReplaceForwardSlashes(id), COVERS_DIRECTORY, COVERS_FRONT);
          return true;
        case ImageType.BackCover:
          path = Path.Combine(CACHE_PATH, ReplaceForwardSlashes(id), COVERS_DIRECTORY, COVERS_BACK);
          return true;
        default:
          return false;
      }
    }

    public async Task DownloadFanArtAsync(string itemId)
    {
      string cache = CreateAndGetCacheName(itemId, "covers");
      string url = string.Format("{0}/{1}/{2}/{3}", BASE_URL, GET_PATH, itemId, COVER_PATH);
      MobyGamesCoverArt result = _downloader.Download<MobyGamesCoverArt>(url, cache);
      if (result == null)
        return;
      await DownloadCoverAsync(result.Front, itemId, COVERS_FRONT).ConfigureAwait(false);
      await DownloadCoverAsync(result.Back, itemId, COVERS_BACK).ConfigureAwait(false);
    }

    protected Task DownloadCoverAsync(string url, string id, string side)
    {
      if (string.IsNullOrEmpty(url))
        return Task.CompletedTask;
      url = string.Format("{0}{1}", BASE_URL, url);
      string filename = Path.GetFileName(new Uri(url).LocalPath);
      string downloadFile = CreateAndGetCacheName(id, Path.Combine(COVERS_DIRECTORY, side), filename);
      return _downloader.DownloadFileAsync(url, downloadFile);
    }

    protected bool Search(string searchTerm, string platform, out List<SearchResult> results)
    {
      results = null;
      if (string.IsNullOrEmpty(searchTerm) || string.IsNullOrEmpty(platform))
        return false;
      string query = string.Format(SEARCH_PATH, HttpUtility.UrlEncode(searchTerm), 9);
      string url = string.Format("{0}/{1}", BASE_URL, query);
      MobyGamesSearchResults searchResults = _downloader.Download<MobyGamesSearchResults>(url);
      if (searchResults != null && searchResults.Results.Count > 0)
        results = searchResults.Results;
      return results != null;
    }

    protected bool Get(string id, out MobyGamesResult result)
    {
      string cache = CreateAndGetCacheName(id, "details");
      string url = string.Format("{0}/{1}/{2}", BASE_URL, GET_PATH, id);
      result = _downloader.Download<MobyGamesResult>(url, cache);
      return result != null;
    }

    protected void UpdateGameInfo(GameInfo gameInfo, MobyGamesResult game)
    {
      gameInfo.GameName = game.Title;
      gameInfo.MatcherId = MatcherId;
      gameInfo.OnlineId = game.Id;
      gameInfo.Certification = game.ESRB;
      gameInfo.Description = game.Overview;
      gameInfo.Developer = game.Developer;
      gameInfo.Genres.AddRange(game.Genres);
      gameInfo.Rating = game.Rating;
      DateTime releaseDate;
      if (DateTime.TryParse(game.ReleaseDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out releaseDate))
        gameInfo.ReleaseDate = releaseDate;
    }

    protected void AddToStorage(string searchTerm, string platform, string id)
    {
      var onlineMatch = new GameMatch<string>
      {
        Id = id,
        ItemName = searchTerm,
        Platform = platform
      };
      _storage.TryAddMatch(onlineMatch);
    }

    protected bool TryGetFromStorage(string searchTerm, string platform, out MobyGamesResult result)
    {
      List<GameMatch<string>> matches = _storage.GetMatches();
      GameMatch<string> match = matches.Find(m =>
          string.Equals(m.ItemName, searchTerm, StringComparison.OrdinalIgnoreCase) &&
          string.Equals(m.Platform, platform, StringComparison.OrdinalIgnoreCase));

      if (match != null && !string.IsNullOrEmpty(match.Id))
        return Get(match.Id, out result);
      result = null;
      return false;
    }

    protected string CreateAndGetCacheName(string id, string category, string filename)
    {
      try
      {
        string folder = Path.Combine(CACHE_PATH, ReplaceForwardSlashes(id), category);
        if (!Directory.Exists(folder))
          Directory.CreateDirectory(folder);
        return Path.Combine(folder, filename);
      }
      catch
      {
        // TODO: logging
        return null;
      }
    }

    protected string CreateAndGetCacheName(string gameId, string category)
    {
      try
      {
        string folder = Path.Combine(CACHE_PATH, ReplaceForwardSlashes(gameId));
        if (!Directory.Exists(folder))
          Directory.CreateDirectory(folder);
        return Path.Combine(folder, string.Format("game_{0}.html", category));
      }
      catch
      {
        // TODO: logging
        return null;
      }
    }

    protected string ReplaceForwardSlashes(string input)
    {
      return input.Replace('/', Path.DirectorySeparatorChar);
    }
  }
}
