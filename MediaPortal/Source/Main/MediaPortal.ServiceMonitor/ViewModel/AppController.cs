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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Localization;
using MediaPortal.Common.PathManager;
using MediaPortal.ServiceMonitor.Collections;
using MediaPortal.ServiceMonitor.UPNP;
using MediaPortal.ServiceMonitor.Utilities;
using MediaPortal.ServiceMonitor.View;
using System.ServiceProcess;
using MediaPortal.Utilities.Process;
using MediaPortal.Utilities.SystemAPI;

namespace MediaPortal.ServiceMonitor.ViewModel
{
  public class ClientData
  {
    public string Name { get; set; }
    public string System { get; set; }
    public bool IsConnected { get; set; }
  }

  /// <summary>
  /// Main controller of the application.
  /// </summary>
  public class AppController : IDisposable, INotifyPropertyChanged, IAppController, IMessageReceiver
  {
    #region Constants

    private const string SERVER_SERVICE_NAME = "MP2-Server"; // Name of the installed MP2 Server Service
    protected const string AUTOSTART_REGISTER_NAME = "MP2 ServiceMonitor";

    private const int WM_POWERBROADCAST = 0x0218;
    private const int PBT_APMQUERYSUSPENDFAILED = 0x0002;
    private const int PBT_APMSUSPEND = 0x0004;
    private const int PBT_APMRESUMECRITICAL = 0x0006;
    private const int PBT_APMRESUMESUSPEND = 0x0007;
    private const int PBT_APMPOWERSTATUSCHANGE = 0x000A;
    private const int PBT_POWERSETTINGCHANGE = 0x8013;
    private const int PBT_APMRESUMEAUTOMATIC = 0x0012;

    #endregion

    #region Private variables

    private readonly SynchronizationContext _synchronizationContext; // To avoid cross thread calls
    private ServerConnectionMessaging.MessageType _serverConnectionStatus; // Store last server connection status
    private WindowMessageSink _messageSink;

    /// <summary>
    /// Provides access to the system tray area.
    /// </summary>
    private TaskbarIcon _taskbarIcon;

    private ServerStatus _serverStatus;

    #endregion

    #region Properties

    public bool IsAutoStartEnabled
    {
      get { return !string.IsNullOrEmpty(WindowsAPI.GetAutostartApplicationPath(AUTOSTART_REGISTER_NAME, true)); }
      set
      {
        try
        {
          // Use -m to start ServiceMonitor minimized
          string applicationPath = string.Format("\"{0}\" -m" , ServiceRegistration.Get<IPathManager>().GetPath("<APPLICATION_PATH>"));
#if DEBUG
          applicationPath = applicationPath.Replace(".vshost", "");
#endif
          if (value)
            WindowsAPI.AddAutostartApplication(applicationPath, AUTOSTART_REGISTER_NAME, true);
          else
            WindowsAPI.RemoveAutostartApplication(AUTOSTART_REGISTER_NAME, true);
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Error("Can't write autostart value to registry", ex);
        }
      }
    }

    public ObservableCollectionMt<ClientData> Clients { get; set; }

    public ServerStatus ServerStatus
    {
      get { return _serverStatus; }
      set { SetProperty(ref _serverStatus, value, "ServerStatus"); }
    }

    #endregion

    #region Ctor

    public AppController()
    {
      _synchronizationContext = SynchronizationContext.Current;
      Clients = new ObservableCollectionMt<ClientData>();
      _serverConnectionStatus = ServerConnectionMessaging.MessageType.HomeServerDisconnected;

      // Register at message channels
      ServiceRegistration.Get<IMessageBroker>().RegisterMessageReceiver(ServerConnectionMessaging.CHANNEL, this);
      ServiceRegistration.Get<IMessageBroker>().RegisterMessageReceiver(LocalizationMessaging.CHANNEL, this);
    }

    #endregion

    #region Main Window handling

    public Window MainWindow
    {
      get { return Application.Current.MainWindow; }
      internal set { Application.Current.MainWindow = value; }
    }

