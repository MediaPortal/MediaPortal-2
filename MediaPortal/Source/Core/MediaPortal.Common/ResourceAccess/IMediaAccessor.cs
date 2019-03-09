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
using System.Threading.Tasks;
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.Common.ResourceAccess
{
  /// <summary>
  /// Public general interface for the media accessor.
  /// </summary>
  public interface IMediaAccessor
  {
    /// <summary>
    /// Returns a mapping of names to media categories for all registered media categories in this system.
    /// </summary>
    /// <remarks>
    /// Modules can register additional media categories using method <see cref="RegisterMediaCategory"/>.
    /// </remarks>
    IDictionary<string, MediaCategory> MediaCategories { get; }

    /// <summary>
    /// Collection of all registered local resource providers, organized as a dictionary of
    /// (GUID; provider) mappings.
    /// This resource provider collection is the proposed entry point to get access to physical media
    /// files.
    /// </summary>
    IDictionary<Guid, IResourceProvider> LocalResourceProviders { get; }

    /// <summery>
    /// Returns a collection of all local base resource providers. This is a convenience property for
    /// using <see cref="LocalResourceProviders"/> and filtering and casting them for <see cref="IBaseResourceProvider"/>.
    /// </summery>
    IEnumerable<IBaseResourceProvider> LocalBaseResourceProviders { get; }

    /// <summery>
    /// Returns a collection of all local chained resource providers. This is a convenience property for
    /// using <see cref="LocalResourceProviders"/> and filtering and casting them for <see cref="IChainedResourceProvider"/>.
    /// </summery>
    IEnumerable<IChainedResourceProvider> LocalChainedResourceProviders { get; }

    /// <summary>
    /// Collection of all registered local metadata extractors, organized as a dictionary of
    /// (GUID; metadata extractor) mappings.
    /// </summary>
    IDictionary<Guid, IMetadataExtractor> LocalMetadataExtractors { get; }

    /// <summary>
    /// Collection of all registered local relationship extractors, organized as a dictionary of
    /// (GUID; relationship extractor) mappings.
    /// </summary>
    IDictionary<Guid, IRelationshipExtractor> LocalRelationshipExtractors { get; }

    /// <summary>
    /// Collection of all registered local merge handlers, organized as a dictionary of
    /// (GUID; merge handler) mappings.
    /// </summary>
    IDictionary<Guid, IMediaMergeHandler> LocalMergeHandlers { get; }

    /// <summary>
    /// Collection of all registered local FanArt handlers, organized as a dictionary of
    /// (GUID; FanArt handler) mappings.
    /// </summary>
    IDictionary<Guid, IMediaFanArtHandler> LocalFanArtHandlers { get; }

    /// <summary>
    /// Initializes resource providers, metadata extractors and internal structures.
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
    /// Registers a media category in this system.
    /// </summary>
    /// <remarks>
    /// Metadata extractors, which are registered for special media categories, aren't invoked before their media categories have been
    /// registered via this method. I.e. if a metadata extractor is provided in an MP2 system by a plugin, that plugin must refresh the
    /// media category registrations for that metadata extractor each time it is loaded.
    /// </remarks>
    /// <param name="name">Unique name for the new media category.</param>
    /// <param name="parentCategories">Parent categories for the new media category.</param>
    /// <returns>New media category which has been created and registered.</returns>
    MediaCategory RegisterMediaCategory(string name, ICollection<MediaCategory> parentCategories);

    /// <summary>
    /// Returns the given <paramref name="mediaCategory"/> and all direct and indirect parent categories.
    /// </summary>
    /// <param name="mediaCategory">Media category to start building the hierarchy collection.</param>
    /// <returns>Collection of media categories from the given <paramref name="mediaCategory"/> along the
    /// <see cref="MediaCategory.ParentCategories"/> up to the root.</returns>
    ICollection<MediaCategory> GetAllMediaCategoriesInHierarchy(MediaCategory mediaCategory);

    /// <summary>
    /// Returns the ids of all local metadata extractors which are classified into the specified
    /// <paramref name="mediaCategory"/>.
    /// </summary>
    /// <param name="mediaCategory">The category to find all local metadata extractors for. If
    /// this parameter is <c>null</c>, the ids of all default metadata extractors are returned,
    /// independent of their category.</param>
    /// <returns>Ids of metadata extractors which can handle the specified <paramref name="mediaCategory"/>.</returns>
    ICollection<Guid> GetMetadataExtractorsForCategory(string mediaCategory);

    /// <summary>
    /// Returns the ids of all local metadata extractors which fill the given media item aspect types.
    /// </summary>
    /// <param name="miaTypeIDs">IDs of media item aspects which should be filled.</param>
    /// <returns>Ids of metadata extractors which fill the specified media item aspects.</returns>
    ICollection<Guid> GetMetadataExtractorsForMIATypes(IEnumerable<Guid> miaTypeIDs);

    /// <summary>
    /// Extracts the specified metadata from the specified local media item.
    /// </summary>
    /// <param name="mediaItemAccessor">Media item file to use as source for this metadata extraction.</param>
    /// <param name="metadataExtractorIds">Enumeration of ids of metadata extractors to apply to the
    /// specified media file.</param>
    /// <param name="forceQuickMode">Specifies if only quick operations for IMetaDataExtractor are allowed.</param>
    /// 
    /// <returns>Dictionary of (media item aspect id; extracted media item aspect)-mappings or
    /// <c>null</c>, if the specified provider doesn't exist or if no metadata could be extracted.
    /// The result might not contain all media item aspects which can be extracted by the specified resource provider,
    /// if it couldn't extract all of them.</returns>
    Task<IDictionary<Guid, IList<MediaItemAspect>>> ExtractMetadataAsync(IResourceAccessor mediaItemAccessor,
      IEnumerable<Guid> metadataExtractorIds, bool forceQuickMode);

    /// <summary>
    /// Extracts the specified metadata from the specified local media item.
    /// </summary>
    /// <param name="mediaItemAccessor">Media item file to use as source for this metadata extraction.</param>
    /// <param name="metadataExtractorIds">Enumeration of ids of metadata extractors to apply to the
    /// specified media file.</param>
    /// <param name="existingAspects">Existing aspects to add and/or enhance.</param>
    /// <param name="forceQuickMode">Specifies if only quick operations for IMetaDataExtractor are allowed.</param>
    /// 
    /// <returns>Dictionary of (media item aspect id; extracted media item aspect)-mappings or
    /// <c>null</c>, if the specified provider doesn't exist or if no metadata could be extracted.
    /// The result might not contain all media item aspects which can be extracted by the specified resource provider,
    /// if it couldn't extract all of them.</returns>
    Task<IDictionary<Guid, IList<MediaItemAspect>>> ExtractMetadataAsync(IResourceAccessor mediaItemAccessor,
      IEnumerable<Guid> metadataExtractorIds, IDictionary<Guid, IList<MediaItemAspect>> existingAspects,
      bool forceQuickMode);

    /// <summary>
    /// Extracts the specified metadata from the specified local media item.
    /// </summary>
    /// <param name="mediaItemAccessor">Media item file to use as source for this metadata extraction.</param>
    /// <param name="metadataExtractors">Enumeration of metadata extractors to apply to the specified media file.</param>
    /// <param name="forceQuickMode">Specifies if only quick operations for IMetaDataExtractor are allowed.</param>
    /// 
    /// <returns>Dictionary of (media item aspect id; extracted media item aspect)-mappings or
    /// <c>null</c>, if none of the specified providers could extract any metadata.
    /// The result might not contain all media item aspects which can be extracted by the specified resource provider,
    /// if it couldn't extract all of them.</returns>
    Task<IDictionary<Guid, IList<MediaItemAspect>>> ExtractMetadataAsync(IResourceAccessor mediaItemAccessor,
      IEnumerable<IMetadataExtractor> metadataExtractors, bool forceQuickMode);

    /// <summary>
    /// Extracts the specified metadata from the specified local media item.
    /// </summary>
    /// <param name="mediaItemAccessor">Media item file to use as source for this metadata extraction.</param>
    /// <param name="metadataExtractors">Enumeration of metadata extractors to apply to the specified media file.</param>
    /// <param name="existingAspects">Existing aspects to add and/or enhance.</param>
    /// <param name="forceQuickMode">Specifies if only quick operations for IMetaDataExtractor are allowed.</param>
    /// 
    /// <returns>Dictionary of (media item aspect id; extracted media item aspect)-mappings or
    /// <c>null</c>, if none of the specified providers could extract any metadata.
    /// The result might not contain all media item aspects which can be extracted by the specified resource provider,
    /// if it couldn't extract all of them.</returns>
    Task<IDictionary<Guid, IList<MediaItemAspect>>> ExtractMetadataAsync(IResourceAccessor mediaItemAccessor,
      IEnumerable<IMetadataExtractor> metadataExtractors, IDictionary<Guid, IList<MediaItemAspect>> existingAspects,
      bool forceQuickMode);

    /// <summary>
    /// Returns a media item for a local resource with metadata extracted by the metadata extractors specified by the
    /// <paramref name="metadataExtractorIds"/> from the specified <paramref name="mediaItemAccessor"/>.
    /// </summary>
    /// <param name="mediaItemAccessor">Accessor describing the media item to extract metadata.</param>
    /// <param name="metadataExtractorIds">Ids of the metadata extractors to employ on the media item.</param>
    /// <returns>Media item with the specified metadata </returns>
    MediaItem CreateLocalMediaItem(IResourceAccessor mediaItemAccessor, IEnumerable<Guid> metadataExtractorIds);

    /// <summary>
    /// Checks if the given directory <paramref name="mediaItemAccessor"/> is considered a "single item" media source (like DVD or BD folders on hard drive).
    /// </summary>
    /// <param name="mediaItemAccessor">The media item resource accessor to open the stream to the physical media.</param>
    /// <returns><c>true</c> if it is a single item.</returns>
    bool IsDirectorySingleResource(IResourceAccessor mediaItemAccessor);

    /// <summary>
    /// Checks if the given <paramref name="mediaItemAccessor"/> points to a stub (a CD, DVD or BD placeholder).
    /// </summary>
    /// <param name="mediaItemAccessor">The media item resource accessor to open the stream to the physical media.</param>
    /// <returns><c>true</c> if it is a stub item.</returns>
    bool IsStubResource(IResourceAccessor mediaItemAccessor);

    /// <summary>
    /// Extracts the stub items from the specified local media item.
    /// </summary>
    /// <param name="mediaItemAccessor">Media item file to use as source for this stub item extraction.</param>
    /// <param name="metadataExtractorIds">Enumeration of ids of metadata extractors to use for extracting stubs.</param>
    /// <returns>List of dictionaries of (media item aspect id; extracted media item aspect)-mappings or
    /// <c>null</c>, if the specified provider doesn't exist or if no metadata could be extracted.
    /// The result might not contain all media item aspects which can be extracted by the specified resource provider,
    /// if it couldn't extract all of them.</returns>
    IEnumerable<IDictionary<Guid, IList<MediaItemAspect>>> ExtractStubItems(IResourceAccessor mediaItemAccessor, IEnumerable<Guid> metadataExtractorIds);
  }
}
