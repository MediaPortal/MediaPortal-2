#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Utilities.Process;
using MediaPortal.Utilities.SystemAPI;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace MediaPortal.Client.Launcher
{
  /// <summary>
  /// The main class for the MediaPortal 2 ClientLauncher.
  /// </summary>
  internal static class ApplicationLauncher
  {
    #region Constants

    // Unique id for global mutex - Global prefix means it is global to the entire machine
    internal const string MUTEX_ID = @"Global\{B47FDAA6-7DFD-438C-A644-61DACE40AFF6}";

    internal const string AUTOSTART_REGISTER_NAME = "MP2-ClientLauncher";

    #endregion

    #region Static fields

    private static NotifyIcon _systemNotificationAreaIcon;
    private static IpcServer _ipcServer;
    private static RawMessageHandler _msgHandler;

    #endregion

    #region Methods

    internal static void InitMsgHandler()
    {
      if (_msgHandler != null)
        return;
      ServiceRegistration.Get<ILogger>().Debug("Initializing Message Handler");
      try
      {
        _msgHandler = new RawMessageHandler();
        _msgHandler.OnStartRequest += (s, e) =>
        {
          ILogger logger = ServiceRegistration.Get<ILogger>();
          logger.Info("Received StartButton press");

          Process[] processes = GetMP2ClientProcesses();

          if (processes.Length == 0)
          {
            logger.Info("MP2-Client is not running - starting it.");
            StartClient();
          }
          else if (processes.Length == 1)
          {
            logger.Info("MP2-Client is already running - switching focus.");
            SwitchFocus();
          }
          else
          {
            logger.Info("More than one window named 'MediaPortal' has been found!");
            foreach (Process procName in processes)
            {
              logger.Debug("MPTray: {0} (Started: {1}, ID: {2})", procName.ProcessName,
                           procName.StartTime.ToShortTimeString(), procName.Id);
            }
          }
        };
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(ex);
      }
    }

    internal static void CloseMsgHandler()
    {
      if (_msgHandler == null)
        return;
      try
      {
        _msgHandler.Dispose();
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(ex);
      }
      _msgHandler = null;
    }

    internal static void InitIpc()
    {
      if (_ipcServer != null)
        return;
      ServiceRegistration.Get<ILogger>().Debug("Initializing IPC");
      try
      {
        _ipcServer = new IpcServer("ClientLauncher");
        _ipcServer.CustomShutdownCallback = () =>
        {
          if (_systemNotificationAreaIcon != null)
          {
            _systemNotificationAreaIcon.Visible = false;
            _systemNotificationAreaIcon = null;
          }
          Application.Exit();
          return true;
        };
        _ipcServer.CustomRestartCallback = () =>
        {
          StopClient();
          StartClient();
          return true;
        };
        _ipcServer.Open();
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(ex);
      }
    }

    internal static void CloseIpc()
    {
      if (_ipcServer == null)
        return;
      try
      {
        _ipcServer.Close();
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(ex);
      }
      _ipcServer = null;
    }

    //private static void InitTrayIcon()
    //{
    //  ServiceRegistration.Get<ILogger>().Debug("Initializing TrayIcon");

    //  if (_systemNotificationAreaIcon == null)
    //    try
    //    {
    //      MenuItem closeItem = new MenuItem { Index = 0, Text = "Close" };
    //      MenuItem startClientItem = new MenuItem { Index = 0, Text = "Start MP2-Client" };
    //      MenuItem preferX64Item = new MenuItem { Index = 0, Text = "Use 64 Bit Client", Enabled = SupportsX64, Checked = UseX64 };
    //      MenuItem addAutostartItem = new MenuItem { Index = 0, Text = "Add to Autostart" };
    //      MenuItem removeAutostartItem = new MenuItem { Index = 0, Text = "Remove from Autostart" };

    //      closeItem.Click += delegate (object sender, EventArgs args)
    //      {
    //        _systemNotificationAreaIcon.Visible = false;
    //        _systemNotificationAreaIcon = null;
    //        Application.Exit();
    //      };
    //      startClientItem.Click += delegate (object sender, EventArgs args)
    //      {
    //        StartClient();
    //      };
    //      preferX64Item.Click += delegate(object sender, EventArgs args)
    //      {
    //        UseX64 = !UseX64;
    //        preferX64Item.Checked = UseX64;
    //      };
    //      addAutostartItem.Click += delegate (object sender, EventArgs args)
    //      {
    //        IsAutoStartEnabled = true;
    //        addAutostartItem.Enabled = !IsAutoStartEnabled;
    //        removeAutostartItem.Enabled = IsAutoStartEnabled;
    //        WriteAutostartAppEntryInRegistry();
    //      };
    //      removeAutostartItem.Click += delegate (object sender, EventArgs args)
    //      {
    //        IsAutoStartEnabled = false;
    //        addAutostartItem.Enabled = !IsAutoStartEnabled;
    //        removeAutostartItem.Enabled = IsAutoStartEnabled;
    //        WriteAutostartAppEntryInRegistry();
    //      };

    //      addAutostartItem.Enabled = !IsAutoStartEnabled;
    //      removeAutostartItem.Enabled = IsAutoStartEnabled;

    //      // Initialize contextMenuTray
    //      ContextMenu contextMenuTray = new ContextMenu();
    //      contextMenuTray.MenuItems.AddRange(new[]
    //      {
    //        startClientItem,
    //        new MenuItem("-"),
    //        preferX64Item,
    //        new MenuItem("-"),
    //        addAutostartItem, removeAutostartItem,
    //        new MenuItem("-"),
    //        closeItem
    //      });


    //      _systemNotificationAreaIcon = new NotifyIcon
    //      {
    //        ContextMenu = contextMenuTray,
    //        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
    //        Text = "MediaPortal Tray Launcher",
    //        Visible = true
    //      };
    //    }
    //    catch (Exception ex)
    //    {
    //      ServiceRegistration.Get<ILogger>().Error("Could not init tray icon", ex);
    //    }
    //}

    private static string GetMP2ClientPath()
    {
      string path = Assembly.GetExecutingAssembly().Location;
      string startExe = "MP2-Client.exe";
      try
      {
        do
        {
          // Get parent dir of current path location
          path = Directory.GetParent(path).Parent?.FullName;
          if (path == null)
            break;
          path = Path.Combine(path, startExe);
        } while (!File.Exists(path));
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("MP2-Client.exe not found in any parent directories.", ex);
        return null;
      }

      return path;
    }

    internal static void StartClient()
    {
      try
      {
        string clientExe = GetMP2ClientPath();
        ProcessStartInfo psi = new ProcessStartInfo(clientExe)
        {
          WorkingDirectory = Directory.GetParent(clientExe).FullName
        };
        Process.Start(psi);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("MP2-Client.exe couldn't be started.", ex);
      }
    }

    private static void StopClient()
    {
      try
      {
        Process[] processes = GetMP2ClientProcesses();
        if (processes.Length == 0)
          return;

        // If process is running, send an IPC command to force closing.
        using (var client = new IpcClient("Client"))
        {
          client.Connect();
          client.ShutdownApplication(2000, true);
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("MP2-Client.exe couldn't be started.", ex);
      }
    }

    private static Process[] GetMP2ClientProcesses()
    {
      return Process.GetProcessesByName("MP2-Client").Union(
        Process.GetProcessesByName("MP2-Client (x64)")).ToArray();
    }

    private static void SwitchFocus()
    {
      Process[] processes = GetMP2ClientProcesses();

      if (processes.Length > 0)
      {
        IntPtr handle = processes[0].MainWindowHandle;

        // Make MediaPortal window normal ( if minimized )
        NativeMethods.ShowWindow(handle, NativeMethods.ShowWindowFlags.ShowNormal);

        // Make Mediaportal window focused
        if (NativeMethods.SetForegroundWindow(handle, true))
          ServiceRegistration.Get<ILogger>().Info("Successfully switched focus.");
      }
      else
        ServiceRegistration.Get<ILogger>().Info("MediaPortal is not running (yet).");
    }

    internal static bool TerminateProcess(string processName)
    {
      ServiceRegistration.Get<ILogger>().Debug("TerminateProcess");

      bool terminatedProcess = false;

      try
      {
        foreach (Process process in Process.GetProcessesByName(processName))
        {
          if (process != Process.GetCurrentProcess())
          {
            process.Kill();
            process.Close();

            if (terminatedProcess == false)
              terminatedProcess = true;
          }
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error while terminating process(es): {0}", ex, processName);
      }

      return terminatedProcess;
    }

    internal static void WriteAutostartAppEntryInRegistry(bool isAutoStartEnabled)
    {
      try
      {
        string applicationPath = string.Format("\"{0}\"", ServiceRegistration.Get<IPathManager>().GetPath("<APPLICATION_PATH>"));
#if DEBUG
        applicationPath = applicationPath.Replace(".vshost", "");
#endif
        if (isAutoStartEnabled)
          WindowsAPI.AddAutostartApplication(applicationPath, AUTOSTART_REGISTER_NAME, true);
        else
          WindowsAPI.RemoveAutostartApplication(AUTOSTART_REGISTER_NAME, true);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Can't write autostart value to registry", ex);
      }
    }

    #endregion
  }
}
