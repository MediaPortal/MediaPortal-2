using System;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.Plugins.WifiRemote.MP_Messages
{
  class MPMessageHandler
  {
    private AsynchronousMessageQueue _messageQueue;

    private static readonly string[] SUBSCRIBED_CHANNELS = new string[]
    {
      PlayerManagerMessaging.CHANNEL
    };

    public void SubscribeToMessages()
    {
      Console.WriteLine("subscribe to player messages");
      if (_messageQueue != null)
        return;
      _messageQueue = new AsynchronousMessageQueue(this, SUBSCRIBED_CHANNELS);
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
      Console.WriteLine("subscribe to player messages end of function");
    }

    public void UnsubscribeFromMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    public void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == PlayerManagerMessaging.CHANNEL)
      {
        HandlePlayerMessages.OnMessageReceived(message);
      }
    }
  }
}
