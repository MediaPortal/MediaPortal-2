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
using System.Globalization;
using System.IO;
using System.Text;
using MediaPortal.Backend.Services.ClientCommunication;
using MediaPortal.Backend.Services.SystemResolver;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Registry;
using MediaPortal.Common.Services.Localization;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Services.Messaging;
using MediaPortal.Common.Services.PathManager;
using MediaPortal.Common.Services.PluginManager;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemResolver;
using MediaPortal.UI.ServerCommunication.Settings;
using MediaPortal.Utilities;
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
      ServiceRegistration.Set<IMessageBroker>(new MessageBroker());
      PluginManager pluginManager = new PluginManager();
      ServiceRegistration.Set<IPluginManager>(pluginManager);
      // Required to load language resource registration from plugins, so ILocalization is able to use them.
      pluginManager.Initialize();
      pluginManager.Startup(true); // Use "maintenance" mode here, this won't activate plugins as they are not needed here.

      StringManager stringManager = new StringManager();
      stringManager.Startup();
      ServiceRegistration.Set<ILocalization>(stringManager);

      ISystemResolver backendSystemResolver = new SystemResolver();

      HomeServerData result = new HomeServerData
        {
            HomeServerName = new LocalizedUPnPDeviceInformation().GetFriendlyName(CultureInfo.InvariantCulture),
            HomeServerSystemId = backendSystemResolver.LocalSystemId
        };

      pluginManager.Shutdown();
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

    /// <summary>
    /// Checks if the current installation mode is SingleSeat and auto-attaches the local client to the local server.
    /// </summary>
    /// <param name="session">Current installation session object.</param>
    /// <returns><see cref="ActionResult.Success"/>.</returns>
    [CustomAction]
    public static ActionResult AttachClientToServer(Session session)
    {
      session.Log("ClientRequestState : {0}", session.Features["Client"].RequestState);
      session.Log("ServerRequestState : {0}", session.Features["Server"].RequestState);

      if (session.Features["Client"].RequestState == InstallState.Local &&
          session.Features["Server"].RequestState == InstallState.Local)
      {
        string clientApplicationPath = Path.Combine(session["INSTALLDIR_CLIENT"], "MP2-Client.exe");
        string serverApplicationPath = Path.Combine(session["INSTALLDIR_SERVER"], "MP2-Server.exe");

        ServiceRegistration.Set<ILogger>(new DefaultLogger(new SessionLogWriter(session), LogLevel.All, false, false)); // Logger for called services

        HomeServerData homeServerData = GetHomeServerData(serverApplicationPath);
        AttachClientToServer(clientApplicationPath, homeServerData);
        ServiceRegistration.RemoveAndDispose<ILogger>();
      }
      else
        session.Log("Installation mode is not SingleSeat. Skipping auto-attach step.");

      return ActionResult.Success;
    }

    /// <summary>
    /// Reads the paths files from a former installation for client and server and sets them in the given installation <paramref name="session"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This action reads all the paths from an existing MP2 installation's Paths.xml file via the <see cref="PathManager"/> and fills
    /// the <paramref name="session"/>'s variables of the form <c>"CLIENT_CONFIG_FOLDER"</c> for all labels given in the
    /// <see cref="ClientPathLabels"/> and <see cref="ServerPathLabels"/>, with the appropriate prefix <c>"CLIENT"</c> or <c>"SERVER"</c>.
    /// </para>
    /// <para>
    /// The paths in the written variables will be OS paths, i.e. they won't contain MP2 path labels/placeholders like <c>"&lt;DATA&gt;"</c>.
    /// </para>
    /// </remarks>
    /// <param name="session">Current installation session object.</param>
    /// <returns><see cref="ActionResult.Success"/>, if no exception happens, else <see cref="ActionResult.Failure"/>.</returns>
    [CustomAction]
    public static ActionResult ReadCustomPathsFromExistingPathsFile(Session session)
    {
      try
      {
        ReadCustomPathsFromExistingPathsFile(session, "CLIENT", ClientPathLabels);
        ReadCustomPathsFromExistingPathsFile(session, "SERVER", ServerPathLabels);
      }
      catch (Exception e)
      {
        session.Log("Error reading custom paths from former installation: " + e.Message);
        return ActionResult.Failure;
      }

      return ActionResult.Success;
    }

    /// <summary>
    /// Tries to read the installation directory of a former installation from the registry.
    /// </summary>
    /// <param name="session">Current installation session object.</param>
    /// <param name="cs">String which can be <c>CLIENT</c> or <c>SERVER</c> to access the properties of session.</param>
    /// <param name="installDir">Returns the installation directory, if the return value is <c>true</c>. Else, this parameter is
    /// undefined.</param>
    /// <returns><c>true</c>, if the installation directory from a former installation could successfully be read, else <c>false</c>.</returns>
    private static bool ReadInstallDirFromRegistry(Session session, string cs, out string installDir)
    {
      installDir = string.Empty;
      string regKey = String.Format("SOFTWARE\\{0}\\{1}", session["Manufacturer"], session["ProductName"]);
      string regValue = "INSTALLDIR_" + cs;

      // Read install dir from registry
      RegistryKey masterKey = Registry.LocalMachine.OpenSubKey(regKey);
      if (masterKey == null)
      {
        session.Log("Registry key 'HKLM\\{0}' not opened/found, former installation directory cannot be found", regKey);
        return false;
      }

      installDir = masterKey.GetValue(regValue, string.Empty) as string;
      if (String.IsNullOrEmpty(installDir))
      {
        session.Log("Registry value '{1}' in registry key 'HKLM\\{0}' not opened/found, former installation directory cannot be found", regKey, regValue);
        return false;
      }

      session.Log("Former installation directory '{0}' read from registry key '{1}'", installDir, regValue);
      return true;
    }
    
    /// <summary>
    /// Reads the path files from a former installation for client or server and sets them in the given installation <paramref name="session"/>.
    /// </summary>
    /// <remarks>
    /// This method reads all the paths from an existing MP2 installation's Paths.xml file via the <see cref="PathManager"/> and fills
    /// the session's variables of the form <c>"CLIENT_CONFIG_FOLDER"</c> for all labels given in the <paramref name="pathLabels"/> for
    /// either client or server, depending on the <paramref name="cs"/> parameter.
    /// </remarks>
    /// <param name="session">Current installation session object.</param>
    /// <param name="cs">String which can be <c>CLIENT</c> or <c>SERVER</c> to access the properties of the <paramref name="session"/>
    /// object.</param>
    /// <param name="pathLabels">List of path labels whose values should be set in the <paramref name="session"/> object.</param>
    private static void ReadCustomPathsFromExistingPathsFile(Session session, string cs, IEnumerable<string> pathLabels)
    {
      string installDir;
      if (!ReadInstallDirFromRegistry(session, cs, out installDir))
      {
        session.Log("No former installation found, skipping loading of custom paths from former installation");
        return;
      }

      // Read other dirs from Paths.xml
      string pathsFile = Path.Combine(installDir, "Defaults\\Paths.xml");
      if (!File.Exists(pathsFile))
      {
        session.Log("Paths file '{0}' not found, skipping loading of custom paths from former installation", pathsFile);
        return;
      }

      session.Log("Reading custom paths from former installation from paths file: '{0}'", pathsFile);
      PathManager pathManager = new PathManager();
      pathManager.InitializeDefaults(Path.Combine(installDir, "Executable.exe"));

      foreach (string label in pathLabels)
      {
        string key = cs + "_" + label + "_FOLDER";
        string path = pathManager.GetPath("<" + label + ">");
        session[key] = path;
        session.Log("Reading custom path '{0}': '{1}", key, path);
      }
    }

    /// <summary>
    /// Prepares the path variables which might have been edited by the user and writes them to the given <paramref name="session"/>.
    /// </summary>
    /// <remarks>
    /// This action reads the session's variables of the form <c>"CLIENT_CONFIG_FOLDER"</c> for client and server and for all labels
    /// in <see cref="ClientPathLabels"/> resp. <see cref="ServerPathLabels"/>, cleans them up (means replaces all common path fragments by
    /// path labels like <c>"CONFIG"</c>) and writes them to session variables of the form <c>"XML_CLIENT_CONFIG_FOLDER"</c>.
    /// </remarks>
    /// <param name="session">Current installation session object.</param>
    [CustomAction]
    public static ActionResult PrepareXmlPathVariables(Session session)
    {
      PrepareXmlPathVariables(session, "CLIENT", ClientPathLabels);
      PrepareXmlPathVariables(session, "SERVER", ServerPathLabels);

      return ActionResult.Success;
    }

    protected class StringLengthComparer : IComparer<string>
    {
      public enum Direction
      {
        Ascending,
        Descending
      }

      protected Direction _direction;

      public StringLengthComparer(Direction direction)
      {
        _direction = direction;
      }

      public int Compare(string x, string y)
      {
        int xLen = string.IsNullOrEmpty(x) ? 0 : x.Length;
        int yLen = string.IsNullOrEmpty(y) ? 0 : y.Length;
        if (x == null || y == null)
          return 0;
        if (xLen == yLen)
          return x.CompareTo(y);
        int result = xLen - yLen;
        if (_direction == Direction.Descending)
          result *= -1;
        return result;
      }
    }

    /// <summary>
    /// Prepares the path variables which might have been edited by the user and writes them to the given <paramref name="session"/>.
    /// </summary>
    /// <remarks>
    /// This method reads the session's variables of the form <c>"CLIENT_CONFIG_FOLDER"</c> for client or server (depending on
    /// parameter <paramref name="cs"/>) and for all given <paramref name="pathLabels"/>, cleans them up (means replaces all
    /// common path fragments by path labels like <c>"CONFIG"</c>) and writes them to session variables of the form
    /// <c>"XML_CLIENT_CONFIG_FOLDER"</c>. Those session variables will be written into the configuration files of MP2 by one
    /// of the next installer steps.
    /// </remarks>
    /// <param name="session">Current installation session object.</param>
    /// <param name="cs">String which can be CLIENT or SERVER to access the properties of sessions.</param>
    /// <param name="pathLabels">List of path labels to be written.</param>
    private static void PrepareXmlPathVariables(Session session, string cs, IEnumerable<string> pathLabels)
    {
      // We want to fold the paths given in the installer session to make each saved path as generic as possible.
      // That means we will replace each known path by its label.
      // During replacing, the order matters because sometimes, paths are inserted into other paths multiple times.
      // To avoid replacing short segments in paths where a longer segment could be replaced, we order the paths which can be replaced
      // by the length of their labels in descending direction. That makes sure always the longest possible path segment is replaced.

      SortedDictionary<string, string> paths2Labels = new SortedDictionary<string, string>(
          new StringLengthComparer(StringLengthComparer.Direction.Descending));

      // Fill in all paths which should be replaced in the paths which were edited by the user
      foreach (KeyValuePair<string, string> label2Path in PathManager.GetStandardSpecialFolderMappings())
        paths2Labels[label2Path.Value] = label2Path.Key;
      foreach (string pathLabel in pathLabels)
      {
        string path = session[cs + "_" + pathLabel + "_FOLDER"];
        paths2Labels[path] = pathLabel;
      }

      string installDir = StringUtils.RemoveSuffixIfPresent(session["INSTALLDIR_" + cs], "\\");

      // Go through each path which was edited by the user and try to replace all known paths by their labels
      foreach (string label in pathLabels)
      {
        string currentPath = session[cs + "_" + label + "_FOLDER"]; // Path which was edited in the installer GUI
        if (string.IsNullOrEmpty(currentPath))
          continue;

        // Replace application dir and all common folders in this system by their path labels to make the path as generic as possible
        if (!string.IsNullOrEmpty(installDir))
          currentPath = currentPath.Replace(installDir, "<APPLICATION_ROOT>");
        foreach (KeyValuePair<string, string> path2Label in paths2Labels)
          if (currentPath != path2Label.Key) // Don't replace a path with its own label
            currentPath.Replace(path2Label.Key, path2Label.Value);

        currentPath = StringUtils.RemoveSuffixIfPresent(currentPath, "\\");

        string sessionKey = "XML_" + cs + "_" + label + "_FOLDER";
        session[sessionKey] = currentPath;
        session.Log("{0} = {1}", sessionKey, currentPath);
      }
    }
  }
}
