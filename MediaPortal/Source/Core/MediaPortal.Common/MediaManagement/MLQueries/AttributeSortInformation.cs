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

using System.Xml.Serialization;

namespace MediaPortal.Common.MediaManagement.MLQueries
{
  /// <summary>
  /// Marker interface for sorting information.
  /// </summary>
  public class AttributeSortInformation : ISortInformation
  {
    protected MediaItemAspectMetadata.AttributeSpecification _attributeType;
    protected SortDirection _sortDirection;

    public AttributeSortInformation(MediaItemAspectMetadata.AttributeSpecification attributeType, SortDirection sortDirection)
    {
      _attributeType = attributeType;
      _sortDirection = sortDirection;
    }

    [XmlIgnore]
    public MediaItemAspectMetadata.AttributeSpecification AttributeType
    {
      get { return _attributeType; }
      set { _attributeType = value; }
    }

    public SortDirection Direction
    {
      get { return _sortDirection; }
      set { _sortDirection = value; }
    }

    #region Additional members for the XML serialization

    internal AttributeSortInformation() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("AttributeType", IsNullable = true)]
    public string XML_AttributeType
    {
      get { return _attributeType == null ? null : SerializationHelper.SerializeAttributeTypeReference(_attributeType); }
      set { _attributeType = value == null ? null : SerializationHelper.DeserializeAttributeTypeReference(value); }
    }

    #endregion
  }
}
