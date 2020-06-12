using Emulators.Common.Games;
using Emulators.Common.Matchers;
using MediaPortal.Common.MediaManagement;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Emulators.Server.FanArtHandlers
{
  public class GameFanArtHandler : IMediaFanArtHandler
  {
    protected static readonly Guid ID = new Guid("E37E37A2-58E9-46FF-AEF9-7AB70E601489");
    protected static readonly Guid[] FANART_ASDPECTS = { GameAspect.ASPECT_ID };
    protected readonly FanArtHandlerMetadata _metadata;

    public GameFanArtHandler()
    {
      _metadata = new FanArtHandlerMetadata(ID, "Game Fanart Handler");
    }

    public FanArtHandlerMetadata Metadata
    {
      get { return _metadata; }
    }

    public Guid[] FanArtAspects
    {
      get { return FANART_ASDPECTS; }
    }

    public Task CollectFanArtAsync(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      string onlineId;
      if (MediaItemAspect.TryGetAttribute(aspects, GameAspect.ATTR_ONLINE_ID, out onlineId))
        return GameMatcher.Instance.DownloadFanArtAsync(onlineId);
      return Task.CompletedTask;
    }

    public void ClearCache()
    {
    }

    public void DeleteFanArt(Guid mediaItemId)
    {
    }
  }
}
