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
using System.IO;
using MediaPortal.Core;
using MediaPortal.Media.MediaManager;
using MediaPortal.Media.Importers;
using MediaPortal.Media.MetaData;


namespace LocalHdProvider
{
  public class FolderContainer : IRootContainer
  {
    #region variables
    private readonly string _folder;
    private readonly IRootContainer _root;
    private IRootContainer _parent;

    private Dictionary<string, object> _metaData;
    IMetaDataMappingCollection _mapping;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="FolderContainer"/> class.
    /// </summary>
    /// <param name="folder">The folder.</param>
    /// <param name="root">The root.</param>
    /// <param name="parent">The parent.</param>
    public FolderContainer(string folder, IRootContainer root, IRootContainer parent)
    {
      _folder = folder;
      _root = root;
      _parent = parent;
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
    /// gets the media items
    /// </summary>
    /// <value></value>
    public List<IAbstractMediaItem> Items
    {
      get
      {
        List<IAbstractMediaItem> container = new List<IAbstractMediaItem>();
        LoadFolders(container);
        LoadFiles(container);
        return container;
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
      get { return Path.GetFileName(_folder); }
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
        return new Uri(_folder);
      }
    }

    /// <summary>
    /// Loads the files.
    /// </summary>
    /// <param name="container">The container.</param>
    private void LoadFiles(ICollection<IAbstractMediaItem> container)
    {
      try
      {
        string[] files = Directory.GetFiles(_folder);
        List<IAbstractMediaItem> fileItems = new List<IAbstractMediaItem>();
        for (int i = 0; i < files.Length; ++i)
        {
          FileContainer cont = new FileContainer(files[i], this);
          fileItems.Add(cont);
        }

        // we need to collect any metadata imported to database
        // for the files
        IImporterManager mgr = ServiceScope.Get<IImporterManager>();
        mgr.GetMetaDataFor(_folder, ref fileItems);
        foreach (IAbstractMediaItem item in fileItems)
        {
          container.Add(item);
        }
      }
      catch (IOException) { }
    }

    /// <summary>
    /// Loads the folders.
    /// </summary>
    /// <param name="container">The container.</param>
    private void LoadFolders(ICollection<IAbstractMediaItem> container)
    {
      try
      {
        string[] folders = Directory.GetDirectories(_folder);
        for (int i = 0; i < folders.Length; ++i)
        {
          FolderContainer cont = new FolderContainer(folders[i], Root, this);
          container.Add(cont);
        }
      }
      catch (IOException) { }
    }

  }
}
