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
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.Utilities;
using ICSharpCode.SharpZipLib.Zip;
using MediaPortal.Utilities.FileSystem;

namespace MediaPortal.Extensions.MediaProviders.ZipMediaProvider
{
  class ZipResourceAccessor : IFileSystemResourceAccessor
  {
    #region Protected fields

    protected ZipMediaProvider _zipProvider;
    protected IResourceAccessor _zipResourceAccessor;
    protected string _pathToDirOrFile;

    protected bool _isDirectory;
    protected string _resourceName;
    protected string _resourcePath;
    protected DateTime _lastChanged;
    protected long _size;
    protected List<ZipEntry> _currentDirList = new List<ZipEntry>();

    protected string _tempPathFile = string.Empty;
    #endregion

    #region Ctor

    public ZipResourceAccessor(ZipMediaProvider zipProvider, IResourceAccessor accessor, string pathFile)
    {
      _zipProvider = zipProvider;
      _zipResourceAccessor = accessor;
      _pathToDirOrFile = LocalFsMediaProviderBase.ToProviderPath(pathFile);

      // default is root config
      _isDirectory = true;
      _resourceName = Path.GetFileName(_zipResourceAccessor.ResourceName);
      _resourcePath = _pathToDirOrFile == "/" ? "/" : LocalFsMediaProviderBase.ToDosPath(_pathToDirOrFile);
      _lastChanged = DateTime.MinValue;
      _size = -1;

      ReadCurrentDirectory();

      if (!IsEmptyOrRoot)
      {
        string path = StringUtils.RemovePrefixIfPresent(_pathToDirOrFile, "/");

        ZipFile zFile = new ZipFile(_zipResourceAccessor.ResourcePathName);
        foreach (ZipEntry entry in zFile)
        {
          if (entry.Name == path)
          {
            _isDirectory = entry.IsDirectory;
            _resourceName = Path.GetFileName(StringUtils.RemoveSuffixIfPresent(path, "/"));
            _lastChanged = entry.DateTime;
            _size = entry.Size;
            break;
          }
        }
      }
    }

    private void ReadCurrentDirectory()
    {
      int rootCount = CountChar(_pathToDirOrFile, '/');

      string path = StringUtils.RemoveSuffixIfPresent(_pathToDirOrFile, "/");
      path = StringUtils.RemovePrefixIfPresent(path, "/");

      _currentDirList.Clear();
      ZipFile zFile = new ZipFile(_zipResourceAccessor.ResourcePathName);
      foreach (ZipEntry entry in zFile)
      {
        if (entry.IsDirectory)
        {
          int zipCount = CountChar(entry.Name, '/');
          if (zipCount == rootCount && entry.Name.StartsWith(path))
            _currentDirList.Add(entry);
        }
        else
        {
          string dirName = Path.GetDirectoryName(entry.Name);
          dirName = dirName == null ? null : dirName.Replace('\\', '/');
          if (path == dirName)
            _currentDirList.Add(entry);
        }
      }
    }

    private static int CountChar(string Name, char c)
    {
      if (string.IsNullOrEmpty(Name))
        return 0;
      return Name.Count(t => t == c);
    }

    #endregion

    ~ZipResourceAccessor()
    {
      Dispose();
    }

    #region IDisposable implementation

    public void Dispose()
    {
      if (!string.IsNullOrEmpty(_tempPathFile))
      {
        try
        {
          File.Delete(_tempPathFile);
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("ZipResourceAccessor: Could not delete temp file '{0}'", e, _tempPathFile);
        }
        _tempPathFile = string.Empty;
      }
    }

    #endregion

    #region Protected methods

    protected bool IsEmptyOrRoot
    {
      get { return (string.IsNullOrEmpty(_pathToDirOrFile) || _pathToDirOrFile == "/"); }
    }

    #endregion

    #region IResourceAccessor implementation

    public IMediaProvider ParentProvider
    {
      get { return _zipProvider; }
    }

    public bool Exists
    {
      get { return _zipResourceAccessor.Exists; }
    }

    public bool IsDirectory
    {
      get { return _isDirectory; }
    }

    public bool IsFile
    {
      get { return !_isDirectory; }
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
        ResourcePath resourcePath = _zipResourceAccessor.LocalResourcePath;
        resourcePath.Append(ZipMediaProvider.ZIP_MEDIA_PROVIDER_ID, _pathToDirOrFile);
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
      if (!string.IsNullOrEmpty(_tempPathFile))
        return;
      string path = _pathToDirOrFile;
      if (path.StartsWith("/"))
        path = path.Substring(1);
      ZipFile zFile = new ZipFile(_zipResourceAccessor.ResourcePathName);
      foreach (ZipEntry entry in zFile)
      {
        if (entry.IsFile && entry.Name == path)
        {
          if (string.IsNullOrEmpty(_tempPathFile))
          {
            _tempPathFile = FileUtils.CreateHumanReadableTempFilePath(_pathToDirOrFile);
            using (FileStream streamWriter = File.Create(_tempPathFile))
            {
              byte[] buffer = new byte[4096];		// 4K is optimum
              StreamUtils.Copy(zFile.GetInputStream(entry), streamWriter, buffer);
            }
          }
          return;
        }
      }
    }

    public Stream OpenRead()
    {
      if (string.IsNullOrEmpty(_tempPathFile))
        PrepareStreamAccess();
      return File.OpenRead(_tempPathFile);
    }

    public Stream OpenWrite()
    {
      return null;
    }

    #endregion

    #region IFileSystemResourceAccessor implementation

    public bool ResourceExists(string path)
    {
      if (path.Equals("/") && _currentDirList.Count > 0)
        return true;
      return _currentDirList.Any(entry => entry.IsDirectory && entry.Name == path);
    }

    public IResourceAccessor GetResource(string path)
    {
      string pathFile = StringUtils.RemovePrefixIfPresent(Path.Combine(_pathToDirOrFile, path), "/");
      return _zipProvider.CreateResourceAccessor(_zipResourceAccessor, pathFile);
    }

    public ICollection<IFileSystemResourceAccessor> GetFiles()
    {
      if (string.IsNullOrEmpty(_pathToDirOrFile))
        return null;
      List<IFileSystemResourceAccessor> files = new List<IFileSystemResourceAccessor>();
      CollectionUtils.AddAll(files, _currentDirList.Where(entry => entry.IsFile).Select(fileEntry =>
          new ZipResourceAccessor(_zipProvider, _zipResourceAccessor, LocalFsMediaProviderBase.ToProviderPath(fileEntry.Name))));
      return files;
    }

    public ICollection<IFileSystemResourceAccessor> GetChildDirectories()
    {
      if (string.IsNullOrEmpty(_pathToDirOrFile))
        return null;
      ICollection<IFileSystemResourceAccessor> directories = new List<IFileSystemResourceAccessor>();
      CollectionUtils.AddAll(directories, _currentDirList.Where(entry => entry.IsDirectory).Select(directoryEntry =>
          new ZipResourceAccessor(_zipProvider, _zipResourceAccessor, LocalFsMediaProviderBase.ToProviderPath(directoryEntry.Name))));
      return directories;
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return _resourcePath;
    }

    #endregion
  }
}
