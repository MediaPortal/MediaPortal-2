#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.Runtime;
using MediaPortal.Core.Settings;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.RemovableMedia;
using MediaPortal.UI.Services.RemovableMedia.Settings;

namespace MediaPortal.UI.Services.RemovableMedia
{
  public class RemovableMediaTracker : IRemovableMediaTracker
  {
    #region Protected fields

    protected DeviceVolumeMonitor _deviceMonitor;
    protected IntPtr _windowHandle = IntPtr.Zero;
    protected AsynchronousMessageQueue _messageQueue = null;
    protected readonly object _syncObj = new object();

    #endregion

    #region Ctor & maintainance

    public RemovableMediaTracker()
    {
      ISystemStateService sss = ServiceRegistration.Get<ISystemStateService>();
      switch (sss.CurrentState)
      {
        case SystemState.Initializing:
          SubscribeToMessages();
          break;
        case SystemState.Running:
          StartListening();
          break;
      }
    }

    ~RemovableMediaTracker()
    {
      Dispose();
    }

    public void Dispose()
    {
      UnsubscribeFromMessages();
      StopListening();
    }

    void SubscribeToMessages()
    {
      lock (_syncObj)
      {
        _messageQueue = new AsynchronousMessageQueue(this, new string[]
          {
             SystemMessaging.CHANNEL
          });
        _messageQueue.MessageReceived += OnMessageReceived;
        _messageQueue.Start();
      }
    }

    void UnsubscribeFromMessages()
    {
      lock (_syncObj)
      {
        if (_messageQueue == null)
          return;
        _messageQueue.Shutdown();
        _messageQueue = null;
      }
    }

    /// <summary>
    /// Called when the plugin manager notifies the system about its events.
    /// Requests the main window handle from the main screen.
    /// </summary>
    /// <param name="queue">Queue which sent the message.</param>
    /// <param name="message">Message containing the notification data.</param>
    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SystemMessaging.CHANNEL)
      {
        if (((SystemMessaging.MessageType) message.MessageType) == SystemMessaging.MessageType.SystemStateChanged)
        {
          SystemState state = (SystemState) message.MessageData[SystemMessaging.NEW_STATE];
          if (state == SystemState.Running)
          {
            IScreenControl sc = ServiceRegistration.Get<IScreenControl>();
            _windowHandle = sc.MainWindowHandle;
            StartListening();
            UnsubscribeFromMessages();
          }
        }
      }
    }

    /// <summary>
    /// Event that gets triggered whenever a new volume is inserted.
    /// </summary>	
    static void OnMediaInserted(string driveLetter)
    {
      ServiceRegistration.Get<ILogger>().Info("RemovableMediaTracker: Media inserted into drive {0}", driveLetter);

      RemovableMediaMessaging.SendMediaChangedMessage(RemovableMediaMessaging.MessageType.MediaInserted, driveLetter);
    }

    /// <summary>
    /// Event that gets triggered whenever a volume is removed.
    /// </summary>	
    static void OnMediaRemoved(string driveLetter)
    {
      ServiceRegistration.Get<ILogger>().Info("RemovableMediaTracker: Media removed from drive {0}", driveLetter);

      RemovableMediaMessaging.SendMediaChangedMessage(RemovableMediaMessaging.MessageType.MediaRemoved, driveLetter);
    }

    #endregion

    public bool StartListening()
    {
      if (_windowHandle == IntPtr.Zero)
        return false;
      RemovableMediaTrackerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<RemovableMediaTrackerSettings>();
      try
      {
        _deviceMonitor = new DeviceVolumeMonitor(_windowHandle);
        _deviceMonitor.MediaInserted += OnMediaInserted;
        _deviceMonitor.MediaRemoved += OnMediaRemoved;
        _deviceMonitor.Enabled = settings.TrackRemovableMedia;

        ServiceRegistration.Get<ILogger>().Info("RemovableMediaTracker: Monitoring system for removable media changes");
        return true;
      }
      catch (DeviceVolumeMonitorException ex)
      {
        ServiceRegistration.Get<ILogger>().Error("RemovableMediaTracker: Error enabling RemovableMediaTracker service", ex);
      }
      return false;
    }

    public void StopListening()
    {
      if (_deviceMonitor != null)
        _deviceMonitor.Dispose();

      _deviceMonitor = null;
    }

    #region IRemovableMediaTracker implementation

    public bool TrackRemovableMedia
    {
      get { return _deviceMonitor.Enabled; }
      set
      {
        RemovableMediaTrackerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<RemovableMediaTrackerSettings>();
        settings.TrackRemovableMedia = value;
        ServiceRegistration.Get<ISettingsManager>().Save(settings);
        _deviceMonitor.Enabled = value;
      }
    }

    #endregion
  }
}
