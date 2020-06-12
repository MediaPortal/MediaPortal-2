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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Emulators.Common.FanartProvider;
using MediaPortal.Common.FanArt;

namespace Emulators.Common.MobyGames
{
  class MobyGamesWrapper : BaseMediaMatcher<GameMatch<string>, string, string, string>, IOnlineMatcher
  {
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

    protected static readonly Dictionary<string, string> DECODE_PLATFORM_MAP = new Dictionary<string, string>
    {
      { "3do", GameInfo.PLATFORM_3DO },
      { "acorn-32-bit", GameInfo.PLATFORM_ACORN_ARCHIMEDES },
      { "amiga", GameInfo.PLATFORM_AMIGA },
      { "amiga-cd32", GameInfo.PLATFORM_AMIGA_CD32 },
      { "cpc", GameInfo.PLATFORM_AMSTRAD_CPC },
      { "android", GameInfo.PLATFORM_ANDROID },
      { "apple2", GameInfo.PLATFORM_APPLE_II },
      { "arcade", GameInfo.PLATFORM_ARCADE },
      { "atari-2600", GameInfo.PLATFORM_ATARI_2600},
      { "atari-8-bit", GameInfo.PLATFORM_ATARI_800 },
      { "atari-st", GameInfo.PLATFORM_ATARI_ST },
      { "cd-i", GameInfo.PLATFORM_PHILIPS_CDI },
      { "colecovision", GameInfo.PLATFORM_COLECOVISION },
      { "c64", GameInfo.PLATFORM_COMMODORE_64 },
      { "dedicated-handheld", GameInfo.PLATFORM_HANDHELD_ELECTRONIC_GAMES_LCD },
      { "dragon-3264", GameInfo.PLATFORM_DRAGON_32_64 },
      { "dreamcast", GameInfo.PLATFORM_SEGA_DREAMCAST },
      { "electron", GameInfo.PLATFORM_ACORN_ELECTRON},
      { "fmtowns", GameInfo.PLATFORM_FM_TOWNS_MARTY },
      { "fm-7", GameInfo.PLATFORM_FUJITSU_FM_7 },
      { "gameboy", GameInfo.PLATFORM_NINTENDO_GAME_BOY },
      { "gameboy-advance", GameInfo.PLATFORM_NINTENDO_GAME_BOY_ADVANCE},
      { "gameboy-color", GameInfo.PLATFORM_NINTENDO_GAME_BOY_COLOR },
      { "game-gear", GameInfo.PLATFORM_SEGA_GAME_GEAR},
      { "gamecube", GameInfo.PLATFORM_NINTENDO_GAMECUBE },
      { "genesis", GameInfo.PLATFORM_SEGA_GENESIS },
      { "intellivision", GameInfo.PLATFORM_INTELLIVISION },
      { "msx", GameInfo.PLATFORM_MSX },
      { "macintosh", GameInfo.PLATFORM_MAC_OS },
      { "nes", GameInfo.PLATFORM_NINTENDO_NES },
      { "neo-geo", GameInfo.PLATFORM_NEO_GEO },
      { "3ds", GameInfo.PLATFORM_NINTENDO_3DS },
      { "n64", GameInfo.PLATFORM_NINTENDO_64 },
      { "nintendo-ds", GameInfo.PLATFORM_NINTENDO_DS },
      { "switch", GameInfo.PLATFORM_NINTENDO_SWITCH },
      { "odyssey-2", GameInfo.PLATFORM_MAGNAVOX_ODYSSEY_2 },
      { "ouya", GameInfo.PLATFORM_OUYA },
      { "pc88", GameInfo.PLATFORM_PC_88 },
      { "pc98", GameInfo.PLATFORM_PC_98 },
      { "ps-vita", GameInfo.PLATFORM_SONY_PLAYSTATION_VITA },
      { "psp", GameInfo.PLATFORM_SONY_PLAYSTATION_PORTABLE},
      { "playstation", GameInfo.PLATFORM_SONY_PLAYSTATION },
      { "ps2", GameInfo.PLATFORM_SONY_PLAYSTATION_2 },
      { "ps3", GameInfo.PLATFORM_SONY_PLAYSTATION_3 },
      { "playstation-4", GameInfo.PLATFORM_SONY_PLAYSTATION_4 },
      { "sega-cd", GameInfo.PLATFORM_SEGA_CD },
      { "sega-master-system", GameInfo.PLATFORM_SEGA_MASTER_SYSTEM},
      { "sega-saturn", GameInfo.PLATFORM_SEGA_SATURN },
      { "snes", GameInfo.PLATFORM_NINTENDO_SNES },
      { "sharp-x1", GameInfo.PLATFORM_SHARP_X1},
      { "sharp-x68000", GameInfo.PLATFORM_SHARP_X68000 },
      { "ti-994a", GameInfo.PLATFORM_TEXAS_INSTRUMENTS_TI_99_4A },
      { "trs-80-coco", GameInfo.PLATFORM_TRS_80_COLOR_COMPUTER },
      { "turbografx-cd", GameInfo.PLATFORM_TURBOGRAFX_CD },
      { "turbo-grafx", GameInfo.PLATFORM_TURBOGRAFX_16 },
      { "vic-20", GameInfo.PLATFORM_COMMODORE_VIC_20 },
      { "wii", GameInfo.PLATFORM_NINTENDO_WII},
      { "wii-u", GameInfo.PLATFORM_NINTENDO_WII_U },
      { "windows", GameInfo.PLATFORM_PC },
      { "xbox", GameInfo.PLATFORM_MICROSOFT_XBOX },
      { "xbox360", GameInfo.PLATFORM_MICROSOFT_XBOX_360 },
      { "xbox-one", GameInfo.PLATFORM_MICROSOFT_XBOX_ONE },
      { "zx-spectrum", GameInfo.PLATFORM_SINCLAIR_ZX_SPECTRUM},
      { "iphone", GameInfo.PLATFORM_IOS },
    };

