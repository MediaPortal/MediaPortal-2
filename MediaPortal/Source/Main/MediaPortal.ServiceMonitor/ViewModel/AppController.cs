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
using System.Windows;
using System.Windows.Controls.Primitives;
using Hardcodet.Wpf.TaskbarNotification;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.ServiceMonitor.Model;
using MediaPortal.ServiceMonitor.UPNP;
using MediaPortal.ServiceMonitor.View;
using System.ServiceProcess;
using MediaPortal.ServiceMonitor.View.SystemTray;

namespace MediaPortal.ServiceMonitor.ViewModel
{
  /// <summary>
  /// Main controller of the application.
  /// </summary>
  public class AppController : IDisposable, INotifyPropertyChanged, IAppController, IMessageReceiver
  {
    #region const variables
    private const string MP2_SERVER_SERVICE_NAME = "MP-II Server Service";

    #endregion

    #region TaskbarIcon

    /// <summary>
    /// Provides access to the system tray area.
    /// </summary>
    private TaskbarIcon _taskbarIcon;

    public TaskbarIcon TaskbarIcon
    {
      get { return _taskbarIcon; }
      set
      {
        if (_taskbarIcon != null)
        {
          //dispose current tray handler
          _taskbarIcon.Dispose();
        }

        _taskbarIcon = value;
        OnPropertyChanged("TaskbarIcon");
      }
    }

    #endregion

    #region Attached Clients

    public AsyncObservableCollection<ClientData> Clients { get; set; }


    #endregion

    #region ServerStatus / ServerConnectionStatus

    private ServerStatus _status;

    public ServerStatus Status
    {
      get { return _status; }
      set { SetProperty(ref _status, value, "Status"); }
    }

    public ServerConnectionMessaging.MessageType ServerConnectionType;

    #endregion

    #region ctor
    public AppController()
    {
      Clients = new AsyncObservableCollection<ClientData>();
      ServerConnectionType = ServerConnectionMessaging.MessageType.HomeServerDisconnected;

      // Init ServerConnection Messaging
      ServiceRegistration.Get<IMessageBroker>().RegisterMessageReceiver(ServerConnectionMessaging.CHANNEL, this);
      // Set Init ServerStatus
      UpdateServerStatus();
    }

    #endregion

    #region Show Main Window

    /// <summary>
    /// Displays the main application window and assigns
    /// it as the application's <see cref="Application.MainWindow"/>.
    /// </summary>
    public void ShowMainWindow()
    {
      var app = Application.Current;

      if (app.MainWindow == null)
      {
        //create and show new main window
        app.MainWindow = new MainWindow();
        app.MainWindow.Show();
        app.MainWindow.Activate();
        app.MainWindow.Closing += OnMainWindowClosing;
        if (TaskbarIcon == null)
        {
          TaskbarIcon = InitSystemTray();
        }
      }
      else
      {
        //just show the window on top of others
        app.MainWindow.Focus();
        app.MainWindow.Activate();
      }

      //hide tray icon
      //if (TaskbarIcon != null) TaskbarIcon.Visibility = Visibility.Collapsed;
    }


    #endregion

    #region Close / Minimize

    /// <summary>
    /// Closes the main window and 
    /// </summary>
    protected void OnMainWindowClosing(object sender, CancelEventArgs e)
    {
      //deregister event listener, if required
      ((Window) sender).Closing -= OnMainWindowClosing;

      //reset main window in order to prevent further code
      //to close it again while it is being closed
      Application.Current.MainWindow = null;

      //close application if necessary
      CloseMainApplication(false);
    }


    /// <summary>
    /// Minimizes the application to the system tray.
    /// </summary>
    public void MinimizeToTray()
    {
      //close main window
      var mainWindow = Application.Current.MainWindow;
      if (mainWindow != null)
      {
        //deregister closing event listener - if this method is not invoked
        //due to the window being closes already, the closing window
        //will not trigger any further action
        mainWindow.Closing -= OnMainWindowClosing;
        mainWindow.Close();
      }

      if (TaskbarIcon == null)
      {
        TaskbarIcon = InitSystemTray();
      }

      //show tray icon
      TaskbarIcon.Visibility = Visibility.Visible;
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
      {
        MinimizeToTray();
      }
      else
      {
        //dispose
        Dispose();

        //shutdown the application
        Application.Current.Shutdown();
      }
    }

    #endregion

    #region Init SystemTray

    /// <summary>
    /// Inits the component that displays status information in the
    /// system tray.
    /// </summary>
    public TaskbarIcon InitSystemTray()
    {
      var taskbarIcon = (TaskbarIcon) Application.Current.FindResource("TrayIcon");
      if (taskbarIcon != null)
        taskbarIcon.DataContext = this;
      return taskbarIcon;

    }

    #endregion

    #region Windows Service Functions

    public bool IsServerServiceInstalled()
    {
      var services = ServiceController.GetServices();
      return services.Any(service => String.Compare(service.ServiceName, MP2_SERVER_SERVICE_NAME, System.StringComparison.OrdinalIgnoreCase) == 0);
    }

