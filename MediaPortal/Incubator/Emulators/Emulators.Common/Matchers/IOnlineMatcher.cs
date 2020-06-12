using Emulators.Common.Games;
using System;
using System.Threading.Tasks;

namespace Emulators.Common.Matchers
{
  interface IOnlineMatcher
  {
    Guid MatcherId { get; }
    Task<bool> FindAndUpdateGameAsync(GameInfo gameInfo);
    Task DownloadFanArtAsync(string itemId);
    bool TryGetImagePath(string id, ImageType imageType, out string path);
  }
}
