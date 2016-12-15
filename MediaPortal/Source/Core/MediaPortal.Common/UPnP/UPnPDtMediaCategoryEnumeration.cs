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
using System.Linq;
using System.Xml;
using MediaPortal.Common.MediaManagement;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Utils;

namespace MediaPortal.Common.UPnP
{
  /// <summary>
  /// Data type serializing and deserializing enumerations of <see cref="MediaCategory"/> objects.
  /// </summary>
  public class UPnPDtMediaCategoryEnumeration : UPnPExtendedDataType
  {
    public const string DATATYPE_NAME = "DtMediaCategoryEnumeration";

    internal UPnPDtMediaCategoryEnumeration() : base(DataTypesConfiguration.DATATYPES_SCHEMA_URI, DATATYPE_NAME)
    {
    }

    public override bool SupportsStringEquivalent
    {
      get { return false; }
    }

    public override bool IsNullable
    {
      get { return true; }
    }

    public override bool IsAssignableFrom(Type type)
    {
      return typeof(IEnumerable).IsAssignableFrom(type);
    }

    protected override void DoSerializeValue(object value, bool forceSimpleValue, XmlWriter writer)
    {
      IEnumerable mediaCategories = (IEnumerable) value;
      foreach (MediaCategory mCat in mediaCategories)
        new MediaCategory_DTO(mCat).Serialize(writer);
    }

    protected override object DoDeserializeValue(XmlReader reader, bool isSimpleValue)
    {
      IDictionary<string, MediaCategory_DTO> result_dtos = new Dictionary<string, MediaCategory_DTO>();
      if (SoapHelper.ReadEmptyStartElement(reader)) // Read start of enclosing element
        return new List<MediaCategory>();
      while (reader.NodeType != XmlNodeType.EndElement)
      {
        MediaCategory_DTO result_dto = MediaCategory_DTO.Deserialize(reader);
        result_dtos.Add(result_dto.CategoryName, result_dto);
      }
      reader.ReadEndElement(); // End of enclosing element
      return new List<MediaCategory>(result_dtos.Select(mCatDtoKvp => mCatDtoKvp.Value.GetMediaCategory(result_dtos)));
    }
  }
}