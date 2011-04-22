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
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Exceptions;
using ICSharpCode.SharpZipLib.Zip;

namespace MediaPortal.Extensions.MediaProviders.ZipMediaProvider
{
  class ZipResourceAccessor : IFileSystemResourceAccessor
  {
    #region Protected fields

    protected ZipMediaProvider _provider;
    protected IResourceAccessor _zipPath;
    protected string _pathOrFile;

    protected bool _isDirectory;
    protected string _resourceName;
    protected string _resourcePath;
    protected DateTime _lastChanged;
    protected long _size;
    protected List<ZipEntry> _currentDirList = new List<ZipEntry>();

    protected string _tempPathFile = string.Empty;
    #endregion

    #region Ctor

    public ZipResourceAccessor(ZipMediaProvider provider, IResourceAccessor accessor, string pathOrFile)
    {
      _provider = provider;
      _zipPath = accessor;
      _pathOrFile = pathOrFile;

      string path = _pathOrFile;
      if (path.StartsWith("/"))
        path = path.Substring(1);
      // default is root config
      _isDirectory = true;
      _resourceName = Path.GetFileName(_zipPath.ResourceName);
      _resourcePath = _pathOrFile == "/" ? "/" : path;
      _lastChanged = DateTime.MinValue;
      _size = -1;

      ReadCurrentDirectory();

      if (!IsEmptyOrRoot)
      {
        ZipFile zFile = new ZipFile(_zipPath.ResourcePathName);
        foreach (ZipEntry entry in zFile)
        {
          if (entry.Name.Equals(path, StringComparison.OrdinalIgnoreCase))
          {
            _isDirectory = entry.IsDirectory;
            if ((entry.IsDirectory) && (entry.Name.EndsWith("/")))
              _resourceName = Path.GetFileName(entry.Name.Substring(0, entry.Name.Length-1));
            else 
              _resourceName = Path.GetFileName(entry.Name);
            _lastChanged = entry.DateTime;
            _size = entry.Size;
            break;
          }
        }
      }
    }

