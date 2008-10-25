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
using MediaPortal.Media.MetaData;

namespace MediaPortal.Media.MediaManager
{

  /// <summary>
  /// Base interface for all media items
  /// </summary>
  public interface IAbstractMediaItem
  {
    /// <summary>
    /// Gets or sets the mapping for the metadata.
    /// </summary>
    /// <value>The mapping for the metadata.</value>
    IMetaDataMappingCollection Mapping { get;set;}

    /// <summary>
    /// Returns the metadata of the media item.
    /// </summary>
    IDictionary<string, object> MetaData { get; }

    /// <summary>
    /// Returns the title of the media item.
    /// </summary>
    string Title { get;set; }

    /// <summary>
    /// the media container in which this media item resides
    /// </summary>
    IRootContainer Parent { get;set;} 

    /// <summary>
    /// Gets or sets the full path.
    /// </summary>
    /// <value>The full path.</value>
    string FullPath { get;set;}

    /// <summary>
    /// Gets the content URI for this item
    /// </summary>
    /// <value>The content URI.</value>
    Uri ContentUri { get;}

    /// <summary>
    /// Gets a value indicating whether this item is located locally or remote
    /// </summary>
    /// <value><c>true</c> if this item is located locally; otherwise, <c>false</c>.</value>
    bool IsLocal { get;}
  }
}
