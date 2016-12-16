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
using System.Xml;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Utils;

namespace MediaPortal.Common.UPnP
{
  /// <summary>
  /// Data type serializing and deserializing values of the same media item attribute type from different
  /// media item instances.
  /// </summary>
  /// <remarks>
  /// This data type uses <see cref="HomogenousCollection"/> as data container.
  /// </remarks>
  public class UPnPDtMediaItemAttributeValues : UPnPExtendedDataType
  {
    public const string DATATYPE_NAME = "DtMediaItemAttributeValues";

    internal UPnPDtMediaItemAttributeValues() : base(DataTypesConfiguration.DATATYPES_SCHEMA_URI, DATATYPE_NAME)
    {
    }

    public override bool SupportsStringEquivalent
    {
      get { return false; }
    }

    public override bool IsNullable
    {
      get { return false; }
    }

    public override bool IsAssignableFrom(Type type)
    {
      return typeof(HomogenousMap).IsAssignableFrom(type);
    }

    protected override void DoSerializeValue(object value, bool forceSimpleValue, XmlWriter writer)
    {
      HomogenousMap map = (HomogenousMap) value;
      writer.WriteStartElement("Values");
      Type type = map.KeyType;
      writer.WriteAttributeString("type", type.FullName);
      foreach (KeyValuePair<object, object> kvp in map)
      {
        MediaItemAspect.SerializeValue(writer, kvp.Key, type);
        MediaItemAspect.SerializeValue(writer, kvp.Value, typeof(int));
      }
    }

    protected override object DoDeserializeValue(XmlReader reader, bool isSimpleValue)
    {
      if (SoapHelper.ReadEmptyStartElement(reader)) // Read start of enclosing element
        return null;
      if (!reader.MoveToAttribute("type"))
        throw new ArgumentException("Cannot deserialize value, 'type' attribute missing");
      String typeStr = reader.ReadContentAsString();
      Type type = Type.GetType(typeStr);
      reader.MoveToElement();
      HomogenousMap result = new HomogenousMap(type, typeof(int));
      if (SoapHelper.ReadEmptyStartElement(reader, "Values"))
        return result;
      while (reader.NodeType != XmlNodeType.EndElement)
        result.Add(MediaItemAspect.DeserializeValue(reader, type), MediaItemAspect.DeserializeValue(reader, typeof(int)));
      reader.ReadEndElement(); // End of enclosing element
      return result;
    }
  }
}
