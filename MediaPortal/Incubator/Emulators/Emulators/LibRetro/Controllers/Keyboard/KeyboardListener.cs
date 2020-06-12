using MediaPortal.Common.Messaging;
using MediaPortal.UI.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Emulators.LibRetro.Controllers.Keyboard
{
  class KeyboardListener : IDisposable
  {
    protected const int WM_KEYDOWN = 0x0100;
    protected const int WM_KEYUP = 0x0101;
    protected readonly object _syncOb = new object();
    protected HashSet<Keys> _pressedKeys;
    protected AsynchronousMessageQueue _messageQueue;

    public KeyboardListener()
    {
      _pressedKeys = new HashSet<Keys>();
      SubscribeToMessages();
    }

    public Keys GetPressedKey()
    {
      lock(_syncOb)
        return _pressedKeys.Count > 0 ? _pressedKeys.First() : Keys.None;
    }

    protected void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new[] { WindowsMessaging.CHANNEL });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    protected virtual void UnsubscribeFromMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    protected void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == WindowsMessaging.CHANNEL)
      {
        WindowsMessaging.MessageType messageType = (WindowsMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case WindowsMessaging.MessageType.WindowsBroadcast:
            Message msg = (Message)message.MessageData[WindowsMessaging.MESSAGE];
            if(msg.Msg == WM_KEYDOWN)
              lock(_syncOb)
                _pressedKeys.Add((Keys)msg.WParam);
            else if(msg.Msg == WM_KEYUP)
              lock(_syncOb)
                _pressedKeys.Remove((Keys)msg.WParam);
            break;
        }
      }
    }

    public void Dispose()
    {
      UnsubscribeFromMessages();
    }
  }
}
