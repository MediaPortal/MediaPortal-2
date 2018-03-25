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
using System.Net;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Utilities;
using Moq;

namespace Tests.Server.FanArt.MockFanArtAccess
{
  class MockResourceAccess
  {
    MockResourceProvider _provider;

    public MockResourceAccess()
    {
      _provider = new MockResourceProvider();

      var resourceProviders = new Dictionary<Guid, IResourceProvider>();
      resourceProviders.Add(MockResourceProvider.PROVIDER_ID, _provider);

      var mockMediaAccessor = new Mock<IMediaAccessor>();
      mockMediaAccessor.Setup(x => x.LocalResourceProviders).Returns(resourceProviders);

      var mockSystemResolver = new Mock<ISystemResolver>();
      mockSystemResolver.Setup(s => s.GetSystemNameForSystemId(It.IsAny<string>())).Returns(new SystemName(IPAddress.Loopback.ToString()));
      
      ServiceRegistration.Set<IMediaAccessor>(mockMediaAccessor.Object);
      ServiceRegistration.Set<ISystemResolver>(mockSystemResolver.Object);
    }

    public MockResourceProvider Provider => _provider;
  }

  public delegate bool CreateDelegate(string path, out IResourceAccessor accessor);

  public class MockResourceProvider : IBaseResourceProvider
  {
    public static readonly Guid PROVIDER_ID = Guid.Parse("{8B2C327C-9436-41B4-8A58-AB06617F57D3}");

    IDictionary<string, IFileSystemResourceAccessor> _accessors = new Dictionary<string, IFileSystemResourceAccessor>();

    ResourceProviderMetadata _metadata = new ResourceProviderMetadata(PROVIDER_ID, "TestResourceProvider", "", false, false);

    public void AddDirectory(string directory, IEnumerable<string> files)
    {
      directory = StringUtils.RemoveSuffixIfPresent(directory, "/");
      ResourcePath directoryPath = ResourcePath.BuildBaseProviderPath(PROVIDER_ID, directory);
      IEnumerable<IFileSystemResourceAccessor> fileAccessors = files.Select(f => new MockFileSystemAccessor(ResourcePathHelper.Combine(directoryPath, f), this, null));
      _accessors.Add(directory, new MockFileSystemAccessor(directoryPath, this, fileAccessors.ToList()));
    }

    public void Clear()
    {
      _accessors.Clear();
    }

    public ResourceProviderMetadata Metadata => _metadata;

    public ResourcePath ExpandResourcePathFromString(string pathStr)
    {
      throw new NotImplementedException();
    }

    public bool IsResource(string path)
    {
      path = StringUtils.RemoveSuffixIfPresent(path, "/");
      return _accessors.ContainsKey(path);
    }

    public bool TryCreateResourceAccessor(string path, out IResourceAccessor result)
    {
      path = StringUtils.RemoveSuffixIfPresent(path, "/");
      if (_accessors.TryGetValue(path, out IFileSystemResourceAccessor fsra))
      {
        result = fsra;
        return true;
      }
      result = null;
      return false;
    }
  }

  public class MockFileSystemAccessor : IFileSystemResourceAccessor
  {
    ResourcePath _path;
    IBaseResourceProvider _provider;
    ICollection<IFileSystemResourceAccessor> _files;
    bool _isFile;
    bool _exists = true;

    public MockFileSystemAccessor(ResourcePath path, IBaseResourceProvider provider, ICollection<IFileSystemResourceAccessor> files)
    {
      _path = path;
      _provider = provider;
      _files = files;
      _isFile = files == null;
    }

    public bool Exists => _exists;

    public bool IsFile => _isFile;

    public DateTime LastChanged => throw new NotImplementedException();

    public long Size => throw new NotImplementedException();

    public IResourceProvider ParentProvider => _provider;

    public string Path => _path.ToString();

    public string ResourceName => _path.FileName;

    public string ResourcePathName => throw new NotImplementedException();

    public ResourcePath CanonicalLocalResourcePath => _path;

    public IResourceAccessor Clone()
    {
      return this;
    }

    public void Dispose()
    {

    }

    public ICollection<IFileSystemResourceAccessor> GetChildDirectories()
    {
      throw new NotImplementedException();
    }

    public ICollection<IFileSystemResourceAccessor> GetFiles()
    {
      return _files ?? new List<IFileSystemResourceAccessor>();
    }

    public IFileSystemResourceAccessor GetResource(string path)
    {
      IResourceAccessor result;
      if (_provider.TryCreateResourceAccessor(ProviderPathHelper.Combine(_path.BasePathSegment.Path, path), out result))
        return (IFileSystemResourceAccessor)result;
      return null;
    }

    public Stream OpenRead()
    {
      throw new NotImplementedException();
    }

    public Task<Stream> OpenReadAsync()
    {
      throw new NotImplementedException();
    }

    public Stream OpenWrite()
    {
      throw new NotImplementedException();
    }

    public void PrepareStreamAccess()
    {
      throw new NotImplementedException();
    }

    public bool ResourceExists(string path)
    {
      if (string.IsNullOrEmpty(path))
        return false;
      path = ProviderPathHelper.Combine(_path.BasePathSegment.Path, path);
      return _provider.IsResource(path);
    }
  }
}
