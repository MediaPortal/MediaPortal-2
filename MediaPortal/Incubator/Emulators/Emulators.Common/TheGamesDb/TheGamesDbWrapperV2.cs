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
using Emulators.Common.FanartProvider;
using MediaPortal.Common.FanArt;

namespace Emulators.Common.TheGamesDb
{
  /// <summary>
  /// Wrapper for v2 of TheGamesDb API.
  /// </summary>
  public class TheGamesDbWrapperV2 : BaseMediaMatcher<GameMatch<int>, int, string, string>, IOnlineMatcher
  {
    #region Consts
    
    public const string API_KEY = "f45deae02380f9171ceb6b93db79bb6241109906da7e3dab29b91b6827fea3ee";

    protected static readonly Guid MATCHER_ID = new Guid("B38349A5-3A0A-435E-843C-DB771D5C5453");
    protected static readonly string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\TheGamesDBv2\");

    protected const int MAX_SEARCH_DISTANCE = 2;
    protected static readonly CultureInfo DATE_CULTURE = CultureInfo.CreateSpecificCulture("en-US");
    protected static readonly Regex REGEX_ID = new Regex(@"(?<name>.*)[\[\(]gg(?<id>\d+)[\)\]]", RegexOptions.IgnoreCase);
    protected static readonly string _matchesSettingsFile = Path.Combine(CACHE_PATH, "Matches.xml");
    protected const string PLATFORMS_JSON = "Emulators.Common.TheGamesDb.Data.Platforms.DefaultPlatforms.json";

