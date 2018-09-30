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
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.Common.Services.MediaManagement.ImportDataflowBlocks
{
  /// <summary>
  /// Interface for a cache that can use an <see cref="IRelationshipRoleExtractor"/> to match a dictionary
  /// of <see cref="MediaItemAspect"/>s to a media item id.
  /// </summary>
  /// <remarks>
  /// As part of the relationship extraction phase of the importer it is necessary to reconcile any newly
  /// extracted relations with existing items in the media library, using an <see cref="IRelationshipRoleExtractor"/> to determine
  /// whether two sets of aspects represent the same <see cref="MediaItem"/>.
  /// <para>
  /// Often the same extracted relation will need to be reconciled a large number of times, e.g. a similar set of actors will be
  /// extracted from every episode in a series. Implementations of this interface can be used to reconcile items without the
  /// need to query the media library.
  /// </para>
  /// </remarks>
  public interface IRelationshipCache
  {
    /// <summary>
    /// Determines whether the media item with the <paramref name="mediaItemId"/>
    /// has ever been cached by this <see cref="IRelationshipCache"/> instance.
    /// <para>
    /// Success here does not necessarily mean that the item is currently contained
    /// in the cache as it may have been pruned.
    /// </para>
    /// </summary>
    /// <param name="mediaItemId">The id of the media item.</param>
    /// <returns><c>true</c>, if an item with the specified id has ever been in the cache.</returns>
    bool HasItemEverBeenCached(Guid mediaItemId);

    /// <summary>
    /// Tries to add a <see cref="MediaItem"/> to the cache, using the <see cref="IRelationshipRoleExtractor"/>
    /// to determine how the item should be cached so that the same <see cref="IRelationshipRoleExtractor"/> can
    /// be used to later retrieve the cached item.
    /// </summary>
    /// <param name="item">The <see cref="MediaItem"/> to cache.</param>
    /// <param name="itemMatcher">The <see cref="IRelationshipRoleExtractor"/> that will be used .</param>
    /// <returns><c>true</c> if the item was added.</returns>
    bool TryAddItem(MediaItem item, IRelationshipRoleExtractor itemMatcher);

    /// <summary>
    /// Tries to match the <paramref name="aspects"/> to a cached media item id, using an
    /// <see cref="IRelationshipRoleExtractor"/> to determine whether two sets of aspects represent the
    /// same <see cref="MediaItem"/>.
    /// </summary>
    /// <param name="aspects">The aspects to try to match against an existing id.</param>
    /// <param name="itemMatcher">The <see cref="IRelationshipRoleExtractor"/> used to determine equality.</param>
    /// <param name="mediaItemId">If successful, the id of the cached item.</param>
    /// <returns><c>true</c> if a matching media item id was found.</returns>
    bool TryGetItemId(IDictionary<Guid, IList<MediaItemAspect>> aspects, IRelationshipRoleExtractor itemMatcher, out Guid mediaItemId);
  }
}
