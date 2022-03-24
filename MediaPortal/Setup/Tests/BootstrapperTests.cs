#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using Microsoft.Deployment.WindowsInstaller;
using MP2BootstrapperApp.BundlePackages;
using MP2BootstrapperApp.Models;
using System;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Tests
{
  public class BootstrapperTests
  {
    [Fact]
    public void Should_ParseBundlePackage()
    {
      const string packageXml = @"
      <root>
        <WixPackageProperties Package=""LAVFilters"" Vital=""no"" DisplayName=""LAV Filters"" Description=""LAV Filters Setup"" DownloadSize=""10746592"" PackageSize=""10746592"" InstalledSize=""10746592"" PackageType=""Exe"" Permanent=""yes"" LogPathVariable=""WixBundleLog_LAVFilters"" RollbackLogPathVariable=""WixBundleRollbackLog_LAVFilters"" Compressed=""no"" DisplayInternalUI=""no"" Version=""0.74.1.0"" InstallCondition=""(NOT LAVFilters_Version &gt;= v0.74.1.0) OR (NOT LAVFilters_Version)"" Cache=""yes"" />
      </root>";

      XDocument doc = XDocument.Parse(packageXml);
      const string wixPackageProperties = "WixPackageProperties";
      XElement packageElement = doc?.Descendants(wixPackageProperties).FirstOrDefault();

      BundlePackageFactory bundlePackageFactory = new BundlePackageFactory();
      IBundlePackage bundlePackage = bundlePackageFactory.CreatePackage(packageElement);

      Assert.Equal("LAVFilters", bundlePackage.Id);
      Assert.Equal(PackageId.LAVFilters, bundlePackage.PackageId);
      Assert.Equal("LAV Filters", bundlePackage.DisplayName);
      Assert.Equal("LAV Filters Setup", bundlePackage.Description);
      Assert.Equal(10746592, bundlePackage.InstalledSize);
      Assert.Equal(new Version("0.74.1.0"), bundlePackage.Version);
      Assert.Equal("(NOT LAVFilters_Version >= v0.74.1.0) OR (NOT LAVFilters_Version)", bundlePackage.InstallCondition);
      Assert.False(bundlePackage.Vital);
    }

    [Fact]
    public void Should_ParseBundleMsiPackage()
    {
      const string packageXml = @"
      <root>
        <WixPackageProperties Package=""MediaPortal2"" Vital=""yes"" DisplayName=""MediaPortal 2"" Description=""MediaPortal 2"" DownloadSize=""195811738"" PackageSize=""195811738"" InstalledSize=""791408691"" PackageType=""Msi"" Permanent=""no"" LogPathVariable=""WixBundleLog_MediaPortal2"" RollbackLogPathVariable=""WixBundleRollbackLog_MediaPortal2"" Compressed=""yes"" DisplayInternalUI=""no"" ProductCode=""{0E70343E-934F-4328-8891-B7BE16F57D78}"" UpgradeCode=""{9743129C-FED3-404A-A66E-3C1557BE0178}"" Version=""2.4.2202.13838"" Cache=""yes"" />
      </root>";

      XDocument doc = XDocument.Parse(packageXml);
      const string wixPackageProperties = "WixPackageProperties";
      XElement packageElement = doc?.Descendants(wixPackageProperties).FirstOrDefault();

      BundlePackageFactory bundlePackageFactory = new BundlePackageFactory();
      IBundleMsiPackage bundlePackage = bundlePackageFactory.CreatePackage(packageElement) as IBundleMsiPackage;

      Assert.NotNull(bundlePackage);
      Assert.Equal("MediaPortal2", bundlePackage.Id);
      Assert.Equal(PackageId.MediaPortal2, bundlePackage.PackageId);
      Assert.Equal("MediaPortal 2", bundlePackage.DisplayName);
      Assert.Equal("MediaPortal 2", bundlePackage.Description);
      Assert.Equal(791408691, bundlePackage.InstalledSize);
      Assert.Equal(new Version("2.4.2202.13838"), bundlePackage.Version);
      Assert.Null(bundlePackage.InstallCondition);
      Assert.True(bundlePackage.Vital);
      Assert.Equal(new Guid("0E70343E-934F-4328-8891-B7BE16F57D78"), bundlePackage.ProductCode);
      Assert.Equal(new Guid("9743129C-FED3-404A-A66E-3C1557BE0178"), bundlePackage.UpgradeCode);
    }

    [Fact]
    public void Should_ParseBundlePackageFeature()
    {
      const string packageXml = @"
      <root>
        <WixPackageFeatureInfo Package=""MediaPortal2"" Feature=""Client"" Size=""450586600"" Parent=""MediaPortal_2"" Title=""Client Title"" Description=""The user interface. Plays media files."" Display=""2"" Level=""1"" Directory=""INSTALLDIR_CLIENT"" Attributes=""8"" />
      </root>";

      XDocument doc = XDocument.Parse(packageXml);
      const string wixPackageFeatureInfo = "WixPackageFeatureInfo";
      XElement featureElement = doc?.Descendants(wixPackageFeatureInfo).FirstOrDefault();

      BundlePackageFactory bundlePackageFactory = new BundlePackageFactory();
      IBundlePackageFeature bundlePackageFeature = bundlePackageFactory.CreatePackageFeature(featureElement);

      Assert.Equal("MediaPortal2", bundlePackageFeature.Package);
      Assert.Equal(FeatureId.Client, bundlePackageFeature.Id);
      Assert.Equal("Client", bundlePackageFeature.FeatureName);
      Assert.Equal("Client Title", bundlePackageFeature.Title);
      Assert.Equal("The user interface. Plays media files.", bundlePackageFeature.Description);
      Assert.Equal(450586600, bundlePackageFeature.InstalledSize);
      Assert.False(bundlePackageFeature.Attributes.HasFlag(FeatureAttributes.UIDisallowAbsent));
    }
  }
}