    protected static readonly Dictionary<int, string> ENCODE_PLATFORM_MAP = new Dictionary<int, string>
    {
      { 35, GameInfo.PLATFORM_3DO },
      { 117, GameInfo.PLATFORM_ACORN_ARCHIMEDES },
      { 210, GameInfo.PLATFORM_ENTEX_ADVENTURE_VISION },
      { 19, GameInfo.PLATFORM_AMIGA },
      { 56, GameInfo.PLATFORM_AMIGA_CD32 },
      { 60, GameInfo.PLATFORM_AMSTRAD_CPC },
      { 91, GameInfo.PLATFORM_ANDROID },
      { 213, GameInfo.PLATFORM_APF_MP_1000 },
      { 31, GameInfo.PLATFORM_APPLE_II },
      { 143, GameInfo.PLATFORM_ARCADE },
      { 162, GameInfo.PLATFORM_EMERSON_ARCADIA_2001 },
      { 28, GameInfo.PLATFORM_ATARI_2600 },
      { 33, GameInfo.PLATFORM_ATARI_5200 },
      { 34, GameInfo.PLATFORM_ATARI_7800 },
      { 39, GameInfo.PLATFORM_ATARI_800 },
      { 24, GameInfo.PLATFORM_ATARI_ST },
      { 160, GameInfo.PLATFORM_BALLY_ASTROCADE },
      { 125, GameInfo.PLATFORM_CASIO_PV_1000 },
      { 73, GameInfo.PLATFORM_PHILIPS_CDI },
      { 76, GameInfo.PLATFORM_FAIRCHILD_CHANNEL_F },
      { 29, GameInfo.PLATFORM_COLECOVISION },
      { 61, GameInfo.PLATFORM_COMMODORE_128 },
      { 27, GameInfo.PLATFORM_COMMODORE_64 },
      { 205, GameInfo.PLATFORM_HANDHELD_ELECTRONIC_GAMES_LCD },
      { 79, GameInfo.PLATFORM_DRAGON_32_64 },
      { 8, GameInfo.PLATFORM_SEGA_DREAMCAST },
      { 93, GameInfo.PLATFORM_ACORN_ELECTRON },
      { 137, GameInfo.PLATFORM_EPOCH_CASSETTE_VISION },
      { 138, GameInfo.PLATFORM_EPOCH_SUPER_CASSETTE_VISION },
      { 126, GameInfo.PLATFORM_FUJITSU_FM_7 },
      { 102, GameInfo.PLATFORM_FM_TOWNS_MARTY },
      { 10, GameInfo.PLATFORM_NINTENDO_GAME_BOY },
      { 12, GameInfo.PLATFORM_NINTENDO_GAME_BOY_ADVANCE },
      { 11, GameInfo.PLATFORM_NINTENDO_GAME_BOY_COLOR },
      { 50, GameInfo.PLATFORM_GAME_COM },
      { 14, GameInfo.PLATFORM_NINTENDO_GAMECUBE },
      { 25, GameInfo.PLATFORM_SEGA_GAME_GEAR },
      { 16, GameInfo.PLATFORM_SEGA_GENESIS },
      { 30, GameInfo.PLATFORM_INTELLIVISION },
      { 86, GameInfo.PLATFORM_IOS },
      { 17, GameInfo.PLATFORM_ATARI_JAGUAR },
      { 163, GameInfo.PLATFORM_PIONEER_LASERACTIVE },
      { 18, GameInfo.PLATFORM_ATARI_LYNX },
      { 74, GameInfo.PLATFORM_MAC_OS },
      { 97, GameInfo.PLATFORM_MILTON_BRADLEY_MICROVISION },
      { 57, GameInfo.PLATFORM_MSX },
      { 36, GameInfo.PLATFORM_NEO_GEO },
      { 54, GameInfo.PLATFORM_NEO_GEO_CD },
      { 52, GameInfo.PLATFORM_NEO_GEO_POCKET },
      { 53, GameInfo.PLATFORM_NEO_GEO_POCKET_COLOR },
      { 22, GameInfo.PLATFORM_NINTENDO_NES },
      { 32, GameInfo.PLATFORM_N_GAGE },
      { 101, GameInfo.PLATFORM_NINTENDO_3DS },
      { 44, GameInfo.PLATFORM_NINTENDO_DS },
      { 203, GameInfo.PLATFORM_NINTENDO_SWITCH },
      { 116, GameInfo.PLATFORM_NUON },
      { 75, GameInfo.PLATFORM_MAGNAVOX_ODYSSEY_1 },
      { 78, GameInfo.PLATFORM_MAGNAVOX_ODYSSEY_2},
      { 144, GameInfo.PLATFORM_OUYA },
      { 94, GameInfo.PLATFORM_PC_88 },
      { 95, GameInfo.PLATFORM_PC_98 },
      { 59, GameInfo.PLATFORM_PC_FX },
      { 6, GameInfo.PLATFORM_SONY_PLAYSTATION },
      { 7, GameInfo.PLATFORM_SONY_PLAYSTATION_2 },
      { 81, GameInfo.PLATFORM_SONY_PLAYSTATION_3 },
      { 141, GameInfo.PLATFORM_SONY_PLAYSTATION_4 },
      { 152, GameInfo.PLATFORM_NINTENDO_POKEMON_MINI },
      { 46, GameInfo.PLATFORM_SONY_PLAYSTATION_PORTABLE },
      { 105, GameInfo.PLATFORM_SONY_PLAYSTATION_VITA },
      { 113, GameInfo.PLATFORM_RCA_STUDIO_II },
      { 120, GameInfo.PLATFORM_SAM_COUPE },
      { 21, GameInfo.PLATFORM_SEGA_32X },
      { 20, GameInfo.PLATFORM_SEGA_CD },
      { 26, GameInfo.PLATFORM_SEGA_MASTER_SYSTEM },
      { 103, GameInfo.PLATFORM_SEGA_PICO },
      { 23, GameInfo.PLATFORM_SEGA_SATURN },
      { 114, GameInfo.PLATFORM_SEGA_SG_1000 },
      { 121, GameInfo.PLATFORM_SHARP_X1 },
      { 106, GameInfo.PLATFORM_SHARP_X68000 },
      { 15, GameInfo.PLATFORM_NINTENDO_SNES },
      { 109, GameInfo.PLATFORM_WATARA_SUPERVISION },
      { 233, GameInfo.PLATFORM_COLECO_TELSTAR_ARCADE },
      { 47, GameInfo.PLATFORM_TEXAS_INSTRUMENTS_TI_99_4A},
      { 151, GameInfo.PLATFORM_TOMY_TUTOR },
      { 62, GameInfo.PLATFORM_TRS_80_COLOR_COMPUTER },
      { 40, GameInfo.PLATFORM_TURBOGRAFX_16 },
      { 45, GameInfo.PLATFORM_TURBOGRAFX_CD },
      { 37, GameInfo.PLATFORM_VECTREX },
      { 43, GameInfo.PLATFORM_COMMODORE_VIC_20 },
      { 38, GameInfo.PLATFORM_NINTENDO_VIRTUAL_BOY },
      { 82, GameInfo.PLATFORM_NINTENDO_WII },
      { 132, GameInfo.PLATFORM_NINTENDO_WII_U },
      { 3, GameInfo.PLATFORM_PC },
      { 48, GameInfo.PLATFORM_WONDERSWAN },
      { 49, GameInfo.PLATFORM_WONDERSWAN_COLOR },
      { 13, GameInfo.PLATFORM_MICROSOFT_XBOX },
      { 69, GameInfo.PLATFORM_MICROSOFT_XBOX_360 },
      { 142, GameInfo.PLATFORM_MICROSOFT_XBOX_ONE },
      { 41, GameInfo.PLATFORM_SINCLAIR_ZX_SPECTRUM },
    };

