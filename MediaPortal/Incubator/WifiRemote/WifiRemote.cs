using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.WifiRemote.Messages;
using MediaPortal.Plugins.WifiRemote.Messages.Now_Playing;
using MediaPortal.Plugins.WifiRemote.MP_Messages;
using MediaPortal.Plugins.WifiRemote.Settings;
using MediaPortal.UI.Control.InputManager;
using WifiRemote;

namespace MediaPortal.Plugins.WifiRemote
{
  public class WifiRemote : IPluginStateTracker
  {
    public const string PLUGIN_NAME = "WifiRemote2";
    public const int DEFAULT_PORT = 8017;
    
    SocketServer socketServer = null;
    private UInt16 port;
    private ZeroConfig zeroConfig = null;
    private MPMessageHandler mpMessageHandler;

    internal static MessageStatus MessageStatus = new MessageStatus();


    /// <summary>
    /// Connects the IR client to the host configured in the settings.
    /// </summary>
    /// <returns><c>true</c>, if the client could successfully be started, else <c>false</c>.</returns>
    public bool StartClient()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      WifiRemoteSettings settings = settingsManager.Load<WifiRemoteSettings>();

      string userName = null;
      string password = null;
      string passcode = null;
      AuthMethod auth = AuthMethod.None;
      int autologinTimeout = 0;
      bool showNotification = false;
      port = DEFAULT_PORT;

      /*port = (UInt16)reader.GetValueAsInt(PLUGIN_NAME, "port", DEFAULT_PORT);
      disableBonjour = reader.GetValueAsBool(PLUGIN_NAME, "disableBonjour", false);
      serviceName = reader.GetValueAsString(PLUGIN_NAME, "serviceName", "");
      userName = reader.GetValueAsString(PLUGIN_NAME, "username", "");
      userName = WifiRemote.DecryptString(userName);
      password = reader.GetValueAsString(PLUGIN_NAME, "password", "");
      password = WifiRemote.DecryptString(password);
      passcode = reader.GetValueAsString(PLUGIN_NAME, "passcode", "");
      passcode = WifiRemote.DecryptString(passcode);

      auth = (AuthMethod)reader.GetValueAsInt(PLUGIN_NAME, "auth", 0);
      autologinTimeout = reader.GetValueAsInt(PLUGIN_NAME, "autologinTimeout", 0);

      showNotification = reader.GetValueAsBool(PLUGIN_NAME, "showNotifications", false);*/

      // Start listening for client connections
      socketServer = new SocketServer(port)
      {
        UserName = userName,
        Password = password,
        PassCode = passcode,
        AllowedAuth = auth,
        AutologinTimeout = autologinTimeout,
        ShowNotifications = showNotification
      };

      socketServer.Start();

      // start ZeroConfig
      zeroConfig = new ZeroConfig(port, PLUGIN_NAME, "");
      zeroConfig.PublishBonjourService();

      // start the MP Message Handler
      mpMessageHandler = new MPMessageHandler();
      mpMessageHandler.SubscribeToMessages();

      // Status updated
      StatusUpdater.Start();

      return true;
    }



    void Startup()
    {
      if (StartClient())
        ServiceRegistration.Get<ILogger>().Info("WifiRemotePlugin: Started");
      else
        ServiceRegistration.Get<ILogger>().Error("WifiRemotePlugin: Failed to start local comms, input service is unavailable for this session");
    }

    private void StopClient()
    {
      zeroConfig.Stop();
      NowPlayingUpdater.Stop();
      StatusUpdater.Stop();
      mpMessageHandler.UnsubscribeFromMessages();
    }

    #region IPluginStateTracker implementation

    public void Activated(PluginRuntime pluginRuntime)
    {
      Startup();
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