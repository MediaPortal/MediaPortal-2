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
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using MediaPortal.Common.SystemCommunication;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Utils;

namespace MediaPortal.Common.UPnP
{
  /// <summary>
  /// Data type serializing and deserializing enumerations of <see cref="MPClientMetadata"/> objects.
  /// </summary>
  public class UPnPDtMPClientMetadataEnumeration : UPnPExtendedDataType
  {
    public const string DATATYPE_NAME = "DtClientMetadataEnumeration";

    internal UPnPDtMPClientMetadataEnumeration() : base(DataTypesConfiguration.DATATYPES_SCHEMA_URI, DATATYPE_NAME)
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
      return typeof(IEnumerable).IsAssignableFrom(type);
    }

    protected override void DoSerializeValue(object value, bool forceSimpleValue, XmlWriter writer)
    {
      IEnumerable clientData = (IEnumerable) value;
      foreach (MPClientMetadata metadata in clientData)
        metadata.Serialize(writer);
    }

    protected override object DoDeserializeValue(XmlReader reader, bool isSimpleValue)
    {
      ICollection<MPClientMetadata> result = new List<MPClientMetadata>();
      if (SoapHelper.ReadEmptyStartElement(reader)) // Read start of enclosing element
        return result;
      while (reader.NodeType != XmlNodeType.EndElement)
        result.Add(MPClientMetadata.Deserialize(reader));
      reader.ReadEndElement(); // End of enclosing element
      return result;
    }
  }
}
