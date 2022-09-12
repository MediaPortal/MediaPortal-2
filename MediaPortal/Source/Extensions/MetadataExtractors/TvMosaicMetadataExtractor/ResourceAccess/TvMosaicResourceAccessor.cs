#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using MediaPortal.Common.ResourceAccess;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TvMosaic.API;
using TvMosaic.Shared;

namespace TvMosaicMetadataExtractor.ResourceAccess
{
  /// <summary>
  /// <see cref="IFileSystemResourceAccessor"/> that can navigate the TvMosaic object API from the root to get containers and their child items.
  /// </summary>
  public class TvMosaicResourceAccessor : IFileSystemResourceAccessor, INetworkResourceAccessor, IResourceDeletor
  {
    protected readonly SemaphoreSlim _syncObj = new SemaphoreSlim(1);

    protected ITvMosaicNavigator _navigator;
    protected TvMosaicResourceProvider _parent;
    protected string _path;
    protected string _objectId;

    /// <summary>
    /// For "files" this will hold the underlying RecordedTV object, this should not be referenced directly,
    /// instead GetItem() should be called to ensure that the item has been loaded.
    /// </summary>
    protected RecordedTV _underlyingItem;
    protected volatile bool _underlyingItemLoaded;

    public TvMosaicResourceAccessor(TvMosaicResourceProvider parent, string path)
    : this(parent, path, new TvMosaicNavigator())
    {
    }

    public TvMosaicResourceAccessor(TvMosaicResourceProvider parent, string path, ITvMosaicNavigator navigator)
    {
      _navigator = navigator;
      _parent = parent;
      _path = path;
      _objectId = TvMosaicResourceProvider.ToObjectId(path);
    }

    internal TvMosaicResourceAccessor(TvMosaicResourceProvider parent, RecordedTV item, ITvMosaicNavigator navigator)
    {
      _navigator = navigator;
      _parent = parent;
      _objectId = item.ObjectID;
      _path = TvMosaicResourceProvider.ToProviderPath(item.ObjectID);
      _underlyingItem = item;
      _underlyingItemLoaded = true;
    }

    /// <summary>
    /// The object id of the TvMosaic container or item.
    /// </summary>
    public string TvMosaicObjectId
    {
      get { return _objectId; }
    }

    /// <summary>
    /// If this is a file resource, attempts to retrieve the <see cref="RecordedTV"/> item that corresponds to this resource.
    /// </summary>
    /// <returns>Task that completes with the <see cref="RecordedTV"/> item; else <c>null</c> if the item is not available or this is a directory resource.</returns>
    public async Task<RecordedTV> GetItem()
    {
      if (_underlyingItemLoaded)
        return _underlyingItem;
      await _syncObj.WaitAsync().ConfigureAwait(false);
      try
      {
        if (_underlyingItemLoaded)
          return _underlyingItem;
        if (IsItemId(_objectId))
          _underlyingItem = await _navigator.GetItemAsync(_objectId).ConfigureAwait(false);
        _underlyingItemLoaded = true;
        return _underlyingItem;
      }
      finally
      {
        _syncObj.Release();
      }
    }

    #region Protected methods

    /// <summary>
    /// Determines whether the specified path points to the root of the TvMosaic API.
    /// </summary>
    /// <param name="providerPath">The path to check.</param>
    /// <returns><c>true</c> if the path is the root path.</returns>
    protected internal static bool IsRootPath(string providerPath)
    {
      return providerPath == TvMosaicResourceProvider.ROOT_PROVIDER_PATH;
    }

    /// <summary>
    /// Determines whether the specified object id is the id of a TvMosaic container.
    /// </summary>
    /// <param name="objectId">The id to check.</param>
    /// <returns><c>true</c> if the id is the id of a TvMosaic container.</returns>
    protected internal static bool IsContainerId(string objectId)
    {
      return !string.IsNullOrEmpty(objectId) && !objectId.Contains("/");
    }

    /// <summary>
    /// Determines whether the specified object id is the id of a TvMosaic item in a container.
    /// </summary>
    /// <param name="objectId">The id to check.</param>
    /// <returns><c>true</c> if the id is the id of a TvMosaic item.</returns>
    protected internal static bool IsItemId(string objectId)
    {
      return !string.IsNullOrEmpty(objectId) && !IsContainerId(objectId);
    }

