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
using System.Xml;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Utilities.Exceptions;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Utils;

namespace MediaPortal.Core.UPnP
{
  /// <summary>
  /// Data type serializing and deserializing <see cref="MediaItemAspectMetadata"/> objects.
  /// </summary>
  public class UPnPDtMediaItemAspectMetadata : UPnPExtendedDataType
  {
    public const string DATATYPE_NAME = "DtMediaItemAspectMetadata";

    internal UPnPDtMediaItemAspectMetadata() : base(DataTypesConfiguration.DATATYPES_SCHEMA_URI, DATATYPE_NAME)
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
      return typeof(MediaItemAspectMetadata).IsAssignableFrom(type);
    }

    public override void DoSerializeValue(object value, bool forceSimpleValue, XmlWriter writer)
    {
      MediaItemAspectMetadata miam = (MediaItemAspectMetadata) value;
      miam.Serialize(writer);
    }

    public override object DoDeserializeValue(XmlReader reader, bool isSimpleValue)
    {
      reader.ReadStartElement(); // Read start of enclosing element
      MediaItemAspectMetadata result = MediaItemAspectMetadata.Deserialize(reader);
      reader.ReadEndElement(); // End of enclosing element
      return result;
    }
  }
}
