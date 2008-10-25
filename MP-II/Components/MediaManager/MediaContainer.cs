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
using MediaPortal.Media.MediaManager;
using MediaPortal.Media.MetaData;

namespace Components.Services.MediaManager
{
  public class MediaContainer : IRootContainer
  {
    #region variables
    private readonly List<IAbstractMediaItem> _items;
    private string _title;
    private  IRootContainer _parent;
    private readonly string _parentPath;
    private Dictionary<string, object> _metaData;
    IMetaDataMappingCollection _mapping;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaContainer"/> class.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <param name="parentPath">The parent path.</param>
    public MediaContainer(string title, string parentPath)
      : this(title, parentPath, null)
    {
      _metaData = new Dictionary<string, object>();
      _metaData["title"] = Title;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaContainer"/> class.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <param name="parentPath">The parent path.</param>
    /// <param name="parent">The parent.</param>
    public MediaContainer(string title, string parentPath, IRootContainer parent)
    {
      _title = title;
      _parentPath = parentPath;
      _items = new List<IAbstractMediaItem>();
      _parent = parent;
      _metaData = new Dictionary<string, object>();
      _metaData["title"] = Title;
		}

		#region IRootContainer Implementation
		/// <summary>
		/// gets the media items
		/// </summary>
		/// <value></value>
		public IList<IAbstractMediaItem> Items
		{
			get { return _items; }
		}

    /// <summary>
    /// Gets the root.
    /// </summary>
    /// <value>The root.</value>
    public IRootContainer Root
    {
      get
      {
        if (_parent == null)
          return this;
        return _parent.Root;
      }
		}

		#endregion

		#region IAbstractMediaItem Implementation

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
        if (_parent == null)
          return true;
        return _parent.IsLocal;
      }
    }

    
    /// <summary>
    /// Returns the title of the media item.
    /// </summary>
    /// <value></value>
    public string Title
    {
      get { return _title; }
      set { _title = value; }
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
    /// <summary>
    /// Gets or sets the full path.
    /// </summary>
    /// <value>The full path.</value>
    public string FullPath
    {
      get
      {
        if (Parent != null && Parent != this)
          return string.Format("{0}/{1}", Parent.FullPath, Title);
        return Title;
      }
      set { }
    }
		
    /// <summary>
    /// Returns the metadata of the media item.
    /// </summary>
    /// <value></value>
    public IDictionary<string, object> MetaData
    {
      get { return _metaData; }
		}
		#endregion

		public string ParentPath
		{
			get { return _parentPath; }
		}

  }
}
