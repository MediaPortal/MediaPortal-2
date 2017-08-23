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
using System.Xml;
using UPnP.Infrastructure.Common;

namespace UPnP.Infrastructure.Dv.DeviceTree
{
  /// <summary>
  /// Device descriptor class for all UPnP extended data types.
  /// </summary>
  public class DvExtendedDataType : DvDataType
  {
    protected UPnPExtendedDataType _dataType;

    public DvExtendedDataType(UPnPExtendedDataType dataType)
    {
      _dataType = dataType;
    }

    /// <summary>
    /// Returns the URI which denotes the XML schema containing a description of this extended data type.
    /// </summary>
    public string SchemaURI
    {
      get { return _dataType.SchemaURI; }
    }

    /// <summary>
    /// The extended data type name in the schema of the specified <see cref="SchemaURI"/>.
    /// </summary>
    public string DataTypeName
    {
      get { return _dataType.DataTypeName; }
    }

    /// <summary>
    /// Returns <c>true</c> if this extended data type can serialize to and deserialize from the "string-equivalent"
    /// form of values.
    /// </summary>
    public bool SupportsStringEquivalent
    {
      get { return _dataType.SupportsStringEquivalent; }
    }

    #region Base overrides

    public override void SoapSerializeValue(object value, bool forceSimpleValue, XmlWriter writer)
    {
      _dataType.SoapSerializeValue(value, forceSimpleValue, writer);
    }

    public override object SoapDeserializeValue(XmlReader reader, bool isSimpleValue)
    {
      return _dataType.SoapDeserializeValue(reader, isSimpleValue);
    }

    public override bool IsAssignableFrom(Type type)
    {
      return _dataType.IsAssignableFrom(type);
    }

    internal override void AddSCPDDescriptionForStandardDataType(XmlWriter writer)
    {
      writer.WriteStartElement("dataType");
      string prefix = writer.LookupPrefix(_dataType.SchemaURI);
      writer.WriteAttributeString("type", prefix + ':' + _dataType.DataTypeName);
      writer.WriteString("string");
      writer.WriteEndElement(); // dataType
    }

    #endregion
  }
}
