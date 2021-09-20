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
using System.Xml.Serialization;

namespace MediaPortal.Common.MediaManagement.MLQueries
{
  /// <summary>
  /// Filter which filters by share path.
  /// </summary>
  public class SharePathFilter : IFilter
  {
    protected List<Guid> _shareIds;

    public SharePathFilter(IEnumerable<Guid> shareIds)
    {
      _shareIds = new List<Guid>(shareIds);
    }

    public SharePathFilter(Guid shareId)
    {
      _shareIds = new List<Guid> { shareId };
    }

    [XmlIgnore]
    public ICollection<Guid> ShareIds
    {
      get { return _shareIds; }
      set { _shareIds = new List<Guid>(value); }
    }

    public override string ToString()
    {
      return $"SHARE_ID IN ('{string.Join(",", _shareIds)}')";
    }

    #region Additional members for the XML serialization

    internal SharePathFilter() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlArray("ShareIds", IsNullable = false)]
    public List<Guid> XML_ShareIds
    {
      get { return _shareIds; }
      set { _shareIds = value; }
    }

    #endregion
  }
}
