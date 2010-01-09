#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using System.Xml.Serialization;

namespace MediaPortal.Core.MediaManagement.MLQueries
{
  /// <summary>
  /// Specifies an expression which filters media items by an attribute between two given values.
  /// </summary>
  public class BetweenFilter : AbstractAttributeFilter
  {
    protected object _value1;
    protected object _value2;

    public BetweenFilter(MediaItemAspectMetadata.AttributeSpecification attributeType,
        object value1, object value2) : base(attributeType)
    {
      _value1 = value1;
      _value2 = value2;
    }

    [XmlIgnore]
    public object Value1
    {
      get { return _value1; }
    }

    [XmlIgnore]
    public object Value2
    {
      get { return _value2; }
    }

    #region Additional members for the XML serialization

    internal BetweenFilter() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("Value1")]
    public object XML_Value1
    {
      get { return _value1; }
      set { _value1 = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("Value2")]
    public object XML_Value2
    {
      get { return _value2; }
      set { _value2 = value; }
    }

    #endregion
  }
}
