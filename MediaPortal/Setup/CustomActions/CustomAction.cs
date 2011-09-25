#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.IO;
using Microsoft.Deployment.WindowsInstaller;

using MediaPortal.Backend.Services.SystemResolver;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Services.PathManager;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemResolver;
using MediaPortal.UI.ServerCommunication.Settings;
using Microsoft.Win32;

namespace CustomActions
{
  public class CustomActions
  {//Can publish up to 16 custom actions per DLL

    private static readonly string[] ClientPathLabels = new string[] { "DATA", "CONFIG", "LOG", "PLUGINS", "REMOTERESOURCES" };
    private static readonly string[] ServerPathLabels = new string[] { "DATA", "CONFIG", "LOG", "PLUGINS", "REMOTERESOURCES", "DATABASE" };

    [CustomAction]
    public static ActionResult AttachClientAndServer(Session session)
    {
      session.Log("ClientRequestState : {0}", session.Features["Client"].RequestState);
      session.Log("ServerRequestState : {0}", session.Features["Server"].RequestState);

      if (session.Features["Client"].RequestState == InstallState.Local
        & session.Features["Server"].RequestState == InstallState.Local)
      {
        // todo: check which code is really needed: serviceregistration? pathmanager?
        ServiceRegistration.Set<ILogger>(new NoLogger());

        IPathManager pathManager = new PathManager();
        ServiceRegistration.Set<IPathManager>(pathManager);
        ServiceRegistration.Set<ISettingsManager>(new SettingsManager());

        string applicationPath = Environment.GetCommandLineArgs()[0];
        pathManager.SetPath("APPLICATION_PATH", applicationPath);
        pathManager.SetPath("APPLICATION_ROOT", Path.GetDirectoryName(applicationPath));
        pathManager.SetPath("LOCAL_APPLICATION_DATA",
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        pathManager.SetPath("COMMON_APPLICATION_DATA",
                            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
        pathManager.SetPath("MY_DOCUMENTS", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));


        pathManager.SetPath("DEFAULTS", session["INSTALLDIR_CLIENT"] + "\\Defaults");
        pathManager.LoadPaths(session["INSTALLDIR_CLIENT"] + "\\Defaults\\Paths.xml");

        ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
        ServerConnectionSettings settings = settingsManager.Load<ServerConnectionSettings>();

        if (settings.HomeServerSystemId == null)
        {
          session.Log("Client is not attached, will be auto attached with the local server");
          pathManager.SetPath("DEFAULTS", session["INSTALLDIR_SERVER"] + "\\Defaults");
          pathManager.LoadPaths(session["INSTALLDIR_SERVER"] + "\\Defaults\\Paths.xml");

          ISystemResolver systemResolver = new SystemResolver();
          ServiceRegistration.Set<ISystemResolver>(systemResolver);

          String _localSystemId = systemResolver.LocalSystemId;
          session.Log("Local systemid = " + systemResolver.LocalSystemId);

          pathManager.SetPath("DEFAULTS", session["INSTALLDIR_CLIENT"] + "\\Defaults");
          pathManager.LoadPaths(session["INSTALLDIR_CLIENT"] + "\\Defaults\\Paths.xml");

          settings.HomeServerSystemId = _localSystemId;
          settings.LastHomeServerName = "MediaPortal 2 server";
          settingsManager.Save(settings);
        }
        else
        {
          session.Log("Client is already attached with server with ID {0}", settings.HomeServerSystemId);
        }
      }
      else
      {
        session.Log("Installation mode is not SingleSeat. No auto-attaching possible.");
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

    private static ActionResult SetInstallState(Session session)
    {
      if (!string.IsNullOrEmpty(session["UPGRADINGPRODUCTCODE"]))
      {
        string feature = string.Empty;
        //string feature = cs.ToUpper()[0] + cs.ToLower().Remove(0, 1);
        session.Log("Product is already installed. UPGRADINGPRODUCTCODE is {0}", session["UPGRADINGPRODUCTCODE"]);
        session.Log("Setting RequestState from {1} to {2} (previous installation state) for feature {0}.",
          feature,
          session.Features[feature].RequestState,
          session.Features[feature].CurrentState);

        session.Features[feature].RequestState = session.Features[feature].CurrentState;
      }

      return ActionResult.Success;
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
