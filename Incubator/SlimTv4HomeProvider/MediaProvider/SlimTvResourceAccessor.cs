#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.Core;
using MediaPortal.Core.MediaManagement.ResourceAccess;
using MediaPortal.Plugins.SlimTvClient.Interfaces;

namespace MediaPortal.Plugins.SlimTv.Providers
{
  class SlimTvResourceAccessor : ILocalFsResourceAccessor
  {
    private readonly string _path;

    public SlimTvResourceAccessor(string path)
    {
      _path = path;
    }

    #region IResourceAccessor Member

    public IMediaProvider ParentProvider
    {
      get { return null; }
    }

    public bool IsFile
    {
      get { return true; }
    }

    public string ResourceName
    {
      get { return Path.GetFileName(_path); }
    }

    public string ResourcePathName
    {
      get { return _path; }
    }

    public ResourcePath LocalResourcePath
    {
      get { return ResourcePath.BuildBaseProviderPath(SlimTvMediaProvider.SLIMTV_MEDIA_PROVIDER_ID, _path); }
    }

    public DateTime LastChanged
    {
      get { return DateTime.Now; }
    }

    public long Size
    {
      get
      {
        try
        {
          FileInfo fi = new FileInfo(_path);
          return fi.Length;
        }
        catch
        {
          return 0;
        }
      }
    }

    public void PrepareStreamAccess()
    {
    }

    public Stream OpenRead()
    {
      return null;
    }

    public Stream OpenWrite()
    {
      return null;
    }

    #endregion

    #region IDisposable Member

    public void Dispose()
    {
      ITvHandler tv = ServiceRegistration.Get<ITvHandler>();
      if (tv != null)
        tv.StopTimeshift();
    }

    #endregion

    #region ILocalFsResourceAccessor Member

    public string LocalFileSystemPath
    {
      get { return _path; }
    }

    #endregion

    #region IFileSystemResourceAccessor Member

    public bool IsDirectory
    {
      get { return false; }
    }

    public bool Exists(string path)
    {
      return true;
    }

    public IResourceAccessor GetResource(string path)
    {
      return new SlimTvResourceAccessor(path);
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
