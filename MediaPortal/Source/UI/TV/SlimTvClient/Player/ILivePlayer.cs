using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;

namespace MediaPortal.Plugins.SlimTv.Client.Player
{
  /// <summary>
  /// Common interface for LiveTvPlayer and LiveRadioPlayer
  /// </summary>
  public interface ILivePlayer : IMediaPlaybackControl
  {
    LiveTvMediaItem CurrentItem { get; }
    /// <summary>
    /// Notify any registered event sinks of BeginZap event
    /// </summary>
    void NotifyBeginZap(object sender);
    /// <summary>
    /// Notify any registered event sinks of BeginZap event
    /// </summary>
    void NotifyEndZap(object sender);
    /// <summary>
    /// About to change channel
    /// </summary>
    void BeginZap();
    /// <summary>
    /// Channel changing complete
    /// </summary>
    void EndZap();
  }
}
