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

namespace MediaPortal.MediaLibrary
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

    IList<MediaItem> Search(MediaItemQuery query);

    /// <summary>
    /// Returns a set of attribute values of the given <paramref name="attributeType"/> for the media items specified
    /// by the <paramref name="filter"/>.
    /// </summary>
    /// <param name="attributeType">Attribute type, whose values will be returned.</param>
    /// <param name="filter">Filter specifying the media items whose attribute values will be returned.</param>
    /// <returns>Distinct set of attribute values of the given <paramref name="attributeType"/>.</returns>
    ICollection<object> GetDistinctAssociatedValues(MediaItemAspectMetadata.AttributeSpecification attributeType,
        IFilter filter);

    #endregion

    #region Media import

    //TODO: Add/Update/Remove media items

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
    /// <param name="providerId">ID of the media provider of the specified <paramref name="nativeSystem"/>.</param>
    /// <param name="path">Lookup path for the specified provider in the specified system.</param>
    /// <param name="shareName">Name of the new share.</param>
    /// <param name="mediaCategories">Categories of media items which are supposed to be contained in
    /// the new share. If set to <c>null</c>, the new share is a general share without attached media
    /// categories.</param>
    /// <param name="metadataExtractorIds">Ids of metadata extractors to attach to the new share.</param>
    /// <returns>ID of the new share.</returns>
    Guid CreateShare(SystemName nativeSystem, Guid providerId, string path,
        string shareName, IEnumerable<string> mediaCategories, IEnumerable<Guid> metadataExtractorIds);

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
    /// <param name="providerId">ID of the media provider which should be installed in the native system of the share.</param>
    /// <param name="path">Lookup path for the specified provider in the specified system.</param>
    /// <param name="shareName">Name of the share.</param>
    /// <param name="mediaCategories">Categories of media items which are supposed to be contained in
    /// the share. If set to <c>null</c>, the new share is a general share without attached media
    /// categories.</param>
    /// <param name="metadataExtractorIds">Ids of metadata extractors to be attached to the share.</param>
    /// <param name="relocationMode">If set to <see cref="RelocationMode.Relocate"/>, the paths of all media items from the
    /// specified share will be adapted to the new base path. If set to <see cref="RelocationMode.Remove"/>,
    /// all media items from the specified share will be removed from the media library.</param>
    /// <returns>Number of relocated or removed media items.</returns>
    int UpdateShare(Guid shareId, SystemName nativeSystem, Guid providerId, string path, string shareName,
        IEnumerable<string> mediaCategories, IEnumerable<Guid> metadataExtractorIds, RelocationMode relocationMode);

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
