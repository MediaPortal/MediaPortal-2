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

using MP2BootstrapperApp.BundlePackages;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using WixToolset.Mba.Core;

namespace Tests.Mocks
{
  public class MockBundlePackageFactory : BundlePackageFactory
  {
    public static string GetTestPackageXml()
    {
      using (StreamReader resourceStream = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Tests.Resources.Packages.BootstrapperApplicationData.xml")))
        return resourceStream.ReadToEnd();
    }

    public static IList<IBundlePackage> CreateCurrentInstall(IEnumerable<PackageId> installedPackages = null, IEnumerable<string> installedFeatures = null, IEnumerable<PackageId> falseInstallConditionPackages = null)
    {
      string xml = GetTestPackageXml();
      MockBundlePackageFactory testBundlePackageFactory = new MockBundlePackageFactory(installedPackages, installedFeatures, null, falseInstallConditionPackages);
      return testBundlePackageFactory.CreatePackagesFromXmlString(xml);
    }

    public static IList<IBundlePackage> CreatePreviousInstall(string previousInstalledVersion, IEnumerable<PackageId> installedPackages = null, IEnumerable<string> installedFeatures = null)
    {
      string xml = GetTestPackageXml();
      MockBundlePackageFactory testBundlePackageFactory = new MockBundlePackageFactory(installedPackages, installedFeatures, previousInstalledVersion);
      return testBundlePackageFactory.CreatePackagesFromXmlString(xml);
    }

    protected IEnumerable<PackageId> _installedPackages;
    protected IEnumerable<string> _installedFeatures;
    protected IEnumerable<PackageId> _falseInstallConditionPackages;
    protected string _previousInstalledVersion;

    public MockBundlePackageFactory(IEnumerable<PackageId> installedPackages = null, IEnumerable<string> installedFeatures = null, string previousInstalledVersion = null, IEnumerable<PackageId> falseInstallConditionPackages = null)
      : base(new MockFeatureMetadataProvider())
    {
      _installedPackages = installedPackages ?? new PackageId[0];
      _installedFeatures = installedFeatures ?? new string[0];
      _falseInstallConditionPackages = falseInstallConditionPackages ?? new PackageId[0];
      _previousInstalledVersion = previousInstalledVersion;
    }

    public override IBundlePackage CreatePackage(XElement packageElement)
    {
      IBundlePackage bundlePackage = base.CreatePackage(packageElement);
      IBundlePackage package;
      if (bundlePackage is IBundleMsiPackage msiPackage)
      {
        IBundleMsiPackage mockMsiPackage = Substitute.For<IBundleMsiPackage>();
        List<IBundlePackageFeature> features = new List<IBundlePackageFeature>();
        mockMsiPackage.Features.Returns(features);
        package = mockMsiPackage;
      }
      else
      {
        package = Substitute.For<IBundlePackage>();
      }

      package.PackageId.Returns(bundlePackage.PackageId);
      package.Id.Returns(bundlePackage.Id);
      package.EvaluatedInstallCondition.Returns(!_falseInstallConditionPackages.Contains(bundlePackage.PackageId));
      package.Vital.Returns(bundlePackage.Vital);
      if (_previousInstalledVersion == null)
      {
        package.CurrentInstallState.Returns(_installedPackages.Contains(bundlePackage.PackageId) ? PackageState.Present : PackageState.Absent);
      }
      else
      {
        package.CurrentInstallState.Returns(PackageState.Absent);
        package.InstalledVersion.Returns(_installedPackages.Contains(bundlePackage.PackageId) ? _previousInstalledVersion : null);
      }
      return package;
    }

    public override IBundlePackageFeature CreatePackageFeature(XElement featureElement)
    {
      IBundlePackageFeature bundleFeature = base.CreatePackageFeature(featureElement);
      IBundlePackageFeature feature = Substitute.For<IBundlePackageFeature>();
      feature.Id.Returns(bundleFeature.Id);
      feature.Package.Returns(bundleFeature.Package);
      feature.Parent.Returns(bundleFeature.Parent);
      feature.Attributes.Returns(bundleFeature.Attributes);
      feature.RelatedFeatures.Returns(bundleFeature.RelatedFeatures);
      feature.ConflictingFeatures.Returns(bundleFeature.ConflictingFeatures);
      if (_previousInstalledVersion == null)
      {
        feature.CurrentFeatureState.Returns(_installedFeatures.Contains(bundleFeature.Id) ? FeatureState.Local : FeatureState.Absent);
      }
      else
      {
        feature.PreviousVersionInstalled.Returns(_installedFeatures.Contains(bundleFeature.Id));
      }
      return feature;
    }
  }
}
