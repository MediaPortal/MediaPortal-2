using MediaPortal.UI.Presentation.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.MediaManagement;
using Emulators.Common.Games;

namespace Emulators.LibRetro
{
  public class LibRetroPlayerBuilder : IPlayerBuilder
  {
    public IPlayer GetPlayer(MediaItem mediaItem)
    {
      LibRetroMediaItem retroItem = mediaItem as LibRetroMediaItem;
      if (retroItem == null)
        return null;
      var player = new LibRetroPlayer();
      player.SetMediaItem(retroItem);
      return player;
    }
  }
}
