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
using System.Globalization;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.Geometries;
using Moq;
using Xunit;

namespace Tests
{
  public class ComSkipChaptersTests
  {
    [Fact]
    public void Chapters_Should_Be_Set()
    {
      // Arrange
      SetMockServices();
      string zipResource = "Video.With.Valid.ComSkip.Chapters";
      VideoPlayerForComSkipTests videoPlayer = new VideoPlayerForComSkipTests(zipResource);

      string[] expectedComSkipChapters =
      {
        "ComSkip 1 [0:00 - 0:09]",
        "ComSkip 2 [0:09 - 0:57]",
        "ComSkip 3 [0:57 - 2:08]",
        "ComSkip 4 [2:08 - 11:50]",
        "ComSkip 5 [11:50 - 14:45]",
        "ComSkip 6 [14:45 - 19:37]",
        "ComSkip 7 [19:37 - 23:29]",
        "ComSkip 8 [23:29 - 28:19]",
        "ComSkip 9 [28:19 - 34:47]"
      };

      // Act
      string[] availableChapters = videoPlayer.GetComSkipChapters();

      // Assert
      Assert.NotNull(availableChapters);
      Assert.Equal(expectedComSkipChapters, availableChapters);
    }

    [Theory]
    [InlineData("Video.With.Invalid.ComSkip.Chapters")]
    [InlineData("Video.With.Missing.ComSkip.Chapters")]
    public void Chapters_Shuld_Be_Null(string resource)
    {
      // Arrange
      SetMockServices();
      VideoPlayerForComSkipTests videoPlayer = new VideoPlayerForComSkipTests(resource);

      // Act
      string[] availableChapters = videoPlayer.GetComSkipChapters();

      // Assert
      Assert.Null(availableChapters);
    }

    private void SetMockServices()
    {
      var mockMessageBroker = new Mock<IMessageBroker>();
      var mockSystemStateService = new Mock<ISystemStateService>();
      var mockGeometryManager = new Mock<IGeometryManager>();
      var mockLolalizationService = new Mock<ILocalization>();
      mockLolalizationService.Setup(x => x.CurrentCulture).Returns(new CultureInfo("en-US"));

      ServiceRegistration.Set(mockMessageBroker.Object);
      ServiceRegistration.Set(mockSystemStateService.Object);
      ServiceRegistration.Set(mockGeometryManager.Object);
      ServiceRegistration.Set(mockLolalizationService.Object);
      ServiceRegistration.Set<ISettingsManager>(new NoSettingsManager());

      ILogger logger = new ConsoleLogger(LogLevel.Debug, true);
      ServiceRegistration.Set<ILogger>(logger);
    }
  }
}
