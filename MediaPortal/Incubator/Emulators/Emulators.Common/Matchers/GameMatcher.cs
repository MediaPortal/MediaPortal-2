using Emulators.Common.Games;
using Emulators.Common.NameProcessing;
using Emulators.Common.TheGamesDb;
using MediaPortal.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Emulators.Common.MobyGames;
using Emulators.Common.RawG;
using MediaPortal.Common.MediaManagement;

namespace Emulators.Common.Matchers
{
  public class GameMatcher
  {
    List<IOnlineMatcher> _onlineMatchers = new List<IOnlineMatcher>();

    public static GameMatcher Instance
    {
      get { return ServiceRegistration.Get<GameMatcher>(); }
    }

    public GameMatcher()
    {
      _onlineMatchers.Add(new TheGamesDbWrapperV2());
      _onlineMatchers.Add(new RawGWrapperV1());
      _onlineMatchers.Add(new MobyGamesWrapper());
    }

    public async Task<bool> FindAndUpdateGameAsync(GameInfo gameInfo)
    {
      TheGamesDbWrapperV2.TryGetTGDBId(gameInfo);
      NameProcessor.CleanupTitle(gameInfo);
      bool success = false;
      foreach (IOnlineMatcher matcher in _onlineMatchers)
      {
        success |= await matcher.FindAndUpdateGameAsync(gameInfo).ConfigureAwait(false);
      }
      return success;
    }

    public async Task DownloadFanArtAsync(Guid mediaItem, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      GameInfo gameInfo = new GameInfo();
      gameInfo.SetIdsAndName(aspects);
      foreach (IOnlineMatcher matcher in _onlineMatchers)
      {
        await matcher.DownloadFanArtAsync(mediaItem, gameInfo).ConfigureAwait(false);
      }
    }
  }
}
