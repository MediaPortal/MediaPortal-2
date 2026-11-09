#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
  /// Contains the metadata specification of the "Trailer" media item aspect, which carries the short films a
  /// title advertises itself with.
  /// </summary>
  /// <remarks>
  /// <para>
  /// It is a multiple aspect because the tagging tools write more than one: a metadata file may name several
  /// <c>&lt;trailer&gt;</c> elements, and a film folder commonly holds a teaser and a trailer beside each
  /// other. One row is one trailer, so a title with three of them carries three rows.
  /// </para>
  /// <para>
  /// <see cref="ATTR_URL"/> keeps the address <b>as the metadata file wrote it</b>, which for the tagging
  /// tools' own format is not something a player can open - <c>plugin://plugin.video.youtube/play/?video_id=…</c>
  /// is how those tools write a web trailer, while the same film sits at <c>https://www.youtube.com/watch?v=…</c>
  /// for everybody else. The address is translated when it is asked for and not when it is stored, so that a
  /// title imported years ago benefits from a better translation the day one is written; the translation
  /// itself is at <c>TrailerAddress</c>.
  /// </para>
  /// <para>
  /// A trailer is not a title of its own: the reference kept it as a field of its media object too, and a
  /// trailer imported as a media item would turn up on the film shelf as a second, much shorter film.
  /// </para>
  /// </remarks>
  public static class TrailerAspect
  {
    /// <summary>
    /// Media item aspect id of the trailer aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("BBE6E53A-EB07-42C7-8FE8-371D4469A26B");

    /// <summary>
    /// The address names something no player here can open. Kept, shown, and refused with a reason.
    /// </summary>
    public const int KIND_OTHER = 0;

    /// <summary>
    /// A file the library can reach, whether on the machine the server runs on or on one of its shares.
    /// </summary>
    public const int KIND_FILE = 1;

    /// <summary>
    /// A web address a player opens as it stands, an Apple trailer file for example.
    /// </summary>
    public const int KIND_STREAM = 2;

    /// <summary>
    /// A film standing at a video site whose page has to be named for the player, YouTube at the moment.
    /// </summary>
    public const int KIND_YOUTUBE = 3;

    /// <summary>
    /// The address of the trailer, as the metadata file wrote it. See the remarks of this class for why that
    /// is not always something to play and where the translation happens.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_URL =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("Url", 1000, Cardinality.Inline, true);

    /// <summary>
    /// What kind of address <see cref="ATTR_URL"/> holds: one of the <c>KIND_</c> values of this class.
    /// Worked out when the trailer is written, so that a shelf can offer "Internet" or "File" without
    /// translating anything itself.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_KIND =
        MediaItemAspectMetadata.CreateMultipleAttributeSpecification("Kind", typeof(int), Cardinality.Inline, false);

    /// <summary>
    /// What the trailer is called, when the metadata file or the file name says. Null for the trailer nobody
    /// named, which is the usual case and reads as "Trailer" on a screen.
    /// </summary>
    public static readonly MediaItemAspectMetadata.MultipleAttributeSpecification ATTR_LABEL =
        MediaItemAspectMetadata.CreateMultipleStringAttributeSpecification("Label", 200, Cardinality.Inline, false);

    public static readonly MultipleMediaItemAspectMetadata Metadata = new MultipleMediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "Trailer",
        new[] {
            ATTR_URL,
            ATTR_KIND,
            ATTR_LABEL,
        },
        new[] {
            ATTR_URL,
        });
  }
}
