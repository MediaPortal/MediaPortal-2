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
using MediaPortal.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TvMosaic.API;

namespace TvMosaicMetadataExtractor.ResourceAccess
{
  /// <summary>
  /// <see cref="IFileSystemResourceAccessor"/> that can navigate the TvMosaic object API from the root to get containers and their child items.
  /// </summary>
  public class TvMosaicResourceAccessor : IFileSystemResourceAccessor, INetworkResourceAccessor
  {
    // We cache the stream url for a recorded tv item when it's first requested, if the url cannot be found,
    // e.g. the item has been removed or the path is invalid, we set it to this value to avoid retrying the next
    // time the url is requested.
    protected const string INVALID_STREAM_URL = "";

    protected ITvMosaicNavigator _navigator;
    protected IResourceProvider _parent;
    protected string _path;
    protected string _cachedStreamUrl;

    public TvMosaicResourceAccessor(IResourceProvider parent, string path)
    : this(parent, path, new TvMosaicNavigator())
    {
    }

    public TvMosaicResourceAccessor(IResourceProvider parent, string path, ITvMosaicNavigator navigator)
    {
      _navigator = navigator;
      _parent = parent;
      _path = path;
    }

    /// <summary>
    /// The object id of the TvMosaic container or item.
    /// </summary>
    public string TvMosaicObjectId
    {
      get { return GetObjectId(_path); }
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
    /// Determines whether the specified path points to a TvMosaic container.
    /// </summary>
    /// <param name="providerPath">The path to check.</param>
    /// <returns><c>true</c> if the path points to a container.</returns>
    protected internal static bool IsContainerPath(string providerPath)
    {
      string objectId = GetObjectId(providerPath);
      return !string.IsNullOrEmpty(objectId) && !objectId.Contains("/");
    }

    /// <summary>
    /// Determines whether the specified path points to a TvMosaic item in a container.
    /// </summary>
    /// <param name="providerPath">The path to check.</param>
    /// <returns><c>true</c> if the path points to an item.</returns>
    protected internal static bool IsItemPath(string providerPath)
    {
      return !IsRootPath(providerPath) && !IsContainerPath(providerPath) && !string.IsNullOrEmpty(GetObjectId(providerPath));
    }

    /// <summary>
    /// Gets the TvMosaic object id for the specified path.
    /// </summary>
    /// <param name="providerPath">The path to get the id from.</param>
    /// <returns>The TvMosaic object id that this path points to.</returns>
    protected internal static string GetObjectId(string providerPath)
    {
      if (IsRootPath(providerPath))
        return null;
      return StringUtils.RemoveSuffixIfPresent(providerPath, "/");
    }

    /// <summary>
    /// Determines whether the path points to an object that exists in the TvMosaic API.
    /// This method will trigger a network request.
    /// </summary>
    /// <param name="providerPath">The path to check.</param>
    /// <returns><c>true</c> if the path points to an item that exists.</returns>
    protected internal bool ObjectExists(string providerPath)
    {
      // Assume the root always exists
      if (IsRootPath(providerPath))
        return true;
      
      string objectId = GetObjectId(providerPath);
      if (string.IsNullOrEmpty(objectId))
        return false;
      return _navigator.ObjectExists(objectId);
    }

    protected string GetStreamUrl()
    {
      if (!IsItemPath(_path) || _cachedStreamUrl == INVALID_STREAM_URL)
        return null;

      if (!string.IsNullOrEmpty(_cachedStreamUrl))
        return _cachedStreamUrl;

      string url = _navigator.GetItem(GetObjectId(_path))?.Url;
      _cachedStreamUrl = string.IsNullOrEmpty(url) ? INVALID_STREAM_URL : url;
      return _cachedStreamUrl != INVALID_STREAM_URL ? _cachedStreamUrl : null;
    }

    #endregion

    #region IFileSystemResourceAccessor implementation

    public bool Exists
    {
      get
      {
        return ObjectExists(_path);
      }
    }

    public bool IsFile
    {
      get
      {
        return IsItemPath(_path);
      }
    }

    public DateTime LastChanged
    {
      get
      {
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
        return TvMosaicResourceProvider.ToProviderResourcePath(_path).Serialize();
      }
    }

    public string ResourceName
    {
      get
      {
        return _navigator.GetObjectFriendlyName(GetObjectId(_path));
      }
    }

    public string ResourcePathName
    {
      get
      {
        return GetObjectId(_path);
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
        return null;      
      return _navigator.GetRootContainerIds().Select(id => new TvMosaicResourceAccessor(_parent, id, _navigator)).ToList<IFileSystemResourceAccessor>();
    }

    public ICollection<IFileSystemResourceAccessor> GetFiles()
    {
      // Only containers contain items
      if (!IsContainerPath(_path))
        return null;

      string objectId = GetObjectId(_path);
      Items items = _navigator.GetChildItems(objectId);
      if (items == null)
        return null;

      return items.Select(item => new TvMosaicResourceAccessor(_parent, item.ObjectID, _navigator)).ToList<IFileSystemResourceAccessor>();
    }

    public IFileSystemResourceAccessor GetResource(string path)
    {
      // Path should be an object id which are always absolute, so just return the new path
      return new TvMosaicResourceAccessor(_parent, path, _navigator);
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
