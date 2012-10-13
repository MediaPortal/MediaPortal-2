using System;
using MediaPortal.Common.General;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.UI.Players.Video;
using MediaPortal.UI.SkinEngine.Players;

namespace MediaPortal.UiComponents.BackgroundManager.Models
{
  public class VideoBackgroundModel: IDisposable
  {
    public const string MODEL_ID_STR = "441288AC-F88D-4186-8993-6E259F7C75D8";

    protected string _videoFilename = @"{E88E64A8-0233-4fdf-BA27-0B44C6A39AE9}://S:/Redwave.wmv";
    protected VideoPlayer _videoPlayer;
    protected AbstractProperty _videoPlayerProperty;
    protected AsynchronousMessageQueue _messageQueue;

    #region Protected fields

    #endregion

    public AbstractProperty VideoPlayerProperty
    {
      get { return _videoPlayerProperty; }
    }

    public ISlimDXVideoPlayer VideoPlayer
    {
      get { return (ISlimDXVideoPlayer) _videoPlayerProperty.GetValue(); }
      set { _videoPlayerProperty.SetValue(value); }
    }

    public VideoBackgroundModel()
    {
      _videoPlayerProperty = new SProperty(typeof(ISlimDXVideoPlayer), null);
      _messageQueue = new AsynchronousMessageQueue(this, new[] { SystemMessaging.CHANNEL });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SystemMessaging.CHANNEL)
      {
        SystemMessaging.MessageType messageType = (SystemMessaging.MessageType) message.MessageType;
        if (messageType == SystemMessaging.MessageType.SystemStateChanged)
        {
          SystemState newState = (SystemState) message.MessageData[SystemMessaging.NEW_STATE];
          if (newState == SystemState.Running)
          {
            StartBackgroundPlayback();
          }
          if (newState == SystemState.ShuttingDown)
          {
            EndBackgroundPlayback();
          }
        }
      }
    }

    public void Dispose()
    {
      EndBackgroundPlayback();
    }

    public void EndBackgroundPlayback()
    {
      ISlimDXVideoPlayer player = VideoPlayer;
      IDisposable disp = player as IDisposable;
      if (player != null)
      {
        player.Stop();
        if (disp != null)
          disp.Dispose();
      }
      VideoPlayer = null;
    }

    public void StartBackgroundPlayback()
    {
      try
      {
        ResourceLocator resourceLocator = new ResourceLocator(ResourcePath.Deserialize(_videoFilename));
        _videoPlayer = new VideoPlayer { AutoRepeat = true };
        _videoPlayer.SetMediaItem(resourceLocator, "VideoBackground");
        VideoPlayer = _videoPlayer;
      }
      catch (Exception)
      {
        if (_videoPlayer != null)
          _videoPlayer.Dispose();
      }
    }
  }
}
