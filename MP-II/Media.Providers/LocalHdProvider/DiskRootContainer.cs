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
using System.IO;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Media.Importers;
using MediaPortal.Media.MediaManager;
using MediaPortal.Media.MetaData;

namespace LocalHdProvider
{
  public class DiskRootContainer : IRootContainer
  {
    #region variables
    private readonly string _name;
    private Dictionary<string, object> _metaData;
    IMetaDataMappingCollection _mapping;
    IRootContainer _parent;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="DiskRootContainer"/> class.
    /// </summary>
    /// <param name="name">The name.</param>
    public DiskRootContainer(string name)
    {
      _name = name;
      _metaData = new Dictionary<string, object>();
      _metaData["title"] = Title;
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
    public Dictionary<string, object> MetaData
    {
      get { return _metaData; }
    }

    /// <summary>
    /// gets the media items
    /// </summary>
    /// <value></value>
    public List<IAbstractMediaItem> Items
    {
      get
      {
        List<IAbstractMediaItem> containers = new List<IAbstractMediaItem>();
        LoadFolders(containers);
        LoadFiles(containers);
        return containers;
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
        string[] files = Directory.GetFiles(Title);
        List<IAbstractMediaItem> fileItems = new List<IAbstractMediaItem>();
        for (int i = 0; i < files.Length; ++i)
        {
          FileContainer cont = new FileContainer(files[i], this);
          fileItems.Add(cont);
        }

        // we need to collect any metadata imported to database
        // for the files
        IImporterManager mgr = ServiceScope.Get<IImporterManager>();
        mgr.GetMetaDataFor(Title, ref fileItems);
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
    private void LoadFolders(List<IAbstractMediaItem> container)
    {
      try
      {
        string[] folders = Directory.GetDirectories(Title);
        for (int i = 0; i < folders.Length; ++i)
        {
          FolderContainer cont = new FolderContainer(folders[i], Root, this);
          container.Add(cont);
        }
      }
      catch (IOException) { }
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
      get { return null; }
    }

    /// <summary>
    /// Returns the title of the media item.
    /// </summary>
    /// <value></value>
    public string Title
    {
      get { return _name; }
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
        return new Uri(_name);
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
        if (Parent != null)
          return String.Format(@"{0}/{1}", Parent.FullPath, Title);
        else if (Root != null)
          return String.Format(@"{0}/{1}", Root.FullPath, Title);
        else
          return Title;
      }
      set { }
    }
    #endregion

  }
}
