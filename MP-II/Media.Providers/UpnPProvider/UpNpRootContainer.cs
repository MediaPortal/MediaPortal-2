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

using System;
using System.Collections;
using System.Collections.Generic;
using Intel.UPNP.AV.CdsMetadata;
using Intel.UPNP.AV.MediaServer.CP;
using MediaPortal.Core;
using MediaPortal.Core.Messaging;
using MediaPortal.Media.MediaManager;
using MediaPortal.Media.MetaData;
using IRootContainer = MediaPortal.Media.MediaManager.IRootContainer;

namespace Media.Providers.UpNpProvider
{
  public class UpNpRootContainer : IRootContainer, IDisposable
  {
    public const string MEDIAMANAGERQUEUE_NAME = "MediaManager";

    #region variables
    private readonly CpRootContainer _root;
    private CdsSpider _spider;
    private Dictionary<string, object> _metaData;
    IMetaDataMappingCollection _mapping;
    CdsSpider.Delegate_OnMatchesChanged _handler;
    IRootContainer _parent;
    #endregion

    ~UpNpRootContainer()
    {
      Dispose();
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="UpNpRootContainer"/> class.
    /// </summary>
    /// <param name="root">The root.</param>
    public UpNpRootContainer(CpRootContainer root)
    {
      _handler = new CdsSpider.Delegate_OnMatchesChanged(OnUpdateDone);
      _root = root;
      _metaData = new Dictionary<string, object>();
      _metaData["title"] = Title;
      _metaData["CoverArt"] = "defaultNetwork.png";
    }

    #region IRootContainer Members

    /// <summary>
    /// Gets or sets the mapping for the metadata.
    /// </summary>
    /// <value>The mapping for the metadata.</value>
    public IMetaDataMappingCollection Mapping
    {
      get
      {
        if (_mapping == null)
        {
          if (Parent != null) return Parent.Mapping;
          return null;
        }
        return _mapping;
      }
      set
      {
        _mapping = value;
      }
    }
    /// <summary>
    /// Returns the metadata of the media item.
    /// </summary>
    /// <value></value>
    public IDictionary<string, object> MetaData
    {
      get { return _metaData; }
    }
    /// <summary>
    /// Gets a value indicating whether this item is located locally or remote
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this item is located locally; otherwise, <c>false</c>.
    /// </value>
    public bool IsLocal
    {
      get
      {
        return false;
      }
    }
    /// <summary>
    /// gets the media items
    /// </summary>
    /// <value></value>
    public IList<IAbstractMediaItem> Items
    {
      get
      {
        if (_spider == null)
        {
          _spider = new CdsSpider();
          _spider.MonitorThis = _root;
          _spider.OnUpdateDone += _handler;
          _spider.Comparer = new MatchOnAny();
        }

        List<IAbstractMediaItem> list = new List<IAbstractMediaItem>();
        foreach (CpMediaContainer cont in _root.Containers)
        {
          UpNpMediaContainer c = new UpNpMediaContainer(cont, this, this);
          list.Add(c);
        }
        return list;
      }
    }

    /// <summary>
    /// Gets the root.
    /// </summary>
    /// <value>The root.</value>
    public IRootContainer Root
    {
      get { return this; }
    }

    /// <summary>
    /// Returns the title of the media item.
    /// </summary>
    /// <value></value>
    public string Title
    {
      get { return _root.ServerFriendlyName; }
      set
      {
      }
    }

    /// <summary>
    /// the media container in which this media item resides
    /// </summary>
    /// <value></value>
    public IRootContainer Parent
    {
      get { return _parent; }
      set
      {
        _parent = value;
      }
    }
    /// <summary>
    /// Gets or sets the full path.
    /// </summary>
    /// <value>The full path.</value>
    public string FullPath
    {
      get
      {
        return Title;
      }
      set { }
    }
    /// <summary>
    /// Gets the content URI for this item
    /// </summary>
    /// <value>The content URI.</value>
    public Uri ContentUri
    {
      get
      {
        return null;
      }
    }
    #endregion

    private void OnUpdateDone(CdsSpider sender, IList mediaObjects)
    {
      if (_root.Containers.Count > 0)
      {
        IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
        IMessageQueue queue = broker.GetOrCreate(MEDIAMANAGERQUEUE_NAME);
        QueueMessage msg = new QueueMessage();
        msg.MessageData["action"] = "changed";
        msg.MessageData["fullpath"] = FullPath;
        msg.MessageData["container"] = this;
        queue.Send(msg);
      }
    }

    #region IDisposable Members

    public void Dispose()
    {
      if (_spider != null)
      {
        _spider.OnUpdateDone -= _handler;
        _spider.MonitorThis = null;
        _spider = null;
      }

    }

    #endregion
  }
}
