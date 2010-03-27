#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Xml.Serialization;

namespace MediaPortal.Core.MediaManagement.MLQueries
{
  public abstract class AbstractAttributeFilter : IAttributeFilter
  {
    protected MediaItemAspectMetadata.AttributeSpecification _attributeType;

    protected AbstractAttributeFilter(MediaItemAspectMetadata.AttributeSpecification attributeType)
    {
      _attributeType = attributeType;
    }

    [XmlIgnore]
    public MediaItemAspectMetadata.AttributeSpecification AttributeType
    {
      get { return _attributeType; }
    }

    #region Additional members for the XML serialization

    internal AbstractAttributeFilter() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("AttributeType", IsNullable = false)]
    public string XML_AttributeType
    {
      get { return SerializationHelper.SerializeAttributeTypeReference(_attributeType); }
      set { _attributeType = SerializationHelper.DeserializeAttributeTypeReference(value); }
    }

    #endregion
  }
}