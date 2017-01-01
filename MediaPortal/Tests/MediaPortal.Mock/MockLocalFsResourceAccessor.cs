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
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider;

namespace Test.OnlineLibraries
{
  public class MockLocalFsResourceAccessor : IFileSystemResourceAccessor
  {
    private string _filename;

    public MockLocalFsResourceAccessor(string filename)
    {
      _filename = filename;
    }
    public bool Exists
    {
      get { throw new NotImplementedException(); }
    }

    public bool IsFile
    {
      get { return true; }
    }

    public DateTime LastChanged
    {
      get { throw new NotImplementedException(); }
    }

    public long Size
    {
      get { throw new NotImplementedException(); }
    }

    public bool ResourceExists(string path)
    {
      throw new NotImplementedException();
    }

    public IFileSystemResourceAccessor GetResource(string path)
    {
      throw new NotImplementedException();
    }

    public ICollection<IFileSystemResourceAccessor> GetFiles()
    {
      throw new NotImplementedException();
    }

    public ICollection<IFileSystemResourceAccessor> GetChildDirectories()
    {
      throw new NotImplementedException();
    }

    public void PrepareStreamAccess()
    {
      throw new NotImplementedException();
    }

    public Stream OpenRead()
    {
      throw new NotImplementedException();
    }

    public System.Threading.Tasks.Task<Stream> OpenReadAsync()
    {
      throw new NotImplementedException();
    }

    public Stream OpenWrite()
    {
      throw new NotImplementedException();
    }

    public IResourceProvider ParentProvider
    {
      get { throw new NotImplementedException(); }
    }

    public string Path
    {
      get { throw new NotImplementedException(); }
    }

    public string ResourceName
    {
      get { throw new NotImplementedException(); }
    }

    public string ResourcePathName
    {
      get { throw new NotImplementedException(); }
    }

    public ResourcePath CanonicalLocalResourcePath
    {
      get { return ResourcePath.BuildBaseProviderPath(LocalFsResourceProvider.LOCAL_FS_RESOURCE_PROVIDER_ID, _filename); }
    }

    public IResourceAccessor Clone()
    {
      throw new NotImplementedException();
    }

    public void Dispose()
    {
      throw new NotImplementedException();
    }
  }
}