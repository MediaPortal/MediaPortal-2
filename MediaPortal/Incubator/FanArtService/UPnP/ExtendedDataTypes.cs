#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

using System.Collections.Generic;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.CP;

namespace MediaPortal.Extensions.UserServices.FanArtService.UPnP
{

  public class UPnPExtendedDataTypes
  {
    public const string DATATYPES_SCHEMA_URI = "urn:team-mediaportal-com:MP2-UPnP";

    public static readonly UPnPExtendedDataType DtImageCollection = new UPnPDtImageCollection();

    protected static IDictionary<string, UPnPExtendedDataType> _dataTypes = new Dictionary<string, UPnPExtendedDataType>();

    static UPnPExtendedDataTypes()
    {
      AddDataType(DtImageCollection);
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
