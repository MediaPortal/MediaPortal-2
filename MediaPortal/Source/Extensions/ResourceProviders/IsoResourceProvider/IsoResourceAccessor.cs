#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Common.MediaManagement.ResourceAccess;
using ISOReader;
using MediaPortal.Utilities;

namespace MediaPortal.Extensions.ResourceProviders.IsoResourceProvider
{
  class IsoResourceAccessor : IFileSystemResourceAccessor
  {
    #region Protected fields

    protected bool _disposed;
    protected IsoResourceProvider _isoProvider;
    protected IResourceAccessor _isoResourceAccessor;
    protected string _pathFile;

    protected bool _isDirectory;
    protected string _resourceName;
    protected string _resourcePath;
    protected DateTime _lastChanged;
    protected long _size;
    IsoReader _isoReader = new IsoReader();
    
    #endregion

    #region Ctor

    public IsoResourceAccessor(IsoResourceProvider isoProvider, IResourceAccessor accessor, string pathFile)
    {
      _isoProvider = isoProvider;
      _isoResourceAccessor = accessor;
      _pathFile = LocalFsResourceProviderBase.ToProviderPath(pathFile);
      _isoReader.Open(_isoResourceAccessor.ResourcePathName);

      string dosPath = LocalFsResourceProviderBase.ToDosPath(_pathFile);

      _isDirectory = true;
      _resourceName = Path.GetFileName(_isoResourceAccessor.ResourceName);
      _resourcePath = _pathFile == "/" ? "/" : dosPath;
      _lastChanged = accessor.LastChanged;
      _size = -1;

      if (!IsEmptyOrRoot)
      {
        RecordEntryInfo entry = _isoReader .GetRecordEntryInfo(dosPath.StartsWith("\\") ? dosPath : "\\" + dosPath);
        _resourceName = Path.GetFileName(StringUtils.RemoveSuffixIfPresent(dosPath, "/"));
        _isDirectory = entry.Directory;
        _lastChanged = entry.Date;
        _size = _isDirectory ? (long) (-1) : entry.Size;

      }
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      Dispose(true);
    }

    public void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          if (_isoReader != null)
          {
            _isoReader.Close();
            _isoReader = null;
          }
        }
      }
      _disposed = true;
    }

    #endregion

    #region Protected methods

    protected bool IsEmptyOrRoot
    {
      get { return string.IsNullOrEmpty(_pathFile) || _pathFile == "/"; }
    }

    #endregion

    #region IResourceAccessor implementation

    public IResourceProvider ParentProvider
    {
      get { return _isoProvider; }
    }

    public bool IsFile
    {
      get { return !_isDirectory; }
    }

    public bool Exists
    {
      get { return true; }
    }

    public string ResourceName
    {
      get { return _resourceName; }
    }

    public string ResourcePathName
    {
      get { return _resourcePath; }
    }

    public ResourcePath LocalResourcePath
    {
      get
      {
        ResourcePath resourcePath = _isoResourceAccessor.LocalResourcePath;
        resourcePath.Append(IsoResourceProvider.ISO_RESOURCE_PROVIDER_ID, _pathFile);
        return resourcePath;
      }
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
      string dosPath = LocalFsResourceProviderBase.ToDosPath(_pathFile);
      return _isoReader.GetFileStream(dosPath.StartsWith("\\") ? dosPath : "\\" + dosPath);
    }

    public Stream OpenWrite()
    {
      return null;
    }

    #endregion

    #region IFileSystemResourceAccessor implementation

    public bool IsDirectory
    {
      get { return _isDirectory; }
    }

    public bool ResourceExists(string path)
    {
      if (path.Equals("/") || path.Equals(_pathFile,StringComparison.OrdinalIgnoreCase)) 
        return true;
      string dosPath = "\\" + LocalFsResourceProviderBase.ToDosPath(_pathFile);
      string dosCombined = "\\" + LocalFsResourceProviderBase.ToDosPath(Path.Combine(_pathFile, path));
      string[] dirList = _isoReader.GetFileSystemEntries(dosPath, SearchOption.TopDirectoryOnly);
      return dirList.Any(entry => entry.Equals(dosCombined, StringComparison.OrdinalIgnoreCase));
    }

    public IResourceAccessor GetResource(string path)
    {
      string pathFile = LocalFsResourceProviderBase.ToProviderPath(Path.Combine(_pathFile, path));
      return _isoProvider.CreateResourceAccessor(_isoResourceAccessor, pathFile);
    }

    public ICollection<IFileSystemResourceAccessor> GetFiles()
    {
      string dosPath = LocalFsResourceProviderBase.ToDosPath(_pathFile);
      string[] files = _isoReader.GetFiles(dosPath.StartsWith("\\") ? dosPath : "\\" + dosPath, SearchOption.TopDirectoryOnly);
      return files.Select(s => new IsoResourceAccessor(_isoProvider, _isoResourceAccessor, LocalFsResourceProviderBase.ToProviderPath(s))).Cast<IFileSystemResourceAccessor>().ToList();
    }

    public ICollection<IFileSystemResourceAccessor> GetChildDirectories()
    {
      string dosPath = LocalFsResourceProviderBase.ToDosPath(_pathFile);
      string[] files = _isoReader.GetDirectories(dosPath.StartsWith("\\") ? dosPath : "\\" + dosPath, SearchOption.TopDirectoryOnly);
      return files.Select(s => new IsoResourceAccessor(_isoProvider, _isoResourceAccessor, LocalFsResourceProviderBase.ToProviderPath(s))).Cast<IFileSystemResourceAccessor>().ToList();
    }

    #endregion
  }
}
