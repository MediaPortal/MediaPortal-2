using Emulators.Common.Games;
using System;
using System.Threading.Tasks;

namespace Emulators.Common.Matchers
{
  interface IOnlineMatcher
  {
    Task<bool> FindAndUpdateGameAsync(GameInfo gameInfo);
    Task<bool> DownloadFanArtAsync(Guid mediaItemId, GameInfo gameInfo);
  }
}
