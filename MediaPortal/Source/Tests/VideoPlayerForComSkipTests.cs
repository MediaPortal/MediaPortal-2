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
using DirectShow;
using MediaPortal.Common;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider;
using MediaPortal.Extensions.ResourceProviders.ZipResourceProvider;
using MediaPortal.UI.Players.Video;
using Moq;

namespace Tests
{
  public class VideoPlayerForComSkipTests : VideoPlayer
  {
    private readonly ZipResourceProvider _zipRsProvider = new ZipResourceProvider();
    readonly LocalFsResourceProvider _localRsProvider = new LocalFsResourceProvider();
    private readonly LocalFsResourceAccessor _localRsAccessor;

    private const string LOCAL_RESOURCE_PROVIDER_ID = "{E88E64A8-0233-4fdf-BA27-0B44C6A39AE9}";
    private const string ZIP_RESOURCE_PROVIDER_ID = "{6B042DB8-69AD-4B57-B869-1BCEA4E43C77}";

    private readonly string _zipResource;

    public VideoPlayerForComSkipTests(string zipResource)
    {
      _zipResource = zipResource;
      _localRsAccessor = new LocalFsResourceAccessor(_localRsProvider, "/" + Path.Combine(@"TestData\ComSkip", _zipResource + ".zip"));
    }

    public string[] GetComSkipChapters()
    {
      var mockMediaAccessor = new Mock<IMediaAccessor>();
      ServiceRegistration.Set(mockMediaAccessor.Object);
      IDictionary<Guid, IResourceProvider> resourceProviders = ResourceProviders();
      mockMediaAccessor.Setup(x => x.LocalResourceProviders).Returns(resourceProviders);

      InitMockGraph();

      _resourceAccessor = GetFileSystemRsAccessor();

      EnumerateExternalChapters();

      return _chapterNames;
    }

    private IDictionary<Guid, IResourceProvider> ResourceProviders()
    {
      IDictionary<Guid, IResourceProvider> resourceProviders = new Dictionary<Guid, IResourceProvider>();

      Guid localResourceId = new Guid(LOCAL_RESOURCE_PROVIDER_ID);
      Guid zipResourceId = new Guid(ZIP_RESOURCE_PROVIDER_ID);

      resourceProviders.Add(localResourceId, _localRsProvider);
      resourceProviders.Add(zipResourceId, _zipRsProvider);

      return resourceProviders;
    }

    private IFileSystemResourceAccessor GetFileSystemRsAccessor()
    {
      IFileSystemResourceAccessor fileSystemResourceAccessor;

      string videoFilename = Path.Combine(_zipResource + ".mkv");
      _zipRsProvider.TryChainUp(_localRsAccessor, "/" + videoFilename, out fileSystemResourceAccessor);

      return fileSystemResourceAccessor;
    }

    private void InitMockGraph()
    {
      var mockGraph = new Mock<IGraphBuilder>();
      _graphBuilder = mockGraph.Object;
      _initialized = true;
    }

    protected override bool EnumerateInternalChapters()
    {
      return false;
    }
  }
}
