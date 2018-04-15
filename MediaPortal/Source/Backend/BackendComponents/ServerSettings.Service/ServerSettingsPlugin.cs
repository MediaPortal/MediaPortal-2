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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MediaPortal.Backend.BackendServer;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.UPnP;
using MediaPortal.Utilities;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.Plugins.ServerSettings
{
  public class ServerSettingsPlugin : IPluginStateTracker
  {
    private HashSet<string> _knownAssemblies = new HashSet<string>();

    public void Activated(PluginRuntime pluginRuntime)
    {
      var meta = pluginRuntime.Metadata;
      Logger.Info(string.Format("{0} v{1} [{2}] by {3}", meta.Name, meta.PluginVersion, meta.Description, meta.Author));

      DvDevice device = ServiceRegistration.Get<IBackendServer>().UPnPBackendServer.FindDevicesByDeviceTypeAndVersion(UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE, UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE_VERSION, true).FirstOrDefault();
      if (device != null)
      {
        var serverSettings = new ServerSettingsImpl();
        Logger.Debug("ServerSettings: Registering ServerSettings service.");
        device.AddService(serverSettings);
        Logger.Debug("ServerSettings: Adding ServerSettings service to MP2 backend root device");

        // List all assemblies
        InitPluginAssemblyList();

        // Set our own resolver to lookup types from any of assemblies from Plugins subfolder.
        SettingsSerializer.CustomAssemblyResolver = PluginsAssemblyResolver;
        // AppDomain.CurrentDomain.AssemblyResolve += PluginsAssemblyResolver;

        Logger.Debug("ServerSettings: Adding Plugins folder to private path");
      }
      else
      {
        Logger.Error("ServerSettings: MP2 backend root device not found!");
      }
    }

    private void InitPluginAssemblyList()
    {
      IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
      foreach (PluginRuntime plugin in pluginManager.AvailablePlugins.Values)
        CollectionUtils.AddAll(_knownAssemblies, plugin.Metadata.AssemblyFilePaths);
    }

    Assembly PluginsAssemblyResolver(object sender, ResolveEventArgs args)
    {
      try
      {
        string[] assemblyDetail = args.Name.Split(',');
        string path = _knownAssemblies.FirstOrDefault(a => a.EndsWith(@"\" + assemblyDetail[0] + ".dll"));
        if (path == null)
          return null;
        Assembly assembly = Assembly.LoadFrom(path);
        return assembly;
      }
      catch { }
      return null;
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
    }

    public void Continue()
    {
    }

    public void Shutdown()
    {
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }

    }
  }
}
