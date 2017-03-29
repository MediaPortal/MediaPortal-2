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

namespace MediaPortal.Backend.MediaLibrary
{
  public enum RelocationMode
  {
    None,
    Relocate,
    Remove
  }

  public enum ProjectionFunction
  {
    /// <summary>
    /// No projection; the attribute is returned without modification/projection.
    /// </summary>
    None,

    /// <summary>
    /// Takes the year from a date.
    /// </summary>
    DateToYear,
  }

  public enum GroupingFunction
  {
    /// <summary>
    /// Values are grouped together by their first letter.
    /// </summary>
    FirstCharacter,
  }

  /// <summary>
  /// The media library is a "dumb" data store. It provides search/update methods for all kinds of content stored.
  /// All media-related management functions (re-imports triggered by a special situation etc.) are handled by the
  /// media management subsystem.
  /// </summary>
  /// <remarks>
  /// All system IDs, which are used for defining share or media item locations, are the device UUIDs of the system's
  /// UPnP servers.
  /// </remarks>
  public interface IMediaLibrary
  {
    #region Startup & Shutdown

    void Startup();
    void ActivateImporterWorker();
    void Shutdown();

    #endregion

    #region Media query

    /// <summary>
    /// Builds a media item query which searches items by a given <paramref name="searchText"/> and which is constrained
    /// to the given <paramref name="necessaryMIATypes"/> and the given <paramref name="filter"/>.
    /// </summary>
    /// <param name="searchText">The text which should be searched in each available column.</param>
    /// <param name="necessaryMIATypes">MIA types which must be present in the result media items.</param>
    /// <param name="optionalMIATypes">Optional MIA types which will be returned if present.</param>
    /// <param name="filter">Additional filter constraint for the query.</param>
    /// <param name="includeCLOBs">If set to <c>true</c>, the query also searches in CLOB fields.</param>
    /// <param name="caseSensitive">If set to <c>true</c>, the query is done case sensitive, else it is done case
    /// insensitive.</param>
    /// <returns>Media item query build from the given parameters.</returns>
    MediaItemQuery BuildSimpleTextSearchQuery(string searchText, IEnumerable<Guid> necessaryMIATypes,
        IEnumerable<Guid> optionalMIATypes, IFilter filter, bool includeCLOBs, bool caseSensitive);

    /// <summary>
    /// Loads the media item at the given <paramref name="systemId"/> and <paramref name="path"/>.
    /// </summary>
    /// <param name="systemId">System id of the item to load.</param>
    /// <param name="path">Native resource path of the item to load.</param>
    /// <param name="necessaryRequestedMIATypeIDs">IDs of media item aspect types which need to be present in the result.
    /// If the media item at the given location doesn't contain one of those media item aspects, it won't be returned.</param>
    /// <param name="optionalRequestedMIATypeIDs">IDs of media item aspect types which will be returned if present.</param>
    /// <param name="userProfile">User profile to load any user specific media item data for.</param>
    /// <returns></returns>
    MediaItem LoadItem(string systemId, ResourcePath path,
        IEnumerable<Guid> necessaryRequestedMIATypeIDs, IEnumerable<Guid> optionalRequestedMIATypeIDs,
        Guid? userProfile = null);

    /// <summary>
    /// Lists all media items with the given parent directory.
    /// </summary>
    /// <param name="parentDirectoryId">Media item id of the parent directory item to browse.</param>
    /// <param name="necessaryRequestedMIATypeIDs">IDs of media item aspect types which need to be present in the result.
    /// If a media item at the given location doesn't contain one of those media item aspects, it won't be returned.</param>
    /// <param name="optionalRequestedMIATypeIDs">IDs of media item aspect types which will be returned if present.</param>
    /// <param name="offset">Number of items to skip when retrieving MediaItems.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <param name="userProfile">User profile to load any user specific media item data for.</param>
    /// <param name="includeVirtual">Specifies if virtual media items should be included.</param>
    /// <returns>Result collection of media items at the given location.</returns>
    IList<MediaItem> Browse(Guid parentDirectoryId,
      IEnumerable<Guid> necessaryRequestedMIATypeIDs, IEnumerable<Guid> optionalRequestedMIATypeIDs,
      Guid? userProfile, bool includeVirtual, uint ? offset = null, uint? limit = null);

