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
using System.Xml;
using MediaPortal.Common.MediaManagement;
using UPnP.Infrastructure.Common;

namespace MediaPortal.Common.UPnP
{
  /// <summary>
  /// Data type serializing and deserializing <see cref="Share"/> objects.
  /// </summary>
  public class UPnPDtShare : UPnPExtendedDataType
  {
    public const string DATATYPE_NAME = "DtShare";

    internal UPnPDtShare() : base(DataTypesConfiguration.DATATYPES_SCHEMA_URI, DATATYPE_NAME)
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
      return typeof(Share).IsAssignableFrom(type);
    }

    protected override void DoSerializeValue(object value, bool forceSimpleValue, XmlWriter writer)
    {
      Share share = (Share) value;
      share.Serialize(writer);
    }

    protected override object DoDeserializeValue(XmlReader reader, bool isSimpleValue)
    {
      reader.ReadStartElement(); // Read start of enclosing element
      Share result = Share.Deserialize(reader);
      reader.ReadEndElement(); // End of enclosing element
      return result;
    }
  }
}
