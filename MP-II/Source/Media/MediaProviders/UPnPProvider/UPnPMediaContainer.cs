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
using MediaPortal.Media.MediaManagement;
using MediaPortal.Media.MetaData;

using IRootContainer = MediaPortal.Media.MediaManagement.IRootContainer;

namespace Media.Providers.UpNpProvider
{
  public class UpNpMediaContainer : IRootContainer, IDisposable
  {
    public const string MEDIAMANAGERQUEUE_NAME = "MediaManager";

    #region IRootContainer Members

    private readonly IRootContainer _root;
    private IRootContainer _parent;
    private CdsSpider _spider;
    private readonly CpMediaContainer _mediaContainer;
    private Dictionary<string, object> _metaData;
    CdsSpider.Delegate_OnMatchesChanged _handler;
    IMetaDataMappingCollection _mapping;

    ~UpNpMediaContainer()
    {
      Dispose();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpNpMediaContainer"/> class.
    /// </summary>
    /// <param name="mediaContainer">The media container.</param>
    /// <param name="root">The root.</param>
    /// <param name="parent">The parent.</param>
    public UpNpMediaContainer(CpMediaContainer mediaContainer, IRootContainer root, IRootContainer parent)
    {
      _mediaContainer = mediaContainer;
      _root = root;
      _parent = parent;
      _metaData = new Dictionary<string, object>();
      _metaData["title"] = Title;
      _handler = new CdsSpider.Delegate_OnMatchesChanged(OnUpdateDone);
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
          return Root.Mapping;
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
    /// gets the media items
    /// </summary>
    /// <value></value>
    public IList<IAbstractMediaItem> Items
    {
      get
      {
        if (_mediaContainer.Containers.Count == 0)
        {
          if (_spider == null)
          {
            _spider = new CdsSpider();
            _spider.MonitorThis = _mediaContainer;
            _spider.OnUpdateDone += _handler;
            _spider.Comparer = new MatchOnAny();
          }
        }
        List<IAbstractMediaItem> list = new List<IAbstractMediaItem>();
        LoadContainers(list);
        LoadItems(list);
        return list;
      }
    }


    /// <summary>
    /// Gets the root.
    /// </summary>
    /// <value>The root.</value>
    public IRootContainer Root
    {
      get { return _root; }
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
        if (Parent != null)
          return String.Format(@"{0}/{1}", Parent.FullPath, Title);
        else
          return String.Format(@"{0}/{1}", Root.FullPath, Title);
      }
      set { }
    }
    /// <summary>
    /// Returns the title of the media item.
    /// </summary>
    /// <value></value>
    public string Title
    {
      get { return _mediaContainer.Title; }
      set
      {
      }
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

    private void LoadContainers(ICollection<IAbstractMediaItem> list)
    {
      foreach (CpMediaContainer cont in _mediaContainer.Containers)
      {
        UpNpMediaContainer c = new UpNpMediaContainer(cont, Root, this);
        list.Add(c);
      }
    }

    private void LoadItems(ICollection<IAbstractMediaItem> list)
    {
      foreach (CpMediaItem cont in _mediaContainer.Items)
      {
        UpNpMediaItem c = new UpNpMediaItem(cont, this);
        list.Add(c);
      }
    }

    private void OnUpdateDone(CdsSpider sender, IList mediaObjects)
    {
      if (sender.MatchedContainers.Count != 0 || sender.MatchedItems.Count != 0)
      {
        QueueMessage msg = new QueueMessage();
        msg.MessageData["action"] = "changed";
        msg.MessageData["fullpath"] = FullPath;
        msg.MessageData["container"] = this;
        ServiceScope.Get<IMessageBroker>().Send(MEDIAMANAGERQUEUE_NAME, msg);
      }
    }

    #region IDisposable Members

    public void Dispose()
    {
      if (_spider != null)
      {
        _spider.MonitorThis = null;
        _spider.OnUpdateDone -= _handler;
        _spider = null;
      }
    }

    #endregion
  }
}
