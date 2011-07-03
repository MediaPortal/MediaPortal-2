using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Deployment.WindowsInstaller;

using MediaPortal.Backend.Services.SystemResolver;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PathManager;
using MediaPortal.Core.Services.Settings;
using MediaPortal.Core.Services.PathManager;
using MediaPortal.Core.Settings;
using MediaPortal.Core.SystemResolver;
using MediaPortal.UI.ServerCommunication.Settings;

namespace CustomActions
{
  public class CustomActions
  {//Can publish up to 16 custom actions per DLL

    private static readonly string[] ClientPathLabels = new string[] { "DATA", "CONFIG", "LOG", "PLUGINS", "REMOTERESOURCES", "PLAYLISTS" };
    private static readonly string[] ServerPathLabels = new string[] { "DATA", "CONFIG", "LOG", "PLUGINS", "REMOTERESOURCES", "DATABASE" };

    [CustomAction]
    public static ActionResult AttachClientAndServer(Session session)
    {  
      session.Log("Begin AttachClientAndServer");

      session.Log("ClientRequestState : {0}", session.Features["Client"].RequestState);
      session.Log("ServerRequestState : {0}", session.Features["Server"].RequestState);

      if ((session.Features["Client"].RequestState == InstallState.Local) & (session.Features["Server"].RequestState == InstallState.Local))
      {
        ServiceRegistration.Set<ILogger>(new NoLogger());

        IPathManager pathManager = new PathManager();
        ServiceRegistration.Set<IPathManager>(pathManager);
        ServiceRegistration.Set<ISettingsManager>(new SettingsManager());

        string applicationPath = Environment.GetCommandLineArgs()[0];
        pathManager.SetPath("APPLICATION_PATH", applicationPath);
        pathManager.SetPath("APPLICATION_ROOT", Path.GetDirectoryName(applicationPath));
        pathManager.SetPath("LOCAL_APPLICATION_DATA", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        pathManager.SetPath("COMMON_APPLICATION_DATA", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
        pathManager.SetPath("MY_DOCUMENTS", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

        pathManager.SetPath("DEFAULTS", session["INSTALLDIR_SERVER"] + "\\Defaults");
        pathManager.LoadPaths(session["INSTALLDIR_SERVER"] + "\\Defaults\\Paths.xml");

        ISystemResolver systemResolver = new SystemResolver();
        ServiceRegistration.Set<ISystemResolver>(systemResolver);

        String _localSystemId = systemResolver.LocalSystemId;
        session.Log("Local systemid = " + systemResolver.LocalSystemId);

        pathManager.SetPath("DEFAULTS", session["INSTALLDIR_CLIENT"] + "\\Defaults");
        pathManager.LoadPaths(session["INSTALLDIR_CLIENT"] + "\\Defaults\\Paths.xml");

        ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
        ServerConnectionSettings settings = settingsManager.Load<ServerConnectionSettings>();
        settings.HomeServerSystemId = _localSystemId;
        settings.LastHomeServerName = "MediaPortal 2 server";
        settingsManager.Save(settings);

        session.Log("End AttachClientAndServer");
      }
      return ActionResult.Success;
    }

    [CustomAction]
    public static ActionResult SetCustomPaths(Session session)
    {
      string path;
      string property;

      foreach (string label in ClientPathLabels)
      {
        property = "CLIENT." + label + ".FOLDER";

        path = session[property];
        path = path.Replace(session["INSTALLDIR_CLIENT"], "<APPLICATION_ROOT>\\");
        path = MediaPortal.Utilities.StringUtils.RemoveSuffixIfPresent(path, "\\");

        session["XML." + property] = path;
        session.Log("XML.{1}={0}", path, property);
      }
      foreach (string label in ServerPathLabels)
      {
        property = "SERVER." + label + ".FOLDER";

        path = session[property];
        path = path.Replace(session["INSTALLDIR_SERVER"], "<APPLICATION_ROOT>\\");
        path = MediaPortal.Utilities.StringUtils.RemoveSuffixIfPresent(path, "\\");

        session["XML." + property] = path;
        session.Log("XML.{1}={0}", path, property);
      }

      return ActionResult.Success;
    }
  }
}
