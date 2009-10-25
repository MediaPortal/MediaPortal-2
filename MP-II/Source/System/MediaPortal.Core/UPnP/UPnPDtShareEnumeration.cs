#region Copyright (C) 2005-2008 Team MediaPortal

/* 
 *	Copyright (C) 2005-2008 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Utils;

namespace MediaPortal.Core.UPnP
{
  /// <summary>
  /// Data type serializing and deserializing <see cref="IEnumerable"/> sequences of <see cref="Share"/> objects.
  /// </summary>
  public class UPnPDtShareEnumeration : UPnPExtendedDataType
  {
    public const string DATATYPE_NAME = "DtShareEnumeration";

    internal UPnPDtShareEnumeration() : base(DataTypesConfiguration.DATATYPES_SCHEMA_URI, DATATYPE_NAME)
    {
    }

    public override bool SupportsStringEquivalent
    {
      get { return false; }
    }

    public override void SoapSerializeValue(object value, bool forceSimpleValue, XmlWriter writer)
    {
      if (value != null && !(typeof(IEnumerable).IsAssignableFrom(value.GetType())))
        throw new InvalidDataException("{0} cannot serialize values of type {1}", typeof(UPnPDtShareEnumeration).Name, value.GetType().Name);
      if (value == null)
      {
        SoapWriteNull(writer);
        return;
      }
      writer.WriteStartElement("Shares", DataTypesConfiguration.DATATYPES_SCHEMA_URI);
      IEnumerable shares = (IEnumerable) value;
      foreach (Share share in shares)
        share.Serialize(writer);
      writer.WriteEndElement();
    }

    public override object SoapDeserializeValue(XmlReader reader, bool isSimpleValue)
    {
      ICollection<Share> result = new List<Share>();
      if (SoapReadNull(reader))
        return result;
      if (SoapHelper.ReadEmptyElement(reader, "Shares", DataTypesConfiguration.DATATYPES_SCHEMA_URI))
        return result;
      reader.ReadStartElement("Shares", DataTypesConfiguration.DATATYPES_SCHEMA_URI);
      while (reader.NodeType != XmlNodeType.EndElement)
        result.Add(Share.Deserialize(reader));
      reader.ReadEndElement();
      return result;
    }

    public override bool IsAssignableFrom(Type type)
    {
      return typeof(IEnumerable).IsAssignableFrom(type);
    }
  }
}
