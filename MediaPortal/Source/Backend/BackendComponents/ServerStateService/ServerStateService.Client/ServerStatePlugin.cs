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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Plugins.ServerStateService.Client.UPnP;
using MediaPortal.Plugins.ServerStateService.Interfaces;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.ServerStateService.Client
{
  public class ServerStatePlugin : IPluginStateTracker, IDisposable
  {
    protected HashSet<string> _knownAssemblies = new HashSet<string>();
    protected ServerStateProxyRegistration _proxyRegistration;
    protected AsynchronousMessageQueue _messageQueue = null;

    public void Activated(PluginRuntime pluginRuntime)
    {
      // List all assemblies
      InitPluginAssemblyList();
      // Set our own resolver to lookup types from any of assemblies from Plugins subfolder.
      ServerStateSerializer.CustomAssemblyResolver = PluginsAssemblyResolver;
      ServiceRegistration.Get<ILogger>().Debug("ServerStatePlugin: Adding Plugins folder to private path");

      Install();
    }

    protected void Install()
    {
      _proxyRegistration = new ServerStateProxyRegistration();
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
            ServerConnectionMessaging.CHANNEL
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ServerConnectionMessaging.CHANNEL)
      {
        ServerConnectionMessaging.MessageType type = (ServerConnectionMessaging.MessageType)message.MessageType;
        switch (type)
        {
          case ServerConnectionMessaging.MessageType.HomeServerAttached:
            _proxyRegistration.RegisterService();
            break;
          case ServerConnectionMessaging.MessageType.HomeServerDetached:
            _proxyRegistration.UnregisterService();
            break;
        }
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

    public void Continue()
    {

    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Shutdown()
    {

    }

    public void Stop()
    {
      Dispose();
    }

    public void Dispose()
    {
      if (_messageQueue != null)
      {
        _messageQueue.Terminate();
        _messageQueue = null;
      }
    }
  }
}