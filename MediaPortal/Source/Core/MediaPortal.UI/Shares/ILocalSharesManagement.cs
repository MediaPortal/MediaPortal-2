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

using System;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.UI.Shares
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
  /// Shares are globally uniquely identified by their share GUID.
  /// </remarks>
  public interface ILocalSharesManagement
  {
    /// <summary>
    /// Returns all local shares. Mapping of share's GUIDs to shares.
    /// </summary>
    IDictionary<Guid, Share> Shares { get; }

    void Initialize();
    void Shutdown();

    /// <summary>
    /// If no shares are present, this method can be called to setup the default client shares. This will create
    /// the music, movies and pictures shares with the Windows default directories.
    /// </summary>
    void SetupDefaultShares();

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
    /// <param name="baseResourcePath">Description of the resource provider chain for the share's base directory.</param>
    /// <param name="shareName">Name of the new share.</param>
    /// <param name="useShareWatcher">Indicates if changes on share should be monitored by a share watcher.</param>
    /// <param name="mediaCategories">Categories of media items which are supposed to be contained in
    /// the new share. If set to <c>null</c>, the new share is a general share without attached media
    /// categories.</param>
    /// <returns>Descriptor of the new share.</returns>
    Share RegisterShare(ResourcePath baseResourcePath, string shareName, bool useShareWatcher, IEnumerable<string> mediaCategories);

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
    /// <param name="baseResourcePath">Description of the resource provider chain for the share's base directory.</param>
    /// <param name="shareName">Name of the share.</param>
    /// <param name="useShareWatcher">Indicates if changes on share should be monitored by a share watcher.</param>
    /// <param name="mediaCategories">Categories of media items which are supposed to be contained in
    /// the share. If set to <c>null</c>, the new share is a general share without attached media
    /// categories.</param>
    /// <param name="relocationMode">If set to <see cref="RelocationMode.Relocate"/>, the paths of all media items from the
    /// specified share will be adapted to the new base path. If set to <see cref="RelocationMode.ClearAndReImport"/>,
    /// all media items from the specified share will be removed from the media library or the local media items cache.</param>
    /// <returns>Changed share descriptor.</returns>
    Share UpdateShare(Guid shareId, ResourcePath baseResourcePath, string shareName, bool useShareWatcher,
        IEnumerable<string> mediaCategories, RelocationMode relocationMode);

    /// <summary>
    /// Triggers a reimport of the given share.
    /// </summary>
    /// <param name="shareId">The id of the share to be re-imported.</param>
    void ReImportShare(Guid shareId);
  }
}
