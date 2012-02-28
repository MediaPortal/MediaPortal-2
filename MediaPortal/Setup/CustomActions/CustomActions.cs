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
using System.Globalization;
using System.IO;
using System.Text;
using MediaPortal.Backend.Services.ClientCommunication;
using MediaPortal.Backend.Services.SystemResolver;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Registry;
using MediaPortal.Common.Services.Localization;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Services.PathManager;
using MediaPortal.Common.Services.PluginManager;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemResolver;
using MediaPortal.UI.ServerCommunication.Settings;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Win32;

namespace CustomActions
{
  public class CustomActions
  {//Can publish up to 16 custom actions per DLL

    protected static readonly string[] ClientPathLabels = new string[] { "DATA", "CONFIG", "LOG", "PLUGINS" };
    protected static readonly string[] ServerPathLabels = new string[] { "DATA", "CONFIG", "LOG", "PLUGINS", "DATABASE" };

    protected class SessionLogWriter : TextWriter
    {
      protected Session _session;

      public SessionLogWriter(Session session)
      {
        _session = session;
      }

      public override void WriteLine(string value)
      {
 	       _session.Log(value);
      }

      public override Encoding Encoding
      {
        get { return Encoding.UTF8; }
      }
    }

    protected struct HomeServerData
    {
      public string HomeServerSystemId;
      public string HomeServerName;
    }

    protected static HomeServerData GetHomeServerData(string serverApplicationPath)
    {
      PathManager pathManager = new PathManager(); // Fake path manager to inject the settings path to SettingsManager and StringManager

      pathManager.InitializeDefaults(serverApplicationPath);
      ServiceRegistration.Set<IPathManager>(pathManager);
      ISettingsManager settingsManager = new SettingsManager();
      ServiceRegistration.Set<ISettingsManager>(settingsManager);
      ServiceRegistration.Set<IRegistry>(new MediaPortal.Common.Services.Registry.Registry());
      ServiceRegistration.Set<IPluginManager>(new PluginManager());

      StringManager stringManager = new StringManager();
      stringManager.Startup();
      ServiceRegistration.Set<ILocalization>(stringManager);

      ISystemResolver backendSystemResolver = new SystemResolver();

      HomeServerData result = new HomeServerData
        {
            HomeServerName = new LocalizedUPnPDeviceInformation().GetFriendlyName(CultureInfo.InvariantCulture),
            HomeServerSystemId = backendSystemResolver.LocalSystemId
        };

      ServiceRegistration.RemoveAndDispose<ILocalization>();
      ServiceRegistration.RemoveAndDispose<IPluginManager>();
      ServiceRegistration.RemoveAndDispose<IRegistry>();
      ServiceRegistration.RemoveAndDispose<ISettingsManager>();
      ServiceRegistration.RemoveAndDispose<IPathManager>();

      return result;
    }

    protected static void AttachClientToServer(string clientApplicationPath, HomeServerData homeServerData)
    {
      PathManager pathManager = new PathManager(); // Fake path manager to inject the settings path to SettingsManager and StringManager

      pathManager.InitializeDefaults(clientApplicationPath);
      ServiceRegistration.Set<IPathManager>(pathManager);
      ISettingsManager settingsManager = new SettingsManager();
      ServiceRegistration.Set<ISettingsManager>(settingsManager);
      ServiceRegistration.Set<IRegistry>(new MediaPortal.Common.Services.Registry.Registry());
      ServiceRegistration.Set<IPluginManager>(new PluginManager());

      StringManager stringManager = new StringManager();
      stringManager.Startup();
      ServiceRegistration.Set<ILocalization>(stringManager);

      ServerConnectionSettings serverConnectionSettings = settingsManager.Load<ServerConnectionSettings>();

      if (serverConnectionSettings.HomeServerSystemId == null)
      {
        ServiceRegistration.Get<ILogger>().Info("Client is not attached, auto-attaching to local server '{0}', system id '{1}'", homeServerData.HomeServerName, homeServerData.HomeServerSystemId);

        serverConnectionSettings.HomeServerSystemId = homeServerData.HomeServerSystemId;
        serverConnectionSettings.LastHomeServerSystem = SystemName.Loopback();
        serverConnectionSettings.LastHomeServerName = homeServerData.HomeServerName;
        settingsManager.Save(serverConnectionSettings);
      }
      else
        ServiceRegistration.Get<ILogger>().Info("Client is already attached to server with system ID '{0}'", serverConnectionSettings.HomeServerSystemId);

      ServiceRegistration.RemoveAndDispose<ILocalization>();
      ServiceRegistration.RemoveAndDispose<IPluginManager>();
      ServiceRegistration.RemoveAndDispose<IRegistry>();
      ServiceRegistration.RemoveAndDispose<ISettingsManager>();
      ServiceRegistration.RemoveAndDispose<IPathManager>();
    }

