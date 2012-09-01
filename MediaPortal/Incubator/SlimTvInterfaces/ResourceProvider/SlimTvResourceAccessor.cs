#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Plugins.SlimTv.Interfaces.ResourceProvider
{
  // TODO: support different ResourceAccessors for either local files (single seat) or network streams (multi seat). Current implementation always uses
  // network streams, even in single seat.
  public class SlimTvResourceAccessor : INetworkResourceAccessor
  {
    private readonly string _path;
    private readonly int _slotIndex;

    public SlimTvResourceAccessor(int slotIndex, string path)
    {
      _path = path;
      _slotIndex = slotIndex;
    }

    #region Static methods

    public static INetworkResourceAccessor GetResourceAccessor(string path)
    {
      // Parse slotindex from path and cut the prefix off.
      int slotIndex;
      if (!int.TryParse(path.Substring(0, 1), out slotIndex))
        return null;
      path = path.Substring(2, path.Length - 2);

      return new SlimTvResourceAccessor(slotIndex, path);
    }

    #endregion

    #region IResourceAccessor Member

    public IResourceProvider ParentProvider
    {
      get { return null; }
    }

    // TODO: Complete implementation
    public bool Exists
    {
      get { return true; }
    }

    public bool IsFile
    {
      get { return true; }
    }

    public string Path
    {
      get { return _path; }
    }

    public string URL
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
        return ResourcePath.BuildBaseProviderPath(SlimTvResourceProvider.SLIMTV_RESOURCE_PROVIDER_ID, 
          String.Format("{0}|{1}", _slotIndex, _path));
      }
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

    public IResourceAccessor Clone()
    {
      return new SlimTvResourceAccessor(_slotIndex, _path);
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

    #endregion

    #region IFileSystemResourceAccessor Member

    public bool IsDirectory
    {
      get { return false; }
    }

    public bool ResourceExists(string path)
    {
      return true;
    }

    public INetworkResourceAccessor GetResource(string path)
    {
      return GetResourceAccessor(path);
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