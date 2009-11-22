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
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.MLQueries;

namespace MediaPortal.Backend.MediaLibrary
{
  public enum RelocationMode
  {
    Relocate,
    Remove
  }

  /// <summary>
  /// The media library is a "dumb" data store. It provides search/update methods for all kinds of content stored.
  /// All media-related management functions (re-imports triggered by a special situation etc.) are handled by the media manager.
  /// </summary>
  public interface IMediaLibrary
  {
    #region Startup & Shutdown

    void Startup();
    void Shutdown();

    #endregion

    #region Media query

    /// <summary>
    /// Starts a search of media items.
    /// </summary>
    /// <param name="query">Query object which specifies the search parameters.</param>
    /// <returns>List of matching media items with the media item aspects of the given
    /// <see cref="MediaItemQuery.NecessaryRequestedMIATypeIDs"/> and <see cref="MediaItemQuery.OptionalRequestedMIATypeIDs"/>,
    /// in the given sorting given by <see cref="MediaItemQuery.SortInformation"/>.</returns>
    IList<MediaItem> Search(MediaItemQuery query);

    /// <summary>
    /// Lists all media items of the given location.
    /// </summary>
    /// <param name="system">System of the location to browse.</param>
    /// <param name="path">Path of the location to browse.</param>
    /// <param name="necessaryRequestedMIATypeIDs">IDs of media item aspect types which need to be present in the result.
    /// If a media item at the given location doesn't contain at least one of those media item aspects, it won't be returned.</param>
    /// <param name="optionalRequestedMIATypeIDs">IDs of media item aspect types which will be returned if present.</param>
    /// <returns>Result collection of media items at the given location.</returns>
    ICollection<MediaItem> Browse(SystemName system, ResourcePath path, IEnumerable<Guid> necessaryRequestedMIATypeIDs,
        IEnumerable<Guid> optionalRequestedMIATypeIDs);

    /// <summary>
    /// Returns a set of attribute values of the given <paramref name="attributeType"/> for the media items specified
    /// by the <paramref name="filter"/>.
    /// </summary>
    /// <param name="attributeType">Attribute type, whose values will be returned.</param>
    /// <param name="filter">Filter specifying the media items whose attribute values will be returned.</param>
    /// <returns>Distinct set of attribute values of the given <paramref name="attributeType"/>.</returns>
    HomogenousCollection GetDistinctAssociatedValues(MediaItemAspectMetadata.AttributeSpecification attributeType,
        IFilter filter);

    #endregion

    #region Media import

    /// <summary>
    /// Adds or updates the media item specified by its location (<paramref name="nativeSystem"/> and <paramref name="path"/>).
    /// </summary>
    /// <param name="nativeSystem">The native system of the media item to be updated.</param>
    /// <param name="path">The path at the given system of the media item to be updated.</param>
    /// <param name="mediaItemAspects">Media item aspects to be updated.</param>
    void AddOrUpdateMediaItem(SystemName nativeSystem, ResourcePath path, IEnumerable<MediaItemAspect> mediaItemAspects);

    /// <summary>
    /// Deletes all media items and directories from the media library which are located at the given
    /// <paramref name="nativeSystem"/> with the specified <paramref name="path"/>.
    /// </summary>
    /// <param name="nativeSystem">The native system of the media item or directory to be deleted.</param>
    /// <param name="path">The path at the given system of the media item or directory to be deleted. The path can be
    /// the full path of a media item or just a part of the path in case of a directory.</param>
    void DeleteMediaItemOrPath(SystemName nativeSystem, ResourcePath path);

    #endregion

    #region Media item aspect schema management

    bool MediaItemAspectStorageExists(Guid aspectId);

    MediaItemAspectMetadata GetMediaItemAspectMetadata(Guid aspectId);

    void AddMediaItemAspectStorage(MediaItemAspectMetadata miam);

    void RemoveMediaItemAspectStorage(Guid aspectId);

    IDictionary<Guid, MediaItemAspectMetadata> GetManagedMediaItemAspectMetadata();

    MediaItemAspectMetadata GetManagedMediaItemAspectMetadata(Guid aspectId);

    #endregion

    #region Shares management

    /// <summary>
    /// Adds an existing share to the media librarie's collection of registered shares.
    /// </summary>
    /// <param name="share">Share to be added.</param>
    void RegisterShare(Share share);

    /// <summary>
    /// Creates a new share and adds it to the media library's collection of registered shares.
    /// </summary>
    /// <param name="nativeSystem">System where the media provider for the new share is located.</param>
    /// <param name="baseResourcePath">Lookup path for the provider resource chain in the specified system.</param>
    /// <param name="shareName">Name of the new share.</param>
    /// <param name="mediaCategories">Categories of media items which are supposed to be contained in
    /// the new share. If set to <c>null</c>, the new share is a general share without attached media
    /// categories.</param>
    /// <returns>ID of the new share.</returns>
    Guid CreateShare(SystemName nativeSystem, ResourcePath baseResourcePath,
        string shareName, IEnumerable<string> mediaCategories);

    /// <summary>
    /// Removes the share with the specified id.
    /// </summary>
    /// <param name="shareId">The id of the share to be removed. The share id is part of the
    /// <see cref="Share"/> which was returned by the <see cref="RegisterShare"/> method.</param>
    void RemoveShare(Guid shareId);

    /// <summary>
    /// Reconfigures the share with the specified <paramref name="shareId"/>.
    /// </summary>
    /// <remarks>
    /// The share's native system cannot be changed by this method, else we would have to consider much more security problems.
    /// </remarks>
    /// <param name="shareId">Id of the share to be changed.</param>
    /// <param name="nativeSystem">System where the share is located.</param>
    /// <param name="baseResourcePath">Lookup path for the provider resource chain in the specified system.</param>
    /// <param name="shareName">Name of the share.</param>
    /// <param name="mediaCategories">Categories of media items which are supposed to be contained in
    /// the share. If set to <c>null</c>, the new share is a general share without attached media
    /// categories.</param>
    /// <param name="relocationMode">If set to <see cref="RelocationMode.Relocate"/>, the paths of all media items from the
    /// specified share will be adapted to the new base path. If set to <see cref="RelocationMode.Remove"/>,
    /// all media items from the specified share will be removed from the media library.</param>
    /// <returns>Number of relocated or removed media items.</returns>
    int UpdateShare(Guid shareId, SystemName nativeSystem, ResourcePath baseResourcePath, string shareName,
        IEnumerable<string> mediaCategories, RelocationMode relocationMode);

    /// <summary>
    /// Returns all shares which are registered in the MediaPortal server's media library.
    /// </summary>
    /// <param name="system">Filters the returned shares by system. If <c>null</c>, the returned set isn't filtered
    /// by system.</param>
    /// <param name="onlyConnectedShares">If set to <c>true</c>, only shares of connected clients will be returned.</param>
    /// <returns>Mapping of share's GUIDs to shares.</returns>
    IDictionary<Guid, Share> GetShares(SystemName system, bool onlyConnectedShares);

    /// <summary>
    /// Returns the share descriptor for the share with the specified <paramref name="shareId"/>.
    /// </summary>
    /// <param name="shareId">Id of the share to return.</param>
    /// <returns>Descriptor of the share with the specified <paramref name="shareId"/>. If the specified
    /// share doesn't exist, the method returns <c>null</c>.</returns>
    Share GetShare(Guid shareId);

    void ConnectShares(ICollection<Guid> shareIds);
    void DisconnectShares(ICollection<Guid> shareIds);

    #endregion
  }
}
