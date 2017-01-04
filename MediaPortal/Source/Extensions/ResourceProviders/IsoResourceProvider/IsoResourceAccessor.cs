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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DiscUtils;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Utilities;
using MediaPortal.Utilities.FileSystem;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Extensions.ResourceProviders.IsoResourceProvider
{
  class IsoResourceAccessor : IFileSystemResourceAccessor, IUncachableResource
  {
    #region Protected fields

    protected IsoResourceProvider _isoProvider;
    internal IsoResourceProxy _isoProxy;
    protected string _pathToDirOrFile;

    protected bool _isDirectory;
    protected DateTime _lastChanged;
    protected long _size;
    protected Stream _stream = null;

    #endregion

    #region Ctor

    public IsoResourceAccessor(IsoResourceProvider isoProvider, IsoResourceProxy isoProxy, string pathToDirOrFile)
    {
      if (!pathToDirOrFile.StartsWith("/"))
        throw new ArgumentException("Wrong path '{0}': Path in ISO file must start with a '/' character", pathToDirOrFile);
      _isoProxy = isoProxy;
      _isoProxy.IncUsage();
      try
      {
        _isoProvider = isoProvider;
        _pathToDirOrFile = pathToDirOrFile;

        _isDirectory = true;
        _lastChanged = _isoProxy.IsoFileResourceAccessor.LastChanged;
        _size = -1;

        if (IsEmptyOrRoot)
          return;
        lock (_isoProxy.SyncObj)
        {
          string isoPath = ToIsoPath(pathToDirOrFile);
          if (_isoProxy.DiskFileSystem.FileExists(isoPath))
          {
            _isDirectory = false;
            _size = _isoProxy.DiskFileSystem.GetFileLength(isoPath);
            _lastChanged = _isoProxy.DiskFileSystem.GetLastWriteTime(isoPath);
            return;
          }
          if (_isoProxy.DiskFileSystem.DirectoryExists(isoPath))
          {
            _isDirectory = true;
            _size = -1;
            _lastChanged = _isoProxy.DiskFileSystem.GetLastWriteTime(isoPath);
            return;
          }
          throw new ArgumentException(string.Format("IsoResourceAccessor cannot access path or file '{0}' in iso-file", isoPath));
        }
      }
      catch (Exception)
      {
        _isoProxy.DecUsage();
        throw;
      }
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      if (_stream != null)
        _stream.Dispose();
      if (_isoProxy == null)
        return;
      _isoProxy.DecUsage();
      _isoProxy = null;
    }

    #endregion

    #region Protected and internal members

    protected bool IsEmptyOrRoot
    {
      get { return string.IsNullOrEmpty(_pathToDirOrFile) || _pathToDirOrFile == "/"; }
    }

    protected internal static string ToIsoPath(string providerPath)
    {
      providerPath = StringUtils.RemovePrefixIfPresent(providerPath, "/");
      return providerPath.Replace('/', System.IO.Path.DirectorySeparatorChar);
    }

    protected internal static string ToProviderPath(string dosPath)
    {
      string path = dosPath.Replace(System.IO.Path.DirectorySeparatorChar, '/');
      return StringUtils.CheckPrefix(path, "/");
    }

    protected internal static bool IsResource(IFileSystem diskFileSystem, string providerPath)
    {
      if (providerPath == "/")
        return true;
      string isoPath = ToIsoPath(providerPath);

      return diskFileSystem.Exists(isoPath);
    }

    protected string ExpandPath(string relativeOrAbsoluteProviderPath)
    {
      return ProviderPathHelper.Combine(_pathToDirOrFile, relativeOrAbsoluteProviderPath);
    }

    #endregion

    #region IFileSystemResourceAccessor implementation

    public IResourceProvider ParentProvider
    {
      get { return _isoProvider; }
    }

    public bool IsFile
    {
      get { return !_isDirectory; }
    }

    public bool IsDirectory
    {
      get { return _isDirectory; }
    }

    public bool Exists
    {
      get { return true; }
    }

    public string Path
    {
      get { return _pathToDirOrFile; }
    }

    public string ResourceName
    {
      get
      {
        if (string.IsNullOrEmpty(_pathToDirOrFile))
          return null;
        if (_pathToDirOrFile == "/")
          return _isoProxy.IsoFileResourceAccessor.ResourceName;
        return ProviderPathHelper.GetFileName(FileUtils.RemoveTrailingPathDelimiter(_pathToDirOrFile));
      }
    }

    public string ResourcePathName
    {
      get { return _isoProxy.IsoFileResourceAccessor.ResourcePathName + " > " + _pathToDirOrFile; }
    }

    public ResourcePath CanonicalLocalResourcePath
    {
      get { return _isoProxy.IsoFileResourceAccessor.CanonicalLocalResourcePath.ChainUp(IsoResourceProvider.ISO_RESOURCE_PROVIDER_ID, _pathToDirOrFile); }
    }

    public DateTime LastChanged
    {
      get { return _lastChanged; }
    }

    public long Size
    {
      get { return _size; }
    }

    public void PrepareStreamAccess()
    {
    }

    public Stream OpenRead()
    {
      lock (_isoProxy.SyncObj)
      {
        if (_stream == null)
        {
          string isoPath = ToIsoPath(_pathToDirOrFile);
          if (!_isoProxy.DiskFileSystem.FileExists(isoPath))
            throw new IllegalCallException ("Resource '{0}' is not a file", isoPath);
          _stream = _isoProxy.DiskFileSystem.OpenFile(isoPath, FileMode.Open, FileAccess.Read);
        }
        return new SynchronizedMasterStreamClient(_stream, _isoProxy.SyncObj);
      }
    }

    public Task<Stream> OpenReadAsync()
    {
      // In this implementation there is no preparational work to do. We therefore return a
      // completed Task; there is no need for any async operation.
      // ToDo: Implement the async virtual methods of SynchronizedMasterStreamClient
      return System.Threading.Tasks.Task.FromResult(OpenRead());
    }

    public Stream OpenWrite()
    {
      return null;
    }

    public bool ResourceExists(string path)
    {
      lock(_isoProxy.SyncObj)
        return path.Equals(_pathToDirOrFile, StringComparison.OrdinalIgnoreCase) || IsResource(_isoProxy.DiskFileSystem, ExpandPath(path));
    }

    public IFileSystemResourceAccessor GetResource(string path)
    {
      string pathToDirOrFile = ExpandPath(path);
      return new IsoResourceAccessor(_isoProvider, _isoProxy, pathToDirOrFile);
    }

    public ICollection<IFileSystemResourceAccessor> GetFiles()
    {
      string isoPath = ToIsoPath(_pathToDirOrFile);
      try
      {
        lock(_isoProxy.SyncObj)
          return _isoProxy.DiskFileSystem.GetFiles(isoPath).Select(path => new IsoResourceAccessor(_isoProvider, _isoProxy,
              ToProviderPath(path))).Cast<IFileSystemResourceAccessor>().ToList();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("IsoResourceAccessor: Error reading files of '{0}'", e, CanonicalLocalResourcePath);
        return null;
      }
    }

    public ICollection<IFileSystemResourceAccessor> GetChildDirectories()
    {
      string isoPath = ToIsoPath(_pathToDirOrFile);
      try
      {
        lock (_isoProxy.SyncObj)
          return _isoProxy.DiskFileSystem.GetDirectories(isoPath).Select(path => new IsoResourceAccessor(_isoProvider, _isoProxy,
            ToProviderPath(path))).Cast<IFileSystemResourceAccessor>().ToList();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("IsoResourceAccessor: Error reading child directories of '{0}'", e, CanonicalLocalResourcePath);
        return null;
      }
    }

    public IResourceAccessor Clone()
    {
      return new IsoResourceAccessor(_isoProvider, _isoProxy, _pathToDirOrFile);
    }

    #endregion

    public override string ToString()
    {
      return CanonicalLocalResourcePath.ToString();
    }
  }
}
