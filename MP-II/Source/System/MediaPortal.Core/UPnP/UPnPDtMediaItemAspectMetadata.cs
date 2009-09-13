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

namespace MediaPortal.Core.UPnP
{
  /// <summary>
  /// Data type serializing and deserializing <see cref="MediaItemAspectMetadata"/> objects.
  /// </summary>
  public class UPnPDtMediaItemAspectMetadata : UPnPExtendedDataType
  {
    public const string DATATYPE_NAME = "DtMediaItemAspectMetadata";

    public UPnPDtMediaItemAspectMetadata() : base(DataTypesConfiguration.DATATYPES_SCHEMA_URI, DATATYPE_NAME)
    {
    }

    public override bool SupportsStringEquivalent
    {
      get { return false; }
    }

    public override string SoapSerializeValue(object value, bool forceSimpleValue)
    {
      if (value != null && !(value is MediaItemAspectMetadata))
        throw new InvalidDataException("{0} cannot serialize values of type {1}", typeof(UPnPDtMediaItemAspectMetadata).Name, value.GetType().Name);
      MediaItemAspectMetadata miam = (MediaItemAspectMetadata) value;
      return miam.Serialize();
    }

    public override object SoapDeserializeValue(XmlElement enclosingElement, bool isSimpleValue)
    {
      return MediaItemAspectMetadata.Deserialize(enclosingElement.InnerXml);
    }

    public override bool IsAssignableFrom(Type type)
    {
      return typeof(MediaItemAspectMetadata).IsAssignableFrom(type);
    }
  }
}