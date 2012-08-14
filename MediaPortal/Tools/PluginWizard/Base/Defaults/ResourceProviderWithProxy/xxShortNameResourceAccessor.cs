#region Copyright (C) 2007-xxCurrentYear Team MediaPortal

/*
    Copyright (C) 2007-xxCurrentYear Team MediaPortal
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
using DiscUtils;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Utilities;
using MediaPortal.Utilities.FileSystem;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Extensions.ResourceProviders.xxPluginName
{
  class xxShortNameResourceAccessor : IFileSystemResourceAccessor
  {
    #region Protected fields

    protected xxPluginName _xxShortNameProvider;
    internal xxShortNameResourceProxy _xxShortNameProxy;
    protected string _pathToDirOrFile;

    protected bool _isDirectory;
    protected DateTime _lastChanged;
    protected long _size;
    protected object _syncObj = new object();
    protected Stream _stream = null;

    #endregion

    #region Ctor

    public xxShortNameResourceAccessor(xxPluginName xxShortNameProvider, xxShortNameResourceProxy xxShortNameProxy, string pathToDirOrFile)
    {
      if (!pathToDirOrFile.StartsWith("/"))
        throw new ArgumentException("Wrong path '{0}': Path in xxShortName file must start with a '/' character", pathToDirOrFile);
      _xxShortNameProxy = xxShortNameProxy;
      _xxShortNameProxy.IncUsage();
      try
      {
        _xxShortNameProvider = xxShortNameProvider;
        _pathToDirOrFile = pathToDirOrFile;

        _isDirectory = true;
        _lastChanged = _xxShortNameProxy.xxShortNameFileResourceAccessor.LastChanged;
        _size = -1;

        if (IsEmptyOrRoot)
          return;
        lock (_xxShortNameProxy.SyncObj)
        {
          string xxShortNamePath = ToxxShortNamePath(pathToDirOrFile);
          if (_xxShortNameProxy.DiskFileSystem.FileExists(xxShortNamePath))
          {
            _isDirectory = false;
            _size = _xxShortNameProxy.DiskFileSystem.GetFileLength(xxShortNamePath);
            _lastChanged = _xxShortNameProxy.DiskFileSystem.GetLastWriteTime(xxShortNamePath);
            return;
          }
          if (_xxShortNameProxy.DiskFileSystem.DirectoryExists(xxShortNamePath))
          {
            _isDirectory = true;
            _size = -1;
            _lastChanged = _xxShortNameProxy.DiskFileSystem.GetLastWriteTime(xxShortNamePath);
            return;
          }
          throw new ArgumentException("xxShortNameResourceAccessor cannot access path or file '{0}' in xxShortName-file", xxShortNamePath);
        }
      }
      catch (Exception)
      {
        _xxShortNameProxy.DecUsage();
        throw;
      }
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      if (_stream != null)
        _stream.Dispose();
      if (_xxShortNameProxy == null)
        return;
      _xxShortNameProxy.DecUsage();
      _xxShortNameProxy = null;
    }

    #endregion

    #region Protected and internal members

    protected bool IsEmptyOrRoot
    {
      get { return string.IsNullOrEmpty(_pathToDirOrFile) || _pathToDirOrFile == "/"; }
    }

    protected internal static string ToxxShortNamePath(string providerPath)
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
      string xxShortNamePath = ToxxShortNamePath(providerPath);

      return diskFileSystem.Exists(xxShortNamePath);
    }

    protected string ExpandPath(string relativeOrAbsoluteProviderPath)
    {
      return ProviderPathHelper.Combine(_pathToDirOrFile, relativeOrAbsoluteProviderPath);
    }

    #endregion

    #region IResourceAccessor implementation

    public IResourceProvider ParentProvider
    {
      get { return _xxShortNameProvider; }
    }

    public bool IsFile
    {
      get { return !_isDirectory; }
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
          return _xxShortNameProxy.xxShortNameFileResourceAccessor.ResourceName;
        return ProviderPathHelper.GetFileName(FileUtils.RemoveTrailingPathDelimiter(_pathToDirOrFile));
      }
    }

    public string ResourcePathName
    {
      get { return _xxShortNameProxy.xxShortNameFileResourceAccessor.ResourcePathName + " > " + _pathToDirOrFile; }
    }

    public ResourcePath CanonicalLocalResourcePath
    {
      get { return _xxShortNameProxy.xxShortNameFileResourceAccessor.CanonicalLocalResourcePath.ChainUp(xxPluginName.xxShortName_RESOURCE_PROVIDER_ID, _pathToDirOrFile); }
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
      lock (_syncObj)
      {
        if (_stream == null)
        {
          string xxShortNamePath = ToxxShortNamePath(_pathToDirOrFile);
          if (!_xxShortNameProxy.DiskFileSystem.FileExists(xxShortNamePath))
            throw new IllegalCallException ("Resource '{0}' is not a file", xxShortNamePath);
          _stream = _xxShortNameProxy.DiskFileSystem.OpenFile(xxShortNamePath, FileMode.Open, FileAccess.Read);
        }
        return new SynchronizedMasterStreamClient(_stream, _syncObj);
      }
    }

    public Stream OpenWrite()
    {
      return null;
    }

    public IResourceAccessor Clone()
    {
      return new xxShortNameResourceAccessor(_xxShortNameProvider, _xxShortNameProxy, _pathToDirOrFile);
    }

    #endregion

    #region IFileSystemResourceAccessor implementation

    public bool IsDirectory
    {
      get { return _isDirectory; }
    }

    public bool ResourceExists(string path)
    {
      return path.Equals(_pathToDirOrFile, StringComparxxShortNamen.OrdinalIgnoreCase) || IsResource(_xxShortNameProxy.DiskFileSystem, ExpandPath(path));
    }

    public IFileSystemResourceAccessor GetResource(string path)
    {
      string pathToDirOrFile = ExpandPath(path);
      return new xxShortNameResourceAccessor(_xxShortNameProvider, _xxShortNameProxy, pathToDirOrFile);
    }

    public ICollection<IFileSystemResourceAccessor> GetFiles()
    {
      string xxShortNamePath = ToxxShortNamePath(_pathToDirOrFile);
      try
      {
        return _xxShortNameProxy.DiskFileSystem.GetFiles(xxShortNamePath).Select(path => new xxShortNameResourceAccessor(_xxShortNameProvider, _xxShortNameProxy,
            ToProviderPath(path))).Cast<IFileSystemResourceAccessor>().ToList();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("xxShortNameResourceAccessor: Error reading files of '{0}'", e, CanonicalLocalResourcePath);
        return null;
      }
    }

    public ICollection<IFileSystemResourceAccessor> GetChildDirectories()
    {
      string xxShortNamePath = ToxxShortNamePath(_pathToDirOrFile);
      try
      {
        return _xxShortNameProxy.DiskFileSystem.GetDirectories(xxShortNamePath).Select(path => new xxShortNameResourceAccessor(_xxShortNameProvider, _xxShortNameProxy,
            ToProviderPath(path))).Cast<IFileSystemResourceAccessor>().ToList();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("xxShortNameResourceAccessor: Error reading child directories of '{0}'", e, CanonicalLocalResourcePath);
        return null;
      }
    }

    #endregion

    public override string ToString()
    {
      return CanonicalLocalResourcePath.ToString();
    }
  }
}