    public void StartUp(CommandLineOptions mpArgs)
    {
      ServiceRegistration.Get<ILogger>().Debug("StartUp ({0})", mpArgs.IsMinimized);
      InitMainWindow(mpArgs.IsMinimized);

      InitSystemTray();

      if (mpArgs.IsMinimized)
        HideMainWindow();
      else
        ShowMainWindow();

      // Set initial ServerStatus
      UpdateServerStatus();

      if (!string.IsNullOrEmpty(mpArgs.Command))
      {
        switch (mpArgs.Command.ToLower())
        {
          case "startservice":
            StartServerService();
            break;
          case "stopservice":
            StopServerService();
            break;
          case "restartservice":
            break;
        }
      }
    }

    protected void InitMainWindow(bool isMinimized)
    {
      ServiceRegistration.Get<ILogger>().Debug("InitMainWindow");
      Window mainWindow = new MainWindow
        {
          ShowActivated = false,
          ShowInTaskbar = false,
          Visibility = Visibility.Collapsed,
          WindowState =  isMinimized ? WindowState.Minimized : WindowState.Normal
        };
      mainWindow.Closed += OnMainWindowClosed;
      mainWindow.SourceInitialized += OnMainWindowSourceInitialized;
      mainWindow.StateChanged += OnMainWindowStateChanged;
      mainWindow.Show();
      MainWindow = mainWindow;
    }

    private void OnMainWindowStateChanged(object sender, EventArgs e)
    {
      Window mainWindow = MainWindow;
      if (mainWindow == null)
        return;
      if (mainWindow.WindowState == WindowState.Minimized)
        HideMainWindow();
    }

    public void ShowMainWindow()
    {
      ServiceRegistration.Get<ILogger>().Debug("ShowMainWindow");
      Window mainWindow = MainWindow;
      if (mainWindow == null)
        return;
      if (!mainWindow.IsVisible)
        mainWindow.Show();
      mainWindow.ShowInTaskbar = true;
      mainWindow.Visibility = Visibility.Visible;

      if (mainWindow.WindowState == WindowState.Minimized)
        mainWindow.WindowState = WindowState.Normal;

      // Show the window on top of others
      mainWindow.Focus();
      mainWindow.Activate();
      OnPropertyChanged("ServerStatus");
    }

    public void HideMainWindow()
    {
      ServiceRegistration.Get<ILogger>().Debug("HideMainWindow");
      Window mainWindow = MainWindow;
      if (mainWindow == null)
        return;
      mainWindow.Visibility = Visibility.Collapsed;
      mainWindow.ShowInTaskbar = false;
    }

    /// <summary>
    /// Hook into Windows message
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnMainWindowSourceInitialized(object sender, EventArgs e)
    {
      ServiceRegistration.Get<ILogger>().Debug("OnMainWindowSourceInitialized");
      ((Window) sender).SourceInitialized -= OnMainWindowSourceInitialized;

      _messageSink = new WindowMessageSink();
      _messageSink.OnWinProc += WndProc;
    }

    private void WndProc(object sender, uint msg, uint wParam, uint lParam)
    {
      if (msg == SingleInstanceHelper.SHOW_MP2_SERVICEMONITOR_MESSAGE)
        ShowMainWindow();

      if (msg == WM_POWERBROADCAST)
      {
        ServiceRegistration.Get<ILogger>().Debug("WndProc: [{0}]", wParam);
        var serverConnectionManager = (ServerConnectionManager) ServiceRegistration.Get<IServerConnectionManager>();
        switch (wParam)
        {
          case PBT_APMSUSPEND:
            ServiceRegistration.Get<ILogger>().Debug("WndProc: Suspend");
            if ((serverConnectionManager != null) && (serverConnectionManager.IsStarted))
              serverConnectionManager.Shutdown();
            break;
          case PBT_APMRESUMECRITICAL:
          case PBT_APMRESUMEAUTOMATIC:
          case PBT_APMRESUMESUSPEND:
            ServiceRegistration.Get<ILogger>().Debug("WndProc: Resume");
            if ((serverConnectionManager != null) && (!serverConnectionManager.IsStarted))
              serverConnectionManager.Startup();
            UpdateServerStatus();
            break;
        }
      }
    }

