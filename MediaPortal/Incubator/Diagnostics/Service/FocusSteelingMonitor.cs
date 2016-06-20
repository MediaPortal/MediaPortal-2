using MediaPortal.Common.Messaging;
using MediaPortal.UI.General;
using System;
using System.Windows.Forms;

namespace MediaPortal.UiComponents.Diagnostics.Service
{
    internal class FocusSteelingMonitor : IDisposable
    {
        #region Fields

        private static FocusSteelingMonitor _instance;

        private AsynchronousMessageQueue _messageQueue;

        #endregion Fields

        #region Properties

        internal static FocusSteelingMonitor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new FocusSteelingMonitor();
                }
                return _instance;
            }
        }

        internal bool IsMonitoring { get; set; }
        #endregion Properties

        #region Methods

        public void Dispose()
        {
            UnsubscribeFromMessages();
        }

        internal void SubscribeToMessages()
        {
            if (_messageQueue != null)
                return;
            _messageQueue = new AsynchronousMessageQueue(this, new[] { WindowsMessaging.CHANNEL, });
            _messageQueue.PreviewMessage += OnPreviewMessage;
            _messageQueue.Start();
            IsMonitoring = true;
        }

        internal void UnsubscribeFromMessages()
        {
            if (_messageQueue == null)
                return;
            _messageQueue.Shutdown();
            _messageQueue = null;
            IsMonitoring = false;
        }

        protected virtual void HandleWindowsMessage(ref Message m)
        {
            ActivationMonitor.HandleMessage(ref m);
        }

        private void OnPreviewMessage(AsynchronousMessageQueue queue, SystemMessage message)
        {
            if (message.ChannelName == WindowsMessaging.CHANNEL)
            {
                WindowsMessaging.MessageType messageType = (WindowsMessaging.MessageType)message.MessageType;
                switch (messageType)
                {
                    case WindowsMessaging.MessageType.WindowsBroadcast:
                        Message msg = (Message)message.MessageData[WindowsMessaging.MESSAGE];
                        HandleWindowsMessage(ref msg);
                        message.MessageData[WindowsMessaging.MESSAGE] = msg;
                        break;
                }
            }
        }

        #endregion Methods
    }
}