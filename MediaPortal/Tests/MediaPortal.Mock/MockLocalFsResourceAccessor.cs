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