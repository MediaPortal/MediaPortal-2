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
using System.Collections.Generic;
using MediaPortal.Database;
using MediaPortal.Media.MetaData;
using MediaPortal.Media.MediaManager;
using MediaPortal.Media.MediaManager.Views;

namespace Media.Providers.DatabaseProvider
{
  public class DatabaseMediaContainer : IRootContainer
  {
    #region variables

    private readonly IDatabase _database;
    private readonly List<IAbstractMediaItem> _items;
    //private string _fullPath;
    private IRootContainer _parent;
    IRootContainer _root;
    private Dictionary<string, object> _metaData;
    IMetaDataMappingCollection _mapping;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseMediaContainer"/> class.
    /// </summary>
    /// <param name="database">The database.</param>
    public DatabaseMediaContainer(IDatabase database, IRootContainer root, IRootContainer parent)
    {
      _root = root;
      _parent = parent;
      _database = database;
      _items = new List<IAbstractMediaItem>();
      _metaData = new Dictionary<string, object>();
      _metaData["title"] = Title;
    }

    /// <summary>
    /// Returns the metadata of the media item.
    /// </summary>
    /// <value></value>
    public Dictionary<string, object> MetaData
    {
      get { return _metaData; }
    }
    /// <summary>
    /// Gets the view.
    /// </summary>
    /// <param name="view">The view.</param>
    /// <returns></returns>
    public List<IAbstractMediaItem> GetView(IView view)
    {
      if (!view.Databases.Contains(_database.Name))
      {
        return new List<IAbstractMediaItem>();
      }
      if (!_database.CanQuery(view.Query))
      {
        return new List<IAbstractMediaItem>();
      }
      IList<IDbItem> items = _database.Query(view.Query);

      foreach (IDbItem item in items)
      {
        if (view.IsLastSubView)
          _items.Add(new DatabaseMediaItem(item, null, Parent));
        else
          _items.Add(new DatabaseMediaItem(item, view.Query, Parent));
      }
      return _items;
    }

    #endregion

    #region IRootContainer Members

    /// <summary>
    /// Gets or sets the mapping for the metadata.
    /// </summary>
    /// <value>The mapping for the metadata.</value>
    public IMetaDataMappingCollection Mapping
    {
      get
      {
        return _mapping;
      }
      set
      {
        _mapping = value;
      }
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
        return true;
      }
    }
    /// <summary>
    /// gets the media items
    /// </summary>
    /// <value></value>
    public List<IAbstractMediaItem> Items
    {
      get { return _items; }
    }

    /// <summary>
    /// gets the root container
    /// </summary>
    /// <value></value>
    public IRootContainer Root
    {
      get { return _root; }
    }

    /// <summary>
    /// gets the parent container
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
    /// returns the title for this container
    /// </summary>
    /// <value></value>
    public string Title
    {
      get { return ""; }
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

  }
}
