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
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Common.SystemCommunication
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

    /// <summary>
    /// Adds an existing share to the media librarie's collection of registered shares.
    /// </summary>
    /// <param name="share">Share to be added.</param>
    void RegisterShare(Share share);

    /// <summary>
    /// Removes the share with the specified id.
    /// </summary>
    /// <param name="shareId">Id of the share to be removed.</param>
    void RemoveShare(Guid shareId);

    /// <summary>
    /// Reconfigures the share with the specified <paramref name="shareId"/>.
    /// </summary>
    /// <remarks>
    /// The share's native system cannot be changed by this method, else we would have to consider much more security problems.
    /// </remarks>
    /// <param name="shareId">Id of the share to be changed.</param>
    /// <param name="baseResourcePath">Lookup path for the provider resource chain in the share's system.</param>
    /// <param name="shareName">Name of the share.</param>
    /// <param name="useShareWatcher">Indicates if changes on share should be monitored by a share watcher.</param>
    /// <param name="mediaCategories">Categories of media items which are supposed to be contained in
    /// the share. If set to <c>null</c>, the new share is a general share without attached media
    /// categories.</param>
    /// <param name="relocationMode">If set to <see cref="RelocationMode.Relocate"/>, the paths of all media items from the
    /// specified share will be adapted to the new base path. If set to <see cref="RelocationMode.Remove"/>,
    /// all media items from the specified share will be removed from the media library.</param>
    /// <returns>Number of relocated or removed media items.</returns>
    int UpdateShare(Guid shareId, ResourcePath baseResourcePath, string shareName, bool useShareWatcher,
        IEnumerable<string> mediaCategories, RelocationMode relocationMode);


    ICollection<Share> GetShares(string systemId, SharesFilter sharesFilter);

    /// <summary>
    /// Returns the share descriptor for the share with the specified <paramref name="shareId"/>.
    /// </summary>
    /// <param name="shareId">Id of the share to return.</param>
    /// <returns>Descriptor of the share with the specified <paramref name="shareId"/>. If the specified
    /// share doesn't exist, the method returns <c>null</c>.</returns>
    Share GetShare(Guid shareId);

    void ReImportShare(Guid guid);

    /// <summary>
    /// Tries to create default shares for the local system. Typically, this are the shares for the system's
    /// MyMusic, MyVideos and MyPictures directories.
    /// </summary>
    void SetupDefaultServerShares();

    #endregion

    #region Media item aspect storage management

    void AddMediaItemAspectStorage(MediaItemAspectMetadata miam);

    void RemoveMediaItemAspectStorage(Guid aspectId);

    IDictionary<Guid, DateTime> GetAllManagedMediaItemAspectCreationDates();

    ICollection<Guid> GetAllManagedMediaItemAspectTypes();

    MediaItemAspectMetadata GetMediaItemAspectMetadata(Guid miamId);

    #endregion

    #region Media query

    /// <summary>
    /// Loads the media item at the given <paramref name="systemId"/> and <paramref name="path"/>.
    /// </summary>
    /// <param name="systemId">System id of the item to load.</param>
    /// <param name="path">Native resource path of the item to load.</param>
    /// <param name="necessaryMIATypes">IDs of media item aspect types which need to be present in the result.
    /// If the media item at the given location doesn't contain one of those media item aspects, it won't be returned.</param>
    /// <param name="optionalMIATypes">IDs of media item aspect types which will be returned if present.</param>
    /// <param name="userProfile">User profile to load any user specific media item data for.</param>
    /// <returns></returns>
    MediaItem LoadItem(string systemId, ResourcePath path,
        IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes, Guid? userProfile);

    /// <summary>
    /// Lists all media items with the given parent directory.
    /// </summary>
    /// <param name="parentDirectoryId">Media item id of the parent directory item to browse.</param>
    /// <param name="necessaryMIATypes">IDs of media item aspect types which need to be present in the result.
    /// If a media item at the given location doesn't contain one of those media item aspects, it won't be returned.</param>
    /// <param name="optionalMIATypes">IDs of media item aspect types which will be returned if present.</param>
    /// <param name="userProfile">User profile to load any user specific media item data for.</param>
    /// <param name="includeVirtual">Specifies if virtual media items should be included.</param>
    /// <param name="offset">Number of items to skip when retrieving MediaItems.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <returns>Result collection of media items at the given location.</returns>
    IList<MediaItem> Browse(Guid parentDirectoryId,
        IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes,
        Guid? userProfile, bool includeVirtual, uint? offset = null, uint? limit = null);

    /// <summary>
    /// Starts a search for media items.
    /// </summary>
    /// <param name="query">Query object which specifies the search parameters.</param>
    /// <param name="onlyOnline">If this parameter is set to <c>true</c>, only media items which are hosted by systems which
    /// are currently online are returned.</param>
    /// <param name="userProfile">User profile to load any user specific media item data for.</param>
    /// <param name="includeVirtual">Specifies if virtual media items should be included.</param>
    /// <param name="offset">Number of items to skip when retrieving MediaItems.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <returns>List of matching media items with the media item aspects of the given
    /// <see cref="MediaItemQuery.NecessaryRequestedMIATypeIDs"/> and <see cref="MediaItemQuery.OptionalRequestedMIATypeIDs"/>,
    /// in the given sorting given by <see cref="MediaItemQuery.SortInformation"/>.</returns>
    IList<MediaItem> Search(MediaItemQuery query, bool onlyOnline, Guid? userProfile, bool includeVirtual, uint? offset = null, uint? limit = null);

    /// <summary>
    /// Starts a search for media items which searches items by a given <paramref name="searchText"/> and which is constrained
    /// to the given <paramref name="necessaryMIATypes"/> and the given <paramref name="filter"/>.
    /// </summary>
    /// <param name="searchText">The text which should be searched in each available column.</param>
    /// <param name="necessaryMIATypes">MIA types which must be present in the result media items.</param>
    /// <param name="optionalMIATypes">Optional MIA types which will be returned if present.</param>
    /// <param name="filter">Additional filter constraint for the query.</param>
    /// <param name="excludeCLOBs">If set to <c>false</c>, the query also searches in CLOB fields.</param>
    /// <param name="onlyOnline">If this parameter is set to <c>true</c>, only media items which are hosted by systems which
    /// are currently online are returned.</param>
    /// <param name="caseSensitive">If set to <c>true</c>, the query is done case sensitive, else it is done case
    /// insensitive.</param>
    /// <param name="userProfile">User profile to load any user specific media item data for.</param>
    /// <param name="includeVirtual">Specifies if virtual media items should be included.</param>
    /// <param name="offset">Number of items to skip when retrieving MediaItems.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <returns>List of matching media items.</returns>
    IList<MediaItem> SimpleTextSearch(string searchText, IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes,
        IFilter filter, bool excludeCLOBs, bool onlyOnline, bool caseSensitive,
      Guid? userProfile, bool includeVirtual, uint? offset = null, uint? limit = null);

    /// <summary>
    /// Returns a map of existing attribute values mapped to their occurence count for the given
    /// <paramref name="attributeType"/> for the media items specified by the <paramref name="filter"/>.
    /// </summary>
    /// <param name="attributeType">Attribute type, whose values will be returned.</param>
    /// <param name="selectAttributeFilter">Filter which is defined on the given <paramref name="attributeType"/> to restrict the
    /// result values.</param>
    /// <param name="projectionFunction">Function used to build the group name from the values of the given
    /// <paramref name="attributeType"/>.</param>
    /// <param name="necessaryMIATypes">IDs of media item aspect types, which need to be present in each media item
    /// whose attribute values are part of the result collection.</param>
    /// <param name="filter">Filter specifying the media items whose attribute values will be returned.</param>
    /// <param name="onlyOnline">If this parameter is set to <c>true</c>, only value groups are returned with items hosted by
    /// systems which are currently online.</param>
    /// <param name="includeVirtual">Specifies if virtual media items should be included.</param>
    /// <returns>Mapping set of existing attribute values to their occurence count for the given
    /// <paramref name="attributeType"/> (long).</returns>
    HomogenousMap GetValueGroups(MediaItemAspectMetadata.AttributeSpecification attributeType, IFilter selectAttributeFilter,
        ProjectionFunction projectionFunction, IEnumerable<Guid> necessaryMIATypes, IFilter filter, bool onlyOnline, bool includeVirtual);

    /// <summary>
    /// Returns a map of existing attribute values mapped to their occurence count for the given
    /// <paramref name="attributeType"/> for the media items specified by the <paramref name="filter"/>.
    /// </summary>
    /// <param name="keyAttributeType">Attribute type, whose keys will be returned.</param>
    /// <param name="valueAttributeType">Attribute type, whose values will be returned.</param>
    /// <param name="selectAttributeFilter">Filter which is defined on the given <paramref name="attributeType"/> to restrict the
    /// result values.</param>
    /// <param name="projectionFunction">Function used to build the group name from the values of the given
    /// <paramref name="keyAttributeType"/>.</param>
    /// <param name="necessaryMIATypes">IDs of media item aspect types, which need to be present in each media item
    /// whose attribute values are part of the result collection.</param>
    /// <param name="filter">Filter specifying the media items whose attribute values will be returned.</param>
    /// <param name="onlyOnline">If this parameter is set to <c>true</c>, only value groups are returned with items hosted by
    /// systems which are currently online.</param>
    /// <param name="includeVirtual">Specifies if virtual media items should be included.</param>
    /// <returns>Mapping set of existing attribute values to their occurence count for the given
    /// <paramref name="valueAttributeType"/> (long) in Item1 and values to their keys
    /// for the given <paramref name="valueAttributeType"/> in Item2.</returns>
    Tuple<HomogenousMap, HomogenousMap> GetKeyValueGroups(MediaItemAspectMetadata.AttributeSpecification keyAttributeType, MediaItemAspectMetadata.AttributeSpecification valueAttributeType, 
      IFilter selectAttributeFilter, ProjectionFunction projectionFunction, IEnumerable<Guid> necessaryMIATypes, IFilter filter, bool onlyOnline, bool includeVirtual);

    /// <summary>
    /// Executes <see cref="GetValueGroups"/> and groups the resulting values by the given <paramref name="groupingFunction"/>.
    /// </summary>
    /// <param name="attributeType">Attribute type, whose values will be returned. See method <see cref="GetValueGroups"/>.</param>
    /// <param name="selectAttributeFilter">Filter which is defined on the given <paramref name="attributeType"/> to restrict the
    /// result value groups.</param>
    /// <param name="projectionFunction">Function used to build the group name from the values of the given
    /// <paramref name="attributeType"/>.</param>
    /// <param name="necessaryMIATypes">Necessary media item types. See method <see cref="GetValueGroups"/>.</param>
    /// <param name="filter">Filter specifying the base media items for the query. See method <see cref="GetValueGroups"/>.</param>
    /// <param name="onlyOnline">If this parameter is set to <c>true</c>, only value groups are returned with items hosted by
    /// systems which are currently online.</param>
    /// <param name="groupingFunction">Determines, how result values are grouped.</param>
    /// <param name="includeVirtual">Specifies if virtual media items should be included.</param>
    /// <returns>List of value groups for the given query.</returns>
    IList<MLQueryResultGroup> GroupValueGroups(MediaItemAspectMetadata.AttributeSpecification attributeType,
        IFilter selectAttributeFilter, ProjectionFunction projectionFunction, IEnumerable<Guid> necessaryMIATypes,
        IFilter filter, bool onlyOnline, GroupingFunction groupingFunction, bool includeVirtual);

    /// <summary>
    /// Counts the count of media items matching the given criteria.
    /// </summary>
    /// <param name="necessaryMIATypes">IDs of media item aspect types, which need to be present in each counted media item. Only
    /// media items with those media item aspect types are counted.</param>
    /// <param name="filter">Filter specifying the media items which will be counted.</param>
    /// <param name="onlyOnline">If this parameter is set to <c>true</c>, only items hosted by systems which are currently online
    /// are counted.</param>
    /// <param name="includeVirtual">Specifies if virtual media items should be included.</param>
    /// <returns>Number of matching media items.</returns>
    int CountMediaItems(IEnumerable<Guid> necessaryMIATypes, IFilter filter, bool onlyOnline, bool includeVirtual);

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
    /// Loads a client-side playlist.
    /// </summary>
    /// <param name="mediaItemIds">Ids of the media items to load.</param>
    /// <param name="necessaryMIATypes">Ids of media item aspects which need to be present for a media item to be returned.</param>
    /// <param name="optionalMIATypes">Ids of media item aspects which will be loaded for each returned media item, if present.</param>
    /// <param name="offset">Number of items to skip when retrieving MediaItems.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <returns>List of media items matching the given <paramref name="mediaItemIds"/> and <paramref name="necessaryMIATypes"/>.</returns>
    IList<MediaItem> LoadCustomPlaylist(IList<Guid> mediaItemIds,
        ICollection<Guid> necessaryMIATypes, ICollection<Guid> optionalMIATypes, uint? offset = null, uint? limit = null);

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

    /// <summary>
    /// Notifies the content directory about a start of a share import which is done by an MP2 client.
    /// </summary>
    /// <param name="shareId">Id of the share which is being imported.</param>
    void ClientStartedShareImport(Guid shareId);

    /// <summary>
    /// Notifies the content directory about the completion of a share import which was done by an MP2 client.
    /// </summary>
    /// <param name="shareId">Id of the share which has been imported.</param>
    void ClientCompletedShareImport(Guid shareId);

    /// <summary>
    /// Returns all shares which are marked as currently being imported.
    /// </summary>
    /// <returns>Collection of share ids.</returns>
    ICollection<Guid> GetCurrentlyImportingShares();

    #endregion

    #region Playback

    void NotifyPlayback(Guid mediaItemId, bool watched);

    #endregion
  }
}
