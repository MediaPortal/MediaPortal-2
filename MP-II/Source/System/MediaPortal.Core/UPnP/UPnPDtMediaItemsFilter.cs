#region Copyright (C) 2007-2009 Team MediaPortal

/* 
 *	Copyright (C) 2007-2009 Team MediaPortal
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
using MediaPortal.Core.MediaManagement.MLQueries;
using UPnP.Infrastructure.Common;

namespace MediaPortal.Core.UPnP
{
  /// <summary>
  /// Data type serializing and deserializing polymorphic objects of type <see cref="IFilter"/>.
  /// </summary>
  public class UPnPDtMediaItemsFilter : UPnPExtendedDataType
  {
    public const string DATATYPE_NAME = "DtMediaItemsFilter";

    internal UPnPDtMediaItemsFilter() : base(DataTypesConfiguration.DATATYPES_SCHEMA_URI, DATATYPE_NAME)
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
      return typeof(IFilter).IsAssignableFrom(type);
    }

    protected override void DoSerializeValue(object value, bool forceSimpleValue, XmlWriter writer)
    {
      IFilter filter = (IFilter) value;
      MediaItemQuery.SerializeFilter(writer, filter);
    }

    protected override object DoDeserializeValue(XmlReader reader, bool isSimpleValue)
    {
      reader.ReadStartElement(); // Read start of enclosing element
      IFilter result = MediaItemQuery.DeserializeFilter(reader);
      reader.ReadEndElement(); // End of enclosing element
      return result;
    }
  }
}
