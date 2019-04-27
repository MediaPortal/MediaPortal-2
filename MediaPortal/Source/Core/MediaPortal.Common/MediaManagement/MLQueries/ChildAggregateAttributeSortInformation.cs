#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
  /// For sorting based on aggregated child attributes.
  /// </summary>
  public class ChildAggregateAttributeSortInformation : ISortInformation
  {
    protected MediaItemAspectMetadata.AttributeSpecification _childAttributeType;
    protected SortDirection _sortDirection;
    protected AggregateFunction _aggregateFunction;
    protected Guid _parentRole;
    protected Guid _childRole;

    public ChildAggregateAttributeSortInformation(Guid parentRole, Guid childRole, MediaItemAspectMetadata.AttributeSpecification childAttributeType, AggregateFunction aggregateFunction, SortDirection sortDirection)
    {
      _aggregateFunction = aggregateFunction;
      _childAttributeType = childAttributeType;
      _sortDirection = sortDirection;
      _parentRole = parentRole;
      _childRole = childRole;
    }

    public Guid ParentRole
    {
      get { return _parentRole; }
      set { _parentRole = value; }
    }

    public Guid ChildRole
    {
      get { return _childRole; }
      set { _childRole = value; }
    }

    [XmlIgnore]
    public MediaItemAspectMetadata.AttributeSpecification ChildAttributeType
    {
      get { return _childAttributeType; }
      set { _childAttributeType = value; }
    }

    public AggregateFunction AggregateFunction
    {
      get { return _aggregateFunction; }
      set { _aggregateFunction = value; }
    }

    public SortDirection Direction
    {
      get { return _sortDirection; }
      set { _sortDirection = value; }
    }

    #region Additional members for the XML serialization

    internal ChildAggregateAttributeSortInformation() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("AttributeType", IsNullable = true)]
    public string XML_AttributeType
    {
      get { return _childAttributeType == null ? null : SerializationHelper.SerializeAttributeTypeReference(_childAttributeType); }
      set { _childAttributeType = value == null ? null : SerializationHelper.DeserializeAttributeTypeReference(value); }
    }

    #endregion
  }
}
