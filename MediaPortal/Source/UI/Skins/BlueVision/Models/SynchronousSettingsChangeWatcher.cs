#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;

namespace MediaPortal.UiComponents.BlueVision.Models
{
  public class SynchronousSettingsChangeWatcher<T>: IDisposable where T : class
  {
    private readonly AsynchronousMessageQueue _messageQueue;
    private T _settings;

    public SynchronousSettingsChangeWatcher()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[] { SettingsManagerMessaging.CHANNEL });
      _messageQueue.PreviewMessage += OnPreviewMessage;
      _messageQueue.Start();
    }

    protected void OnPreviewMessage(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SettingsManagerMessaging.CHANNEL)
      {
        SettingsManagerMessaging.MessageType messageType = (SettingsManagerMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case SettingsManagerMessaging.MessageType.SettingsChanged:
            Type settingsType = (Type)message.MessageData[SettingsManagerMessaging.SETTINGSTYPE];
            // If our contained Type has been changed, clear the cache and reload it
            if (typeof(T) == settingsType)
              Refresh();
            break;
        }
      }
    }

    /// <summary>
    /// Informs listeners that the current setting has been changed.
    /// </summary>
    public EventHandler SettingsChanged;

    /// <summary>
    /// Gets the current setting. This property will automatically return new values after changes.
    /// </summary>
    public T Settings
    {
      get
      {
        return _settings ?? (_settings = ServiceRegistration.Get<ISettingsManager>().Load<T>());
      }
    }

    /// <summary>
    /// Forces the refresh of the settings.
    /// </summary>
    public void Refresh()
    {
      _settings = null;
      if (SettingsChanged != null)
        SettingsChanged(this, EventArgs.Empty);
    }

    #region IDisposable members

    public void Dispose()
    {
      _messageQueue.PreviewMessage -= OnPreviewMessage;
      _messageQueue.Shutdown();
    }

    #endregion
  }
}
