#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
  /// Contains the metadata specification of a stub.
  /// </summary>
  public static class StubAspect
  {
    /// <summary>
    /// Media item aspect id of the stub aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("39CE567C-278F-489C-B846-D0E0671AD4C2");

    /// <summary>
    /// Disc name in windows explorer.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_DISC_NAME =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("DiscName", 250, Cardinality.Inline, true);

    /// <summary>
    /// Message to show when the disc is needed.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_MESSAGE =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("Message", 250, Cardinality.Inline, false);

    public static readonly SingleMediaItemAspectMetadata Metadata = new SingleMediaItemAspectMetadata(
        ASPECT_ID, "StubItem", new[] {
            ATTR_DISC_NAME,
            ATTR_MESSAGE
        });
  }
}
