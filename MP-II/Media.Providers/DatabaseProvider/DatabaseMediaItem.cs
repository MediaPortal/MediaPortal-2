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
using MediaPortal.Media.MediaManager;
using MediaPortal.Media.MediaManager.Views;
using MediaPortal.Media.MetaData;

namespace Media.Providers.DatabaseProvider
{
  public class DatabaseMediaItem : IMediaItem
  {
    private Dictionary<string, object> _metaData;
    private readonly IDbItem _item;
    private string _title;
    private Uri _contentUri;
    //private string _fullPath;
    private IRootContainer _parent;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseMediaItem"/> class.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="query"></param>
    public DatabaseMediaItem(IDbItem item, IQuery query, IRootContainer parent)
    {
      _parent = parent;
      _item = item;
      LoadMetaData();
      LoadTitle(query);
      LoadContentUri();
      _metaData["title"] = Title;
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

    public string FullPath
    {
      get
      {
        return String.Format(@"{0}/{1}", Parent.FullPath, Title);
      }
      set { }
    }

    /// <summary>
    /// meta data belonging to this media item
    /// </summary>
    /// <value></value>
    public IDictionary<string, object> MetaData
    {
      get { return _metaData; }
    }

    /// <summary>
    /// gets the title of this media item
    /// </summary>
    /// <value></value>
    public string Title
    {
      get { return _title; }
      set
      {
      }
    }

    /// <summary>
    /// gets the content uri for this media item
    /// </summary>
    /// <value></value>
    public Uri ContentUri
    {
      get { return _contentUri; }
    }

    #endregion

    private void LoadMetaData()
    {
      _metaData = new Dictionary<string, object>();
      IEnumerator<KeyValuePair<string, IDbAttribute>> enumer = _item.Attributes.GetEnumerator();
      while (enumer.MoveNext())
      {
        _metaData[enumer.Current.Key] = enumer.Current.Value.Value;
      }
    }

    private void LoadTitle(IQuery query)
    {
      if (_title == null)
      {
        if (query != null && MetaData.ContainsKey(query.Key))
        {
          _title = Convert.ToString(_metaData[query.Key]);
        }
        else if (MetaData.ContainsKey("title"))
        {
          if (_metaData["title"] != null)
            _title = (string)_metaData["title"];
        }
      }
    }

    private void LoadContentUri()
    {
      if (_metaData != null)
      {
        Dictionary<string, object>.Enumerator enumer = _metaData.GetEnumerator();
        while (enumer.MoveNext())
        {
          if (String.Compare(enumer.Current.Key, "contenturi", true) == 0)
          {
            if (enumer.Current.Value != null)
            {
              _contentUri = new Uri(enumer.Current.Value.ToString());
              return;
            }
          }
        }
      }
    }

  }
}