    public bool IsServerServiceRunning()
    {
      try
      {
        using (var serviceController = new ServiceController(MP2_SERVER_SERVICE_NAME))
        {
          return serviceController.Status == ServiceControllerStatus.Running;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(
          "Check whether the MP-II Server Service is running failed. Please check your installation.", ex);
        return false;
      }
    }


    public bool StartServerService()
    {
      try
      {
        using (var serviceController = new ServiceController(MP2_SERVER_SERVICE_NAME))
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
        ServiceRegistration.Get<ILogger>().Error("Starting MP-II Server Service failed.", ex);
        UpdateServerStatus();
        return false;
      }
    }


    public bool StopServerService()
    {
      try
      {
        using (var serviceController = new ServiceController(MP2_SERVER_SERVICE_NAME))
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
        ServiceRegistration.Get<ILogger>().Error("Stopping MP-II Server Service failed.", ex);
        UpdateServerStatus();
        return false;
      }
    }
    
    #endregion

    #region Implementation of IDisposable

    public void Dispose()
    {
      //reset system tray handler
      TaskbarIcon = null;
    }

    #endregion

    #region Implementation of INotifyPropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;

    protected void SetProperty<T>(ref T field, T value, string propertyName)
    {
      if (!EqualityComparer<T>.Default.Equals(field, value))
      {
        field = value;
        OnPropertyChanged(propertyName);
      }
    }

    protected void OnPropertyChanged(string propertyName)
    {
      var handler = PropertyChanged;
      if (handler != null)
      {
        handler(this, new PropertyChangedEventArgs(propertyName));
      }
    }


    #endregion

    #region Implementation of IMessageReceiver

    public void Receive(SystemMessage message)
    {
      var connectionType = (ServerConnectionMessaging.MessageType)message.MessageType;
      switch (connectionType)
      {
        case ServerConnectionMessaging.MessageType.ClientsOnlineStateChanged:
          UpdateClients();
          break;
        case ServerConnectionMessaging.MessageType.AvailableServersChanged:
          //var serverAdded = (bool)message.MessageData[ServerConnectionMessaging.SERVERS_WERE_ADDED];
          //var availableServers = (ICollection<ServerDescriptor>)message.MessageData[ServerConnectionMessaging.AVAILABLE_SERVERS];
          break;
        case ServerConnectionMessaging.MessageType.HomeServerAttached:
        case ServerConnectionMessaging.MessageType.HomeServerConnected:
        case ServerConnectionMessaging.MessageType.HomeServerDetached:
        case ServerConnectionMessaging.MessageType.HomeServerDisconnected:
          ServerConnectionType = connectionType;
          UpdateServerStatus();
          break;
      }
    }

    #endregion

    #region Update visible Clients / Server Info
    public void UpdateServerStatus()
    {
      var oldStatus = Status;
      if (!IsServerServiceInstalled())
        Status = new ServerStatus { Message = "Service is NOT installed" };
      else
      {
        if (!IsServerServiceRunning())
          Status = new ServerStatus {Message = "Service is NOT started"};
        else
        {
          switch (ServerConnectionType)
          {
            case ServerConnectionMessaging.MessageType.HomeServerAttached:
              Status = new ServerStatus { Message = "Attached to Server" };
              break;
            case ServerConnectionMessaging.MessageType.HomeServerConnected:
              Status = new ServerStatus { Message = "Connected to Server" };
              break;
            case ServerConnectionMessaging.MessageType.HomeServerDetached:
              Status = new ServerStatus { Message = "Detached from Server" };
              break;
            default:
              Status = new ServerStatus { Message = "Disconnected from Server" };
              break;
          }
        }
      }
      if ((oldStatus == null) || (oldStatus.Message.Equals(Status.Message))) return;

      ServiceRegistration.Get<ILogger>().Info("ServerStatus: {0}", Status.Message);

      var balloon = new BalloonPopup {BalloonText = Status.Message};
      //show balloon and close it after 5 seconds
      TaskbarIcon.ShowCustomBalloon(balloon, PopupAnimation.Slide, 5000);
    }

    public void UpdateClients()
    {

      var balloon = new BalloonPopup { BalloonText = "Updating Client(s)..." };
      //show balloon and close it after 5 seconds
      TaskbarIcon.ShowCustomBalloon(balloon, PopupAnimation.Slide, 5000);

      try
      {
        Clients.Clear();
        var serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();
        if (!serverConnectionManager.IsHomeServerConnected) return;

        var serverControler = serverConnectionManager.ServerController;
        if (serverControler == null) return;

        foreach (var attachedClient in serverControler.GetAttachedClients())
        {
          Clients.Add(new ClientData
                        {
                          IsConnected = !string.IsNullOrEmpty(attachedClient.SystemId),
                          Name = attachedClient.LastClientName,
                          System = attachedClient.LastSystem == null ? string.Empty : attachedClient.LastSystem.HostName
                        });
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("AppController.UpDateClients", ex);
      }
    }
    #endregion

  }
}