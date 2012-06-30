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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.ServiceMonitor.Model;
using MediaPortal.ServiceMonitor.UPNP;
using MediaPortal.ServiceMonitor.View;
using MediaPortal.Utilities.Collections;

namespace MediaPortal.ServiceMonitor.ViewModel
{
  /// <summary>
  /// Main controller of the application.
  /// </summary>
  public class AppController : IDisposable, INotifyPropertyChanged, IAppController, IMessageReceiver

  {
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

    #region ServerStatus

    private ServerStatus _status;
    public ServerStatus Status
    {
      get { return _status; }
      set { SetProperty(ref _status, value, "Status"); }
    }

    #endregion


    public AppController()
    {
      Clients = new AsyncObservableCollection<ClientData>();
      
      // Init ServerConnection Messaging
      ServiceRegistration.Get<IMessageBroker>().RegisterMessageReceiver(ServerConnectionMessaging.CHANNEL, this);
      // Set Init ServerStatus
      Status = new ServerStatus{Message = "Not Connected to Server"};
    }



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
      ((Window)sender).Closing -= OnMainWindowClosing;

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
      var taskbarIcon = (TaskbarIcon)Application.Current.FindResource("TrayIcon");
      if (taskbarIcon != null)
        taskbarIcon.DataContext = this;
      return taskbarIcon;
      
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
      var type = (ServerConnectionMessaging.MessageType)message.MessageType;
      switch (type)
      {
        case ServerConnectionMessaging.MessageType.AvailableServersChanged:
          //var serverAdded = (bool)message.MessageData[ServerConnectionMessaging.SERVERS_WERE_ADDED];
          //var availableServers = (ICollection<ServerDescriptor>)message.MessageData[ServerConnectionMessaging.AVAILABLE_SERVERS];
          break;
        case ServerConnectionMessaging.MessageType.HomeServerAttached:
          Status = new ServerStatus { Message = "Attached to Server" };
          break;
        case ServerConnectionMessaging.MessageType.HomeServerConnected:
          Status = new ServerStatus { Message = "Connected to Server" };
          UpdateClients();
          break;
        case ServerConnectionMessaging.MessageType.HomeServerDetached:
          Status = new ServerStatus { Message = "Detached from Server" };
          break;
        case ServerConnectionMessaging.MessageType.HomeServerDisconnected:
          Status = new ServerStatus { Message = "Disconnected from Server" };
          break;
      }
    }

    #endregion


    public void UpdateClients()
    {
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


  }
}