    /// <summary>
    /// Determines whether this accessor points to an object that exists in the TvMosaic API.
    /// This method will trigger a network request.
    /// </summary>
    /// <returns><c>true</c> if the path points to an item that exists.</returns>
    protected internal bool ObjectExists()
    {
      // Assume the root always exists
      if (IsRootPath(_path))
        return true;
      if (string.IsNullOrEmpty(_objectId))
        return false;
      if (IsItemId(_objectId))
        return GetItem().Result != null;
      return _navigator.ObjectExistsAsync(_objectId).Result;
    }

    protected string GetStreamUrl()
    {
      if (!IsItemId(_objectId))
        return null;
      return GetItem().Result?.Url;
    }

    #endregion

    #region IFileSystemResourceAccessor implementation

    public bool Exists
    {
      get
      {
        return ObjectExists();
      }
    }

    public bool IsFile
    {
      get
      {
        return IsItemId(_objectId) && ObjectExists();
      }
    }

    public DateTime LastChanged
    {
      get
      {
        if (IsItemId(_objectId))
        {
          RecordedTV item = GetItem().Result;
          if (item != null)
            return (item.VideoInfo.StartTime + item.VideoInfo.Duration).FromUnixTime();
        }
        return DateTime.MinValue;
      }
    }

    public long Size
    {
      get
      {
        return 0;
      }
    }

    public IResourceProvider ParentProvider
    {
      get
      {
        return _parent;
      }
    }

    public string Path
    {
      get
      {
        return _path;
      }
    }

    public string ResourceName
    {
      get
      {
        return _navigator.GetObjectFriendlyNameAsync(_objectId).Result;
      }
    }

    public string ResourcePathName
    {
      get
      {
        return _objectId;
      }
    }

    public ResourcePath CanonicalLocalResourcePath
    {
      get
      {
        return TvMosaicResourceProvider.ToProviderResourcePath(_path);
      }
    }

    public IResourceAccessor Clone()
    {
      return new TvMosaicResourceAccessor(_parent, _path, _navigator);
    }

    public Stream CreateOpenWrite(string file, bool overwrite)
    {
      return null;
    }

    public ICollection<IFileSystemResourceAccessor> GetChildDirectories()
    {
      // The only 'directories' are the top level containers under the root, so just ignore for all other paths
      if (!IsRootPath(_path))
        return new List<IFileSystemResourceAccessor>();
      return _navigator.GetRootContainerIds().Select(id => new TvMosaicResourceAccessor(_parent, TvMosaicResourceProvider.ToProviderPath(id), _navigator)).ToList<IFileSystemResourceAccessor>();
    }

    public ICollection<IFileSystemResourceAccessor> GetFiles()
    {
      // Only containers contain items
      if (!IsContainerId(_objectId))
        return null;

      IList<RecordedTV> items = _navigator.GetChildItemsAsync(_objectId).Result;
      // This path is a directory so callers expect this method to not return
      // null, so return an empty list in case of an error retrieving the items
      if (items == null)
        return new List<IFileSystemResourceAccessor>();
      return items.Select(item => new TvMosaicResourceAccessor(_parent, item, _navigator)).ToList<IFileSystemResourceAccessor>();
    }

    public IFileSystemResourceAccessor GetResource(string path)
    {
      // Path should be an object id which are always absolute, so just return the new path      
      return _parent.IsResource(path) ? new TvMosaicResourceAccessor(_parent, path, _navigator) : null;
    }

    public Stream OpenRead()
    {
      // ToDo: We could potentially implement stream methods by retrieving the http streams from the TvMosaic API
      return null;
    }

    public Task<Stream> OpenReadAsync()
    {
      // ToDo: We could potentially implement stream methods by retrieving the http streams from the TvMosaic API
      return null;
    }

    public Stream OpenWrite()
    {
      // ToDo: We could potentially implement stream methods by retrieving the http streams from the TvMosaic API
      return null;
    }

    public void PrepareStreamAccess()
    {
      // Nothing to do
    }

    public bool ResourceExists(string path)
    {
      return GetResource(path).Exists;
    }

    public void Dispose()
    {
      _syncObj.Dispose();
    }

    #endregion

    #region IResourceDeletor implementation

    public bool Delete()
    {
      if (IsItemId(_objectId))
        return _navigator.RemoveObject(_objectId).Result;
      return false;
    }

    #endregion

    #region INetworkResourceAccessor implementation

    public string URL
    {
      get { return GetStreamUrl(); }
    }

    #endregion
  }
}
