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

namespace MediaPortal.MediaLibrary
{
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

    ICollection<MediaItem> Search(string query);

    ICollection<MediaItem> Browse(MediaItem parent);

    #endregion

    #region Media import

    //TODO
    void Import();

    #endregion

    #region Media item aspect management

    bool MediaItemAspectStorageExists(MediaItemAspectMetadata miam);

    void AddMediaItemAspectStorage(MediaItemAspectMetadata miam);

    void RemoveMediaItemAspectStorage(MediaItemAspectMetadata miam);

    #endregion

    #region Shares management

    /// <summary>
    /// Adds a share to the media library's collection of registered shares.
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
    Guid RegisterShare(SystemName nativeSystem, Guid providerId, string path,
        string shareName, IEnumerable<string> mediaCategories, IEnumerable<Guid> metadataExtractorIds);

    /// <summary>
    /// Removes the share with the specified id.
    /// </summary>
    /// <param name="shareId">The id of the share to be removed. The share id is part of the
    /// <see cref="ShareDescriptor"/> which was returned by the <see cref="RegisterShare"/> method.</param>
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
    /// <param name="relocateMediaItems">If set to <c>true</c>, the paths of all media items from the
    /// specified share will be adapted to the new base path.</param>
    /// <returns>Changed share descriptor.</returns>
    void UpdateShare(Guid shareId, SystemName nativeSystem, Guid providerId, string path, string shareName, IEnumerable<string> mediaCategories, IEnumerable<Guid> metadataExtractorIds, bool relocateMediaItems);

    /// <summary>
    /// Returns all shares which are registered in the MediaPortal server's media library.
    /// </summary>
    /// <param name="onlyConnectedShares">If set to <c>true</c>, only shares of connected clients will be returned.</param>
    /// <returns>Mapping of share's GUIDs to shares.</returns>
    IDictionary<Guid, ShareDescriptor> GetShares(bool onlyConnectedShares);

    /// <summary>
    /// Returns the share descriptor for the share with the specified <paramref name="shareId"/>.
    /// </summary>
    /// <param name="shareId">Id of the share to return.</param>
    /// <returns>Descriptor of the share with the specified <paramref name="shareId"/>. If the specified
    /// share doesn't exist, the method returns <c>null</c>.</returns>
    ShareDescriptor GetShare(Guid shareId);

    /// <summary>
    /// Returns a collection of shares for the specified <paramref name="systemName"/>.
    /// </summary>
    /// <param name="systemName">System whose shares should be returned.</param>
    /// <returns>Mapping of share's GUIDs to shares.</returns>
    IDictionary<Guid, ShareDescriptor> GetSharesBySystem(SystemName systemName);

    #endregion

    #region Client management

    /// <summary>
    /// Returns a collection of all known managed MediaPortal clients in the system.
    /// </summary>
    /// <returns>Collection of client system names.</returns>
    ICollection<SystemName> GetManagedClients();

    #endregion
  }
}
