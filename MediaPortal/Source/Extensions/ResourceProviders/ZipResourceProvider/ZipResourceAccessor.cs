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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities;
using ICSharpCode.SharpZipLib.Zip;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Extensions.ResourceProviders.ZipResourceProvider
{
  internal class ZipResourceAccessor : ILocalFsResourceAccessor, IUncachableResource
  {
    #region Protected consts and fields

    protected const int ASYNC_STREAM_BUFFER_SIZE = 4096;

    protected ZipResourceProvider _zipProvider;
    protected ZipResourceProxy _zipProxy;
    protected string _pathToDirOrFile;

    // Initialize with root config
    protected ZipEntry _zipEntry = null;
    protected bool _isDirectory = true;
    protected DateTime _lastChanged = DateTime.MinValue;
    protected long _size = -1;
    protected string _tempFileName = null;
    protected List<ZipEntry> _currentDirList = new List<ZipEntry>();

    #endregion

    #region Ctor

    public ZipResourceAccessor(ZipResourceProvider zipProvider, ZipResourceProxy zipProxy, string pathToDirOrFile)
    {
      _zipProvider = zipProvider;
      _zipProxy = zipProxy;
      _pathToDirOrFile = pathToDirOrFile;

      _zipProxy.IncUsage();
      try
      {
        ReadCurrentDirectory();
        if (!_isDirectory && _zipEntry == null)
        {
          _zipProxy.DecUsage();
          throw new IllegalCallException("ZipResourceAccessor: Cannot find zip entry for path '{0}' in ZIP file '{1}'",
              pathToDirOrFile, _zipProxy.ZipFileResourceAccessor.CanonicalLocalResourcePath);
        }
      }
      catch (Exception)
      {
        _zipProxy.DecUsage();
        throw;
      }
    }

    #endregion

    #region Private, protected and internal members

    private void ReadCurrentDirectory()
    {
      string entryPath = ToEntryPath(_pathToDirOrFile) ?? string.Empty;

      int dirDepth = EvaluateDirDepth(entryPath);

      _currentDirList.Clear();
      lock (_zipProxy.SyncObj)
        foreach (ZipEntry entry in _zipProxy.ZipFile)
        {
          if (entry.IsDirectory)
          {
            int entryDirDepth = EvaluateDirDepth(StringUtils.RemoveSuffixIfPresent(entry.Name, "/"));
            if (entryDirDepth == dirDepth && entry.Name.StartsWith(entryPath))
              _currentDirList.Add(entry);
          }
          else
          {
            string dirName = GetDirectoryName(entry.Name);
            if (entryPath == dirName)
              _currentDirList.Add(entry);
          }
          if (entry.Name == entryPath)
          {
            _isDirectory = entry.IsDirectory;
            _lastChanged = entry.DateTime;
            _size = entry.Size;
            _zipEntry = entry;
          }
        }
    }

    protected internal static string ToEntryPath(string providerPath)
    {
      if (providerPath == "/")
        return null;
      if (providerPath.StartsWith("/"))
        return providerPath.Substring(1);
      throw new IllegalCallException("ZipResourceProvider: '{0}' is not a valid provider path", providerPath);
    }

    protected internal static string ToProviderPath(string entryPath)
    {
      if (entryPath.StartsWith("/"))
        throw new IllegalCallException("ZipResourceProvider: '{0}' is not a valid entry path", entryPath);
      return '/' + entryPath;
    }

    protected internal static bool IsResource(ZipFile zFile, string entryPath)
    {
      return entryPath.Equals("/") || zFile.Cast<ZipEntry>().Any(entry => entry.IsDirectory && entry.Name == entryPath);
    }

    protected string ExpandPath(string relativeOrAbsoluteProviderPath)
    {
      return ProviderPathHelper.Combine(_pathToDirOrFile, relativeOrAbsoluteProviderPath);
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      if (_zipProxy == null)
        return;
      _zipProxy.DecUsage();
      _zipProxy = null;
    }

    #endregion

    #region Protected methods

    protected bool IsEmptyOrRoot
    {
      get { return (string.IsNullOrEmpty(_pathToDirOrFile) || _pathToDirOrFile == "/"); }
    }

    protected static int EvaluateDirDepth(string path)
    {
      if (string.IsNullOrEmpty(path))
        return 0;
      return path.Count(t => t == '/');
    }

    protected static string GetDirectoryName(string path)
    {
      int index = path.LastIndexOf('/');
      return index == -1 ? string.Empty : path.Substring(0, index + 1);
    }

    protected static string GetFileName(string path)
    {
      int index = path.LastIndexOf('/');
      return index == -1 ? path : path.Substring(index + 1);
    }

    #endregion

    #region IFileSystemResourceAccessor implementation

    public IResourceProvider ParentProvider
    {
      get { return _zipProvider; }
    }

    public bool Exists
    {
      get { return _zipEntry != null; }
    }

    public bool IsDirectory
    {
      get { return _isDirectory; }
    }

    public bool IsFile
    {
      get { return _zipEntry != null && !_isDirectory; }
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
          return _zipProxy.ZipFileResourceAccessor.ResourceName;
        return GetFileName(StringUtils.RemoveSuffixIfPresent(_pathToDirOrFile, "/"));
      }
    }

    public string ResourcePathName
    {
      get { return _zipProxy.ZipFileResourceAccessor.ResourcePathName + " > " + _pathToDirOrFile; }
    }

    public ResourcePath CanonicalLocalResourcePath
    {
      get { return _zipProxy.ZipFileResourceAccessor.CanonicalLocalResourcePath.ChainUp(ZipResourceProvider.ZIP_RESOURCE_PROVIDER_ID, _pathToDirOrFile); }
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
      if (!string.IsNullOrEmpty(_tempFileName))
        return;
      if (_zipEntry == null)
        return;
      _tempFileName = _zipProxy.GetTempFile(_zipEntry);
    }

    public Stream OpenRead()
    {
     PrepareStreamAccess();
      if (File.Exists(_tempFileName))
      {
        // We need to operate on a temporary file because the underlaying ZIP library doesn't support seeking in the returned entry stream
        return File.OpenRead(_tempFileName);
      }
      return null;
    }

    public async Task<Stream> OpenReadAsync()
    {
      // ToDo: Implement PrepareStreamAccess in an async way
      await Task.Run(() => PrepareStreamAccess());
      return new FileStream(_tempFileName, FileMode.Open, FileAccess.Read, FileShare.Read, ASYNC_STREAM_BUFFER_SIZE, true);
    }

    public Stream OpenWrite()
    {
      return null;
    }

    public bool ResourceExists(string path)
    {
      return path.Equals(_pathToDirOrFile) || IsResource(_zipProxy.ZipFile, ToEntryPath(ExpandPath(path)));
    }

    public IFileSystemResourceAccessor GetResource(string path)
    {
      string pathToDirOrFile = ExpandPath(path);
      return new ZipResourceAccessor(_zipProvider, _zipProxy, pathToDirOrFile);
    }

    public ICollection<IFileSystemResourceAccessor> GetFiles()
    {
      if (string.IsNullOrEmpty(_pathToDirOrFile))
        return null;
      try
      {
        List<IFileSystemResourceAccessor> result = new List<IFileSystemResourceAccessor>();
        CollectionUtils.AddAll(result, _currentDirList.Where(entry => entry.IsFile).Select(fileEntry =>
            new ZipResourceAccessor(_zipProvider, _zipProxy, ToProviderPath(StringUtils.RemoveSuffixIfPresent(fileEntry.Name, "/")))));
        return result;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("ZipResourceAccessor: Error reading files of '{0}'", e, CanonicalLocalResourcePath);
        return null;
      }
    }

    public ICollection<IFileSystemResourceAccessor> GetChildDirectories()
    {
      if (string.IsNullOrEmpty(_pathToDirOrFile))
        return null;
      try
      {
        ICollection<IFileSystemResourceAccessor> result = new List<IFileSystemResourceAccessor>();
        CollectionUtils.AddAll(result, _currentDirList.Where(entry => entry.IsDirectory).Select(directoryEntry =>
            new ZipResourceAccessor(_zipProvider, _zipProxy, ToProviderPath(StringUtils.CheckSuffix(directoryEntry.Name, "/")))));
        return result;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("ZipResourceAccessor: Error reading child directories of '{0}'", e, CanonicalLocalResourcePath);
        return null;
      }
    }

    public IResourceAccessor Clone()
    {
      return new ZipResourceAccessor(_zipProvider, _zipProxy, _pathToDirOrFile);
    }

    #endregion

    #region ILocalFsResourceAccessor implementation

    public string LocalFileSystemPath
    {
      get
      {
        PrepareStreamAccess();
        return _tempFileName;
      }
    }

    public IDisposable EnsureLocalFileSystemAccess()
    {
      // Nothing to do here; access to the resource is ensured as of accessing the
      // LocalFileSystemPath property at the latest.
      return null;
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return CanonicalLocalResourcePath.ToString();
    }

    #endregion
  }
}
