using Emulators.Common.Games;
using Emulators.Common.Matchers;
using Emulators.Common.NameProcessing;
using Emulators.Common.TheGamesDb.Api;
using Emulators.Common.TheGamesDb.Data;
using Emulators.Common.TheGamesDb.Data.Platforms;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Emulators.Common.TheGamesDb
{
  /// <summary>
  /// Wrapper for v2 of TheGamesDb API.
  /// </summary>
  public class TheGamesDbWrapperV2 : BaseMatcher<GameMatch<int>, int, string, string>, IOnlineMatcher
  {
    #region Consts
    
    public const string API_KEY = "f45deae02380f9171ceb6b93db79bb6241109906da7e3dab29b91b6827fea3ee";

    protected static readonly Guid MATCHER_ID = new Guid("B38349A5-3A0A-435E-843C-DB771D5C5453");
    protected static readonly string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\TheGamesDBv2\");

    protected const int MAX_SEARCH_DISTANCE = 2;
    protected static readonly CultureInfo DATE_CULTURE = CultureInfo.CreateSpecificCulture("en-US");
    protected static readonly Regex REGEX_ID = new Regex(@"[\[\(]gg(\d+)[\)\]]", RegexOptions.IgnoreCase);
    protected static readonly string _matchesSettingsFile = Path.Combine(CACHE_PATH, "Matches.xml");
    protected const string PLATFORMS_JSON = "Emulators.Common.TheGamesDb.Data.Platforms.DefaultPlatforms.json";

    #endregion

    #region Protected Members 

    protected readonly SemaphoreSlim _initSyncObj = new SemaphoreSlim(1, 1);
    protected bool _isInit = false;
    protected TheGamesDbV2 _gamesDbApi = new TheGamesDbV2(API_KEY, CACHE_PATH);
    protected MemoryCache<int, GameResult> _memoryCache = new MemoryCache<int, GameResult>();

    protected static Dictionary<string, Platform> _platforms;
    protected Dictionary<string, NamedItem> _genres;
    protected Dictionary<string, NamedItem> _developers;

    #endregion

    #region Static Methods

    public static bool TryGetTGDBId(GameInfo gameInfo)
    {
      if (string.IsNullOrEmpty(gameInfo.GameName))
        return false;
      Match m = REGEX_ID.Match(gameInfo.GameName);
      if (m.Success)
      {
        gameInfo.GamesDbId = int.Parse(m.Groups[1].Value);
        return true;
      }
      return false;
    }

    #endregion

    #region Public Properties

    public static List<Platform> Platforms
    {
      get { return GetPlatforms().Values.OrderBy(p => p.Name).ToList(); }
    }

    public Guid MatcherId
    {
      get { return MATCHER_ID; }
    }

    protected override string MatchesSettingsFile
    {
      get { return _matchesSettingsFile; }
    }

    #endregion

    #region Public Methods

    public override async Task<bool> InitAsync()
    {
      await _initSyncObj.WaitAsync().ConfigureAwait(false);
      try
      {
        bool result = await base.InitAsync().ConfigureAwait(false);
        if (_isInit)
          return true;

        _genres = (await _gamesDbApi.GetGenresAsync().ConfigureAwait(false))?.Data?.Genres ?? new Dictionary<string, NamedItem>();
        _developers = (await _gamesDbApi.GetDevelopersAsync().ConfigureAwait(false))?.Data?.Developers ?? new Dictionary<string, NamedItem>();
        _isInit = true;
        return result;
      }
      finally
      {
        _initSyncObj.Release();
      }
    }

    public async Task<bool> FindAndUpdateGameAsync(GameInfo gameInfo)
    {
      if (!await InitAsync().ConfigureAwait(false))
        return false;

      Game game = await TryGetBestMatchAsync(gameInfo).ConfigureAwait(false);
      if (game == null)
        return false;

      gameInfo.GameName = game.GameTitle;
      gameInfo.GamesDbId = game.Id;
      gameInfo.PlatformId = game.Platform.ToString();
      gameInfo.MatcherId = MatcherId;
      gameInfo.OnlineId = game.Id.ToString();
      gameInfo.Certification = game.Rating;
      gameInfo.Description = game.Overview;
      //gameInfo.Rating = game.Rating;
      DateTime releaseDate;
      if (DateTime.TryParse(game.ReleaseDate, DATE_CULTURE, DateTimeStyles.None, out releaseDate))
        gameInfo.ReleaseDate = releaseDate;

      if (game.Genres != null && game.Genres.Length > 0)
        foreach (int genreId in game.Genres)
          if (_genres.TryGetValue(genreId.ToString(), out NamedItem genre))
            gameInfo.Genres.Add(genre.Name);

      if (game.Developers != null && game.Developers.Length > 0)
        foreach (int developerId in game.Developers)
          if (_developers.TryGetValue(developerId.ToString(), out NamedItem developer))
          {
            gameInfo.Developer = developer.Name;
            break;
          }

      return true;
    }

    public Task DownloadFanArtAsync(string itemId)
    {
      int gamesDbId;
      if (!int.TryParse(itemId, out gamesDbId))
        return Task.FromResult(false);
      return DownloadFanArtAsync(gamesDbId);
    }

    public bool TryGetImagePath(string id, ImageType imageType, out string path)
    {
      path = null;
      if (string.IsNullOrEmpty(id))
        return false;

      string type;
      string side;
      if (!TryGetImageType(imageType, out type, out side))
        return false;

      path = _gamesDbApi.GetGameImageCachePath(id, type, side);
      return true;
    }

    #endregion

    #region Protected Methods

    protected async Task<Game> TryGetBestMatchAsync(GameInfo gameInfo)
    {
      Game game;
      try
      {
        if (gameInfo.GamesDbId > 0)
        {
          GameResult result = await GetAsync(gameInfo.GamesDbId).ConfigureAwait(false);
          if (TryGetGameFromResult(result, gameInfo.GamesDbId, out game))
          {
            AddToStorage(game.GameTitle, GetPlatformName(game.Platform.ToString()), game.Id);
            return game;
          }
        }

        GameMatch<int> match;
        if (TryGetFromStorage(gameInfo, out match))
        {
          if (match.Id > 0)
          {
            Logger.Debug("TheGamesDb: Matched '{0}' to '{1}' from cache", gameInfo.GameName, match.ItemName);
            GameResult result = await GetAsync(match.Id).ConfigureAwait(false);
            if (TryGetGameFromResult(result, match.Id, out game))
              return game;
          }
          return null;
        }

        game = await TryGetClosestMatchAsync(gameInfo).ConfigureAwait(false);
        AddToStorage(gameInfo.GameName, gameInfo.Platform, game?.Id ?? 0);
        return game;
      }
      catch (Exception ex)
      {
        Logger.Debug("TheGamesDb: Exception processing game '{0}'", ex, gameInfo.GameName);
        return null;
      }
    }

    protected async Task<GameResult> GetAsync(int id)
    {
      if (id < 1)
        return null;

      GameResult result;
      if (_memoryCache.TryGetValue(id, out result))
        return result;

      result = await _gamesDbApi.GetGameAsync(id).ConfigureAwait(false);
      if (!IsValid(result))
        return null;

      _memoryCache.Add(id, result);
      return result;
    }

    protected async Task<Game> TryGetClosestMatchAsync(GameInfo gameInfo)
    {
      Logger.Debug("TheGamesDb: Searching for '{0}', {1}", gameInfo.GameName, gameInfo.Platform);
      GameResult result = await _gamesDbApi.SearchGameByNameAsync(gameInfo.GameName, GetPlatformId(gameInfo.Platform)).ConfigureAwait(false);
      if (!IsValid(result))
        return null;

      Game game = result.Data.Games.FirstOrDefault(g => NameProcessor.AreStringsEqual(g.GameTitle, gameInfo.GameName, MAX_SEARCH_DISTANCE) ||
        (g.Alternates != null && g.Alternates.Any(a => NameProcessor.AreStringsEqual(a, gameInfo.GameName, MAX_SEARCH_DISTANCE))));

      if (game != null)
      {
        Logger.Debug("TheGamesDb: Matched '{0}' to '{1}'", gameInfo.GameName, game.GameTitle);
        _gamesDbApi.CacheGame(game.Id, result);
        return game;
      }

      Logger.Debug("TheGamesDb: No match found for: '{0}'", gameInfo.GameName);
      return null;
    }

    protected static bool TryGetImageType(ImageType imageType, out string type, out string side)
    {
      side = null;
      switch (imageType)
      {
        case ImageType.FrontCover:
          type = TheGamesDbV2.IMAGE_TYPE_BOXART;
          side = TheGamesDbV2.IMAGE_SIDE_FRONT;
          return true;
        case ImageType.BackCover:
          type = TheGamesDbV2.IMAGE_TYPE_BOXART;
          side = TheGamesDbV2.IMAGE_SIDE_BACK;
          return true;
        case ImageType.Fanart:
          type = TheGamesDbV2.IMAGE_TYPE_FANART;
          return true;
        case ImageType.Screenshot:
          type = TheGamesDbV2.IMAGE_TYPE_SCREENSHOT;
          return true;
        case ImageType.Banner:
          type = TheGamesDbV2.IMAGE_TYPE_BANNER;
          return true;
        case ImageType.ClearLogo:
          type = TheGamesDbV2.IMAGE_TYPE_CLEARLOGO;
          return true;
        default:
          type = null;
          return false;
      }
    }

    protected static bool IsValid(GameResult result)
    {
      return result != null && result.Data != null && result.Data.Games != null && result.Data.Games.Length > 0;
    }

    protected static bool TryGetGameFromResult(GameResult result, int gameId, out Game game)
    {
      if (IsValid(result))
        game = result.Data.Games.FirstOrDefault(g => g.Id == gameId);
      else
        game = null;
      return game != null;
    }

    protected async Task DownloadFanArtAsync(int gameId)
    {
      ImageResult result = await _gamesDbApi.GetGameImagesAsync(gameId).ConfigureAwait(false);

      if (result == null || result.Data == null || result.Data.BaseUrl == null || result.Data.Images == null)
        return;

      Image[] images;
      if (!result.Data.Images.TryGetValue(gameId.ToString(), out images) || images == null)
        return;

      ServiceRegistration.Get<ILogger>().Debug("GameTheGamesDbWrapper Download: Begin saving images for game {0}", gameId);

      string baseUrl = result.Data.BaseUrl.Large;
      foreach (Image image in images)
        await _gamesDbApi.DownloadImageAsync(gameId, baseUrl, image).ConfigureAwait(false);

      ServiceRegistration.Get<ILogger>().Debug("GameTheGamesDbWrapper Download: Finished saving images for game {0}", gameId);
    }

    protected static Dictionary<string, Platform> GetPlatforms()
    {
      if (_platforms == null)
      {
        using (StreamReader reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(PLATFORMS_JSON)))
          _platforms = JsonConvert.DeserializeObject<Dictionary<string, Platform>>(reader.ReadToEnd());
      }
      return _platforms;
    }

    protected static string GetPlatformId(string platformName)
    {
      Platform platform = GetPlatforms().Values.FirstOrDefault(p => p.Name == platformName);
      return platform != null ? platform.Id.ToString() : null;
    }

    protected static string GetPlatformName(string platformId)
    {
      return GetPlatforms().TryGetValue(platformId, out Platform platform) ? platform.Name : null;
    }

    #endregion

    #region Storage

    protected void AddToStorage(string searchTerm, string platform, int id)
    {
      var onlineMatch = new GameMatch<int>
      {
        Id = id,
        ItemName = string.Format("{0}:{1}", searchTerm, platform),
        GameName = searchTerm,
        Platform = platform
      };
      _storage.TryAddMatch(onlineMatch);
    }

    protected bool TryGetFromStorage(GameInfo gameInfo, out GameMatch<int> match)
    {
      List<GameMatch<int>> matches = _storage.GetMatches();
      match = matches.Find(m =>
          string.Equals(m.GameName, gameInfo.GameName, StringComparison.OrdinalIgnoreCase) &&
          string.Equals(m.Platform, gameInfo.Platform, StringComparison.OrdinalIgnoreCase));
      return match != null;
    }

    #endregion
  }
}
