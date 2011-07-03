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
    public static ActionResult SetVarsForCustomSetup(Session session)
    {
      session.Log("Begin SetupVarsForCustomSetup");

      String _xml_client_plugins=session["CLIENT.PLUGINS.FOLDER"];
      _xml_client_plugins = _xml_client_plugins.Replace(session["INSTALLDIR_CLIENT"], "<APPLICATION_ROOT>\\");
      _xml_client_plugins = MediaPortal.Utilities.StringUtils.RemoveSuffixIfPresent(_xml_client_plugins,"\\");
      session["XML_CLIENT_PLUGINS"]=_xml_client_plugins;
      session.Log("_xml_client_plugins={0}",_xml_client_plugins);

      String _xml_server_plugins=session["SERVER.PLUGINS.FOLDER"];
      _xml_server_plugins = _xml_server_plugins.Replace(session["INSTALLDIR_SERVER"], "<APPLICATION_ROOT>\\");
      _xml_server_plugins = MediaPortal.Utilities.StringUtils.RemoveSuffixIfPresent(_xml_server_plugins, "\\");
      session["XML_SERVER_PLUGINS"]=_xml_server_plugins;
      session.Log("_xml_server_plugins={0}", _xml_server_plugins);

      session.Log("End SetupVarsForCustomSetup");
      return ActionResult.Success;
    }
  }
}
