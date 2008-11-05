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
using System.IO;
using MediaPortal.Media.MediaManager;
using MediaPortal.Media.MetaData;

namespace LocalHdProvider
{
  public class FileContainer : IMediaItem
  {
    private IRootContainer _parent;
    private readonly Dictionary<string, object> _metaData;
    private readonly Uri _uri;
    private string _title;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileContainer"/> class.
    /// </summary>
    /// <param name="file">The file.</param>
    /// <param name="parent">The parent.</param>
    public FileContainer(string file, IRootContainer parent)
    {
      _uri = new Uri(file);
      _title = Path.GetFileNameWithoutExtension(file);
      _parent = parent;
      _metaData = LoadMetaData();
    }

    #region IMediaItem Members
    public IMetaDataMappingCollection Mapping
    {
      get
      {
        if (_parent != null)
          return _parent.Mapping;
        return null;
      }
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
    /// Returns the metadata of the media item.
    /// </summary>
    /// <value></value>
    public Dictionary<string, object> MetaData
    {
      get { return _metaData; }
    }

    /// <summary>
    /// Returns the title of the media item.
    /// </summary>
    /// <value></value>
    public string Title
    {
      get
      {
        return _title;
      }
      set
      {
        _title = value;
      }
    }

    /// <summary>
    /// gets the content uri for this media item
    /// </summary>
    /// <value></value>
    public Uri ContentUri
    {
      get { return _uri; }
    }

    /// <summary>
    /// Gets or sets the full path.
    /// </summary>
    /// <value>The full path.</value>
    public string FullPath
    {
      get
      {
        return String.Format(@"{0}/{1}", Parent.FullPath, Title);
      }
      set { }
    }
    #endregion

    /// <summary>
    /// Loads the meta data.
    /// </summary>
    /// <returns></returns>
    private Dictionary<string, object> LoadMetaData()
    {
      Dictionary<string, object> data = new Dictionary<string, object>();
      FileInfo info = new FileInfo(_uri.LocalPath);
      data.Add("Size", info.Length);
      data.Add("Date", info.LastWriteTime);
      data.Add("readonly", info.IsReadOnly);
      data.Add("CoverArt", _uri);
      return data;
    }

  }
}
