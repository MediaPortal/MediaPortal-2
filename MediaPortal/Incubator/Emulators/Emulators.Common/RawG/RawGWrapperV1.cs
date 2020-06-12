#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using Emulators.Common.Games;
using Emulators.Common.Matchers;
using Emulators.Common.NameProcessing;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emulators.Common.FanartProvider;
using Emulators.Common.RawG.Api;
using MediaPortal.Common.FanArt;
using Emulators.Common.RawG.Data;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.Services.ThumbnailGenerator;

namespace Emulators.Common.RawG
{
  /// <summary>
  /// Wrapper for v1 of RAWG API.
  /// </summary>
  public class RawGWrapperV1 : BaseMediaMatcher<GameMatch<long>, long, string, string>, IOnlineMatcher
  {
    #region Consts
    
    protected static readonly Guid MATCHER_ID = new Guid("B7F4D4F9-74A9-4C75-A8C2-BEF18489D994");
    protected static readonly string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\RawGV1\");

    protected const int MAX_SEARCH_DISTANCE = 2;
    protected static readonly CultureInfo DATE_CULTURE = CultureInfo.CreateSpecificCulture("en-US");
    protected static readonly string _matchesSettingsFile = Path.Combine(CACHE_PATH, "Matches.xml");

    protected static readonly Dictionary<int, string> PLATFORM_MAP = new Dictionary<int, string>
    {
      { 4, GameInfo.PLATFORM_PC },
      { 1, GameInfo.PLATFORM_MICROSOFT_XBOX_ONE },
      { 18, GameInfo.PLATFORM_SONY_PLAYSTATION_4 },
      { 3, GameInfo.PLATFORM_IOS },
      { 21, GameInfo.PLATFORM_ANDROID },
      { 5, GameInfo.PLATFORM_MAC_OS},
      { 7, GameInfo.PLATFORM_NINTENDO_SWITCH },
      { 8, GameInfo.PLATFORM_NINTENDO_3DS },
      { 9, GameInfo.PLATFORM_NINTENDO_DS },
      { 14, GameInfo.PLATFORM_MICROSOFT_XBOX_360 },
      { 80, GameInfo.PLATFORM_MICROSOFT_XBOX },
      { 16, GameInfo.PLATFORM_SONY_PLAYSTATION_3 },
      { 15, GameInfo.PLATFORM_SONY_PLAYSTATION_2 },
      { 27, GameInfo.PLATFORM_SONY_PLAYSTATION },
      { 19, GameInfo.PLATFORM_SONY_PLAYSTATION_VITA },
      { 17, GameInfo.PLATFORM_SONY_PLAYSTATION_PORTABLE },
      { 10, GameInfo.PLATFORM_NINTENDO_WII_U },
      { 11, GameInfo.PLATFORM_NINTENDO_WII },
      { 105, GameInfo.PLATFORM_NINTENDO_GAMECUBE },
      { 83, GameInfo.PLATFORM_NINTENDO_64 },
      { 24, GameInfo.PLATFORM_NINTENDO_GAME_BOY_ADVANCE },
      { 43, GameInfo.PLATFORM_NINTENDO_GAME_BOY_COLOR},
      { 26, GameInfo.PLATFORM_NINTENDO_GAME_BOY },
      { 79, GameInfo.PLATFORM_NINTENDO_SNES },
      { 49, GameInfo.PLATFORM_NINTENDO_NES },
      { 55, GameInfo.PLATFORM_MAC_OS },
      { 41, GameInfo.PLATFORM_APPLE_II},
      { 166, GameInfo.PLATFORM_AMIGA },
      { 28, GameInfo.PLATFORM_ATARI_7800},
      { 31, GameInfo.PLATFORM_ATARI_5200 },
      { 23, GameInfo.PLATFORM_ATARI_2600 },
      { 25, GameInfo.PLATFORM_ATARI_800 },
      { 34, GameInfo.PLATFORM_ATARI_ST },
      { 46, GameInfo.PLATFORM_ATARI_LYNX},
      { 50, GameInfo.PLATFORM_ATARI_XE },
      { 167, GameInfo.PLATFORM_SEGA_GENESIS },
      { 107, GameInfo.PLATFORM_SEGA_SATURN },
      { 119, GameInfo.PLATFORM_SEGA_CD },
      { 117, GameInfo.PLATFORM_SEGA_32X },
      { 74, GameInfo.PLATFORM_SEGA_MASTER_SYSTEM },
      { 106, GameInfo.PLATFORM_SEGA_DREAMCAST },
      { 111, GameInfo.PLATFORM_3DO },
      { 112, GameInfo.PLATFORM_ATARI_JAGUAR },
      { 77, GameInfo.PLATFORM_SEGA_GAME_GEAR },
      { 12, GameInfo.PLATFORM_NEO_GEO },
    };

