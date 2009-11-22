#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Public general interface for the media accessor.
  /// </summary>
  public interface IMediaAccessor
  {
    /// <summary>
    /// Collection of all registered local media providers, organized as a dictionary of
    /// (GUID; provider) mappings.
    /// This media provider collection is the proposed entry point to get access to physical media
    /// files.
    /// </summary>
    IDictionary<Guid, IMediaProvider> LocalMediaProviders { get; }

    /// <summery>
    /// Returns a collection of all local base media providers. This is a convenience property for
    /// using <see cref="LocalMediaProviders"/> and filtering and casting them for <see cref="IBaseMediaProvider"/>.
    /// </summery>
    IEnumerable<IBaseMediaProvider> LocalBaseMediaProviders { get; }

    /// <summery>
    /// Returns a collection of all local chained media providers. This is a convenience property for
    /// using <see cref="LocalMediaProviders"/> and filtering and casting them for <see cref="IChainedMediaProvider"/>.
    /// </summery>
    IEnumerable<IChainedMediaProvider> LocalChainedMediaProviders { get; }

    /// <summary>
    /// Collection of all registered local metadata extractors, organized as a dictionary of
    /// (GUID; metadata extractor) mappings.
    /// </summary>
    IDictionary<Guid, IMetadataExtractor> LocalMetadataExtractors { get; }

    /// <summary>
    /// Initializes media providers, metadata extractors and internal structures.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Cleans up the runtime data of the media accessor.
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Creates shares for the system's MyMusic, MyVideos and MyPictures directories.
    /// </summary>
    /// <returns>Collection of shares. The shares are not saved to the settings yet.</returns>
    ICollection<Share> CreateDefaultShares();

    /// <summary>
    /// Returns an enumeration of local metadata extractors which are classified into the specified
    /// <paramref name="mediaCategory"/>.
    /// </summary>
    /// <param name="mediaCategory">The category to find all local metadata extractors for. If
    /// this parameter is <c>null</c>, the ids of all default metadata extractors are returned,
    /// independent of their category.</param>
    /// <returns>Enumeration of ids of metadata extractors which can handle the
    /// specified <paramref name="mediaCategory"/>.</returns>
    IEnumerable<Guid> GetMetadataExtractorsForCategory(string mediaCategory);

    /// <summary>
    /// Extracts the specified metadata from the specified local media item.
    /// </summary>
    /// <param name="mediaItemAccessor">Media item file to use as source for this metadata extraction.</param>
    /// <param name="metadataExtractorIds">Enumeration of ids of metadata extractors to apply to the
    /// specified media file.</param>
    /// <returns>Dictionary of (media item aspect id; extracted media item aspect)-mappings or
    /// <c>null</c>, if the specified provider doesn't exist or if no metadata could be extracted.
    /// The result might not contain all media item aspects which can be extracted by the specified media provider,
    /// if it couldn't extract all of them.</returns>
    IDictionary<Guid, MediaItemAspect> ExtractMetadata(IResourceAccessor mediaItemAccessor,
        IEnumerable<Guid> metadataExtractorIds);

    /// <summary>
    /// Extracts the specified metadata from the specified local media item.
    /// </summary>
    /// <param name="mediaItemAccessor">Media item file to use as source for this metadata extraction.</param>
    /// <param name="metadataExtractors">Enumeration of metadata extractors to apply to the specified media file.</param>
    /// <returns>Dictionary of (media item aspect id; extracted media item aspect)-mappings or
    /// <c>null</c>, if the specified provider doesn't exist or if no metadata could be extracted.
    /// The result might not contain all media item aspects which can be extracted by the specified media provider,
    /// if it couldn't extract all of them.</returns>
    IDictionary<Guid, MediaItemAspect> ExtractMetadata(IResourceAccessor mediaItemAccessor,
        IEnumerable<IMetadataExtractor> metadataExtractors);

    /// <summary>
    /// Returns a resource locator instance for the specified media <paramref name="item"/>.
    /// </summary>
    /// <param name="item">Media item to return a locator.</param>
    /// <returns>Resource locator instance or <c>null</c>, if the item is invalid.</returns>
    IResourceLocator GetResourceLocator(MediaItem item);
  }
}
