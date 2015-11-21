using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.Plugins.WifiRemote.Messages
{
  class MessageNowPlayingBase
  {
    /// <summary>
    /// Duration of the media in seconds
    /// </summary>
    public int Duration
    {
      get
      {
        if (!Helper.IsNowPlaying())
        {
          return 0;
        }

        IPlayer player = ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.CurrentPlayer;
        if (player != null)
        {
          IMediaPlaybackControl mediaPlaybackControl = player as IMediaPlaybackControl;
          return mediaPlaybackControl == null ? 0 : Convert.ToInt32(mediaPlaybackControl.Duration.TotalSeconds);
        }
        return 0;
      }
    }

    /// <summary>
    /// The filename of the currently playing item
    /// </summary>
    public String File
    {
      get
      {
        if (!Helper.IsNowPlaying())
        {
          return String.Empty;
        }

        return ServiceRegistration.Get<IPlayerContextManager>().PrimaryPlayerContext.CurrentPlayer.MediaItemTitle;
      }
    }

    /// <summary>
    /// Current position in the file in seconds
    /// </summary>
    public int Position
    {
      get
      {
        if (!Helper.IsNowPlaying())
        {
          return 0;
        }

        IPlayer player = ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerContext.CurrentPlayer;
        if (player != null)
        {
          IMediaPlaybackControl mediaPlaybackControl = player as IMediaPlaybackControl;
          return mediaPlaybackControl == null ? 0 : Convert.ToInt32(mediaPlaybackControl.CurrentTime.TotalSeconds);
        }
        return 0;
      }
    }

    // TODO: reimplement
    /// <summary>
    /// Is the current playing item tv
    /// </summary>
    public bool IsTv
    {
      get
      {
        return false;
      }
    }

    /// <summary>
    /// Is the player in fullscreen mode
    /// </summary>
    public bool IsFullscreen
    {
      get { return ServiceRegistration.Get<IPlayerContextManager>().IsFullscreenContentWorkflowStateActive; }
    }
  }
}