    protected static readonly Dictionary<int, string> PLATFORM_MAP = new Dictionary<int, string>
    {
      { 1, GameInfo.PLATFORM_PC },
      { 2, GameInfo.PLATFORM_NINTENDO_GAMECUBE },
      { 3, GameInfo.PLATFORM_NINTENDO_64 },
      { 4, GameInfo.PLATFORM_NINTENDO_GAME_BOY },
      { 5, GameInfo.PLATFORM_NINTENDO_GAME_BOY_ADVANCE },
      { 6, GameInfo.PLATFORM_NINTENDO_SNES },
      { 7, GameInfo.PLATFORM_NINTENDO_NES },
      { 8, GameInfo.PLATFORM_NINTENDO_DS },
      { 9, GameInfo.PLATFORM_NINTENDO_WII },
      { 10, GameInfo.PLATFORM_SONY_PLAYSTATION },
      { 11, GameInfo.PLATFORM_SONY_PLAYSTATION_2 },
      { 12, GameInfo.PLATFORM_SONY_PLAYSTATION_3 },
      { 13, GameInfo.PLATFORM_SONY_PLAYSTATION_PORTABLE },
      { 14, GameInfo.PLATFORM_MICROSOFT_XBOX },
      { 15, GameInfo.PLATFORM_MICROSOFT_XBOX_360 },
      { 16, GameInfo.PLATFORM_SEGA_DREAMCAST },
      { 17, GameInfo.PLATFORM_SEGA_SATURN },
      { 18, GameInfo.PLATFORM_SEGA_GENESIS },
      { 20, GameInfo.PLATFORM_SEGA_GAME_GEAR },
      { 21, GameInfo.PLATFORM_SEGA_CD },
      { 22, GameInfo.PLATFORM_ATARI_2600 },
      { 23, GameInfo.PLATFORM_ARCADE },
      { 24, GameInfo.PLATFORM_NEO_GEO },
      { 25, GameInfo.PLATFORM_3DO },
      { 26, GameInfo.PLATFORM_ATARI_5200 },
      { 27, GameInfo.PLATFORM_ATARI_7800 },
      { 28, GameInfo.PLATFORM_ATARI_JAGUAR },
      { 29, GameInfo.PLATFORM_ATARI_JAGUAR_CD },
      { 30, GameInfo.PLATFORM_ATARI_XE },
      { 31, GameInfo.PLATFORM_COLECOVISION },
      { 32, GameInfo.PLATFORM_INTELLIVISION },
      { 33, GameInfo.PLATFORM_SEGA_32X },
      { 34, GameInfo.PLATFORM_TURBOGRAFX_16 },
      { 35, GameInfo.PLATFORM_SEGA_MASTER_SYSTEM },
      { 36, GameInfo.PLATFORM_SEGA_MEGA_DRIVE },
      { 37, GameInfo.PLATFORM_MAC_OS },
      { 38, GameInfo.PLATFORM_NINTENDO_WII_U },
      { 39, GameInfo.PLATFORM_SONY_PLAYSTATION_VITA },
      { 40, GameInfo.PLATFORM_COMMODORE_64 },
      { 41, GameInfo.PLATFORM_NINTENDO_GAME_BOY_COLOR },
      { 4911,  GameInfo.PLATFORM_AMIGA },
      { 4912,  GameInfo.PLATFORM_NINTENDO_3DS },
      { 4913,  GameInfo.PLATFORM_SINCLAIR_ZX_SPECTRUM },
      { 4914,  GameInfo.PLATFORM_AMSTRAD_CPC },
      { 4915,  GameInfo.PLATFORM_IOS },
      { 4916,  GameInfo.PLATFORM_ANDROID },
      { 4917,  GameInfo.PLATFORM_PHILIPS_CDI },
      { 4918,  GameInfo.PLATFORM_NINTENDO_VIRTUAL_BOY },
      { 4919,  GameInfo.PLATFORM_SONY_PLAYSTATION_4 },
      { 4920,  GameInfo.PLATFORM_MICROSOFT_XBOX_ONE },
      { 4921,  GameInfo.PLATFORM_OUYA },
      { 4922,  GameInfo.PLATFORM_NEO_GEO_POCKET },
      { 4923,  GameInfo.PLATFORM_NEO_GEO_POCKET_COLOR },
      { 4924,  GameInfo.PLATFORM_ATARI_LYNX },
      { 4925,  GameInfo.PLATFORM_WONDERSWAN },
      { 4926,  GameInfo.PLATFORM_WONDERSWAN_COLOR },
      { 4927,  GameInfo.PLATFORM_MAGNAVOX_ODYSSEY_2 },
      { 4928,  GameInfo.PLATFORM_FAIRCHILD_CHANNEL_F },
      { 4929,  GameInfo.PLATFORM_MSX },
      { 4930,  GameInfo.PLATFORM_PC_FX },
      { 4931,  GameInfo.PLATFORM_SHARP_X68000 },
      { 4932,  GameInfo.PLATFORM_FM_TOWNS_MARTY },
      { 4933,  GameInfo.PLATFORM_PC_88 },
      { 4934,  GameInfo.PLATFORM_PC_98 },
      { 4935,  GameInfo.PLATFORM_NUON },
      { 4936,  GameInfo.PLATFORM_FAMICOM_DISK_SYSTEM },
      { 4937,  GameInfo.PLATFORM_ATARI_ST },
      { 4938,  GameInfo.PLATFORM_N_GAGE },
      { 4939,  GameInfo.PLATFORM_VECTREX },
      { 4940,  GameInfo.PLATFORM_GAME_COM },
      { 4941,  GameInfo.PLATFORM_TRS_80_COLOR_COMPUTER },
      { 4942,  GameInfo.PLATFORM_APPLE_II },
      { 4943,  GameInfo.PLATFORM_ATARI_800 },
      { 4944,  GameInfo.PLATFORM_ACORN_ARCHIMEDES },
      { 4945,  GameInfo.PLATFORM_COMMODORE_VIC_20 },
      { 4946,  GameInfo.PLATFORM_COMMODORE_128 },
      { 4947,  GameInfo.PLATFORM_AMIGA_CD32 },
      { 4948,  GameInfo.PLATFORM_MEGA_DUCK },
      { 4949,  GameInfo.PLATFORM_SEGA_SG_1000 },
      { 4950,  GameInfo.PLATFORM_GAME_WATCH },
      { 4951,  GameInfo.PLATFORM_HANDHELD_ELECTRONIC_GAMES_LCD },
      { 4952,  GameInfo.PLATFORM_DRAGON_32_64 },
      { 4953,  GameInfo.PLATFORM_TEXAS_INSTRUMENTS_TI_99_4A },
      { 4954,  GameInfo.PLATFORM_ACORN_ELECTRON },
      { 4955,  GameInfo.PLATFORM_TURBOGRAFX_CD },
      { 4956,  GameInfo.PLATFORM_NEO_GEO_CD },
      { 4957,  GameInfo.PLATFORM_NINTENDO_POKEMON_MINI },
      { 4958,  GameInfo.PLATFORM_SEGA_PICO },
      { 4959,  GameInfo.PLATFORM_WATARA_SUPERVISION },
      { 4960,  GameInfo.PLATFORM_TOMY_TUTOR },
      { 4961,  GameInfo.PLATFORM_MAGNAVOX_ODYSSEY_1 },
      { 4962,  GameInfo.PLATFORM_GAKKEN_COMPACT_VISION },
      { 4963,  GameInfo.PLATFORM_EMERSON_ARCADIA_2001 },
      { 4964,  GameInfo.PLATFORM_CASIO_PV_1000 },
      { 4965,  GameInfo.PLATFORM_EPOCH_CASSETTE_VISION },
      { 4966,  GameInfo.PLATFORM_EPOCH_SUPER_CASSETTE_VISION },
      { 4967,  GameInfo.PLATFORM_RCA_STUDIO_II },
      { 4968,  GameInfo.PLATFORM_BALLY_ASTROCADE },
      { 4969,  GameInfo.PLATFORM_APF_MP_1000 },
      { 4970,  GameInfo.PLATFORM_COLECO_TELSTAR_ARCADE },
      { 4971,  GameInfo.PLATFORM_NINTENDO_SWITCH },
      { 4972,  GameInfo.PLATFORM_MILTON_BRADLEY_MICROVISION },
      { 4973,  GameInfo.PLATFORM_ENTEX_SELECT_A_GAME },
      { 4974,  GameInfo.PLATFORM_ENTEX_ADVENTURE_VISION },
      { 4975,  GameInfo.PLATFORM_PIONEER_LASERACTIVE },
      { 4976,  GameInfo.PLATFORM_ACTION_MAX },
      { 4977,  GameInfo.PLATFORM_SHARP_X1 },
      { 4978,  GameInfo.PLATFORM_FUJITSU_FM_7 },
      { 4979,  GameInfo.PLATFORM_SAM_COUPE },
    };

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
      if (string.IsNullOrEmpty(gameInfo.SearchName))
        return false;
      Match m = REGEX_ID.Match(gameInfo.SearchName);
      if (m.Success)
      {
        gameInfo.SearchName = m.Groups["name"].Value.Trim();
        gameInfo.GamesDbId = int.Parse(m.Groups["id"].Value);
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
      try
      {
        if (!await InitAsync().ConfigureAwait(false))
          return false;

        Game game = await TryGetBestMatchAsync(gameInfo).ConfigureAwait(false);
        if (game == null)
          return false;

        GameInfo foundGame = new GameInfo();
        foundGame.GameName = game.GameTitle;
        gameInfo.GamesDbId = game.Id;
        if (game.Rating != null)
        {
          if (game.Rating.Contains("10+"))
            foundGame.Certification = "ESRB_E10+";
          else if (game.Rating.StartsWith("E"))
            foundGame.Certification = "ESRB_E";
          else if (game.Rating.StartsWith("T"))
            foundGame.Certification = "ESRB_T";
          else if (game.Rating.StartsWith("M"))
            foundGame.Certification = "ESRB_M";
          else if (game.Rating.StartsWith("A"))
            foundGame.Certification = "ESRB_A";
        }
        foundGame.Description = game.Overview;
        //foundGame.Rating = game.Rating;

        if (DateTime.TryParse(game.ReleaseDate, DATE_CULTURE, DateTimeStyles.None, out DateTime releaseDate))
          foundGame.ReleaseDate = releaseDate;

        if (game.Genres != null && game.Genres.Length > 0)
        {
          foreach (int genreId in game.Genres)
            if (_genres.TryGetValue(genreId.ToString(), out NamedItem genre))
              foundGame.Genres.Add(genre.Name);
        }

        if (game.Developers != null && game.Developers.Length > 0)
        {
          foreach (int developerId in game.Developers)
            if (_developers.TryGetValue(developerId.ToString(), out NamedItem developer))
            {
              foundGame.Developer = developer.Name;
              break;
            }
        }

        gameInfo.Merge(foundGame);
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("TheGamesDb: Exception while processing game {0}", ex, gameInfo.SearchName);
      }

      return false;
    }

