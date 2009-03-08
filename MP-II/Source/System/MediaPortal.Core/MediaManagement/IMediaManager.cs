#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Core.MediaManagement.MediaProviders;

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Public general interface for the media manager.
  /// </summary>
  public interface IMediaManager
  {
    /// <summary>
    /// Initializes media providers, metadata extractors and internal structures.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Cleans up the runtime data of the media manager.
    /// </summary>
    void Dispose();

    /// <summary>
    /// Collection of all registered local media providers, organized as a dictionary of
    /// (GUID; provider) mappings.
    /// This media provider collection is the proposed entry point to get access to physical media
    /// files.
    /// </summary>
    IDictionary<Guid, IMediaProvider> LocalMediaProviders { get; }

    /// <summary>
    /// Collection of all registered local metadata extractors, organized as a dictionary of
    /// (GUID; metadata extractor) mappings.
    /// </summary>
    IDictionary<Guid, IMetadataExtractor> LocalMetadataExtractors { get; }

    /// <summary>
    /// Synchronous metadata extraction method for an extraction of the specified metadata
    /// from the specified media provider location. Only the specified location will be processed,
    /// i.e. if the location denotes a media item, that item will be processed, else if the location
    /// denotes a folder, metadata for the folder itself will be extracted, no sub items will be processed.
    /// </summary>
    /// <param name="providerId">Id of the media provider to use as source for this metadata extraction.</param>
    /// <param name="path">Path in the provider to extract metadata from.</param>
    /// <param name="metadataExtractorIds">Enumeration of ids of metadata extractors to apply to the
    /// specified media file.</param>
    /// <returns>Dictionary of (media item aspect id; extracted media item aspect)-mappings or
    /// <c>null</c>, if the specified provider doesn't exist or if no metadata could be extracted.</returns>
    IDictionary<Guid, MediaItemAspect> ExtractMetadata(Guid providerId, string path,
        IEnumerable<Guid> metadataExtractorIds);
  }
}