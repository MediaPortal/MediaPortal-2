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
  /// Contains the metadata specification of the "Character" media item aspect which is assigned to series and movie media items.
  /// It describes the fictional character that is played by an actor.
  /// </summary>
  public static class CharacterAspect
  {
    /// <summary>
    /// Media item aspect id of the character aspect.
    /// </summary>
    public static readonly Guid ASPECT_ID = new Guid("1B64DA77-B206-4867-817C-2AE7CEFD0E6F");

    /// <summary>
    /// Character name.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_CHARACTER_NAME =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("CharacterName", 100, Cardinality.Inline, true);

    /// <summary>
    /// Actor name.
    /// </summary>
    public static readonly MediaItemAspectMetadata.SingleAttributeSpecification ATTR_ACTOR_NAME =
        MediaItemAspectMetadata.CreateSingleStringAttributeSpecification("ActorName", 100, Cardinality.Inline, true);

    public static readonly SingleMediaItemAspectMetadata Metadata = new SingleMediaItemAspectMetadata(
        ASPECT_ID, "CharacterItem", new[] {
            ATTR_CHARACTER_NAME,
            ATTR_ACTOR_NAME
        });

    public static readonly Guid ROLE_CHARACTER = new Guid("D326A553-5CF5-47B8-B8E3-338D739E5F2C");
  }
}
