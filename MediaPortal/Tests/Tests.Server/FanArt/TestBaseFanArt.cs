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

using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.ResourceAccess;
using NUnit.Framework;
using Tests.Server.FanArt.FanArtHandlersForTests;
using Tests.Server.FanArt.MockFanArtAccess;

namespace Tests.Server.FanArt
{
  [TestFixture]
  public class TestBaseFanArt
  {
    #region Test data

    static string[] TEST_FANART_PATHS_JPG =
    {
      "Test/thumb.jpg",
      "Test/poster.jpg",
      "Test/folder.jpg",
      "Test/cover.jpg",
      "Test/logo.jpg",
      "Test/clearart.jpg",
      "Test/cdart.jpg",
      "Test/discart.jpg",
      "Test/disc.jpg",
      "Test/banner.jpg",
      "Test/backdrop.jpg",
      "Test/fanart.jpg",
    };

    static string[] TEST_FANART_PATHS_PNG =
    {
      "Test/thumb.png",
      "Test/poster.png",
      "Test/folder.png",
      "Test/cover.png",
      "Test/logo.png",
      "Test/clearart.png",
      "Test/cdart.png",
      "Test/discart.png",
      "Test/disc.png",
      "Test/banner.png",
      "Test/backdrop.png",
      "Test/fanart.png",
    };

    static string[] TEST_FANART_PATHS_TBN =
    {
      "Test/thumb.tbn",
      "Test/poster.tbn",
      "Test/folder.tbn",
      "Test/cover.tbn",
      "Test/logo.tbn",
      "Test/clearart.tbn",
      "Test/cdart.tbn",
      "Test/discart.tbn",
      "Test/disc.tbn",
      "Test/banner.tbn",
      "Test/backdrop.tbn",
      "Test/fanart.tbn",
    };

    static string[] NAMED_TEST_FANART_PATHS_JPG =
    {
      "Test/name-thumb.jpg",
      "Test/name-poster.jpg",
      "Test/name-folder.jpg",
      "Test/name-cover.jpg",
      "Test/name-logo.jpg",
      "Test/name-clearart.jpg",
      "Test/name-cdart.jpg",
      "Test/name-discart.jpg",
      "Test/name-disc.jpg",
      "Test/name-banner.jpg",
      "Test/name-backdrop.jpg",
      "Test/name-fanart.jpg",
    };

    static string[] NAMED_TEST_FANART_PATHS_PNG =
    {
      "Test/name-thumb.png",
      "Test/name-poster.png",
      "Test/name-folder.png",
      "Test/name-cover.png",
      "Test/name-logo.png",
      "Test/name-clearart.png",
      "Test/name-cdart.png",
      "Test/name-discart.png",
      "Test/name-disc.png",
      "Test/name-banner.png",
      "Test/name-backdrop.png",
      "Test/name-fanart.png",
    };

    static string[] NAMED_TEST_FANART_PATHS_TBN =
    {
      "Test/name-thumb.tbn",
      "Test/name-poster.tbn",
      "Test/name-folder.tbn",
      "Test/name-cover.tbn",
      "Test/name-logo.tbn",
      "Test/name-clearart.tbn",
      "Test/name-cdart.tbn",
      "Test/name-discart.tbn",
      "Test/name-disc.tbn",
      "Test/name-banner.tbn",
      "Test/name-backdrop.tbn",
      "Test/name-fanart.tbn",
    };

    #endregion

    static object[] FanArtPathsTestCases = new[]
    {
      new object[]{ TEST_FANART_PATHS_JPG, 12 },
      new object[]{ TEST_FANART_PATHS_PNG, 12 },
      new object[]{ TEST_FANART_PATHS_TBN, 12 }
    };

    [Test]
    [TestCaseSource("FanArtPathsTestCases")]
    public void TestFanArtGetAllFolderFanArt(string[] paths, int expectedCount)
    {
      //Arrange
      List<ResourcePath> resourcePaths = paths.Select(p => ResourcePath.BuildBaseProviderPath(MockResourceProvider.PROVIDER_ID, p)).ToList();
      BaseFanArtHandlerForTests fh = new BaseFanArtHandlerForTests();

      //Act
      var fanart = fh.TestGetAllFolderFanArt(resourcePaths);
      
      //Assert
      Assert.AreEqual(expectedCount, fanart.Sum(kvp => kvp.Value.Count));
    }

    static object[] NamedFanArtPathsTestCases = new[]
    {
      new object[]{ NAMED_TEST_FANART_PATHS_JPG, 12 },
      new object[]{ NAMED_TEST_FANART_PATHS_PNG, 12 },
      new object[]{ NAMED_TEST_FANART_PATHS_TBN, 12 }
    };

    [Test]
    [TestCaseSource("NamedFanArtPathsTestCases")]
    public void TestFanArtGetAllNamedFolderFanArt(string[] paths, int expectedCount)
    {
      //Arrange
      List<ResourcePath> resourcePaths = paths.Select(p => ResourcePath.BuildBaseProviderPath(MockResourceProvider.PROVIDER_ID, p)).ToList();
      BaseFanArtHandlerForTests fh = new BaseFanArtHandlerForTests();

      //Act
      var fanart = fh.TestGetAllFolderFanArt(resourcePaths, "Name");

      //Assert
      Assert.AreEqual(expectedCount, fanart.Sum(kvp => kvp.Value.Count));
    }
  }
}
