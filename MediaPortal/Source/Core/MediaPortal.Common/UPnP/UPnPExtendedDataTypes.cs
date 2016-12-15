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
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.CP;

namespace MediaPortal.Common.UPnP
{
  public class UPnPExtendedDataTypes
  {
    public const string DATATYPES_SCHEMA_URI = "urn:team-mediaportal-com:MP2-UPnP";

    public static readonly UPnPExtendedDataType DtShare = new UPnPDtShare();
    public static readonly UPnPExtendedDataType DtShareEnumeration = new UPnPDtShareEnumeration();
    public static readonly UPnPExtendedDataType DtMediaItemAspectMetadata = new UPnPDtMediaItemAspectMetadata();
    public static readonly UPnPExtendedDataType DtMediaItemQuery = new UPnPDtMediaItemQuery();
    public static readonly UPnPExtendedDataType DtMediaItem = new UPnPDtMediaItem();
    public static readonly UPnPExtendedDataType DtMediaItemEnumeration = new UPnPDtMediaItemEnumeration();
    public static readonly UPnPExtendedDataType DtMediaItemsFilter = new UPnPDtMediaItemsFilter();
    public static readonly UPnPExtendedDataType DtMediaItemAttributeValues = new UPnPDtMediaItemAttributeValues();
    public static readonly UPnPExtendedDataType DtMediaItemAspectEnumeration = new UPnPDtMediaItemAspectEnumeration();
    public static readonly UPnPExtendedDataType DtResourcePathMetadata = new UPnPDtResourcePathMetadata();
    public static readonly UPnPExtendedDataType DtResourcePathMetadataEnumeration = new UPnPDtResourcePathMetadataEnumeration();
    public static readonly UPnPExtendedDataType DtResourceProviderMetadata = new UPnPDtResourceProviderMetadata();
    public static readonly UPnPExtendedDataType DtResourceProviderMetadataEnumeration = new UPnPDtResourceProviderMetadataEnumeration();
    public static readonly UPnPExtendedDataType DtMediaCategoryEnumeration = new UPnPDtMediaCategoryEnumeration();
    public static readonly UPnPExtendedDataType DtMLQueryResultGroupEnumeration = new UPnPDtMLQueryResultGroupEnumeration();
    public static readonly UPnPExtendedDataType DtMPClientMetadataEnumeration = new UPnPDtMPClientMetadataEnumeration();
    public static readonly UPnPExtendedDataType DtPlaylistInformationDataEnumeration = new UPnPDtPlaylistInformationDataEnumeration();
    public static readonly UPnPExtendedDataType DtPlaylistRawData = new UPnPDtPlaylistRawData();
    public static readonly UPnPExtendedDataType DtPlaylistContents = new UPnPDtPlaylistContents();
    public static readonly UPnPExtendedDataType DtUserProfile = new UPnPDtUserProfile();
    public static readonly UPnPExtendedDataType DtUserProfileEnumeration = new UPnPDtUserProfileEnumeration();
    public static readonly UPnPExtendedDataType DtDictionaryGuidDateTime = new UPnPDtDictionary<Guid, DateTime>();
    public static readonly UPnPExtendedDataType DtDictionaryGuidInt32 = new UPnPDtDictionary<Guid, int>();

    protected static IDictionary<string, UPnPExtendedDataType> _dataTypes = new Dictionary<string, UPnPExtendedDataType>();

    static UPnPExtendedDataTypes()
    {
      AddDataType(DtShare);
      AddDataType(DtShareEnumeration);
      AddDataType(DtMediaItemAspectMetadata);
      AddDataType(DtMediaItemQuery);
      AddDataType(DtMediaItem);
      AddDataType(DtMediaItemEnumeration);
      AddDataType(DtMediaItemsFilter);
      AddDataType(DtMediaItemAttributeValues);
      AddDataType(DtMediaItemAspectEnumeration);
      AddDataType(DtResourcePathMetadata);
      AddDataType(DtResourcePathMetadataEnumeration);
      AddDataType(DtResourceProviderMetadata);
      AddDataType(DtResourceProviderMetadataEnumeration);
      AddDataType(DtMediaCategoryEnumeration);
      AddDataType(DtMLQueryResultGroupEnumeration);
      AddDataType(DtMPClientMetadataEnumeration);
      AddDataType(DtPlaylistInformationDataEnumeration);
      AddDataType(DtPlaylistRawData);
      AddDataType(DtPlaylistContents);
      AddDataType(DtUserProfile);
      AddDataType(DtUserProfileEnumeration);
      AddDataType(DtDictionaryGuidDateTime);
      AddDataType(DtDictionaryGuidInt32);
    }

    public static void AddDataType(UPnPExtendedDataType type)
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