    /// <summary>
    /// Starts a search for media items.
    /// </summary>
    /// <param name="query">Query object which specifies the search parameters.</param>
    /// <param name="filterOnlyOnline">If this parameter is set to <c>true</c>, only media items which are hosted by systems which
    /// are currently online are returned.</param>
    /// <param name="userProfile">User profile to load any user specific media item data for.</param>
    /// <param name="includeVirtual">Specifies if virtual media items should be included.</param>
    /// <returns>List of matching media items with the media item aspects of the given
    /// <see cref="MediaItemQuery.NecessaryRequestedMIATypeIDs"/> and <see cref="MediaItemQuery.OptionalRequestedMIATypeIDs"/>,
    /// in the given sorting given by <see cref="MediaItemQuery.SortInformation"/>.</returns>
    IList<MediaItem> Search(MediaItemQuery query, bool filterOnlyOnline, Guid? userProfile, bool includeVirtual);

    /// <summary>
    /// Returns a map of existing attribute values mapped to their occurence count for the given
    /// <paramref name="attributeType"/> for the media items specified by the <paramref name="filter"/>.
    /// </summary>
    /// <param name="attributeType">Attribute type, whose values will be returned.</param>
    /// <param name="selectAttributeFilter">Filter which is defined on the given <paramref name="attributeType"/> to restrict the
    /// result values.</param>
    /// <param name="projectionFunction">Function used to build the group name from the values of the given
    /// <paramref name="attributeType"/>.</param>
    /// <param name="necessaryMIATypeIDs">IDs of media item aspect types, which need to be present in each media item
    /// whose attribute values are part of the result collection.</param>
    /// <param name="filter">Filter specifying the media items whose attribute values will be returned.</param>
    /// <param name="filterOnlyOnline">If this parameter is set to <c>true</c>, only value groups are returned with items hosted by
    /// systems which are currently online.</param>
    /// <param name="includeVirtual">Specifies if virtual media items should be included.</param>
    /// <returns>Mapping set of existing attribute values to their occurence count for the given
    /// <paramref name="attributeType"/> (long).</returns>
    HomogenousMap GetValueGroups(MediaItemAspectMetadata.AttributeSpecification attributeType, IFilter selectAttributeFilter,
        ProjectionFunction projectionFunction, IEnumerable<Guid> necessaryMIATypeIDs, IFilter filter, bool filterOnlyOnline, bool includeVirtual);

    /// <summary>
    /// Returns a map of existing attribute values mapped to their occurence count for the given <paramref name="keyAttributeType"/> and
    /// <paramref name="valueAttributeType"/> for the media items specified by the <paramref name="filter"/>.
    /// </summary>
    /// <param name="keyAttributeType">Key attribute type, whose values will be returned as the key.</param>
    /// <param name="valueAttributeType">Value attribute type, whose values will be returned as the value.</param>
    /// <param name="selectAttributeFilter">Filter which is defined on the given <paramref name="valueAttributeType"/> to restrict the
    /// result values.</param>
    /// <param name="projectionFunction">Function used to build the group name from the values of the given
    /// <paramref name="valueAttributeType"/>.</param>
    /// <param name="necessaryMIATypeIDs">IDs of media item aspect types, which need to be present in each media item
    /// whose attribute values are part of the result collection.</param>
    /// <param name="filter">Filter specifying the media items whose attribute values will be returned.</param>
    /// <param name="filterOnlyOnline">If this parameter is set to <c>true</c>, only value groups are returned with items hosted by
    /// systems which are currently online.</param>
    /// <param name="includeVirtual">Specifies if virtual media items should be included.</param>
    /// <returns>Mapping set of existing attribute values to their occurence count for the given
    /// <paramref name="valueAttributeType"/> (long) in Item1 and values to their keys
    /// for the given <paramref name="valueAttributeType"/> in Item2.</returns>
    Tuple<HomogenousMap, HomogenousMap> GetKeyValueGroups(MediaItemAspectMetadata.AttributeSpecification keyAttributeType, MediaItemAspectMetadata.AttributeSpecification valueAttributeType, 
      IFilter selectAttributeFilter, ProjectionFunction projectionFunction, IEnumerable<Guid> necessaryMIATypeIDs, IFilter filter, bool filterOnlyOnline, bool includeVirtual);

