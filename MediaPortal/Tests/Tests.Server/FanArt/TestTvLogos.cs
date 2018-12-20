#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.IO;
using System.Reflection;
using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.PathManager;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Mock;
using MediaPortal.Plugins.SlimTv.SlimTvResources.FanartProvider;
using NUnit.Framework;

namespace Tests.Server.FanArt
{
  [TestFixture]
  public class TestTvLogos
  {
    [SetUp]
    public void SetUp()
    {
      MockDBUtils.Reset();
      MockCore.Reset();
      ServiceRegistration.Set<ILocalization>(new NoLocalization());
    }

    [Test]
    [TestCase(FanArtMediaTypes.ChannelTv, "ZDF HD")]
    [TestCase(FanArtMediaTypes.ChannelRadio, "Bayern 3")]
    public void TestTvLogoDownload(string channelType, string channelName)
    {
      //Arrange
      string designsFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, "Designs");
      SlimTvFanartProvider provider = new SlimTvFanartProvider(designsFolder);

      //Act
      IList<IResourceLocator> fanartResources;
      var result = provider.TryGetFanArt(channelType, FanArtTypes.Logo, channelName, 256, 256, false, out fanartResources);

      //Assert
      Assert.IsTrue(result);
      Assert.AreEqual(1, fanartResources.Count);
    }
  }
}
