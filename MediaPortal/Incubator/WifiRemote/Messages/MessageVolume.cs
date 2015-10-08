using System;
using MediaPortal.Common;
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.Plugins.WifiRemote.Messages
{
  /// <summary>
  /// Message containing information about the current volume on the htpc
  /// </summary>
  internal class MessageVolume : IMessage
  {
    public string Type
    {
      get { return "volume"; }
    }

    /// <summary>
    /// Current volume in percent
    /// </summary>
    public int Volume
    {
      get
      {
        try
        {
          return ServiceRegistration.Get<IPlayerManager>().Volume;
        }
        catch (Exception)
        {
          return 101;
        }
      }
    }

    /// <summary>
    /// Is the volume muted
    /// </summary>
    public bool IsMuted
    {
      get { return ServiceRegistration.Get<IPlayerManager>().Muted; }
    }
  }
}