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
using System.Xml.Serialization;

namespace MediaPortal.Common.MediaManagement.MLQueries
{
  /// <summary>
  /// Filter which filters based on the relationship
  /// </summary>
  public class FilteredRelationshipFilter : AbstractRelationshipFilter
  {
    protected IFilter _filter;

    public FilteredRelationshipFilter(Guid role, IFilter filter) :
      base(role)
    {
      _filter = filter;
    }

    [XmlIgnore]
    public IFilter Filter
    {
      get { return _filter; }
      set { _filter = value; }
    }

    public override string ToString()
    {
      return "(ITEM_ID IN (" + _filter + ")" + (_role != Guid.Empty ? " AND (ROLE='" + _role + "' OR LINKED_ROLE='" + _role + "')" : "") + ")";
    }

    #region Additional members for the XML serialization

    internal FilteredRelationshipFilter() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("Between", typeof(BetweenFilter))]
    [XmlElement("BooleanCombination", typeof(BooleanCombinationFilter))]
    [XmlElement("In", typeof(InFilter))]
    [XmlElement("Like", typeof(LikeFilter))]
    [XmlElement("Not", typeof(NotFilter))]
    [XmlElement("Relational", typeof(RelationalFilter))]
    [XmlElement("Empty", typeof(EmptyFilter))]
    [XmlElement("RelationalUserData", typeof(RelationalUserDataFilter))]
    [XmlElement("EmptyUserData", typeof(EmptyUserDataFilter))]
    [XmlElement("False", typeof(FalseFilter))]
    [XmlElement("MediaItemIds", typeof(MediaItemIdFilter))]
    [XmlElement("Relationship", typeof(RelationshipFilter))]
    [XmlElement("FilteredRelationship", typeof(FilteredRelationshipFilter))]
    public object XML_Filter
    {
      get { return _filter; }
      set { _filter = value as IFilter; }
    }

    #endregion
  }
}
