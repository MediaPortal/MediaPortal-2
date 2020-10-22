#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

namespace MediaPortal.Common.MediaManagement.TransientAspects
{
  /// <summary>
  /// Contains the metadata specification for subtitles.
  /// It is used to pass subtitle information to the server and is not persisted to database.
  /// </summary>
  public static class TempSubtitleAspect
  {
    /// <summary>
    /// Media item aspect id of the subtitle aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("4F74898B-83D5-4A52-9F8F-5958DD22B76D");

    /// <summary>
    /// Subtitle file name.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_NAME =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Name", 100, Cardinality.Inline, false);

    /// <summary>
    /// Subtitle display name.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_DISPLAY_NAME =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("DisplayName", 100, Cardinality.Inline, false);

    /// <summary>
    /// Subtitle language stored as <see cref="CultureInfo.TwoLetterISOLanguageName"/>.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_LANGUAGE =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Language", 100, Cardinality.Inline, false);

    /// <summary>
    /// Subtitle provider.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_PROVIDER =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Provider", 100, Cardinality.Inline, false);

    /// <summary>
    /// Subtitle categories.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_CATEGORY =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Category", 100, Cardinality.Inline, false);

    /// <summary>
    /// Subtitle link or ID needed to download the subtitle.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_SUBTITLEID =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("SubtitleId", 1000, Cardinality.Inline, false);


    public static readonly SingleMediaItemAspectMetadata Metadata = new SingleMediaItemAspectMetadata(
        ASPECT_ID, "TempSubtitleItem", new[] {
            ATTR_NAME,
            ATTR_DISPLAY_NAME,
            ATTR_LANGUAGE,
            ATTR_PROVIDER,
            ATTR_CATEGORY,
            ATTR_SUBTITLEID
        }, true);
  }
}
