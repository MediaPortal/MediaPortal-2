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

using System.Collections.Generic;
using MediaPortal.Presentation.DataObjects;

using MediaPortal.Media.MetaData;

namespace Models.Movies
{

  internal class MovieComparer : IComparer<ListItem>
  {
    private int _sortOption;
    IMetadataMapping _mapping;
    /// <summary>
    /// Initializes a new instance of the <see cref="MovieComparer"/> class.
    /// </summary>
    /// <param name="option">The option.</param>
    public MovieComparer(int option, IMetaDataMappingCollection mapping)
    {
      _sortOption = option;
      if (_sortOption >= mapping.Mappings.Count)
        _sortOption = 0;
      _mapping = mapping.Mappings[_sortOption];
    }

    #region IComparer<ListItem> Members

    public int Compare(ListItem x, ListItem y)
    {
      FolderItem item1 = x as FolderItem;
      FolderItem item2 = y as FolderItem;
      if (item1 != null && item2 == null)
      {
        return -1;
      }
      if (item2 != null && item1 == null)
      {
        return 1;
      }
      if (item1 != null && item2 != null)
      {
        //both are folders
        string label1 = item1.Label(_mapping.Items[1].SkinLabel, "").Evaluate();
        string label2 = item2.Label(_mapping.Items[1].SkinLabel, "").Evaluate();
        if (label1 == label2)
        {
          return 0;
        }
        if (label1 == "..")
        {
          return -1;
        }
        if (label2 == "..")
        {
          return 1;
        }
        return string.Compare(label1, label2);
      }
      //both are files...
      object obj1 = null;
      object obj2 = null;
      MovieItem movie1 = x as MovieItem;
      MovieItem movie2 = y as MovieItem;
      int count = _mapping.Items.Count - 1;
      if (movie1 != null && movie2 != null)
      {
        string metaDataField = _mapping.Items[count].MetaDataField;
        if (movie1.MediaItem != null && movie1.MediaItem.MetaData.ContainsKey(metaDataField))
        {
          obj1 = movie1.MediaItem.MetaData[metaDataField];
        }
        if (movie2.MediaItem != null && movie2.MediaItem.MetaData.ContainsKey(metaDataField))
        {
          obj2 = movie2.MediaItem.MetaData[metaDataField];
        }
      }
      return _mapping.Items[count].Formatter.CompareTo(obj1, obj2);

    }

    #endregion
  }
}
