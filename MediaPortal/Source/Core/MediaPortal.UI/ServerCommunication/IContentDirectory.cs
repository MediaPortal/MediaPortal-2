#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.UI.ServerCommunication
{
  public enum SharesFilter
  {
    All,
    ConnectedShares
  }

  // Corresponds to MediaPortal.Backend.MediaLibrary.ProjectionFunction
  public enum ProjectionFunction
  {
    None,
    DateToYear,
  }

  // Corresponds to MediaPortal.Backend.MediaLibrary.GroupingFunction
  public enum  GroupingFunction
  {
    FirstCharacter
  }

  /// <summary>
  /// Interface of the MediaPortal 2 server's ContentDirectory service. This interface is implemented by the
  /// MediaPortal 2 server.
  /// </summary>
  /// <remarks>
  /// At the client side, the messaging class <see cref="ContentDirectoryMessaging"/> represents the messaging part of
  /// the content directory (state variables).
  /// </remarks>
  public interface IContentDirectory
  {
    #region Shares management

    void RegisterShare(Share share);
    void RemoveShare(Guid shareId);
    int UpdateShare(Guid shareId, ResourcePath baseResourcePath, string shareName,
        IEnumerable<string> mediaCategories, RelocationMode relocationMode);
    ICollection<Share> GetShares(string systemId, SharesFilter sharesFilter);
    Share GetShare(Guid shareId);
    void ReImportShare(Guid guid);
    void SetupDefaultServerShares();

    #endregion

    #region Media item aspect storage management

    void AddMediaItemAspectStorage(MediaItemAspectMetadata miam);
    void RemoveMediaItemAspectStorage(Guid aspectId);
    ICollection<Guid> GetAllManagedMediaItemAspectTypes();
    MediaItemAspectMetadata GetMediaItemAspectMetadata(Guid miamId);

    #endregion

    #region Media query

    MediaItem LoadItem(string systemId, ResourcePath path,
        IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes);
    ICollection<MediaItem> Browse(Guid parentDirectoryId,
        IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes);
    IList<MediaItem> Search(MediaItemQuery query, bool onlyOnline);
    IList<MediaItem> SimpleTextSearch(string searchText, IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes,
        IFilter filter, bool excludeCLOBs, bool onlyOnline, bool caseSensitive);
    HomogenousMap GetValueGroups(MediaItemAspectMetadata.AttributeSpecification attributeType, IFilter selectAttributeFilter,
        ProjectionFunction projectionFunction, IEnumerable<Guid> necessaryMIATypes, IFilter filter, bool onlyOnline);
    IList<MLQueryResultGroup> GroupValueGroups(MediaItemAspectMetadata.AttributeSpecification attributeType,
        IFilter selectAttributeFilter, ProjectionFunction projectionFunction, IEnumerable<Guid> necessaryMIATypes,
        IFilter filter, bool onlyOnline, GroupingFunction groupingFunction);
    int CountMediaItems(IEnumerable<Guid> necessaryMIATypes, IFilter filter, bool onlyOnline);

    #endregion

    #region Playlist management

    /// <summary>
    /// Returns the ids and names of all playlists that are stored at the server.
    /// </summary>
    /// <returns>Collection of playlist data.</returns>
    ICollection<PlaylistInformationData> GetPlaylists();

    /// <summary>
    /// Saves a playlist at the server.
    /// </summary>
    /// <param name="playlistData">Raw data of the playlist to store.</param>
    void SavePlaylist(PlaylistRawData playlistData);

    /// <summary>
    /// Deletes a playlist at the server.
    /// </summary>
    /// <param name="playlistId">Id of the playlist to delete.</param>
    /// <returns><c>true</c>, if the playlist could successfully be deleted, else <c>false</c>.</returns>
    bool DeletePlaylist(Guid playlistId);

    /// <summary>
    /// Loads the raw data of a server-side playlist.
    /// </summary>
    /// <param name="playlistId">Id of the playlist to load.</param>
    /// <returns>Raw playlist data of the requested playlist or <c>null</c>, if there is no playlist with the
    /// given <paramref name="playlistId"/>.</returns>
    PlaylistRawData ExportPlaylist(Guid playlistId);

    /// <summary>
    /// Loads a server-side playlist.
    /// </summary>
    /// <param name="playlistId">Id of the playlist to load.</param>
    /// <param name="necessaryMIATypes">Ids of media item aspects which need to be present for a media item to be returned.</param>
    /// <param name="optionalMIATypes">Ids of media item aspects which will be loaded for each returned media item, if present.</param>
    /// <returns>PlaylistContents instance with the data of the given playlist and media items matching the given
    /// <paramref name="necessaryMIATypes"/>.</returns>
    PlaylistContents LoadServerPlaylist(Guid playlistId,
        ICollection<Guid> necessaryMIATypes, ICollection<Guid> optionalMIATypes);

    /// <summary>
    /// Loads a client-side playlist.
    /// </summary>
    /// <param name="mediaItemIds">Ids of the media items to load.</param>
    /// <param name="necessaryMIATypes">Ids of media item aspects which need to be present for a media item to be returned.</param>
    /// <param name="optionalMIATypes">Ids of media item aspects which will be loaded for each returned media item, if present.</param>
    /// <returns>List of media items matching the given <paramref name="mediaItemIds"/> and <paramref name="necessaryMIATypes"/>.</returns>
    IList<MediaItem> LoadCustomPlaylist(IList<Guid> mediaItemIds,
        ICollection<Guid> necessaryMIATypes, ICollection<Guid> optionalMIATypes);

    #endregion

    #region Media import

    // TODO: We don't have a way to remove media item aspects from media items yet. This function has to be added.

    /// <summary>
    /// Adds the media item with the given path or updates it if it already exists.
    /// </summary>
    /// <param name="parentDirectoryId">Id of the parent directory media item.</param>
    /// <param name="systemId">Id of the system where the given <paramref name="path"/> is located.</param>
    /// <param name="path">Path of the media item to be added or updated.</param>
    /// <param name="mediaItemAspects">Enumeration of media item aspects to be assigned to the media item.</param>
    /// <returns>Id of the added or updated media item.</returns>
    Guid AddOrUpdateMediaItem(Guid parentDirectoryId, string systemId, ResourcePath path,
        IEnumerable<MediaItemAspect> mediaItemAspects);

    /// <summary>
    /// Deletes all media items in the content directory whose resource path starts with the given <paramref name="path"/>.
    /// If <paramref name="inclusive"/> is set to <c>true</c>, the media item with exactly the given path will be deleted too.
    /// The complicated contract is necessary because we also have media items for directories which are tracked too.
    /// </summary>
    /// <param name="systemId">Id of the system where the given media item <paramref name="path"/> is located.</param>
    /// <param name="path">Path to be deleted. All items whose path starts with this path are deleted.</param>
    /// <param name="inclusive">If set to <c>true</c>, the media item with the given <paramref name="path"/> is deleted too, else,
    /// only the children of the <paramref name="path"/> are deleted.</param>
    void DeleteMediaItemOrPath(string systemId, ResourcePath path, bool inclusive);

    #endregion
  }
}
