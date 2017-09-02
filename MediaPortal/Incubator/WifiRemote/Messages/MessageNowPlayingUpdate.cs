using System;
using MediaPortal.Common;
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.Plugins.WifiRemote.Messages
{
  /// <summary>
  /// Message that is sent to the client in regular updates as when Media is
  /// being played on the htpc
  /// </summary>
  internal class MessageNowPlayingUpdate : MessageNowPlayingBase, IMessage
  {
    public String Type
    {
      get { return "nowplayingupdate"; }
    }


    /// <summary>
    /// Current speed of the player
    /// </summary>
    public int Speed
    {
      get
      {
        try
        {
          return Convert.ToInt32(ServiceRegistration.Get<IMediaPlaybackControl>().PlaybackRate);
        }
        catch (Exception)
        {
          return 1;
        }
      }
    }
  }
}