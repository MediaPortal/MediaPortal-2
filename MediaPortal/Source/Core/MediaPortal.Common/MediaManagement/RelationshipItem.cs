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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Common.MediaManagement
{
  /// <summary>
  /// Encapsulates a relationship extracted from a <see cref="IRelationshipRoleExtractor"/>.
  /// </summary>
  public class RelationshipItem
  {
    /// <summary>
    /// Creates a new <see cref="RelationshipItem"/> and sets <see cref="HasChanged"/> to true if <paramref name="mediaItemId"/> is empty. 
    /// </summary>
    /// <param name="aspects">The extracted aspects of the relationship item.</param>
    /// <param name="mediaItemId">The media item id of the relationship item.</param>
    public RelationshipItem(IDictionary<Guid, IList<MediaItemAspect>> aspects, Guid mediaItemId)
      : this(aspects, mediaItemId, mediaItemId == Guid.Empty)
    { }

    /// <summary>
    /// Creates a new <see cref="RelationshipItem"/>.
    /// </summary>
    /// <param name="aspects">The extracted aspects of the relationship item.</param>
    /// <param name="mediaItemId">The media item id of the relationship item.</param>
    /// <param name="hasChanged">Whether aspects have been added or updated.</param>
    public RelationshipItem(IDictionary<Guid, IList<MediaItemAspect>> aspects, Guid mediaItemId, bool hasChanged)
    {
      Aspects = aspects;
      MediaItemId = mediaItemId;
      HasChanged = hasChanged;
    }

    /// <summary>
    /// The extracted aspects of the relationship item.
    /// </summary>
    public IDictionary<Guid, IList<MediaItemAspect>> Aspects { get; set; }

    /// <summary>
    /// The media item id of the relationship item if known,
    /// </summary>
    public Guid MediaItemId { get; set; }

    /// <summary>
    /// Whether aspects have been added or updated.
    /// </summary>
    public bool HasChanged { get; set; }
  }
}