    #endregion

    #region Protected Members 

    protected readonly SemaphoreSlim _initSyncObj = new SemaphoreSlim(1, 1);
    protected bool _isInit = false;
    protected RawGV1 _rawGApi = new RawGV1(CACHE_PATH);
    protected MemoryCache<long, GameDetails> _memoryCache = new MemoryCache<long, GameDetails>();

    #endregion

    #region Public Properties

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

        GameDetails game = await TryGetBestMatchAsync(gameInfo).ConfigureAwait(false);
        if (game == null)
          return false;

        GameInfo foundGame = new GameInfo();
        foundGame.GameName = game.Name;
        foundGame.RAWGVgDbId = game.Id;
        if (game.EsrbRating != null)
        {
          if (game.EsrbRating.Id == 1)
            foundGame.Certification = "ESRB_E";
          else if (game.EsrbRating.Id == 2)
            foundGame.Certification = "ESRB_E10+";
          else if (game.EsrbRating.Id == 3)
            foundGame.Certification = "ESRB_T";
          else if (game.EsrbRating.Id == 4)
            foundGame.Certification = "ESRB_M";
          else if (game.EsrbRating.Id == 5)
            foundGame.Certification = "ESRB_A";
        }

        foundGame.Description = game.Description;
        if (game.Rating <= game.RatingTop)
        {
          var scale = 10.0 / game.RatingTop;
          foundGame.Rating = new SimpleRating(game.Rating * scale, Convert.ToInt32(game.RatingsCount));
        }

        if (DateTime.TryParse(game.Released, DATE_CULTURE, DateTimeStyles.None, out DateTime releaseDate))
          foundGame.ReleaseDate = releaseDate;

        if (game.Genres != null && game.Genres.Length > 0)
        {
          foreach (var genre in game.Genres)
            foundGame.Genres.Add(genre.Name);
        }

        if (string.IsNullOrWhiteSpace(gameInfo.Developer) && game.Developers != null && game.Developers.Length > 0)
        {
          foundGame.Developer = game.Developers.First().Name;
        }

        gameInfo.Merge(foundGame);
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("RawG: Exception while processing game {0}", ex, gameInfo.SearchName);
      }

