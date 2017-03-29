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
using System.IO;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Plugins.SlimTv.Interfaces.ResourceProvider
{
  public class SlimTvFsResourceAccessor : ILocalFsResourceAccessor
  {
    protected const int ASYNC_STREAM_BUFFER_SIZE = 4096;
    private readonly string _path;
    private readonly int _slotIndex;

    public SlimTvFsResourceAccessor(int slotIndex, string path)
    {
      _path = path;
      _slotIndex = slotIndex;
    }

    #region IResourceAccessor Member

    public IResourceProvider ParentProvider
    {
      get { return null; }
    }

    public bool Exists
    {
      get
      {
        if (string.IsNullOrEmpty(_path) || _path == "/")
          return false;
        string dosPath = LocalFsResourceProviderBase.ToDosPath(_path);
        return !string.IsNullOrEmpty(dosPath) && (File.Exists(dosPath) || Directory.Exists(dosPath));
      }
    }

    public bool IsFile
    {
      get { return true; }
    }

    public string Path
    {
      get { return _path; }
    }

    public string ResourceName
    {
      get { return System.IO.Path.GetFileName(_path); }
    }

    public string ResourcePathName
    {
      get { return _path; }
    }

    public ResourcePath CanonicalLocalResourcePath
    {
      get
      {
        // format the path with the slotindex as prefix.
        return ResourcePath.BuildBaseProviderPath(SlimTvResourceProvider.SLIMTV_RESOURCE_PROVIDER_ID, String.Format("{0}|{1}", _slotIndex, _path));
      }
    }

    public DateTime LastChanged
    {
      get
      {
        string dosPath = LocalFsResourceProviderBase.ToDosPath(_path);
        return LocalFsResourceProviderBase.GetSafeLastWriteTime(dosPath);
      }
    }

    public long Size
    {
      get
      {
        string dosPath = LocalFsResourceProviderBase.ToDosPath(_path);
        if (string.IsNullOrEmpty(dosPath) || !File.Exists(dosPath))
          return -1;
        return new FileInfo(dosPath).Length;
      }
    }

    public void PrepareStreamAccess()
    {
    }

    public Stream OpenRead()
    {
      string dosPath = LocalFsResourceProviderBase.ToDosPath(_path);
      if (string.IsNullOrEmpty(dosPath) || !File.Exists(dosPath))
        return null;
      return new FileStream(dosPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }

    public Task<Stream> OpenReadAsync()
    {
      string dosPath = LocalFsResourceProviderBase.ToDosPath(_path);
      if (string.IsNullOrEmpty(dosPath) || !File.Exists(dosPath))
        return null;
      // In this implementation there is no preparational work to do. We therefore return a
      // completed Task; there is no need for any async operation.
      return Task.FromResult((Stream)new FileStream(dosPath, FileMode.Open, FileAccess.Read, FileShare.Read, ASYNC_STREAM_BUFFER_SIZE, true));
    }

    public Stream OpenWrite()
    {
      return null;
    }

    public IResourceAccessor Clone()
    {
      return new SlimTvFsResourceAccessor(_slotIndex, _path);
    }

    #endregion

    #region IDisposable Member

    public void Dispose()
    {
      ITvHandler tv = ServiceRegistration.Get<ITvHandler>(false);
      if (tv != null)
        tv.DisposeSlot(_slotIndex);
    }

    #endregion

    #region ILocalFsResourceAccessor Member

    public string LocalFileSystemPath
    {
      get { return _path; }
    }

    public IDisposable EnsureLocalFileSystemAccess()
    {
      // Nothing to do here; access is ensured as of the instantiation of this class
      return null;
    }

    #endregion

    #region IFileSystemResourceAccessor Member

    public bool IsDirectory
    {
      get
      {
        if (_path == "/")
          return true;
        string dosPath = LocalFsResourceProviderBase.ToDosPath(_path);
        return !string.IsNullOrEmpty(dosPath) && Directory.Exists(dosPath);
      }
    }

    public bool ResourceExists(string path)
    {
      return true;
    }

    IFileSystemResourceAccessor IFileSystemResourceAccessor.GetResource(string path)
    {
      throw new NotImplementedException();
    }

    public System.Collections.Generic.ICollection<IFileSystemResourceAccessor> GetFiles()
    {
      return null;
    }

    public System.Collections.Generic.ICollection<IFileSystemResourceAccessor> GetChildDirectories()
    {
      return null;
    }

    #endregion
  }
}