    protected HtmlDownloader _downloader = new HtmlDownloader() { Encoding = Encoding.UTF8 };

    protected override string MatchesSettingsFile
    {
      get { return _matchesSettingsFile; }
    }

    public async Task<bool> FindAndUpdateGameAsync(GameInfo gameInfo)
    {
      try
      {
        if (!await InitAsync().ConfigureAwait(false))
          return false;

        MobyGamesResult result;
        GameInfo foundGame = new GameInfo();
        if (TryGetFromStorage(gameInfo.SearchName, gameInfo.Platform, out result))
        {
          Logger.Debug("MobyGames: Retrieved from cache: '{0}' - '{1}'", gameInfo.SearchName, gameInfo.Platform);
          UpdateGameInfo(foundGame, result);
          gameInfo.Merge(foundGame);
          return true;
        }

        int platformId = ENCODE_PLATFORM_MAP.FirstOrDefault(p => p.Value == gameInfo.Platform).Key;
        if (platformId <= 0)
        {
          Logger.Debug("MobyGames: Invalid platform found for '{0}' - '{1}'", gameInfo.SearchName, gameInfo.Platform);
          return false;
        }

        List<SearchResult> results;
        if (!Search(gameInfo.SearchName, platformId, out results))
        {
          Logger.Debug("MobyGames: No results found for '{0}' - '{1}'", gameInfo.SearchName, gameInfo.Platform);
          return false;
        }

        Logger.Debug("MobyGames: Found {0} search results for '{1}' - '{2}'", results.Count, gameInfo.SearchName, gameInfo.Platform);
        results = results.FindAll(r => r.Title == gameInfo.SearchName || NameProcessor.GetLevenshteinDistance(r.Title, gameInfo.SearchName) <= MAX_SEARCH_DISTANCE);
        if (results.Count == 0 || !Get(results[0].Id, out result))
        {
          Logger.Debug("MobyGames: No close match found for: '{0}' - '{1}'", gameInfo.SearchName, gameInfo.Platform);
          return false;
        }

        Logger.Debug("MobyGames: Matched '{0}' to '{1}' - '{2}'", results[0].Title, gameInfo.SearchName, gameInfo.Platform);
        AddToStorage(gameInfo.SearchName, gameInfo.Platform, result.Id);
        UpdateGameInfo(foundGame, result);
        gameInfo.Merge(foundGame);
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("MobyGames: Exception while processing game {0}", ex, gameInfo.SearchName);
      }

      return false;
    }

