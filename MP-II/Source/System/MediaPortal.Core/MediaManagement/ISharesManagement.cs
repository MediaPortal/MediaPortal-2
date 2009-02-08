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

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Interface to be implemented by classes which are able to manage MP-II system shares.
  /// This interface will be implemented by the MediaPortal server's media library itself, as well as by all
  /// classes "on the way to the client" which are exposing the functionality to the next level.
  /// It will also be implemented by a local shares management for the disconnected client mode.
  /// </summary>
  /// <remarks>
  /// Shares are managed redundantly at MediaPortal clients and at the MediaPortal server's media library.
  /// So it is possible to access local shares while the client is not connected to the server.
  /// Shares, which have been reconfigured by the client in disconnected mode MUST be synchronized with the
  /// server when the next server connection is made. So the client will synchronize its shares each time the
  /// server gets connected.
  /// <br/>
  /// Shares are globally uniquely identified by share GUIDs.
  /// </remarks>
  public interface ISharesManagement
  {
    /// <summary>
    /// Adds a share to the media library's collection of registered shares.
    /// </summary>
    /// <param name="systemName">System where the media provider for the new share is located.</param>
    /// <param name="providerId">ID of the media provider of the specified <paramref name="systemName"/>.</param>
    /// <param name="path">Lookup path for the specified provider in the specified system.</param>
    /// <param name="shareName">Name of the new share.</param>
    /// <param name="mediaCategories">Categories of media items which are supposed to be contained in
    /// the new share. If set to <c>null</c>, the new share is a general share without attached media
    /// categories.</param>
    /// <param name="metadataExtractorIds">Ids of metadata extractors to attach to the new share.
    /// The system will automatically import the desired metadata on all of the share's media items.</param>
    /// <returns>Descriptor of the new share.</returns>
    /// TODO: What about access rights? Not everybody may add and remove shares on remote clients...
    ShareDescriptor RegisterShare(SystemName systemName, Guid providerId, string path,
        string shareName, IEnumerable<string> mediaCategories, IEnumerable<Guid> metadataExtractorIds);

    /// <summary>
    /// Removes the share with the specified id. This will invalidate all references to this share; the share
    /// can no longer be accessed over the server's share management.
    /// </summary>
    /// <param name="shareId">The id of the share to be removed. The share id is part of the
    /// <see cref="ShareDescriptor"/> which was returned by the <see cref="RegisterShare"/> method.</param>
    /// TODO: What about access rights? Not everybody may add and remove shares on remote clients...
    void RemoveShare(Guid shareId);

    /// <summary>
    /// Returns all shares which are registered in the MediaPortal server's media library.
    /// If a MediaPortal client isn't connected to its server, this method should fallback to
    /// a collection of default shares for the client system (consisting of some default provider's
    /// default paths).
    /// </summary>
    /// <returns>Mapping of share's GUIDs to shares.</returns>
    IDictionary<Guid, ShareDescriptor> GetShares();

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

    /// <summary>
    /// Returns a collection of all known managed MediaPortal clients in the system.
    /// </summary>
    /// <returns>Collection of client system names.</returns>
    ICollection<SystemName> GetManagedClients();

    /// <summary>
    /// Returns a collection of metadata extractors which are available in the specified
    /// <paramref name="systemName"/>.
    /// </summary>
    /// <param name="systemName">System whose metadata extractors should be returned.</param>
    /// <returns>Mapping of metadata extractor's GUIDs to metadata extractors.</returns>
    IDictionary<Guid, MetadataExtractorMetadata> GetMetadataExtractorsBySystem(SystemName systemName);

    /// <summary>
    /// Changes the name of the share with the specified <paramref name="shareId"/>.
    /// </summary>
    /// <param name="shareId">Id of the share to be changed.</param>
    /// <param name="name">Name to set.</param>
    void SetShareName(Guid shareId, string name);

    /// <summary>
    /// Reconfigures the share with the specified <paramref name="shareId"/>. Sets its media categories
    /// and metadata extractor ids. This will automatically trigger a re-import of the share.
    /// </summary>
    /// <param name="shareId">Id of the share to be changed.</param>
    /// <param name="mediaCategories">Media categories to be set</param>
    /// <param name="metadataExtractorIds">Ids of the metadata extractors.
    /// TODO: Do the MEs need to be currently registered at the system of the share?</param>
    void SetShareCategoriesAndMetadataExtractors(Guid shareId,
        IEnumerable<string> mediaCategories,
        IEnumerable<Guid> metadataExtractorIds);
  }
}
