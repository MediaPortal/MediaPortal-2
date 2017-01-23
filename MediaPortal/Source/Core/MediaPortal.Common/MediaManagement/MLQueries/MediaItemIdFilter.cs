#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

    public MediaItemIdFilter(IEnumerable<Guid> mediaItemIds)
    {
      _mediaItemIds = new List<Guid>(mediaItemIds);
    }

    public MediaItemIdFilter(Guid mediaItemId)
    {
      _mediaItemIds = new List<Guid> {mediaItemId};
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