    public async Task<bool> DownloadFanArtAsync(Guid mediaItemId, GameInfo gameInfo)
    {
      if (string.IsNullOrWhiteSpace(gameInfo.MobyId))
        return false;

      var frontCoverPath = Path.Combine(CACHE_PATH, ReplaceForwardSlashes(gameInfo.MobyId), COVERS_DIRECTORY, COVERS_FRONT);
      var backCoverPath = Path.Combine(CACHE_PATH, ReplaceForwardSlashes(gameInfo.MobyId), COVERS_DIRECTORY, COVERS_BACK);
      if (File.Exists(frontCoverPath) || File.Exists(backCoverPath))
      {
        if (!HasFanArt(mediaItemId, FanArtTypes.Cover))
          await SaveOldCoverAsync(mediaItemId, frontCoverPath, gameInfo.MobyId, gameInfo.GameName, FanArtTypes.Cover).ConfigureAwait(false);
        if (!HasFanArt(mediaItemId, GameFanartTypes.BackCover))
          await SaveOldCoverAsync(mediaItemId, backCoverPath, gameInfo.MobyId, gameInfo.GameName, GameFanartTypes.BackCover).ConfigureAwait(false);
        return true;
      }

      string cache = CreateAndGetCacheName(gameInfo.MobyId, "covers");
      string url = string.Format("{0}/{1}/{2}/{3}", BASE_URL, GET_PATH, gameInfo.MobyId, COVER_PATH);
      MobyGamesCoverArt result = _downloader.Download<MobyGamesCoverArt>(url, cache);
      if (result == null)
        return false;
      if (!HasFanArt(mediaItemId, FanArtTypes.Cover))
        await DownloadCoverAsync(mediaItemId, result.Front, gameInfo.MobyId, gameInfo.GameName, FanArtTypes.Cover).ConfigureAwait(false);
      if (!HasFanArt(mediaItemId, GameFanartTypes.BackCover))
        await DownloadCoverAsync(mediaItemId, result.Back, gameInfo.MobyId, gameInfo.GameName, GameFanartTypes.BackCover).ConfigureAwait(false);
      return true;
    }

