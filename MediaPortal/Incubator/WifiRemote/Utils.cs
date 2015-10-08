using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.Plugins.WifiRemote
{
  internal class Utils
  {
    internal static bool IsNowPlaying()
    {
      bool isPlaying = false;
      if (ServiceRegistration.Get<IPlayerManager>().NumActiveSlots > 0)
        isPlaying = ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.CurrentPlayer != null;
        //isPlaying = ServiceRegistration.Get<IPlayerContextManager>().PrimaryPlayerContext.PlaybackState == PlaybackState.Playing;
      return isPlaying;
    }
  }
}
