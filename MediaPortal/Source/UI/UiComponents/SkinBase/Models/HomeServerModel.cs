#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.SkinBase.General;

namespace MediaPortal.UiComponents.SkinBase.Models
{
  /// <summary>
  /// Model which attends the workflow state "ShowHomeServer".
  /// </summary>
  public class HomeServerModel : IDisposable
  {
    #region Consts

    public const string STR_HOME_SERVER_MODEL_ID = "854ABA9A-71A1-420b-A657-9641815F9C01";
    public static Guid HOME_SERVER_MODEL_ID = new Guid(STR_HOME_SERVER_MODEL_ID);

    #endregion

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue;
    protected AbstractProperty _homeServerProperty;
    protected AbstractProperty _isHomeServerAttachedProperty;
    protected AbstractProperty _isHomeServerConnectedProperty;

    #endregion

    public HomeServerModel()
    {
      _homeServerProperty = new WProperty(typeof(string), string.Empty);
      _isHomeServerAttachedProperty = new WProperty(typeof(bool), false);
      _isHomeServerConnectedProperty = new WProperty(typeof(bool), false);
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
          {
            ServerConnectionMessaging.CHANNEL
          });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
      SynchronizeHomeServer();
    }

    public virtual void Dispose()
    {
      _messageQueue.UnsubscribeFromAllMessageChannels();
      _messageQueue.Shutdown();
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ServerConnectionMessaging.CHANNEL)
      {
        ServerConnectionMessaging.MessageType messageType =
            (ServerConnectionMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case ServerConnectionMessaging.MessageType.HomeServerAttached:
          case ServerConnectionMessaging.MessageType.HomeServerDetached:
          case ServerConnectionMessaging.MessageType.HomeServerConnected:
          case ServerConnectionMessaging.MessageType.HomeServerDisconnected:
            SynchronizeHomeServer();
            break;
        }
      }
    }

    protected void SynchronizeHomeServer()
    {
      IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
      if (string.IsNullOrEmpty(scm.HomeServerSystemId))
      {
        IsHomeServerAttached = false;
        HomeServer = null;
        IsHomeServerConnected = false;
      }
      else
      {
        string serverName = scm.LastHomeServerName ?? LocalizationHelper.Translate(Consts.RES_UNKNOWN_SERVER_NAME);
        SystemName system = scm.LastHomeServerSystem;
        HomeServer = LocalizationHelper.Translate(Consts.RES_SERVER_FORMAT_TEXT, serverName,
            system == null ? LocalizationHelper.Translate(Consts.RES_UNKNOWN_SERVER_SYSTEM) : system.HostName);
        IsHomeServerConnected = scm.IsHomeServerConnected;
        IsHomeServerAttached = true;
      }
    }

    public Guid ModelId
    {
      get { return HOME_SERVER_MODEL_ID; }
    }

    #region Public members to be called from the GUI

    public bool IsHomeServerAttached
    {
      get { return (bool) _isHomeServerAttachedProperty.GetValue(); }
      set { _isHomeServerAttachedProperty.SetValue(value); }
    }

    public AbstractProperty IsHomeServerAttachedProperty
    {
      get { return _isHomeServerAttachedProperty; }
    }

    public string HomeServer
    {
      get { return (string) _homeServerProperty.GetValue(); }
      set { _homeServerProperty.SetValue(value); }
    }

    public AbstractProperty HomeServerProperty
    {
      get { return _homeServerProperty; }
    }

    public bool IsHomeServerConnected
    {
      get { return (bool) _isHomeServerConnectedProperty.GetValue(); }
      set { _isHomeServerConnectedProperty.SetValue(value); }
    }

    public AbstractProperty IsHomeServerConnectedProperty
    {
      get { return _isHomeServerConnectedProperty; }
    }

    #endregion
  }
}
