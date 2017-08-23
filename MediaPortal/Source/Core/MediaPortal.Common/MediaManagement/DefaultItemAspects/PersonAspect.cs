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
  /// Contains the metadata specification of the "Person" media item aspect which is assigned to media items.
  /// It describes a real person working on the media in some capacity. A music band will also be considered a person in this regard.
  /// </summary>
  public static class PersonAspect
  {
    // TODO: Put this somewhere else?
    public static readonly string OCCUPATION_ACTOR = "ACTOR";
    public static readonly string OCCUPATION_ARTIST = "ARTIST";
    public static readonly string OCCUPATION_WRITER = "WRITER";
    public static readonly string OCCUPATION_DIRECTOR = "DIRECTOR";
    public static readonly string OCCUPATION_COMPOSER = "COMPOSER";

    /// <summary>
    /// Media item aspect id of the person aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("A6EEE1E3-A10A-44DB-B53C-B11FF93E9CE2");

    /// <summary>
    /// Person name.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_PERSON_NAME =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("PersonName", 100, Cardinality.Inline, true);

    /// <summary>
    /// Specifies the persons occupation.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_OCCUPATION =
      MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Occupation", 15, Cardinality.Inline, true);

    /// <summary>
    /// Person biography.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_BIOGRAPHY =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Biography", 10000, Cardinality.Inline, false);

    /// <summary>
    /// The origin of the person.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_ORIGIN =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Origin", 300, Cardinality.Inline, false);

    /// <summary>
    /// Date and time the person was born.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_DATEOFBIRTH =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("BornDate", typeof(DateTime), Cardinality.Inline, false);

    /// <summary>
    /// Date and time the person died.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_DATEOFDEATH =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("DeathDate", typeof(DateTime), Cardinality.Inline, false);

    /// <summary>
    /// If set to <c>true</c>, the person is actually a group of people i.e. a music band.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_GROUP =
        MediaItemAspectMetadata.CreateSingleAttributeSpecification("IsGroup", typeof(bool), Cardinality.Inline, true);

    public static readonly SingleMediaItemAspectMetadata Metadata = new SingleMediaItemAspectMetadata(
        ASPECT_ID, "PersonItem", new[] {
            ATTR_PERSON_NAME,
            ATTR_OCCUPATION,
            ATTR_BIOGRAPHY,
            ATTR_ORIGIN,
            ATTR_DATEOFBIRTH,
            ATTR_DATEOFDEATH,
            ATTR_GROUP
        });

    public static readonly Guid ROLE_ACTOR = new Guid("794B29B7-6EC4-4B25-91F7-621C35B804E4");
    public static readonly Guid ROLE_DIRECTOR = new Guid("E9CBD8F7-D686-4ABE-88D6-109C67B28663");
    public static readonly Guid ROLE_WRITER = new Guid("FBFFA5E4-745A-43C9-B79A-9C9F58CED55C");
    public static readonly Guid ROLE_ARTIST = new Guid("B79D187F-93D5-4A88-A592-E2F686C69A0A");
    public static readonly Guid ROLE_ALBUMARTIST = new Guid("{AC13A230-500D-4903-97E2-EF0AEA934B30}");
    public static readonly Guid ROLE_COMPOSER = new Guid("DFB6EEF9-4C57-437D-9984-18ACA0964500");
  }
}
