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

using System.Collections.Generic;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.CP;

namespace MediaPortal.Core.UPnP
{
  public class UPnPExtendedDataTypes
  {
    public const string DATATYPES_SCHEMA_URI = "urn:team-mediaportal-com:MP2-UPnP";

    public static readonly UPnPExtendedDataType DtShare = new UPnPDtShare();
    public static readonly UPnPExtendedDataType DtShareEnumeration = new UPnPDtShareEnumeration();
    public static readonly UPnPExtendedDataType DtMediaItemAspectMetadata = new UPnPDtMediaItemAspectMetadata();
    public static readonly UPnPExtendedDataType DtMediaItemQuery = new UPnPDtMediaItemQuery();
    public static readonly UPnPExtendedDataType DtMediaItems = new UPnPDtMediaItems();
    public static readonly UPnPExtendedDataType DtMediaItemsFilter = new UPnPDtMediaItemsFilter();
    public static readonly UPnPExtendedDataType DtMediaItemAttributeValues = new UPnPDtMediaItemAttributeValues();
    public static readonly UPnPExtendedDataType DtMediaItemAspects = new UPnPDtMediaItemAspects();

    protected static IDictionary<string, UPnPExtendedDataType> _dataTypes = new Dictionary<string, UPnPExtendedDataType>();

    static UPnPExtendedDataTypes()
    {
      AddDataType(DtShare);
      AddDataType(DtShareEnumeration);
      AddDataType(DtMediaItemAspectMetadata);
      AddDataType(DtMediaItemQuery);
      AddDataType(DtMediaItems);
      AddDataType(DtMediaItemsFilter);
      AddDataType(DtMediaItemAttributeValues);
      AddDataType(DtMediaItemAspects);
    }

    protected static void AddDataType(UPnPExtendedDataType type)
    {
      _dataTypes.Add(type.SchemaURI + ":" + type.DataTypeName, type);
    }

    /// <summary>
    /// <see cref="DataTypeResolverDlgt"/>
    /// </summary>
    public static bool ResolveDataType(string dataTypeName, out UPnPExtendedDataType dataType)
    {
      return _dataTypes.TryGetValue(dataTypeName, out dataType);
    }
  }
}
