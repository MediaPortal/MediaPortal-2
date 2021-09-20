#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using MediaPortal.Utilities;

namespace MediaPortal.Common.MediaManagement.MLQueries
{
  /// <summary>
  /// Filter which filters based on the media item id.
  /// </summary>
  public class MediaItemIdFilter : IFilter
  {
    protected List<Guid> _mediaItemIds;
    protected string _subQueryTable;
    protected string _subQueryColumn;

    public MediaItemIdFilter(IEnumerable<Guid> mediaItemIds)
    {
      _mediaItemIds = new List<Guid>(mediaItemIds);
    }

    public MediaItemIdFilter(Guid mediaItemId)
    {
      _mediaItemIds = new List<Guid> {mediaItemId};
    }

    public MediaItemIdFilter(string subQueryTable, string subQueryColumn)
    {
      _mediaItemIds = new List<Guid>();
      _subQueryTable = subQueryTable.Replace(" ", "");
      _subQueryColumn = subQueryColumn.Replace(" ", "");
    }

    public bool TryGetSubQuery(out string query)
    {
      if (!string.IsNullOrWhiteSpace(_subQueryTable) && !string.IsNullOrWhiteSpace(_subQueryColumn))
      {
        query = $"SELECT {_subQueryColumn} FROM {_subQueryTable}";
        return true;
      }
      query = null;
      return false;
    }

    /// <summary>
    /// Returns the table name which holds Id's which the filtered items must match.
    /// </summary>
    public string SubQueryTableName
    {
      get { return _subQueryTable; }
      set { _subQueryTable = value.Replace(" ", ""); }
    }

    /// <summary>
    /// Returns the table column name which holds Id's which the filtered items must match.
    /// </summary>
    public string SubQueryColumnName
    {
      get { return _subQueryColumn; }
      set { _subQueryColumn = value.Replace(" ", ""); }
    }

    /// <summary>
    /// Returns a collection of media item ids which the filtered items must match.
    /// </summary>
    [XmlIgnore]
    public ICollection<Guid> MediaItemIds
    {
      get { return _mediaItemIds; }
      set { _mediaItemIds = new List<Guid>(value); }
    }

    public override string ToString()
    {
      if (TryGetSubQuery(out var q))
        return $"MEDIA_ITEM_ID IN ({q})";
      if (_mediaItemIds.Count == 0)
        return "1 = 2";
      if (_mediaItemIds.Count == 1)
        return "MEDIA_ITEM_ID = '" + _mediaItemIds.First() + "'";
      return "MEDIA_ITEM_ID IN (" + StringUtils.Join(", ", _mediaItemIds) + ")";
    }

    #region Additional members for the XML serialization

    internal MediaItemIdFilter() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlArray("MediaItemIds", IsNullable = false)]
    public List<Guid> XML_MediaItemIds
    {
      get { return _mediaItemIds; }
      set { _mediaItemIds = value; }
    }

    #endregion
  }
}
