#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

    private string _serverStatus;

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

    public TaskbarIcon TaskbarIcon
    {
      get { return _taskbarIcon; }
      set
      {
        if (_taskbarIcon != null)
          _taskbarIcon.Dispose();

        _taskbarIcon = value;
        OnPropertyChanged("TaskbarIcon");
      }
    }

    public ObservableCollectionMt<ClientData> Clients { get; set; }

    public string ServerStatus
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

    public void StartUp(bool hideMainWindow)
    {
      ServiceRegistration.Get<ILogger>().Debug("StartUp ({0})", hideMainWindow);
      if (Application.Current.MainWindow == null)
        InitMainWindow();
      
      if (TaskbarIcon == null)
        TaskbarIcon = InitSystemTray();
      
      if (hideMainWindow)
        HideMainWindow();
      else
        ShowMainWindow();

      // Set initial ServerStatus
      UpdateServerStatus();
    }

    protected void InitMainWindow()
    {
      ServiceRegistration.Get<ILogger>().Debug("InitMainWindow");
      Application.Current.MainWindow = new MainWindow
        {ShowActivated = false, ShowInTaskbar = false, Visibility = Visibility.Collapsed };
      Application.Current.MainWindow.Closing += OnMainWindowClosing;
      Application.Current.MainWindow.SourceInitialized += OnMainWindowSourceInitialized;
      Application.Current.MainWindow.Show();
    }

    /// <summary>
    /// Displays the main application window and assigns it as the application's <see cref="Application.MainWindow"/>.
    /// </summary>
    public void ShowMainWindow()
    {
      ServiceRegistration.Get<ILogger>().Debug("ShowMainWindow");
      
      if (Application.Current.MainWindow.WindowState == WindowState.Minimized)
        Application.Current.MainWindow.WindowState = WindowState.Normal;

      // Show the window on top of others
      Application.Current.MainWindow.Visibility = Visibility.Visible;
      Application.Current.MainWindow.ShowInTaskbar = true;
      Application.Current.MainWindow.Focus();
      Application.Current.MainWindow.Activate();
      OnPropertyChanged("ServerStatus");
    }

    /// <summary>
    /// Minimizes the application to the system tray.
    /// </summary>
    public void HideMainWindow()
    {
      ServiceRegistration.Get<ILogger>().Debug("HideMainWindow");
      if (Application.Current.MainWindow != null)
      {
        gkjb
        Application.Current.MainWindow.ShowInTaskbar = false;
        Application.Current.MainWindow.Visibility = Visibility.Collapsed;
      }
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
      if (msg == WinApi.MP2_SHOWME)
        ShowMainWindow();
      
      if (msg == WM_POWERBROADCAST)
      {
        ServiceRegistration.Get<ILogger>().Debug("WndProc: [{0}]", wParam);
        var serverConnectionManager = (ServerConnectionManager)ServiceRegistration.Get<IServerConnectionManager>();
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
    protected void OnMainWindowClosing(object sender, CancelEventArgs e)
    {
      ServiceRegistration.Get<ILogger>().Debug("OnMainWindowClosing");
      
      CloseMainApplication(false);
      e.Cancel = true;
    }

    /// <summary>
    /// Closes the main window and either exits the application or displays
    /// the taskbar icon and remains active.
    /// </summary>
    /// <param name="forceShutdown">Whether the application
    /// should perform a shutdown anyway.</param>
    public void CloseMainApplication(bool forceShutdown)
    {
      if (!forceShutdown)
        HideMainWindow();
      else
      {
        ServiceRegistration.Get<ILogger>().Debug("ClosingMainApplication");
        Application.Current.MainWindow.Closing -= OnMainWindowClosing;

        _messageSink.OnWinProc -= WndProc;
        _messageSink.Dispose();
        _messageSink = null;
        
        // Reset main window in order to prevent further code to close it again while it is being closed
        Application.Current.MainWindow = null;
        Dispose();
        Application.Current.Shutdown();
      }
    }

    /// <summary>
    /// Inits the component that displays status information in the system tray.
    /// </summary>
    public TaskbarIcon InitSystemTray()
    {
      ServiceRegistration.Get<ILogger>().Debug("InitSystemTray");
      var taskbarIcon = (TaskbarIcon) Application.Current.FindResource("TrayIcon");
      if (taskbarIcon != null)
      {
        taskbarIcon.DataContext = this;
        taskbarIcon.Visibility = Visibility.Visible;
      }
      return taskbarIcon;

    }

    #endregion

    #region Windows Service Functions

    /// <summary>
    /// Checks if the MP2 Server Service is installed.
    /// </summary>
    public bool IsServerServiceInstalled()
    {
      var services = ServiceController.GetServices();
      return
        services.Any(
          service =>
          String.Compare(service.ServiceName, SERVER_SERVICE_NAME, StringComparison.OrdinalIgnoreCase) == 0);
    }

    /// <summary>
    /// Checks if the installed MP2 Server Service is running.
    /// </summary>
    public bool IsServerServiceRunning()
    {
      try
      {
        using (var serviceController = new ServiceController(SERVER_SERVICE_NAME))
        {
          return serviceController.Status == ServiceControllerStatus.Running;
        }
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
        if (((LocalizationMessaging.MessageType) message.MessageType) == LocalizationMessaging.MessageType.LanguageChanged)
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
      	ServerStatus = ServiceRegistration.Get<ILocalization>().ToString("[ServerStatus.NotStarted]");
      }
      switch (_serverConnectionStatus)
      {
        case ServerConnectionMessaging.MessageType.HomeServerAttached:
      		ServerStatus = ServiceRegistration.Get<ILocalization>().ToString("[ServerStatus.Attached]");
          break;
        case ServerConnectionMessaging.MessageType.HomeServerConnected:
          ServerStatus = ServiceRegistration.Get<ILocalization>().ToString("[ServerStatus.Connected]");
          break;
        case ServerConnectionMessaging.MessageType.HomeServerDetached:
          ServerStatus = ServiceRegistration.Get<ILocalization>().ToString("[ServerStatus.Detached]");
          break;
        default:
          ServerStatus = ServiceRegistration.Get<ILocalization>().ToString("[ServerStatus.Disconnected]");
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
      ServerStatus = ServiceRegistration.Get<ILocalization>().ToString("[ServerStatus.ClientConnected]");
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
      TaskbarIcon = null;
    }

    #endregion
  }
}