    /// <summary>
    /// Executes <see cref="GetValueGroups"/> and groups the resulting values by the given <paramref name="groupingFunction"/>.
    /// </summary>
    /// <param name="attributeType">Attribute type, whose values will be returned. See method <see cref="GetValueGroups"/>.</param>
    /// <param name="selectAttributeFilter">Filter which is defined on the given <paramref name="attributeType"/> to restrict the
    /// result value groups.</param>
    /// <param name="projectionFunction">Function used to build the group name from the values of the given
    /// <paramref name="attributeType"/>.</param>
    /// <param name="necessaryMIATypeIDs">Necessary media item types. See method <see cref="GetValueGroups"/>.</param>
    /// <param name="filter">Filter specifying the base media items for the query. See method <see cref="GetValueGroups"/>.</param>
    /// <param name="filterOnlyOnline">If this parameter is set to <c>true</c>, only value groups are returned with items hosted by
    /// systems which are currently online.</param>
    /// <param name="groupingFunction">Determines, how result values are grouped.</param>
    /// <param name="includeVirtual">Specifies if virtual media items should be included.</param>
    /// <returns>List of value groups for the given query.</returns>
    IList<MLQueryResultGroup> GroupValueGroups(MediaItemAspectMetadata.AttributeSpecification attributeType,
        IFilter selectAttributeFilter, ProjectionFunction projectionFunction, IEnumerable<Guid> necessaryMIATypeIDs,
        IFilter filter, bool filterOnlyOnline, GroupingFunction groupingFunction, bool includeVirtual);

    /// <summary>
    /// Counts the count of media items matching the given criteria.
    /// </summary>
    /// <param name="necessaryMIATypeIDs">IDs of media item aspect types, which need to be present in each counted media item. Only
    /// media items with those media item aspect types are counted.</param>
    /// <param name="filter">Filter specifying the media items which will be counted.</param>
    /// <param name="filterOnlyOnline">If this parameter is set to <c>true</c>, only items hosted by systems which are currently online
    /// are counted.</param>
    /// <param name="includeVirtual">Specifies if virtual media items should be included.</param>
    /// <returns>Number of matching media items.</returns>
    int CountMediaItems(IEnumerable<Guid> necessaryMIATypeIDs, IFilter filter, bool filterOnlyOnline, bool includeVirtual);

    #endregion

    #region Playlist management

    /// <summary>
    /// Gets all playlists which are stored in the media library.
    /// </summary>
    /// <returns>Collection of playlist data.</returns>
    ICollection<PlaylistInformationData> GetPlaylists();

    /// <summary>
    /// Saves the given <paramref name="playlistData"/>.
    /// </summary>
    /// <param name="playlistData">Playlist to save.</param>
    void SavePlaylist(PlaylistRawData playlistData);

    /// <summary>
    /// Deletes the playlist with the given <paramref name="playlistId"/>, if present.
    /// </summary>
    /// <param name="playlistId">Id of the playlist to delete.</param>
    /// <returns><c>true</c>, if a playlist with the given <paramref name="playlistId"/> was found and could be deleted,
    /// else <c>false</c>.</returns>
    bool DeletePlaylist(Guid playlistId);

    /// <summary>
    /// Exports the raw data of the playlist with the given <paramref name="playlistId"/>.
    /// </summary>
    /// <param name="playlistId">Id of the playlist to export.</param>
    /// <returns>Exported playlist data or <c>null</c>, if the playlist is not present.</returns>
    PlaylistRawData ExportPlaylist(Guid playlistId);