    [CustomAction]
    public static ActionResult AttachClientToServer(Session session)
    {
      session.Log("ClientRequestState : {0}", session.Features["Client"].RequestState);
      session.Log("ServerRequestState : {0}", session.Features["Server"].RequestState);

      if (session.Features["Client"].RequestState == InstallState.Local
        & session.Features["Server"].RequestState == InstallState.Local)
      {
        string clientApplicationPath = Path.Combine(session["INSTALLDIR_CLIENT"], "MP2-Client.exe");
        string serverApplicationPath = Path.Combine(session["INSTALLDIR_SERVER"], "MP2-Server.exe");

        ServiceRegistration.Set<ILogger>(new DefaultLogger(new SessionLogWriter(session), LogLevel.All, false, false)); // Logger for called services

        HomeServerData homeServerData = GetHomeServerData(serverApplicationPath);
        AttachClientToServer(clientApplicationPath, homeServerData);
        ServiceRegistration.RemoveAndDispose<ILogger>();
      }
      else
      {
        session.Log("Installation mode is not SingleSeat. Skipping auto-attach step.");
      }

      return ActionResult.Success;
    }

    [CustomAction]
    public static ActionResult ReadCustomPaths(Session session)
    {
      ReadCustomPaths(session, "CLIENT", ClientPathLabels);
      ReadCustomPaths(session, "SERVER", ServerPathLabels);

      return ActionResult.Success;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="session"></param>
    /// <param name="cs">String which can be CLIENT or SERVER to access the properties of sessions.</param>
    /// <param name="pathLabels">List of path labels which are available for Client or Server.</param>
    private static void ReadCustomPaths(Session session, string cs, IEnumerable<string> pathLabels)
    {
      string regKey = String.Format("SOFTWARE\\{0}\\{1}", session["Manufacturer"], session["ProductName"]);
      string regValue = "INSTALLDIR_" + cs;

      // reading install dir from registry
      RegistryKey masterKey = Registry.LocalMachine.OpenSubKey(regKey);
      if (masterKey == null)
      {
        session.Log("RegKey HKLM\\{0} not opened/found.", regKey);
        return;
      }

      string installDir = masterKey.GetValue(regValue, string.Empty) as string;
      if (String.IsNullOrEmpty(installDir))
      {
        session.Log("RegValue {1} in HKLM\\{0} not opened/found.", regKey, regValue);
        return;
      }

      session.Log("{0} read from registry: {1}", regValue, installDir);

      // reading other dirs from paths.xml
      installDir = MediaPortal.Utilities.StringUtils.RemoveSuffixIfPresent(installDir, "\\");

      string pathsFile = installDir + "\\Defaults\\Paths.xml";
      if (!File.Exists(pathsFile)) return;

      session.Log("Paths file found, reading it: {0}", pathsFile);
      IPathManager pathManager = new PathManager();
      pathManager.SetPath("APPLICATION_ROOT", installDir);
      pathManager.SetPath("LOCAL_APPLICATION_DATA",
                          Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
      pathManager.SetPath("COMMON_APPLICATION_DATA",
                          Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
      pathManager.SetPath("MY_DOCUMENTS", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
      pathManager.SetPath("DEFAULTS", installDir + "\\Defaults");
      pathManager.LoadPaths(pathsFile);

      foreach (string label in pathLabels)
      {
        session[cs + "." + label + ".FOLDER"] = pathManager.GetPath("<" + label + ">");
      }
    }

    [CustomAction]
    public static ActionResult SetCustomPaths(Session session)
    {
      SetCustomPaths(session, "CLIENT", ClientPathLabels);
      SetCustomPaths(session, "SERVER", ServerPathLabels);

      return ActionResult.Success;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="session"></param>
    /// <param name="cs">String which can be CLIENT or SERVER to access the properties of sessions.</param>
    /// <param name="pathLabels">List of path labels which are available for Client or Server.</param>
    private static void SetCustomPaths(Session session, string cs, IEnumerable<string> pathLabels)
    {
      foreach (string label in pathLabels)
      {
        string path = session[cs + "." + label + ".FOLDER"];

        string tmpPath = session["INSTALLDIR_" + cs];
        path = path.Replace(MediaPortal.Utilities.StringUtils.RemoveSuffixIfPresent(tmpPath, "\\"),
          "<APPLICATION_ROOT>");

        tmpPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        path = path.Replace(MediaPortal.Utilities.StringUtils.RemoveSuffixIfPresent(tmpPath, "\\"),
          "<LOCAL_APPLICATION_DATA>");

        tmpPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        path = path.Replace(MediaPortal.Utilities.StringUtils.RemoveSuffixIfPresent(tmpPath, "\\"),
          "<COMMON_APPLICATION_DATA>");

        tmpPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        path = path.Replace(MediaPortal.Utilities.StringUtils.RemoveSuffixIfPresent(tmpPath, "\\"),
          "<MY_DOCUMENTS>");

        path = MediaPortal.Utilities.StringUtils.RemoveSuffixIfPresent(path, "\\");

        foreach (string l in ClientPathLabels)
        {
          // only check if parts of string equals if we have a different labels
          if (l.Equals(label)) continue;

          // only replace if p is not empty
          string p = session["XML." + cs + "." + l + ".FOLDER"];
          if (String.IsNullOrEmpty(p)) continue;

          p = p.Replace(path, "<" + label + ">");
          path = path.Replace(p, "<" + l + ">");

          session["XML." + cs + "." + l + ".FOLDER"] = p;
        }

        session["XML." + cs + "." + label + ".FOLDER"] = path;
        session.Log("XML.{1}={0}", path, cs + "." + label + ".FOLDER");
      }
    }
  }
}
