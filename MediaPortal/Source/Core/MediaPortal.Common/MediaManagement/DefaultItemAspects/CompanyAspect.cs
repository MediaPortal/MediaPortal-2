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

namespace MediaPortal.Common.MediaManagement.DefaultItemAspects
{
  /// <summary>
  /// Contains the metadata specification of the "Company" media item aspect which is assigned to album, series and movie media items.
  /// It describes a company involved in making the media.
  /// </summary>
  public static class CompanyAspect
  {
    // TODO: Put this somewhere else?
    public static readonly string COMPANY_PRODUCTION = "PRODUCTION";
    public static readonly string COMPANY_TV_NETWORK = "TVNETWORK";
    public static readonly string COMPANY_MUSIC_LABEL = "MUSICLABEL";

    /// <summary>
    /// Media item aspect id of the company aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("2203C271-759B-4F08-9B46-7E329CAD1312");

    /// <summary>
    /// Company name.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_COMPANY_NAME =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("CompanyName", 100, Cardinality.Inline, true);

    /// <summary>
    /// Album description
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_DESCRIPTION =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Description", 5000, Cardinality.Inline, false);

    /// <summary>
    /// Specifies the type of company. Use <see cref="CompanyType"/> to cast it to a meaningful value.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_COMPANY_TYPE =
      MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("CompanyType", 15, Cardinality.Inline, true);


    public static readonly SingleMediaItemAspectMetadata Metadata = new SingleMediaItemAspectMetadata(
        ASPECT_ID, "CompanyItem", new[] {
            ATTR_COMPANY_NAME,
            ATTR_DESCRIPTION,
            ATTR_COMPANY_TYPE
        });

    public static readonly Guid ROLE_COMPANY = new Guid("4CD8D4D2-4013-4AC7-A10F-E54325776A16");
    public static readonly Guid ROLE_TV_NETWORK = new Guid("D28A9778-7608-4E6A-9646-C938FE6DB769");
    public static readonly Guid ROLE_MUSIC_LABEL = new Guid("B6F2F3BF-9B4A-41EB-A8D1-8690B6D31FE7");
  }
}
