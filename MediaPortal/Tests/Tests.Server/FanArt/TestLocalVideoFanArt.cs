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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.ResourceAccess;
using NUnit.Framework;
using Tests.Server.FanArt.FanArtHandlersForTests;
using Tests.Server.FanArt.MockFanArtAccess;

namespace Tests.Server.FanArt
{
  [TestFixture]
  public class TestLocalVideoFanArt
  {
    MockResourceAccess _mockResourceAccess;
    MockFanArtCache _fanArtCache;

    [OneTimeSetUp]
    public void Setup()
    {
      _mockResourceAccess = new MockResourceAccess();
      _mockResourceAccess.Provider.AddDirectory("Videos/TestVideos", new[] { "thumb.png", "video.mkv" });
      _mockResourceAccess.Provider.AddDirectory("Videos/TestVideos/ExtraFanArt", new[] { "fanart.png" });

      _fanArtCache = new MockFanArtCache();
      ServiceRegistration.Set<IFanArtCache>(_fanArtCache);
    }

    [Test]
    public void TestFanArtExtractVideoFolderFanArt()
    {
      //Arrange
      _fanArtCache.Clear();
      Guid videoId = Guid.NewGuid();
      VideoFanArtHandlerForTests fh = new VideoFanArtHandlerForTests();

      //Act
      fh.TestExtractVideoFolderFanArt(videoId, ResourcePath.BuildBaseProviderPath(MockResourceProvider.PROVIDER_ID, "Videos/TestVideos/video.mkv")).Wait();

      //Assert
      List<string> fanart;
      Assert.IsTrue(_fanArtCache.FanArt.TryGetValue(videoId, out fanart));
      ICollection<string> fanartBasePaths = fanart.Select(p => ResourcePath.Deserialize(p).BasePathSegment.Path).ToList();
      CollectionAssert.Contains(fanartBasePaths, "Videos/TestVideos/thumb.png");
      CollectionAssert.Contains(fanartBasePaths, "Videos/TestVideos/ExtraFanArt/fanart.png");
    }
  }
}