    /// <summary>
    /// Closes the main window 
    /// </summary>
    protected void OnMainWindowClosed(object sender, EventArgs e)
    {
      ServiceRegistration.Get<ILogger>().Debug("OnMainWindowClosed");

      CloseMainApplication();
    }

    /// <summary>
    /// Common routine to close the main window and exit the application.
    /// </summary>
    public void CloseMainApplication()
    {
      if (SynchronizationContext.Current == _synchronizationContext)
        RaiseCloseMainApplication(); // Execute on the current thread
      else
        _synchronizationContext.Post(state => RaiseCloseMainApplication(), null);
    }

    private void RaiseCloseMainApplication()
    {
      ServiceRegistration.Get<ILogger>().Debug("ClosingMainApplication");
      Dispose();
      Application.Current.Shutdown();
    }

    /// <summary>
    /// Inits the component that displays status information in the system tray.
    /// </summary>
    public void InitSystemTray()
    {
      ServiceRegistration.Get<ILogger>().Debug("InitSystemTray");
      _taskbarIcon = (TaskbarIcon) Application.Current.FindResource("TrayIcon");
      if (_taskbarIcon != null)
      {
        _taskbarIcon.DataContext = this;
        _taskbarIcon.Visibility = Visibility.Visible;
      }
    }

    #endregion

    #region Windows Service Functions

    /// <summary>
    /// Checks if the MP2 Server Service is installed.
    /// </summary>
    public bool IsServerServiceInstalled()
    {
      var services = ServiceController.GetServices();
      return services.Any(service => String.Compare(service.ServiceName, SERVER_SERVICE_NAME, StringComparison.OrdinalIgnoreCase) == 0);
    }

    /// <summary>
    /// Checks if the installed MP2 Server Service is running.
    /// </summary>
    public bool IsServerServiceRunning()
    {
      try
      {
        using (var serviceController = new ServiceController(SERVER_SERVICE_NAME))
          return serviceController.Status == ServiceControllerStatus.Running;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(
          "Check whether the MP2 Server Service is running failed. Please check your installation.", ex);
        return false;
      }
    }

    /// <summary>
    /// Starts the installed MP2 Server Service in case it is not running.
    /// </summary>
    public bool StartServerService()
    {
      try
      {
        if (!UacServiceHelper.IsAdmin())
          return UacServiceHelper.StartService();

        using (var serviceController = new ServiceController(SERVER_SERVICE_NAME))
        {
          switch (serviceController.Status)
          {
            case ServiceControllerStatus.Stopped:
              serviceController.Start();
              break;
            case ServiceControllerStatus.StartPending:
              break;
            case ServiceControllerStatus.Running:
              return true;
            default:
              return false;
          }
          serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(60));
          UpdateServerStatus();
          return (serviceController.Status == ServiceControllerStatus.Running);
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Starting MP2 Server Service failed.", ex);
        UpdateServerStatus();
        return false;
      }
    }

    /// <summary>
    /// Stops the installed MP2 Server Service.
    /// </summary>
    public bool StopServerService()
    {
      try
      {
        if (!UacServiceHelper.IsAdmin())
          return UacServiceHelper.StopService();

        using (var serviceController = new ServiceController(SERVER_SERVICE_NAME))
        {
          switch (serviceController.Status)
          {
            case ServiceControllerStatus.Running:
              serviceController.Stop();
              break;
            case ServiceControllerStatus.StopPending:
              break;
            case ServiceControllerStatus.Stopped:
              return true;
            default:
              return false;
          }
          serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(60));
          UpdateServerStatus();
          return serviceController.Status == ServiceControllerStatus.Stopped;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Stopping MP2 Server Service failed.", ex);
        UpdateServerStatus();
        return false;
      }
    }

    #endregion

    #region Implementation of IMessageReceiver

