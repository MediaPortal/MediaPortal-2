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

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Exposes methods to manage local shares.
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
  public interface ILocalSharesManagement
  {
    /// <summary>
    /// Returns all local shares. Mapping of share's GUIDs to shares.
    /// </summary>
    IDictionary<Guid, Share> Shares { get; }

    /// <summary>
    /// Returns the share descriptor for the local share with the specified <paramref name="shareId"/>.
    /// </summary>
    /// <param name="shareId">Id of the share to return.</param>
    /// <returns>Descriptor of the share with the specified <paramref name="shareId"/>. If the specified
    /// share doesn't exist, the method returns <c>null</c>.</returns>
    Share GetShare(Guid shareId);

    /// <summary>
    /// Adds a local share and adds it to the media library's collection of registered shares as soon as possible.
    /// </summary>
    /// <param name="providerId">ID of the local media provider, this share should get its media items from.</param>
    /// <param name="path">Lookup path for the specified provider in the specified system.</param>
    /// <param name="shareName">Name of the new share.</param>
    /// <param name="mediaCategories">Categories of media items which are supposed to be contained in
    /// the new share. If set to <c>null</c>, the new share is a general share without attached media
    /// categories.</param>
    /// <param name="metadataExtractorIds">Ids of metadata extractors to attach to the new share.
    /// The system will automatically import the desired metadata on all of the share's media items.</param>
    /// <returns>Descriptor of the new share.</returns>
    Share RegisterShare(Guid providerId, string path, string shareName, IEnumerable<string> mediaCategories,
        IEnumerable<Guid> metadataExtractorIds);

    /// <summary>
    /// Removes the local share with the specified id. This will invalidate all references to this share; the share
    /// can no longer be accessed. The share will be removed from the media library as soon as possible.
    /// </summary>
    /// <param name="shareId">The id of the share to be removed. The share id is part of the
    /// <see cref="Share"/> which was returned by the <see cref="RegisterShare"/> method.</param>
    void RemoveShare(Guid shareId);

    /// <summary>
    /// Reconfigures the local share with the specified <paramref name="shareId"/>.
    /// This will automatically trigger a re-import of the share.
    /// </summary>
    /// <param name="shareId">Id of the share to be changed.</param>
    /// <param name="providerId">ID of the local media provider, this share should get its media items from.</param>
    /// <param name="path">Lookup path for the specified provider in the specified system.</param>
    /// <param name="shareName">Name of the share.</param>
    /// <param name="mediaCategories">Categories of media items which are supposed to be contained in
    /// the share. If set to <c>null</c>, the new share is a general share without attached media
    /// categories.</param>
    /// <param name="metadataExtractorIds">Ids of metadata extractors to be attached to the share.</param>
    /// <param name="relocateMediaItems">If set to <c>true</c>, the paths of all media items from the
    /// specified share will be adapted to the new base path.</param>
    /// <returns>Changed share descriptor.</returns>
    Share UpdateShare(Guid shareId, Guid providerId, string path, string shareName,
        IEnumerable<string> mediaCategories, IEnumerable<Guid> metadataExtractorIds, bool relocateMediaItems);
  }
}