      return false;
    }

    public Task<bool> DownloadFanArtAsync(Guid mediaItemId, GameInfo gameInfo)
    {
      return DownloadFanArtAsync(mediaItemId, gameInfo.RAWGVgDbId, gameInfo.GameName);
    }

    public bool TryGetImagePath(string id, ImageType imageType, out string path)
    {
      path = null;

      return false;
    }

    #endregion

    #region Protected Methods

    protected async Task<GameDetails> TryGetBestMatchAsync(GameInfo gameInfo)
    {
      try
      {
        if (gameInfo.RAWGVgDbId > 0)
        {
          GameDetails result = await GetAsync(gameInfo.RAWGVgDbId).ConfigureAwait(false);
          if (result != null)
          {
            AddToStorage(gameInfo.SearchName, gameInfo.Platform, result.Id);
            return result;
          }
        }

        GameMatch<long> match;
        if (TryGetFromStorage(gameInfo, out match))
        {
          if (match.Id > 0)
          {
            Logger.Debug("RawG: Matched '{0}' to '{1}' from cache", gameInfo.SearchName, match.ItemName);
            GameDetails result = await GetAsync(match.Id).ConfigureAwait(false);
            if (result != null)
              return result;
          }
          return null;
        }

        var game = await TryGetClosestMatchAsync(gameInfo).ConfigureAwait(false);
        AddToStorage(gameInfo.SearchName, gameInfo.Platform, game?.Id ?? 0);
        return game;
      }
      catch (Exception ex)
      {
        Logger.Debug("RawG: Exception processing game '{0}'", ex, gameInfo.SearchName);
        return null;
      }
    }

    protected async Task<GameDetails> GetAsync(long id)
    {
      if (id < 1)
        return null;

      GameDetails result;
      if (_memoryCache.TryGetValue(id, out result))
        return result;

      result = await _rawGApi.GetGameAsync(id).ConfigureAwait(false);
      if (result == null)
        return null;

      _memoryCache.Add(id, result);
      return result;
    }

    protected async Task<GameDetails> TryGetClosestMatchAsync(GameInfo gameInfo)
    {
      Logger.Debug("RawG: Searching for '{0}', {1}", gameInfo.SearchName, gameInfo.Platform);

      var platformId = PLATFORM_MAP.FirstOrDefault(p => p.Value == gameInfo.Platform).Key;
      if (platformId <= 0)
      {
        Logger.Debug("RawG: Invalid platform for '{0}', {1}", gameInfo.SearchName, gameInfo.Platform);
      }

      GameResult result = await _rawGApi.SearchGameByNameAsync(gameInfo.SearchName, platformId).ConfigureAwait(false);
      if (!IsValid(result))
        return null;

      Game game = result.Results.FirstOrDefault(g => NameProcessor.AreStringsEqual(g.Name, gameInfo.SearchName, MAX_SEARCH_DISTANCE));
      if (game == null)
      {
        Logger.Debug("RawG: No match found for: '{0}'", gameInfo.SearchName);
        return null;
      }

      GameDetails gameDetails = await _rawGApi.GetGameAsync(game.Id).ConfigureAwait(false);
      if (gameDetails == null)
      {
        Logger.Debug("RawG: No match found for: '{0}'", gameInfo.SearchName);
        return null;
      }

      Logger.Debug("RawG: Matched '{0}' to '{1}'", gameInfo.SearchName, gameDetails.Name);
      _rawGApi.CacheGame(game.Id, gameDetails);
      return gameDetails;
    }

    protected static bool IsValid(GameResult result)
    {
      return result != null && result.Results != null && result.Results.Count > 0;
    }

    protected async Task<bool> DownloadFanArtAsync(Guid mediaItemId, long gameId, string gameName)
    {
      GameDetails result = await GetAsync(gameId).ConfigureAwait(false);

      List<string> urls = new List<string>();
      if (!string.IsNullOrWhiteSpace(result?.BackgroundImageUrl))
        urls.Add(result.BackgroundImageUrl);
      if (!string.IsNullOrWhiteSpace(result?.BackgroundImageAdditionalUrl))
        urls.Add(result.BackgroundImageAdditionalUrl);

      var screenshots = await _rawGApi.GetGameImagesAsync(gameId).ConfigureAwait(false) ?? new List<Screenshot>();

      ServiceRegistration.Get<ILogger>().Debug("RawGWrapper Download: Begin saving images for game {0}", gameId);

      if (!HasFanArt(mediaItemId, FanArtTypes.FanArt))
        await SaveImagesAsync(urls, mediaItemId, gameId, gameName, FanArtTypes.FanArt).ConfigureAwait(false);
      if (!HasFanArt(mediaItemId, GameFanartTypes.ScreenShot))
        await SaveImagesAsync(screenshots.Where(s => !s.IsDeleted && !s.IsHidden).Select(s => s.ImageUrl).ToList(), mediaItemId, gameId, gameName, GameFanartTypes.ScreenShot).ConfigureAwait(false);

      ServiceRegistration.Get<ILogger>().Debug("RawGWrapper Download: Finished saving images for game {0}", gameId);
      return true;
    }

    protected Task<int> SaveImagesAsync(List<string> imageUrls, Guid mediaItemId, long gameId, string gameName, string fanartType)
    {
      IFanArtCache fanArtCache = ServiceRegistration.Get<IFanArtCache>();
      return fanArtCache.TrySaveFanArt(mediaItemId, gameName, fanartType,
        imageUrls,
        (p, i) => _rawGApi.DownloadImageAsync(gameId, i, p));
    }

    protected bool HasFanArt(Guid mediaItemId, string fanArtType)
    {
      IFanArtCache fanArtCache = ServiceRegistration.Get<IFanArtCache>();
      return fanArtCache.GetFanArtFiles(mediaItemId, fanArtType).Any();
    }

    #endregion

    #region Storage

    protected void AddToStorage(string searchTerm, string platform, long id)
    {
      var onlineMatch = new GameMatch<long>
      {
        Id = id,
        ItemName = string.Format("{0}:{1}", searchTerm, platform),
        GameName = searchTerm,
        Platform = platform
      };
      _storage.TryAddMatch(onlineMatch);
    }

    protected bool TryGetFromStorage(GameInfo gameInfo, out GameMatch<long> match)
    {
      List<GameMatch<long>> matches = _storage.GetMatches();
      match = matches.Find(m =>
          string.Equals(m.GameName, gameInfo.SearchName, StringComparison.OrdinalIgnoreCase) &&
          string.Equals(m.Platform, gameInfo.Platform, StringComparison.OrdinalIgnoreCase));
      return match != null;
    }

    #endregion
  }
}
