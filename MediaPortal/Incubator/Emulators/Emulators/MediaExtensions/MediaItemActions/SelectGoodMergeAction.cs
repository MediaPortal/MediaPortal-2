using Emulators.Common.GoodMerge;
using Emulators.Game;
using MediaPortal.Common;
using MediaPortal.Common.Async;
using MediaPortal.Common.MediaManagement;
using MediaPortal.UiComponents.Media.MediaItemActions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Emulators.MediaExtensions.MediaItemActions
{
  public class SelectGoodMergeAction : AbstractMediaItemAction
  {
    public override Task<bool> IsAvailableAsync(MediaItem mediaItem)
    {
      IEnumerable<string> goodMergeItems;
      return Task.FromResult(MediaItemAspect.TryGetAttribute(mediaItem.Aspects, GoodMergeAspect.ATTR_GOODMERGE_ITEMS, out goodMergeItems) && goodMergeItems != null);
    }

    public override Task<AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>> ProcessAsync(MediaItem mediaItem)
    {
      var result = new AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>(false, ContentDirectoryMessaging.MediaItemChangeType.None);
      IEnumerable<string> goodMergeItems;
      if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, GoodMergeAspect.ATTR_GOODMERGE_ITEMS, out goodMergeItems))
      {
        MediaItemAspect.SetAttribute<string>(mediaItem.Aspects, GoodMergeAspect.ATTR_LAST_PLAYED_ITEM, null);
        ServiceRegistration.Get<IGameLauncher>().LaunchGame(mediaItem);
      }
      return Task.FromResult(result);
    }
  }
}
