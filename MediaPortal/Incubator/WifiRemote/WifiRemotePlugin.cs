#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.WifiRemote.Messages;
using MediaPortal.Plugins.WifiRemote.Messages.MediaInfo;
using MediaPortal.Plugins.WifiRemote.MP_Messages;
using MediaPortal.Plugins.WifiRemote.Settings;

namespace MediaPortal.Plugins.WifiRemote
{
  public class WifiRemotePlugin : IPluginStateTracker
  {
    private SocketServer _socketServer = null;
    private ZeroConfig _zeroConfig = null;
    private MPMessageHandler _mpMessageHandler;
    private SettingsChangeWatcher<WifiRemoteSettings> _settingWatcher;

    internal static MessageStatus MessageStatus = new MessageStatus();

    public WifiRemotePlugin()
    {
      _settingWatcher = new SettingsChangeWatcher<WifiRemoteSettings>();
      _settingWatcher.SettingsChanged += SettingsChanged;
    }

    private void SettingsChanged(object sender, EventArgs e)
    {
      StopClient();
      StartClient();
    }

    /// <summary>
    /// Connects the IR client to the host configured in the settings.
    /// </summary>
    /// <returns><c>true</c>, if the client could successfully be started, else <c>false</c>.</returns>
    private bool StartClient()
    {
      try
      {
        ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
        WifiRemoteSettings settings = settingsManager.Load<WifiRemoteSettings>();

        // Start listening for client connections
        _socketServer = new SocketServer(Convert.ToUInt16(settings.Port))
        {
          PassCode = settings.PassCode,
          AllowedAuth = (AuthMethod)settings.AuthenticationMethod,
          AutologinTimeout = settings.AutoLoginTimeout,
          ShowNotifications = false
        };

        _socketServer.Start();

        if (settings.EnableBonjour)
        {
          // start ZeroConfig
          _zeroConfig = new ZeroConfig(Convert.ToUInt16(settings.Port), settings.ServiceName, "");
          _zeroConfig.PublishBonjourService();
        }

        // start the MP Message Handler
        _mpMessageHandler = new MPMessageHandler();
        _mpMessageHandler.SubscribeToMessages();

        // Status updated
        StatusUpdater.Start();

        ServiceRegistration.Get<ILogger>().Info("WifiRemote: Server started");
        return true;
      }
      catch(Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("WifiRemote: Failed to start server, input service is unavailable for this session", ex);
      }
      return false;
    }

    private void StopClient()
    {
      try
      {
        _zeroConfig?.Stop();
        _zeroConfig = null;
        NowPlayingUpdater.Stop();
        StatusUpdater.Stop();
        _mpMessageHandler.UnsubscribeFromMessages();
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("WifiRemote: Failed to stop server", ex);
      }
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }

    #region IPluginStateTracker implementation

    public void Activated(PluginRuntime pluginRuntime)
    {
      if (pluginRuntime != null)
      {
        var meta = pluginRuntime.Metadata;
        Logger.Info(string.Format("{0} v{1} [{2}] by {3}", meta.Name, meta.PluginVersion, meta.Description, meta.Author));
      }
      StartClient();
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
      StopClient();
    }

    public void Continue() { }

    public void Shutdown()
    {
      StopClient();
    }

    #endregion
  }
}
