using MediaPortal.Extensions.UserServices.FanArtService.Client.ImageSourceProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Extensions.UserServices.FanArtService.Client;
using MediaPortal.UI.Presentation.DataObjects;
using Emulators.Models.Navigation;
using Emulators.Common.FanartProvider;

namespace Emulators.Fanart
{
  public class GameFanartImageProvider : IFanartImageSourceProvider
  {
    public bool TryCreateFanartImageSource(ListItem listItem, out FanArtImageSource fanartImageSource)
    {
      GameItem item = listItem as GameItem;
      if (item != null)
      {
        fanartImageSource = new FanArtImageSource()
        {
          FanArtMediaType = GameFanartTypes.MEDIA_TYPE_GAME,
          FanArtName = item.MediaItem.MediaItemId.ToString()
        };
        return true;
      }
      fanartImageSource = null;
      return false;
    }
  }
}