    private void ReadCurrentDirectory()
    {
      int rootCount = CountChar(_pathOrFile, '/');

      string path = _pathOrFile;
      if (path.StartsWith("/"))
        path = path.Substring(1);
      if (path.EndsWith("/"))
        path = path.Substring(0, path.Length - 1);

      _currentDirList.Clear();
      ZipFile zFile = new ZipFile(_zipPath.ResourcePathName);
      foreach (ZipEntry entry in zFile)
      {
        if (entry.IsDirectory)
        {
          int zipCount = CountChar(entry.Name, '/');
          if (zipCount == rootCount)
            _currentDirList.Add(entry);
        }
        else
        {
          string p = Path.GetDirectoryName(entry.Name);
          if (p != null)
            p = p.Replace('\\', '/');
          if (path.Equals(p, StringComparison.OrdinalIgnoreCase))
          {
            _currentDirList.Add(entry);
          }
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

    #region Implementation of IDisposable

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <filterpriority>2</filterpriority>
    public void Dispose()
    {
      if (!string.IsNullOrEmpty(_tempPathFile))
      {
        File.Delete(_tempPathFile);
        _tempPathFile = string.Empty;
      }
    }

    #endregion

    #region Protected methods
    
    protected bool IsEmptyOrRoot
    {
      get { return (string.IsNullOrEmpty(_pathOrFile) || _pathOrFile == "/"); }
    }
    
    #endregion

    #region Implementation of IResourceAccessor

    /// <summary>
    /// Returns the media provider which provides this resource, if available. If this resource accessor is not hosted
    /// by a media provider, this property returns <c>null</c>.
    /// </summary>
    public IMediaProvider ParentProvider
    {
      get { return _provider; }
    }


    /// <summary>
    /// Returns the information if this resource is a directory which might contain files and sub directories.
    /// </summary>
    /// <value><c>true</c>, if this resource denotes a directory.</value>
    public bool IsDirectory
    {
      get
      {
        return _isDirectory;
      }
    }

    /// <summary>
    /// Returns the information if this resource is a file which can be opened to an input stream.
    /// </summary>
    /// <value><c>true</c>, if this resource denotes a file which can be opened, else <c>false</c>.</value>
    public bool IsFile
    {
      get
      {
        return !_isDirectory;
      }
    }

    /// <summary>
    /// Returns a short, human readable name for this resource.
    /// </summary>
    /// <value>A human readable name of this resource. For a filesystem resource accessor,
    /// this could be the file name or directory name, for example.</value>
    public string ResourceName
    {
      get { return _resourceName; }
    }

    /// <summary>
    /// Returns the full human readable path name for this resource.
    /// </summary>
    /// <value>A human readable name of this resource. For a filesystem resource accessor,
    /// this could be the file path, for example.</value>
    public string ResourcePathName
    {
      get { return _resourcePath; }
    }

    /// <summary>
    /// Returns the technical resource path which points to this resource.
    /// </summary>
    public ResourcePath LocalResourcePath
    {
      get
      {
        ResourcePath resourcePath = _zipPath.LocalResourcePath;
        if (IsEmptyOrRoot)
          resourcePath.Append(ZipMediaProvider.ZIP_MEDIA_PROVIDER_ID, "/");
        else
          resourcePath.Append(ZipMediaProvider.ZIP_MEDIA_PROVIDER_ID, "/" + ResourcePathName);
        return resourcePath;
      }
    }

    /// <summary>
    /// Gets the date and time when this resource was changed for the last time.
    /// </summary>
    public DateTime LastChanged
    {
      get { return _lastChanged; }
    }

    /// <summary>
    /// Gets the file size in bytes, if this resource represents a file. Else returns <c>-1</c>.
    /// </summary>
    public long Size
    {
      get { return _size; }
    }

    /// <summary>
    /// Prepares this resource accessor to get a stream for the resource's contents.
    /// This might take some time, so this method might block some seconds.
    /// </summary>
    public void PrepareStreamAccess()
    {
    }

    /// <summary>
    /// Opens a stream to read this resource.
    /// </summary>
    /// <returns>Stream opened for read operations, if supported. Else, <c>null</c> is returned.</returns>
    /// <exception cref="IllegalCallException">If this resource is not a file (see <see cref="IResourceAccessor.IsFile"/>).</exception>
    public Stream OpenRead()
    {
      string path = _pathOrFile;
      if (path.StartsWith("/"))
        path = path.Substring(1);
      ZipFile zFile = new ZipFile(_zipPath.ResourcePathName);
      foreach (ZipEntry entry in zFile)
      {
        if ((entry.IsFile) && (entry.Name.Equals(path, StringComparison.OrdinalIgnoreCase)))
        {
          if (string.IsNullOrEmpty(_tempPathFile))
          {
            _tempPathFile = Path.GetTempFileName();
            using (FileStream streamWriter = File.Create(_tempPathFile))
            {
              byte[] buffer = new byte[4096];		// 4K is optimum
              StreamUtils.Copy(zFile.GetInputStream(entry), streamWriter, buffer);
            }
          }
          return File.OpenRead(_tempPathFile); 
        }
      }
      return null;
    }

    /// <summary>
    /// Opens a stream to write this resource.
    /// </summary>
    /// <returns>Stream opened for write operations, if supported. Else, <c>null</c> is returned.</returns>
    /// <exception cref="IllegalCallException">If this resource is not a file (see <see cref="IResourceAccessor.IsFile"/>).</exception>
    public Stream OpenWrite()
    {
      return null;
    }

    #endregion

    #region Implementation of IFileSystemResourceAccessor

    /// <summary>
    /// Returns the information if the resource at the given path exists in the media provider of this resource.
    /// </summary>
    /// <remarks>
    /// This method is defined in interface <see cref="IFileSystemResourceAccessor"/> rather than in interface
    /// <see cref="IMediaProvider"/> because we would need two different signatures for
    /// <see cref="IBaseMediaProvider"/> and <see cref="IChainedMediaProvider"/>, which is not convenient.
    /// Furthermore, this method supports relative paths which are related to this resource.
    /// </remarks>
    /// <param name="path">Absolute or relative path to check for a resource.</param>
    /// <returns><c>true</c> if a resource at the given path exists in the <see cref="IResourceAccessor.ParentProvider"/>,
    /// else <c>false</c>.</returns>
    public bool Exists(string path)
    {
      if (path.Equals("/") && _currentDirList.Count > 0)
        return true;
      return _currentDirList.Any(entry => (entry.IsDirectory) && (entry.Name.Equals(path, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Returns a resource which is located in the same underlaying media provider and which might be located relatively
    /// to this resource.
    /// </summary>
    /// <param name="path">Relative or absolute path which is valid in the underlaying media provider.</param>
    /// <returns>Resource accessor for the desired resource, if it exists, else <c>null</c>.</returns>
    public IResourceAccessor GetResource(string path)
    {
      return _provider.CreateResourceAccessor(_zipPath, path);
    }

    /// <summary>
    /// Returns the resource accessors for all child files of this directory resource.
    /// </summary>
    /// <returns>Collection of child resource accessors of sub files or <c>null</c>, if this resource
    /// is no directory resource or if it is invalid.</returns>
    public ICollection<IFileSystemResourceAccessor> GetFiles()
    {
      if (string.IsNullOrEmpty(_pathOrFile))
        return null;
      List<IFileSystemResourceAccessor> files = new List<IFileSystemResourceAccessor>();
      CollectionUtils.AddAll(files, _currentDirList.Where(entry => entry.IsFile).Select(fileEntry => new ZipResourceAccessor(_provider, _zipPath, "/" + fileEntry.Name)));
      return files;
    }

    /// <summary>
    /// Returns the resource accessors for all child directories of this directory resource.
    /// </summary>
    /// <returns>Collection of child resource accessors of sub directories or <c>null</c>, if
    /// this resource is no directory resource or if it is invalid in this provider.</returns>
    public ICollection<IFileSystemResourceAccessor> GetChildDirectories()
    {
      if (string.IsNullOrEmpty(_pathOrFile))
        return null;
      ICollection<IFileSystemResourceAccessor> directories = new List<IFileSystemResourceAccessor>();
      CollectionUtils.AddAll(directories, _currentDirList.Where(entry => entry.IsDirectory).Select(directoryEntry => new ZipResourceAccessor(_provider, _zipPath, "/" + directoryEntry.Name)));
      return directories;
    }

    #endregion
  }
}
