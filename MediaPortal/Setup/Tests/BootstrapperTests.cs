using MP2BootstrapperApp.ChainPackages;
using MP2BootstrapperApp.Models;
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
        <WixPackageProperties Package=""LAVFilters"" Vital=""yes"" DisplayName=""LAV Filters"" Description=""LAV Filters Setup"" DownloadSize=""10746592"" PackageSize=""10746592"" InstalledSize=""10746592"" PackageType=""Exe"" Permanent=""yes"" LogPathVariable=""WixBundleLog_LAVFilters"" RollbackLogPathVariable=""WixBundleRollbackLog_LAVFilters"" Compressed=""no"" DisplayInternalUI=""no"" Version=""0.74.1.0"" InstallCondition=""(NOT LAVFilters_Version &gt;= v0.74.1.0) OR (NOT LAVFilters_Version)"" Cache=""yes"" />
      </root>";

      XDocument doc = XDocument.Parse(packageXml);
      const string wixPackageProperties = "WixPackageProperties";
      XElement packageElement = doc?.Descendants(wixPackageProperties).FirstOrDefault();

      BundlePackage bundlePackage = new BundlePackage(packageElement);

      Assert.Equal("LAVFilters", bundlePackage.Id);
      Assert.Equal("LAV Filters", bundlePackage.DisplayName);
      Assert.Equal("LAV Filters Setup", bundlePackage.Description);
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

      BundlePackageFeature bundlePackageFeature = new BundlePackageFeature(featureElement);

      Assert.Equal("MediaPortal2", bundlePackageFeature.Package);
      Assert.Equal(FeatureId.Client, bundlePackageFeature.Id);
      Assert.Equal("Client", bundlePackageFeature.FeatureName);
      Assert.Equal("Client Title", bundlePackageFeature.Title);
      Assert.Equal("The user interface. Plays media files.", bundlePackageFeature.Description);
    }
  }
}
