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
using MediaPortal.Utilities.FileSystem;

namespace MediaPortal.Extensions.ResourceProviders.IsoResourceProvider
{
  class IsoResourceAccessor : IFileSystemResourceAccessor
  {
    #region Protected fields

    protected IsoResourceProvider _isoProvider;
    protected IResourceAccessor _baseIsoResourceAccessor;
    protected ILocalFsResourceAccessor _baseLocalFsIsoResourceAccessor;
    protected string _pathInIsoFile;

    protected bool _isDirectory;
    protected DateTime _lastChanged;
    protected long _size;
    protected IsoReader _isoReader;
    
    #endregion

    #region Ctor

    public IsoResourceAccessor(IsoResourceProvider isoProvider, IResourceAccessor baseIsoAccessor, string pathInIsoFile)
    {
      if (!pathInIsoFile.StartsWith("/"))
        throw new ArgumentException("Wrong path '{0}': Path in ISO file must start with a '/' character", pathInIsoFile);
      _isoProvider = isoProvider;
      _baseIsoResourceAccessor = baseIsoAccessor;
      _baseLocalFsIsoResourceAccessor = StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(baseIsoAccessor);
      _pathInIsoFile = pathInIsoFile;
      _isoReader = new IsoReader();
      _isoReader.Open(_baseLocalFsIsoResourceAccessor.LocalFileSystemPath);

      _isDirectory = true;
      _lastChanged = baseIsoAccessor.LastChanged;
      _size = -1;

      if (!IsEmptyOrRoot)
      {
        string dosPath = LocalFsResourceProviderBase.ToDosPath(pathInIsoFile);
        RecordEntryInfo entry = _isoReader .GetRecordEntryInfo(dosPath); // Path must start with /
        _isDirectory = entry.Directory;
        _lastChanged = entry.Date;
        _size = _isDirectory ? (long) (-1) : entry.Size;

      }
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      if (_isoReader != null)
      {
        _isoReader.Dispose();
        _isoReader = null;
      }
      if (_baseLocalFsIsoResourceAccessor != null)
      {
        _baseLocalFsIsoResourceAccessor.Dispose();
        _baseLocalFsIsoResourceAccessor = null;
      }
    }

    #endregion

    #region Protected methods

    protected bool IsEmptyOrRoot
    {
      get { return string.IsNullOrEmpty(_pathInIsoFile) || _pathInIsoFile == "/"; }
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
      get
      {
        if (string.IsNullOrEmpty(_pathInIsoFile))
          return null;
        if (_pathInIsoFile == "/")
          return _baseIsoResourceAccessor.ResourceName;
        string dosPath = LocalFsResourceProviderBase.ToDosPath(_pathInIsoFile);
        return Path.GetFileName(FileUtils.RemoveTrailingPathDelimiter(dosPath));
      }
    }

    public string ResourcePathName
    {
      get { return _baseIsoResourceAccessor.ResourcePathName + " > " + _pathInIsoFile; }
    }

    public ResourcePath CanonicalLocalResourcePath
    {
      get
      {
        ResourcePath resourcePath = new ResourcePath(_baseIsoResourceAccessor.CanonicalLocalResourcePath);
        resourcePath.Append(IsoResourceProvider.ISO_RESOURCE_PROVIDER_ID, _pathInIsoFile);
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
      string dosPath = LocalFsResourceProviderBase.ToDosPath(_pathInIsoFile);
      return _isoReader.GetFileStream(dosPath.StartsWith("\\") ? dosPath : "\\" + dosPath);
    }

    public Stream OpenWrite()
    {
      return null;
    }

    public IResourceAccessor Clone()
    {
      return new IsoResourceAccessor(_isoProvider, _baseIsoResourceAccessor.Clone(), _pathInIsoFile);
    }

    #endregion

    #region IFileSystemResourceAccessor implementation

    public bool IsDirectory
    {
      get { return _isDirectory; }
    }

    public bool ResourceExists(string path)
    {
      if (path.Equals("/") || path.Equals(_pathInIsoFile, StringComparison.OrdinalIgnoreCase)) 
        return true;
      string dosPath = "\\" + LocalFsResourceProviderBase.ToDosPath(_pathInIsoFile);
      string dosCombined = "\\" + LocalFsResourceProviderBase.ToDosPath(Path.Combine(_pathInIsoFile, path));
      string[] dirList = _isoReader.GetFileSystemEntries(dosPath, SearchOption.TopDirectoryOnly);
      return dirList.Any(entry => entry.Equals(dosCombined, StringComparison.OrdinalIgnoreCase));
    }

    public IFileSystemResourceAccessor GetResource(string path)
    {
      string pathFile = LocalFsResourceProviderBase.ToProviderPath(Path.Combine(_pathInIsoFile, path));
      return (IFileSystemResourceAccessor) _isoProvider.CreateResourceAccessor(_baseLocalFsIsoResourceAccessor.Clone(), pathFile);
    }

    public ICollection<IFileSystemResourceAccessor> GetFiles()
    {
      string dosPath = LocalFsResourceProviderBase.ToDosPath(_pathInIsoFile);
      string[] files = _isoReader.GetFiles(dosPath.StartsWith("\\") ? dosPath : "\\" + dosPath, SearchOption.TopDirectoryOnly);
      return files.Select(s => new IsoResourceAccessor(_isoProvider, _baseLocalFsIsoResourceAccessor.Clone(), LocalFsResourceProviderBase.ToProviderPath(s))).Cast<IFileSystemResourceAccessor>().ToList();
    }

    public ICollection<IFileSystemResourceAccessor> GetChildDirectories()
    {
      string dosPath = LocalFsResourceProviderBase.ToDosPath(_pathInIsoFile);
      string[] files = _isoReader.GetDirectories(dosPath.StartsWith("\\") ? dosPath : "\\" + dosPath, SearchOption.TopDirectoryOnly);
      return files.Select(s => new IsoResourceAccessor(_isoProvider, _baseLocalFsIsoResourceAccessor.Clone(), LocalFsResourceProviderBase.ToProviderPath(s))).Cast<IFileSystemResourceAccessor>().ToList();
    }

    #endregion

    public override string ToString()
    {
      return CanonicalLocalResourcePath.ToString();
    }
  }
}
