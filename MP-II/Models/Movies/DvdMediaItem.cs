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

namespace Models.Movies
{
  public class DvdMediaItem : IMediaItem
  {
    #region IMediaItem Members

    public IDictionary<string, object> MetaData
    {
      get
      {
        return new Dictionary<string, object>();
      }
    }

    public Uri ContentUri
    {
      get
      {
        return new Uri(@"c:\1.ifo");
      }
    }

    #endregion

    #region IAbstractMediaItem Members


    public IMetaDataMappingCollection Mapping 
    {
      get
      {
        return null;
      }
      set
      {
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
    public string Title
    {
      get
      {
        return "Dvd";
      }
      set { }
    }

    public IRootContainer Parent
    {
      get
      {
        return null;
      }
      set
      {
      }
    }

    public string FullPath
    {
      get
      {
        return "";
      }
      set { }
    }

    #endregion
  }
}