    protected bool HasFanArt(Guid mediaItemId, string fanArtType)
    {
      IFanArtCache fanArtCache = ServiceRegistration.Get<IFanArtCache>();
      return fanArtCache.GetFanArtFiles(mediaItemId, fanArtType).Any();
    }

    protected Task DownloadCoverAsync(Guid mediaItemId, string url, string id, string gameName, string imageType)
    {
      if (string.IsNullOrEmpty(url))
        return Task.CompletedTask;
      url = string.Format("{0}{1}", BASE_URL, url);
      string filename = Path.GetFileName(new Uri(url).LocalPath);
      IFanArtCache fanArtCache = ServiceRegistration.Get<IFanArtCache>();
      return fanArtCache.TrySaveFanArt(mediaItemId, gameName, imageType,
        (new[] { url }).ToList(),
        (p, i) =>
        {
          string downloadFile = CreateAndGetImageCacheName(id, filename, p);
          return _downloader.DownloadFileAsync(url, downloadFile);
        });
    }

    protected Task SaveOldCoverAsync(Guid mediaItemId, string path, string id, string gameName, string imageType)
    {
      if (string.IsNullOrEmpty(path))
        return Task.CompletedTask;
      string filename = Path.GetFileName(path);
      IFanArtCache fanArtCache = ServiceRegistration.Get<IFanArtCache>();
      return fanArtCache.TrySaveFanArt(mediaItemId, gameName, imageType,
        (new[] { path }).ToList(),
        (p, i) =>
        {
          string downloadFile = CreateAndGetImageCacheName(id, filename, p);
          File.Copy(i, Path.Combine(p, filename));
          return Task.FromResult(true);
        });
    }

    protected string CreateAndGetImageCacheName(string id, string fileName, string folderPath)
    {
      try
      {
        string prefix = string.Format(@"MBY({0})_", id);
        if (!Directory.Exists(folderPath))
          Directory.CreateDirectory(folderPath);
        return Path.Combine(folderPath, prefix + fileName);
      }
      catch (Exception ex)
      {
        Logger.Warn("MobyGames: Error creating image cache directory '{0}'", ex, folderPath);
        return null;
      }
    }

    protected bool Search(string searchTerm, int platformId, out List<SearchResult> results)
    {
      results = null;
      if (string.IsNullOrEmpty(searchTerm) || platformId <= 0)
        return false;
      string query = string.Format(SEARCH_PATH, HttpUtility.UrlEncode(searchTerm), platformId);
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

    protected void UpdateGameInfo(GameInfo foundGame, MobyGamesResult game)
    {
      foundGame.GameName = game.Title;
      foundGame.MobyId = game.Id;
      if (game.ESRB != null)
      {
        if (game.ESRB.Contains("10+"))
          foundGame.Certification = "ESRB_E10+";
        else if (game.ESRB.StartsWith("E"))
          foundGame.Certification = "ESRB_E";
        else if (game.ESRB.StartsWith("T"))
          foundGame.Certification = "ESRB_T";
        else if (game.ESRB.StartsWith("M"))
          foundGame.Certification = "ESRB_M";
        else if (game.ESRB.StartsWith("A"))
          foundGame.Certification = "ESRB_A";
      }
      foundGame.Description = game.Overview;
      foundGame.Developer = game.Developer;
      if (game.Genres?.Count > 0)
        foundGame.Genres.AddRange(game.Genres);
      foundGame.Rating = game.Rating;
      if (DateTime.TryParse(game.ReleaseDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime releaseDate))
        foundGame.ReleaseDate = releaseDate;
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
