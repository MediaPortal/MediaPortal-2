#region Copyright (C) 2007-2010 Team MediaPortal

/* 
 *	Copyright (C) 2007-2010 Team MediaPortal
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
using System.Xml;
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Utils;

namespace MediaPortal.Core.UPnP
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
      return typeof(HomogenousCollection).IsAssignableFrom(type);
    }

    protected override void DoSerializeValue(object value, bool forceSimpleValue, XmlWriter writer)
    {
      HomogenousCollection hc = (HomogenousCollection) value;
      writer.WriteStartElement("ValueCollection");
      Type type = hc.DataType;
      writer.WriteAttributeString("type", type.FullName);
      foreach (object obj in hc)
        MediaItemAspect.SerializeValue(writer, obj, type);
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
      HomogenousCollection result = new HomogenousCollection(type);
      if (SoapHelper.ReadEmptyStartElement(reader, "ValueCollection"))
        return result;
      while (reader.NodeType != XmlNodeType.EndElement)
        result.Add(MediaItemAspect.DeserializeValue(reader, type));
      reader.ReadEndElement(); // End of enclosing element
      return result;
    }
  }
}
