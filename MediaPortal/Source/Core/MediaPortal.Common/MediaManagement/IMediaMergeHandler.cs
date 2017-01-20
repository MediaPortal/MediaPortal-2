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

using MediaPortal.Common.MediaManagement.MLQueries;
using System;
using System.Collections.Generic;

namespace MediaPortal.Common.MediaManagement
{
  /// <summary>
  /// A media merge handler is responsible for merging two media items describing the same media.
  /// </summary>
  public interface IMediaMergeHandler
  {
    /// <summary>
    /// Returns the metadata descriptor for this merge handler.
    /// </summary>
    MergeHandlerMetadata Metadata { get; }

    /// <summary>
    /// Aspects that the media item being merged must have
    /// </summary>
    Guid[] MergeableAspects { get; }

    /// <summary>
    /// Aspects that must be present in order to accurately match items in <see cref="TryMatch"/>  
    /// </summary>
    Guid[] MatchAspects { get; }

    /// <summary>
    /// Get optimized filter that can be used to find a direct match to any existing media item
    /// </summary>
    /// <param name="extractedAspects"></param>
    /// <returns></returns>
    IFilter GetSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects);

    /// <summary>
    /// Some resources cannot exist on their own and must be merged
    /// </summary>
    bool RequiresMerge(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects);

    /// <summary>
    /// Part 1 of the merge - check if the extracted media item matches an existing media item.
    /// If the extracted data contains external identifiers these will be queried
    /// by MediaLibrary against any existing media items. There's no guarantee that
    /// an MI which contains a particular source / type / ID is the same item as the
    /// extracted data (for example a TVDB series identifier is shared by all seasons
    /// and episodes of that series) and since MediaLibrary doesn't know how to choose
    /// between them it delegates to the extractor
    /// </summary>
    bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects);

    /// <summary>
    /// Part 2 of the merge - two media items have been matched.
    /// Merge the necessary aspects into the existing media item and since MediaLibrary doesn't know 
    /// how to merge them it delegates to the extractor
    /// </summary>
    bool TryMerge(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects);
  }
}
