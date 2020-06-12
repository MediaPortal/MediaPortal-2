using Emulators.Common.Games;
using Emulators.Common.NameProcessing;
using Emulators.Common.TheGamesDb;
using MediaPortal.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Emulators.Common.Matchers
{
  public class GameMatcher
  {
    IOnlineMatcher _onlineMatcher;
    Dictionary<Guid, IOnlineMatcher> _imageHandlers;

    public static GameMatcher Instance
    {
      get { return ServiceRegistration.Get<GameMatcher>(); }
    }

    public GameMatcher()
    {
      _onlineMatcher = new TheGamesDbWrapperV2();

      _imageHandlers = new Dictionary<Guid, IOnlineMatcher>();
      _imageHandlers.Add(_onlineMatcher.MatcherId, _onlineMatcher);

      IOnlineMatcher legacyImageHandler = new TheGamesDbLegacyWrapper();
      _imageHandlers.Add(legacyImageHandler.MatcherId, legacyImageHandler);
    }

    public Task<bool> FindAndUpdateGameAsync(GameInfo gameInfo)
    {
      TheGamesDbWrapperV2.TryGetTGDBId(gameInfo);
      NameProcessor.CleanupTitle(gameInfo);
      return _onlineMatcher.FindAndUpdateGameAsync(gameInfo);
    }

    public Task DownloadFanArtAsync(string onlineId)
    {
      return _onlineMatcher.DownloadFanArtAsync(onlineId);
    }

    public bool TryGetImagePath(Guid matcherId, string onlineId, ImageType imageType, out string path)
    {
      if (_imageHandlers.TryGetValue(matcherId, out IOnlineMatcher matcher))
        return matcher.TryGetImagePath(onlineId, imageType, out path);

      path = null;
      return false;
    }
  }
}