    /// <summary>
    /// Loads the playlist contents for the given <paramref name="mediaItemIds"/>.
    /// </summary>
    /// <param name="mediaItemIds">Ids of the media items whose data should be loaded.</param>
    /// <param name="necessaryMIATypes">Media item aspect types which must be present in all media items.
    /// If media items don't have those media item aspects attached, they won't be returned even if they are present in
    /// the <paramref name="mediaItemIds"/> collection.</param>
    /// <param name="optionalMIATypes">Media item aspect types which will additionally be loaded, if present.</param>
    /// <param name="offset">Number of items to skip when retrieving MediaItems.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <returns>List of media items.</returns>
    IList<MediaItem> LoadCustomPlaylist(IList<Guid> mediaItemIds,
        IEnumerable<Guid> necessaryMIATypes, IEnumerable<Guid> optionalMIATypes, uint? offset = null, uint? limit = null);

    #endregion

    #region Media import

    /// <summary>
    /// Adds or updates the media item specified by its location (<paramref name="systemId"/> and <paramref name="path"/>).
    /// This method will typically be used by a media item importer.
    /// </summary>
    /// <param name="parentDirectoryId">Id of the parent directory's media item or <see cref="Guid.Empty"/>, if the
    /// parent directory is not present in the media library.</param>
    /// <param name="systemId">The Id of the system where the media item to be updated is located.</param>
    /// <param name="path">The path at the given system of the media item to be updated.</param>
    /// <param name="mediaItemAspects">Media item aspects to be updated.</param>
    /// <param name="isRefresh">Is the media item being added/updated because of a refresh cycle.</param>
    /// <returns>Id of the media item which has been added or updated.</returns>
    Guid AddOrUpdateMediaItem(Guid parentDirectoryId, string systemId, ResourcePath path, IEnumerable<MediaItemAspect> mediaItemAspects, bool isRefresh);

    /// <summary>
    /// Writes some media item aspects of an existing media item to the media library.
    /// </summary>
    /// <param name="mediaItemId">Id of the media item to be updated.</param>
    /// <param name="mediaItemAspects">Media item aspects to be written.</param>
    /// <param name="isRefresh">Is the media item being added/updated because of a refresh cycle.</param>
    void UpdateMediaItem(Guid mediaItemId, IEnumerable<MediaItemAspect> mediaItemAspects, bool isRefresh);

    /// <summary>
    /// Deletes all media items and directories from the media library which are located at the client with the given
    /// <paramref name="systemId"/> and the specified <paramref name="path"/>.
    /// </summary>
    /// <param name="systemId">ID of the system whose media item or directory should be deleted.</param>
    /// <param name="path">The path of the media item or directory at the system of the given client to be deleted.
    /// The path can be the full path of a media item or just the first part of the path in case of a directory.
    /// If this parameter is set to <c>null</c>, all media items of the given client will be deleted.</param>
    /// <param name="inclusive">If set to <c>true</c>, the media item with the given <paramref name="path"/> will
    /// also be deleted. If set to <c>false</c>, only sub directories will be deleted. This parameter is only used
    /// if <paramref name="path"/> is not <c>null</c>.</param>
    void DeleteMediaItemOrPath(string systemId, ResourcePath path, bool inclusive);

    /// <summary>
    /// Notifies the media library about a start of a share import which is done by an MP2 client.
    /// </summary>
    /// <param name="shareId">Id of the share which is being imported.</param>
    void ClientStartedShareImport(Guid shareId);

    /// <summary>
    /// Notifies the media library about the completion of a share import which was done by an MP2 client.
    /// </summary>
    /// <param name="shareId">Id of the share which has been imported.</param>
    void ClientCompletedShareImport(Guid shareId);

    /// <summary>
    /// Gets the ids of all shares which are marked as currently being imported.
    /// </summary>
    /// <returns>Collection of share ids.</returns>
    ICollection<Guid> GetCurrentlyImportingShareIds();

