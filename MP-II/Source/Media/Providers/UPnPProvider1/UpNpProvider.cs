#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using Intel.UPNP.AV.MediaServer.CP;
using MediaPortal.Core;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.Logging;
using MediaPortal.Media.MediaManagement;
using MediaPortal.Media.MediaManagement.Views;

namespace Media.Providers.UpNpProvider
{
  public class UpNpProvider : IMediaProvider
  {
    public const string MEDIAMANAGERQUEUE_NAME = "MediaManager";

    #region variables

    private ContainerDiscovery _discovery;
    private readonly List<CpRootContainer> _roots;
    //CdsSpider _spider;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="UpNpProvider"/> class.
    /// </summary>
    public UpNpProvider()
    {
      ServiceScope.Get<ILogger>().Info("UPNP provider: loaded");
      _roots = new List<CpRootContainer>();

      //Thread startupThread = new Thread(new ThreadStart(Start));
      // startupThread.IsBackground = true;
      //startupThread.Name = "UpnP server search";
      //// startupThread.Priority = ThreadPriority.BelowNormal;
      // startupThread.Start();
      Start();
    }

    /// <summary>
    /// Starts the upnp provider.
    /// this gets done in a seperate thread to increase the application startup time
    /// </summary>
    void Start()
    {
      ServiceScope.Get<ILogger>().Info("UPNP provider: starting");
      _discovery = ContainerDiscovery.GetInstance();
      _discovery.AllRoots.OnContainerChanged += new CpRootContainer.Delegate_OnContainerChanged(AllRoots_OnContainerChanged);


    }

    /// <summary>
    /// call back from the UPnP stack when it discovered
    /// a new UPnP server, or when an existing UPnP server has gone offline
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="thisChanged">The this changed.</param>
    private void AllRoots_OnContainerChanged(CpRootContainer sender, CpMediaContainer thisChanged)
    {
      bool sendMsg = false;
      lock (_roots)
      {
        foreach (CpRootContainer root in _discovery.AllRoots.Containers)
        {
          if (_roots.Contains(root)) continue;
          ServiceScope.Get<ILogger>().Info("UPNP provider: online upnp server:{0}", root.ServerFriendlyName);
          _roots.Add(root);
          sendMsg = true;
        }
        bool didRemoveOfflineUpnpServers;
        do
        {
          didRemoveOfflineUpnpServers = false;
          for (int i = 0; i < _roots.Count; ++i)
          {
            if (!_discovery.AllRoots.Containers.Contains(_roots[i]))
            {
              ServiceScope.Get<ILogger>().Info("UPNP provider: offline upnp server:{0}", _roots[i].ServerFriendlyName);
              didRemoveOfflineUpnpServers = true;
              _roots.RemoveAt(i);
              sendMsg = true;
              break;
            }
          }
        } while (didRemoveOfflineUpnpServers);

      }
      if (sendMsg)
      {
        IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
        IMessageQueue queue = broker.GetOrCreate(MEDIAMANAGERQUEUE_NAME);
        QueueMessage msg = new QueueMessage();
        msg.MessageData["action"] = "changed";
        msg.MessageData["fullpath"] = "/";
        msg.MessageData["container"] = this;
        queue.Send(msg);
      }
    }

    #region IMediaProvider Members
    /// <summary>
    /// get the root containers for this provider
    /// </summary>
    /// <value></value>
    public List<IRootContainer> RootContainers
    {
      get
      {
        List<IRootContainer> list = new List<IRootContainer>();
        lock (_roots)
        {
          foreach (CpRootContainer coll in _roots)
          {
            UpNpRootContainer con = new UpNpRootContainer(coll);
            list.Add(con);
          }
        }
        return list;
      }
    }

    /// <summary>
    /// get the title for this provider
    /// </summary>
    /// <value></value>
    public string Title
    {
      get { return "UpnP Provider"; }
      set
      {
      }
    }

    /// <summary>
    /// Gets the view.
    /// </summary>
    /// <param name="query">The query for the view.</param>
    /// <returns>list of containers & items for the query</returns>
    public List<IAbstractMediaItem> GetView(IView query, IRootContainer root, IRootContainer parent)
    {
      // we dont support views yet...
      return new List<IAbstractMediaItem>();
    }

    #endregion
  }
}
