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
using System.Diagnostics;
using System.Net;
using System.Xml.Linq;
using CustomActions;
using Xunit;
using Moq;

namespace Tests
{
  public class LAVFiltersTests
  {
    [Fact]
    public void Should_DownloadInstaller_LavNotInstalledOnSystem()
    {
      // Arrange
      var mockHelper = new Mock<IRunnerHelper>();
      string splitterKey = @"CLSID\{171252A0-8820-4AFE-9DF8-5C92B2D66B04}\InprocServer32";
      mockHelper.Setup(s => s.GetPathForRegistryKey(splitterKey)).Returns(string.Empty);
      string minorOnlineVersion = "69";
      mockHelper.Setup(s => s.LoadXmlDocument(It.IsAny<string>())).Returns(LavFiltersMetadataFile("0", minorOnlineVersion, "0", "0"));
      // Act
      var runner = new CustomActionRunner(mockHelper.Object);
      bool isInstalled = runner.IsLavFiltersAlreadyInstalled();

      // Assert
      Assert.Equal(isInstalled, false);
    }

    [Fact]
    public void Should_DownloadInstaller_InstalledVersionLowerThanOnline()
    {
      // Arrange
      var mockHelper = new Mock<IRunnerHelper>();
      string majorOnlineversion = "0";
      string minorOnlineVersion = "69";
      string buildOnlineVersion = "0";
      string privOnlineVersion = "42";
      mockHelper.Setup(s => s.LoadXmlDocument(It.IsAny<string>())).Returns(LavFiltersMetadataFile(majorOnlineversion, minorOnlineVersion, buildOnlineVersion, privOnlineVersion));
      string splitterKey = @"CLSID\{171252A0-8820-4AFE-9DF8-5C92B2D66B04}\InprocServer32";
      string fileName = "LavSplitter.ax";
      mockHelper.Setup(s => s.GetPathForRegistryKey(splitterKey)).Returns(fileName);

      int localMajorVersion = 0;
      int localMinorVersion = 69;
      int localBuildVersion = 0;
      int localPrivVersion = 33;
      mockHelper.Setup(s => s.GetFileMajorVersion(fileName)).Returns(localMajorVersion);
      mockHelper.Setup(s => s.GetFileMinorVersion(fileName)).Returns(localMinorVersion);
      mockHelper.Setup(s => s.GetFileBuildVersion(fileName)).Returns(localBuildVersion);
      mockHelper.Setup(s => s.GetFilePrivateVersion(fileName)).Returns(localPrivVersion);

      // Act
      var runner = new CustomActionRunner(mockHelper.Object);
      bool installed = runner.IsLavFiltersAlreadyInstalled();

      // Assert
      Assert.Equal(false, installed);
    }

    [Fact]
    public void Should_NotDownloadInstaller_InstalledVersionHigherOrEqualOnlineVersion()
    {
      // Arrange
      var mockHelper = new Mock<IRunnerHelper>();
      string minorVersion = "69";
      mockHelper.Setup(s => s.LoadXmlDocument(It.IsAny<string>())).Returns(LavFiltersMetadataFile("0", minorVersion, "0", "0"));
      string splitterKey = @"CLSID\{171252A0-8820-4AFE-9DF8-5C92B2D66B04}\InprocServer32";
      string fileName = "LavSplitter.ax";
      mockHelper.Setup(s => s.GetPathForRegistryKey(splitterKey)).Returns(fileName);
      mockHelper.Setup(s => s.GetFileMajorVersion(fileName)).Returns(0);
      int localMinorVersion = 70;
      mockHelper.Setup(s => s.GetFileMinorVersion(fileName)).Returns(localMinorVersion);
      mockHelper.Setup(s => s.GetFileBuildVersion(fileName)).Returns(0);
      mockHelper.Setup(s => s.GetFilePrivateVersion(fileName)).Returns(0);

      // Act
      var runner = new CustomActionRunner(mockHelper.Object);
      bool installed = runner.IsLavFiltersAlreadyInstalled();

      // Assert
      Assert.Equal(true, installed);
    }

    [Fact]
    public void Should_InstallPackage_DownloadWasSuccesfull()
    {
      // Arrange
      var mockHelper = new Mock<IRunnerHelper>();
      mockHelper.Setup(s => s.DownloadFileAndReleaseResources(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
      mockHelper.Setup(s => s.Exists(It.IsAny<string>())).Returns(true);

      // Act
      var runner = new CustomActionRunner(mockHelper.Object);
      bool isDownloaded = runner.IsLavFiltersDownloaded();

      // Assert
      Assert.Equal(true, isDownloaded);
    }

    [Fact]
    public void Should_NotInstallPackage_DownloadFailed()
    {
      // Arrange
      var mockHelper = new Mock<IRunnerHelper>();
      mockHelper.Setup(s => s.DownloadFileAndReleaseResources(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
      mockHelper.Setup(s => s.Exists(It.IsAny<string>())).Returns(false);

      // Act
      var runner = new CustomActionRunner(mockHelper.Object);
      bool isDownloaded = runner.IsLavFiltersDownloaded();

      // Assert
      Assert.Equal(false, isDownloaded);
    }

    [Fact]
    public void Should_InstallPackage_NoTimeout()
    {
      // Arrange
      var mockHelper = new Mock<IRunnerHelper>();
      mockHelper.Setup(s => s.Start(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())).Returns(true);

      // Act
      var runner = new CustomActionRunner(mockHelper.Object);
      bool isInstalled = runner.InstallLavFilters();

      // Assert
      Assert.Equal(true, isInstalled);
    }

    [Fact]
    public void Should_FailToInstallPackage_TimeoutOccured()
    {
      // Arrange
      var mockHelper = new Mock<IRunnerHelper>();
      mockHelper.Setup(s => s.Start(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())).Returns(false);

      // Act
      var runner = new CustomActionRunner(mockHelper.Object);
      bool isInstalled = runner.InstallLavFilters();

      // Assert
      Assert.Equal(false, isInstalled);
    }

    private XDocument LavFiltersMetadataFile(string major, string minor, string build, string priv)
    {
      return new XDocument(
        new XDeclaration("1.0", "UTF-8", "yes"),
        new XElement("LAVFilters",
          new XElement("Version",
            new XElement("Major", major),
            new XElement("Minor", minor),
            new XElement("Build", build),
            new XElement("Private", priv)))
      );
    }
  }
}