    #endregion

    #region Playback

    void NotifyPlayback(Guid mediaItemId, bool watched);

    #endregion

    #region User data management

    void UserDataUpdated(Guid userProfileId, Guid mediaItemId, string userDataKey, string userData);

    #endregion

    #region Media item aspect schema management

    bool MediaItemAspectStorageExists(Guid aspectId);

    MediaItemAspectMetadata GetMediaItemAspectMetadata(Guid aspectId);

    void AddMediaItemAspectStorage(MediaItemAspectMetadata miam);

    void RemoveMediaItemAspectStorage(Guid aspectId);

    IDictionary<Guid, MediaItemAspectMetadata> GetManagedMediaItemAspectMetadata();

    IDictionary<Guid, DateTime> GetManagedMediaItemAspectCreationDates();

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
    /// <param name="systemId">ID of the system where the media provider for the new share is located.</param>
    /// <param name="baseResourcePath">Lookup path for the provider resource chain in the specified system.</param>
    /// <param name="shareName">Name of the new share.</param>
    /// <param name="useShareWatcher">Indicates if changes on share should be monitored by a share watcher.</param>
    /// <param name="mediaCategories">Categories of media items which are supposed to be contained in
    /// the new share. If set to <c>null</c>, the new share is a general share without attached media
    /// categories.</param>
    /// <returns>ID of the new share.</returns>
    Guid CreateShare(string systemId, ResourcePath baseResourcePath,
        string shareName, bool useShareWatcher, IEnumerable<string> mediaCategories);

    /// <summary>
    /// Removes the share with the specified id.
    /// </summary>
    /// <param name="shareId">Id of the share to be removed.</param>
    void RemoveShare(Guid shareId);

    /// <summary>
    /// Removes all shares with the specified native <paramref name="systemId"/>.
    /// </summary>
    void RemoveSharesOfSystem(string systemId);

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

    /// <summary>
    /// Returns all shares which are registered in the MediaPortal server's media library.
    /// </summary>
    /// <param name="systemId">Filters the returned shares by system. If <c>null</c>, the returned set isn't filtered
    /// by system.</param>
    /// <returns>Mapping of share's GUIDs to shares.</returns>
    IDictionary<Guid, Share> GetShares(string systemId);

    /// <summary>
    /// Returns the share descriptor for the share with the specified <paramref name="shareId"/>.
    /// </summary>
    /// <param name="shareId">Id of the share to return.</param>
    /// <returns>Descriptor of the share with the specified <paramref name="shareId"/>. If the specified
    /// share doesn't exist, the method returns <c>null</c>.</returns>
    Share GetShare(Guid shareId);

    /// <summary>
    /// Tries to create default shares for the local system. Typically, this are the shares for the system's
    /// MyMusic, MyVideos and MyPictures directories.
    /// </summary>
    void SetupDefaultLocalShares();

    #endregion

    #region Client online registration

    /// <summary>
    /// Returns a dictionary of clients which are currently online.
    /// </summary>
    /// <value>Dictionary with (system id; system name) mappings of all clients which are known to be online.</value>
    IDictionary<string, SystemName> OnlineClients { get; }

    /// <summary>
    /// Notifies the media library that the client with the specified <paramref name="systemId"/> appeared online at the
    /// given <paramref name="currentSystemName"/>. This has to be done to make the media librarie's online filters work.
    /// </summary>
    /// <param name="systemId">System id of the client which appeared online.</param>
    /// <param name="currentSystemName">System name of the client.</param>
    void NotifySystemOnline(string systemId, SystemName currentSystemName);

    /// <summary>
    /// Notifies the media library that the client with the specified <paramref name="systemId"/> switched offline.
    /// This has to be done to make the media librarie's online filters work.
    /// </summary>
    /// <param name="systemId">System id of the client which switched offline.</param>
    void NotifySystemOffline(string systemId);

    #endregion
  }
}
