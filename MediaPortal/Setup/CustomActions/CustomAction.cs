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
      session.Log("ClientRequestState : {0}", session.Features["Client"].RequestState);
      session.Log("ServerRequestState : {0}", session.Features["Server"].RequestState);

      if (session.Features["Client"].RequestState == InstallState.Local
        & session.Features["Server"].RequestState == InstallState.Local)
      {
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
    public static ActionResult SetCustomPaths(Session session)
    {
      SetCustomPaths(session, "CLIENT", ClientPathLabels);
      SetCustomPaths(session, "SERVER", ServerPathLabels);

      return ActionResult.Success;
    }

    private static void SetCustomPaths(Session session, string cs, IEnumerable<string> pathVariables)
    {
      foreach (string label in pathVariables)
      {
        string property = cs + "." + label + ".FOLDER";

        string path = session[property];

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

        session["XML." + property] = path;
        session.Log("XML.{1}={0}", path, property);
      }
    }
  }
}