    /// <summary>
    /// Receive Server change messages from the <see cref="IServerConnectionManager"/> or from the <see cref="ILocalization"/> service.
    /// </summary>
    public void Receive(SystemMessage message)
    {
      if (message.ChannelName == ServerConnectionMessaging.CHANNEL)
      {
        var connectionType = (ServerConnectionMessaging.MessageType) message.MessageType;
        switch (connectionType)
        {
          case ServerConnectionMessaging.MessageType.HomeServerAttached:
          case ServerConnectionMessaging.MessageType.HomeServerConnected:
          case ServerConnectionMessaging.MessageType.HomeServerDetached:
          case ServerConnectionMessaging.MessageType.HomeServerDisconnected:
            _serverConnectionStatus = connectionType;
            break;
        }
        UpdateServerStatus();
      }
      else if (message.ChannelName == LocalizationMessaging.CHANNEL)
        if (((LocalizationMessaging.MessageType) message.MessageType) ==
            LocalizationMessaging.MessageType.LanguageChanged)
        {
          UpdateServerStatus();
        }
    }

    #endregion

    #region Update Server (& Clients) Status

    /// <summary>
    /// Common routine to set the ServerStatus information.
    /// </summary>
    public void UpdateServerStatus()
    {
      if (SynchronizationContext.Current == _synchronizationContext)
        RaiseUpdateServerStatus(); // Execute on the current thread
      else
        _synchronizationContext.Post(state => RaiseUpdateServerStatus(), null);
    }

    private void RaiseUpdateServerStatus()
    {
      if (IsServerServiceInstalled() && !IsServerServiceRunning())
      {
        ServerStatus = ServerStatus.NotStarted;
      }
      switch (_serverConnectionStatus)
      {
        case ServerConnectionMessaging.MessageType.HomeServerAttached:
          ServerStatus = ServerStatus.Attached;
          break;
        case ServerConnectionMessaging.MessageType.HomeServerConnected:
          ServerStatus = ServerStatus.Connected;
          break;
        case ServerConnectionMessaging.MessageType.HomeServerDetached:
          ServerStatus = ServerStatus.Detached;
          break;
        default:
          ServerStatus = ServerStatus.Disconnected;
          break;
      }

      ServiceRegistration.Get<ILogger>().Debug("ServerStatus: {0}", ServerStatus);

      try
      {
        Clients.ClearNotifyForEach();
        var serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();
        if (!serverConnectionManager.IsHomeServerConnected) return;

        var serverControler = serverConnectionManager.ServerController;
        if (serverControler == null) return;
        var connectedClientSystemIDs = serverControler.GetConnectedClients();

        foreach (var attachedClient in serverControler.GetAttachedClients().Where(attachedClient => !string.IsNullOrEmpty(attachedClient.LastClientName)))
        {
          Clients.Add(new ClientData
            {
              IsConnected = connectedClientSystemIDs != null && connectedClientSystemIDs.Contains(attachedClient.SystemId),
              Name = attachedClient.LastClientName,
              System = attachedClient.LastSystem == null ? string.Empty : attachedClient.LastSystem.HostName
            });
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("AppController.UpDateClients", ex);
      }

      if (!Clients.Any(client => client.IsConnected)) return;
      ServerStatus = ServerStatus.ClientConnected;
      ServiceRegistration.Get<ILogger>().Debug("ServerStatus: {0}", ServerStatus);
    }

    #endregion

    #region Implementation of INotifyPropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;

    protected void SetProperty<T>(ref T field, T value, string propertyName)
    {
      if (EqualityComparer<T>.Default.Equals(field, value))
        return;
      field = value;
      OnPropertyChanged(propertyName);
    }

    protected void OnPropertyChanged(string propertyName)
    {
      var handler = PropertyChanged;
      if (handler != null)
        handler(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region Implementation of IDisposable

    public void Dispose()
    {
      if (_messageSink == null)
        return;

      _messageSink.OnWinProc -= WndProc;
      _messageSink.Dispose();
      _messageSink = null;

      _taskbarIcon.Visibility = Visibility.Collapsed;
      _taskbarIcon.Dispose();
      _taskbarIcon = null;
    }

    #endregion
  }
}
