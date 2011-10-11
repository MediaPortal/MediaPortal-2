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
using ICSharpCode.SharpZipLib.Core;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.ResourceAccess;
using MediaPortal.Utilities;
using ICSharpCode.SharpZipLib.Zip;
using MediaPortal.Utilities.FileSystem;

namespace MediaPortal.Extensions.ResourceProviders.ZipResourceProvider
{
  class ZipResourceAccessor : IFileSystemResourceAccessor
  {
    #region Protected fields

    protected ZipResourceProvider _zipProvider;
    protected IResourceAccessor _baseZipResourceAccessor;
    protected Stream _zipFileStream;
    protected ZipFile _zipFile;
    protected string _pathToDirOrFile;

    // Default is root config
    protected ZipEntry _zipEntry = null;
    protected bool _isDirectory = true;
    protected DateTime _lastChanged = DateTime.MinValue;
    protected long _size = -1;
    protected string _tempFileName = null;
    protected List<ZipEntry> _currentDirList = new List<ZipEntry>();

    #endregion

    #region Ctor

    public ZipResourceAccessor(ZipResourceProvider zipProvider, IResourceAccessor accessor, string pathToDirOrFile)
    {
      _zipProvider = zipProvider;
      _baseZipResourceAccessor = accessor;
      _pathToDirOrFile = pathToDirOrFile;

      ReadCurrentDirectory();
      if (!_isDirectory && _zipEntry == null)
        throw new ArgumentException(string.Format("ZipResourceAccessor: Cannot find zip entry for path '{0}' in ZIP file '{1}'", pathToDirOrFile, _baseZipResourceAccessor));
    }

    private void ReadCurrentDirectory()
    {
      string entryPath = ZipResourceProvider.ToEntryPath(_pathToDirOrFile) ?? string.Empty;

      int dirDepth = EvaluateDirDepth(entryPath);

      _currentDirList.Clear();
      CloseZipFile();
      _zipFileStream = _baseZipResourceAccessor.OpenRead(); // Not sure if the ZipFile closes the stream appropriately, so we keep a reference to it
      _zipFile = new ZipFile(_zipFileStream);
      foreach (ZipEntry entry in _zipFile)
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

    #endregion

    protected string ExpandPath(string path)
    {
      return path.StartsWith("/") ? path : StringUtils.CheckSuffix(_pathToDirOrFile, "/") + StringUtils.RemovePrefixIfPresent(path, "/");
    }

    #region IDisposable implementation

    public void Dispose()
    {
      if (!string.IsNullOrEmpty(_tempFileName))
      {
        try
        {
          File.Delete(_tempFileName);
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("ZipResourceAccessor: Unable to delete temp file '{0}'", e, _tempFileName);
        }
        _tempFileName = null;
      }
      CloseZipFile();
      if (_baseZipResourceAccessor != null)
      {
        _baseZipResourceAccessor.Dispose();
        _baseZipResourceAccessor= null;
      }
    }

    #endregion

    #region Protected methods

    protected bool IsEmptyOrRoot
    {
      get { return (string.IsNullOrEmpty(_pathToDirOrFile) || _pathToDirOrFile == "/"); }
    }

    protected void CloseZipFile()
    {
      if (_zipFile != null)
      {
        _zipFile.Close();
        _zipFileStream.Dispose();
        _zipFile = null;
        _zipFileStream = null;
      }
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

    #region IResourceAccessor implementation

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
      get { return _zipEntry != null; }
    }

    public string ResourceName
    {
      get
      {
        if (string.IsNullOrEmpty(_pathToDirOrFile))
          return null;
        if (_pathToDirOrFile == "/")
          return _baseZipResourceAccessor.ResourceName;
        return GetFileName(StringUtils.RemoveSuffixIfPresent(_pathToDirOrFile, "/"));
      }
    }

    public string ResourcePathName
    {
      get { return _baseZipResourceAccessor.ResourcePathName + " > " + _pathToDirOrFile; }
    }

    public ResourcePath CanonicalLocalResourcePath
    {
      get
      {
        ResourcePath resourcePath = new ResourcePath(_baseZipResourceAccessor.CanonicalLocalResourcePath);
        resourcePath.Append(ZipResourceProvider.ZIP_RESOURCE_PROVIDER_ID, _pathToDirOrFile);
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
      if (!string.IsNullOrEmpty(_tempFileName))
        return;
      if (string.IsNullOrEmpty(_tempFileName))
      {
        _tempFileName = FileUtils.CreateHumanReadableTempFilePath(_pathToDirOrFile);
        using (FileStream streamWriter = File.Create(_tempFileName))
        {
          byte[] buffer = new byte[4096]; // 4K is optimum
          StreamUtils.Copy(_zipFile.GetInputStream(_zipEntry), streamWriter, buffer);
        }
      }
    }

    public Stream OpenRead()
    {
     if (string.IsNullOrEmpty(_tempFileName))
       PrepareStreamAccess();
      // We need to operate on a temporary file because the underlaying ZIP library doesn't support seeking in the returned entry stream
     return File.OpenRead(_tempFileName);
    }

    public Stream OpenWrite()
    {
      return null;
    }

    public IResourceAccessor Clone()
    {
      return new ZipResourceAccessor(_zipProvider, _baseZipResourceAccessor.Clone(), _pathToDirOrFile);
    }

    #endregion

    #region IFileSystemResourceAccessor implementation

    public bool ResourceExists(string path)
    {
      if (path.Equals("/"))
        return true;
      path = ExpandPath(path);
      return _currentDirList.Any(entry => entry.IsDirectory && entry.Name == path);
    }

    public IFileSystemResourceAccessor GetResource(string path)
    {
      string pathFile = ExpandPath(path);
      return (IFileSystemResourceAccessor) _zipProvider.CreateResourceAccessor(_baseZipResourceAccessor.Clone(), pathFile);
    }

    public ICollection<IFileSystemResourceAccessor> GetFiles()
    {
      if (string.IsNullOrEmpty(_pathToDirOrFile))
        return null;
      List<IFileSystemResourceAccessor> files = new List<IFileSystemResourceAccessor>();
      CollectionUtils.AddAll(files, _currentDirList.Where(entry => entry.IsFile).Select(fileEntry =>
          new ZipResourceAccessor(_zipProvider, _baseZipResourceAccessor.Clone(), ZipResourceProvider.ToProviderPath(fileEntry.Name))));
      return files;
    }

    public ICollection<IFileSystemResourceAccessor> GetChildDirectories()
    {
      if (string.IsNullOrEmpty(_pathToDirOrFile))
        return null;
      ICollection<IFileSystemResourceAccessor> directories = new List<IFileSystemResourceAccessor>();
      CollectionUtils.AddAll(directories, _currentDirList.Where(entry => entry.IsDirectory).Select(directoryEntry =>
          new ZipResourceAccessor(_zipProvider, _baseZipResourceAccessor.Clone(), ZipResourceProvider.ToProviderPath(directoryEntry.Name))));
      return directories;
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