    public async Task<bool> DownloadFanArtAsync(Guid mediaItemId, GameInfo gameInfo)
    {
      if (await SaveOldFanArtAsync(mediaItemId, gameInfo.GamesDbId, gameInfo.GameName))
        return true;

      return await DownloadFanArtAsync(mediaItemId, gameInfo.GamesDbId, gameInfo.GameName);
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
            AddToStorage(gameInfo.SearchName, PLATFORM_MAP[game.Platform], game.Id);
            return game;
          }
        }

        GameMatch<int> match;
        if (TryGetFromStorage(gameInfo, out match))
        {
          if (match.Id > 0)
          {
            Logger.Debug("TheGamesDb: Matched '{0}' to '{1}' from cache", gameInfo.SearchName, match.ItemName);
            GameResult result = await GetAsync(match.Id).ConfigureAwait(false);
            if (TryGetGameFromResult(result, match.Id, out game))
              return game;
          }
          return null;
        }

        game = await TryGetClosestMatchAsync(gameInfo).ConfigureAwait(false);
        AddToStorage(gameInfo.SearchName, gameInfo.Platform, game?.Id ?? 0);
        return game;
      }
      catch (Exception ex)
      {
        Logger.Debug("TheGamesDb: Exception processing game '{0}'", ex, gameInfo.SearchName);
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
      Logger.Debug("TheGamesDb: Searching for '{0}', {1}", gameInfo.SearchName, gameInfo.Platform);
      int platformId = PLATFORM_MAP.FirstOrDefault(p => p.Value == gameInfo.Platform).Key;
      if (platformId == 0)
      {
        Logger.Debug("TheGamesDb: Invalid platform {0} for game: '{1}'", gameInfo.Platform, gameInfo.SearchName);
        return null;
      }

      GameResult result = await _gamesDbApi.SearchGameByNameAsync(gameInfo.SearchName, platformId).ConfigureAwait(false);
      if (!IsValid(result))
        return null;

      Game game = result.Data.Games.FirstOrDefault(g => NameProcessor.AreStringsEqual(g.GameTitle, gameInfo.SearchName, MAX_SEARCH_DISTANCE) ||
        (g.Alternates != null && g.Alternates.Any(a => NameProcessor.AreStringsEqual(a, gameInfo.SearchName, MAX_SEARCH_DISTANCE))));

      if (game != null)
      {
        Logger.Debug("TheGamesDb: Matched '{0}' to '{1}'", gameInfo.SearchName, game.GameTitle);
        _gamesDbApi.CacheGame(game.Id, result);
        return game;
      }

      Logger.Debug("TheGamesDb: No match found for: '{0}'", gameInfo.SearchName);
      return null;
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

    protected bool HasFanArt(Guid mediaItemId, string fanArtType)
    {
      IFanArtCache fanArtCache = ServiceRegistration.Get<IFanArtCache>();
      return fanArtCache.GetFanArtFiles(mediaItemId, fanArtType).Any();
    }

    protected async Task<bool> SaveOldFanArtAsync(Guid mediaItemId, int gameId, string gameName)
    {
      ServiceRegistration.Get<ILogger>().Debug("GameTheGamesDbWrapper Download: Begin saving old images for game {0}", gameId);

      bool found = false;
      var frontCoverPath = _gamesDbApi.GetGameImageCachePath(gameId.ToString(), TheGamesDbV2.IMAGE_TYPE_BOXART, TheGamesDbV2.IMAGE_SIDE_FRONT);
      if (File.Exists(frontCoverPath) && !HasFanArt(mediaItemId, FanArtTypes.Cover))
      {
        found = true;
        await SaveOldImagesAsync(frontCoverPath, mediaItemId, gameId, gameName, FanArtTypes.Cover, TheGamesDbV2.IMAGE_TYPE_BOXART, TheGamesDbV2.IMAGE_SIDE_FRONT).ConfigureAwait(false);
      }
      var backCoverPath = _gamesDbApi.GetGameImageCachePath(gameId.ToString(), TheGamesDbV2.IMAGE_TYPE_BOXART, TheGamesDbV2.IMAGE_SIDE_BACK);
      if (File.Exists(backCoverPath) && !HasFanArt(mediaItemId, GameFanartTypes.BackCover))
      {
        found = true;
        await SaveOldImagesAsync(backCoverPath, mediaItemId, gameId, gameName, GameFanartTypes.BackCover, TheGamesDbV2.IMAGE_TYPE_BOXART, TheGamesDbV2.IMAGE_SIDE_BACK).ConfigureAwait(false);
      }
      var bannerPath = _gamesDbApi.GetGameImageCachePath(gameId.ToString(), TheGamesDbV2.IMAGE_TYPE_BANNER, null);
      if (File.Exists(bannerPath) && !HasFanArt(mediaItemId, FanArtTypes.Banner))
      {
        found = true;
        await SaveOldImagesAsync(bannerPath, mediaItemId, gameId, gameName, FanArtTypes.Banner, TheGamesDbV2.IMAGE_TYPE_BANNER).ConfigureAwait(false);
      }
      var fanartPath = _gamesDbApi.GetGameImageCachePath(gameId.ToString(), TheGamesDbV2.IMAGE_TYPE_FANART, null);
      if (File.Exists(fanartPath) && !HasFanArt(mediaItemId, FanArtTypes.FanArt))
      {
        found = true;
        await SaveOldImagesAsync(fanartPath, mediaItemId, gameId, gameName, FanArtTypes.FanArt, TheGamesDbV2.IMAGE_TYPE_FANART).ConfigureAwait(false);
      }
      var logoPath = _gamesDbApi.GetGameImageCachePath(gameId.ToString(), TheGamesDbV2.IMAGE_TYPE_CLEARLOGO, null);
      if (File.Exists(logoPath) && !HasFanArt(mediaItemId, FanArtTypes.Logo))
      {
        found = true;
        await SaveOldImagesAsync(logoPath, mediaItemId, gameId, gameName, FanArtTypes.Logo, TheGamesDbV2.IMAGE_TYPE_CLEARLOGO).ConfigureAwait(false);
      }
      var screenshotPath = _gamesDbApi.GetGameImageCachePath(gameId.ToString(), TheGamesDbV2.IMAGE_TYPE_SCREENSHOT, null);
      if (File.Exists(screenshotPath) && !HasFanArt(mediaItemId, GameFanartTypes.ScreenShot))
      {
        found = true;
        await SaveOldImagesAsync(screenshotPath, mediaItemId, gameId, gameName, GameFanartTypes.ScreenShot, TheGamesDbV2.IMAGE_TYPE_SCREENSHOT).ConfigureAwait(false);
      }

      ServiceRegistration.Get<ILogger>().Debug("GameTheGamesDbWrapper Download: Finished saving old images for game {0}", gameId);
      return found;
    }

    protected async Task<bool> DownloadFanArtAsync(Guid mediaItemId, int gameId, string gameName)
    {
      ImageResult result = await _gamesDbApi.GetGameImagesAsync(gameId).ConfigureAwait(false);

      if (result?.Data?.BaseUrl == null || result?.Data?.Images == null)
        return false;

      Image[] images;
      if (!result.Data.Images.TryGetValue(gameId.ToString(), out images) || images == null)
        return false;

      ServiceRegistration.Get<ILogger>().Debug("GameTheGamesDbWrapper Download: Begin saving images for game {0}", gameId);

      string baseUrl = result.Data.BaseUrl.Large;
      if (!HasFanArt(mediaItemId, FanArtTypes.Cover))
        await SaveImagesAsync(images, mediaItemId, gameId, gameName, baseUrl, FanArtTypes.Cover, TheGamesDbV2.IMAGE_TYPE_BOXART, TheGamesDbV2.IMAGE_SIDE_FRONT).ConfigureAwait(false);
      if (!HasFanArt(mediaItemId, GameFanartTypes.BackCover))
        await SaveImagesAsync(images, mediaItemId, gameId, gameName, baseUrl, GameFanartTypes.BackCover, TheGamesDbV2.IMAGE_TYPE_BOXART, TheGamesDbV2.IMAGE_SIDE_BACK).ConfigureAwait(false);
      if (!HasFanArt(mediaItemId, FanArtTypes.Banner))
        await SaveImagesAsync(images, mediaItemId, gameId, gameName, baseUrl, FanArtTypes.Banner, TheGamesDbV2.IMAGE_TYPE_BANNER).ConfigureAwait(false);
      if (!HasFanArt(mediaItemId, FanArtTypes.FanArt))
        await SaveImagesAsync(images, mediaItemId, gameId, gameName, baseUrl, FanArtTypes.FanArt, TheGamesDbV2.IMAGE_TYPE_FANART).ConfigureAwait(false);
      if (!HasFanArt(mediaItemId, FanArtTypes.Logo))
        await SaveImagesAsync(images, mediaItemId, gameId, gameName, baseUrl, FanArtTypes.Logo, TheGamesDbV2.IMAGE_TYPE_CLEARLOGO).ConfigureAwait(false);
      if (!HasFanArt(mediaItemId, GameFanartTypes.ScreenShot))
        await SaveImagesAsync(images, mediaItemId, gameId, gameName, baseUrl, GameFanartTypes.ScreenShot, TheGamesDbV2.IMAGE_TYPE_SCREENSHOT).ConfigureAwait(false);

      ServiceRegistration.Get<ILogger>().Debug("GameTheGamesDbWrapper Download: Finished saving images for game {0}", gameId);
      return true;
    }

    protected Task<int> SaveImagesAsync(Image[] images, Guid mediaItemId, int gameId, string gameName, string baseUrl, string fanartType, string imageType, string imageSide = null)
    {
      IFanArtCache fanArtCache = ServiceRegistration.Get<IFanArtCache>();
      return fanArtCache.TrySaveFanArt(mediaItemId, gameName, fanartType,
        images.Where(i => i.Type == imageType && (imageSide == null || i.Side == imageSide)).ToList(),
        (p, i) => _gamesDbApi.DownloadImageAsync(gameId, baseUrl, i, p));
    }

    protected Task<bool> SaveOldImagesAsync(string filepath, Guid mediaItemId, int gameId, string gameName, string fanartType, string imageType, string imageSide = null)
    {
      IFanArtCache fanArtCache = ServiceRegistration.Get<IFanArtCache>();
      return fanArtCache.TrySaveFanArt(mediaItemId, gameName, fanartType,
        (p) =>
        {
          string downloadFile = _gamesDbApi.CreateAndGetImageCacheName(gameId, Path.GetFileName(filepath), p);
          File.Copy(filepath, Path.Combine(p, downloadFile));
          return Task.FromResult(true);
        });
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
          string.Equals(m.GameName, gameInfo.SearchName, StringComparison.OrdinalIgnoreCase) &&
          string.Equals(m.Platform, gameInfo.Platform, StringComparison.OrdinalIgnoreCase));
      return match != null;
    }

    #endregion
  }
}
