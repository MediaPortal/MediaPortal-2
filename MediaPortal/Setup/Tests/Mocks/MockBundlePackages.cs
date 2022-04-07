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
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MP2BootstrapperApp.BundlePackages;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests.Mocks
{
  public static class MockBundlePackages
  {
    static readonly string[] TEST_FEATURES = new[] { FeatureId.MediaPortal_2, FeatureId.Client, FeatureId.Server, FeatureId.ServiceMonitor, FeatureId.LogCollector };

    public static IList<IBundlePackage> CreateCurrentInstall(IEnumerable<PackageId> installedPackages = null, IEnumerable<string> installedFeatures = null, IEnumerable<PackageId> falseInstallConditionPackages = null)
    {
      if (installedPackages == null)
        installedPackages = new PackageId[0];
      if (installedFeatures == null)
        installedFeatures = new string[0];
      if (falseInstallConditionPackages == null)
        falseInstallConditionPackages = new PackageId[0];

      installedFeatures = installedFeatures.Union(new[] { FeatureId.MediaPortal_2 });

      List<IBundlePackage> packages = new List<IBundlePackage>();
      foreach (PackageId packageId in Enum.GetValues(typeof(PackageId)))
      {
        if (packageId == PackageId.Unknown)
          continue;

        if (packageId == PackageId.MediaPortal2)
        {
          IBundleMsiPackage msiPackage = CreateMsiPackage(packageId, installedPackages.Contains(packageId), false, null);
          List<IBundlePackageFeature> features = new List<IBundlePackageFeature>();
          foreach (string featureId in TEST_FEATURES)
          {
            IBundlePackageFeature feature = CreateFeature(featureId, installedFeatures.Contains(featureId), featureId != FeatureId.MediaPortal_2, false);
            features.Add(feature);
          }
          msiPackage.Features.Returns(features);
          packages.Add(msiPackage);
        }
        else
        {
          IBundlePackage package = CreatePackage(packageId, installedPackages.Contains(packageId), packageId == PackageId.LAVFilters, null, !falseInstallConditionPackages.Contains(packageId));
          packages.Add(package);
        }
      }
      return packages;
    }

    public static IList<IBundlePackage> CreatePreviousInstall(Version previousInstalledVersion, IEnumerable<PackageId> installedPackages = null, IEnumerable<string> installedFeatures = null)
    {
      if (installedPackages == null)
        installedPackages = new PackageId[0];
      if (installedFeatures == null)
        installedFeatures = new string[0];

      installedFeatures = installedFeatures.Union(new[] { FeatureId.MediaPortal_2 });

      List<IBundlePackage> packages = new List<IBundlePackage>();
      foreach (PackageId packageId in Enum.GetValues(typeof(PackageId)))
      {
        if (packageId == PackageId.Unknown)
          continue;

        if (packageId == PackageId.MediaPortal2)
        {
          IBundleMsiPackage msiPackage = CreateMsiPackage(packageId, false, false, installedPackages.Contains(packageId) ? previousInstalledVersion : null);
          List<IBundlePackageFeature> features = new List<IBundlePackageFeature>();
          foreach (string featureId in TEST_FEATURES)
          {
            IBundlePackageFeature feature = CreateFeature(featureId, false, featureId != FeatureId.MediaPortal_2, installedFeatures.Contains(featureId));
            features.Add(feature);
          }
          msiPackage.Features.Returns(features);
          packages.Add(msiPackage);
        }
        else
        {
          IBundlePackage package = CreatePackage(packageId, false, packageId == PackageId.LAVFilters, installedPackages.Contains(packageId) ? previousInstalledVersion : null, true);
          packages.Add(package);
        }
      }
      return packages;
    }

    public static IBundlePackage CreatePackage(PackageId packageId, bool installed, bool optional, Version installedVersion, bool installCondition)
    {
      IBundlePackage package = Substitute.For<IBundlePackage>();
      package.PackageId.Returns(packageId);
      package.EvaluatedInstallCondition.Returns(installCondition);
      package.Vital.Returns(!optional);
      package.CurrentInstallState.Returns(installed ? PackageState.Present : PackageState.Absent);
      package.InstalledVersion.Returns(installedVersion);
      return package;
    }

    public static IBundleMsiPackage CreateMsiPackage(PackageId packageId, bool installed, bool optional, Version installedVersion)
    {
      IBundleMsiPackage package = Substitute.For<IBundleMsiPackage>();
      package.PackageId.Returns(packageId);
      package.EvaluatedInstallCondition.Returns(true);
      package.Vital.Returns(!optional);
      package.CurrentInstallState.Returns(installed ? PackageState.Present : PackageState.Absent);
      package.InstalledVersion.Returns(installedVersion);
      return package;
    }

    public static IBundlePackageFeature CreateFeature(string featureId, bool installed, bool optional, bool previousVersionInstalled)
    {
      IBundlePackageFeature feature = Substitute.For<IBundlePackageFeature>();
      feature.Id.Returns(featureId);
      feature.Attributes.Returns(optional ? FeatureAttributes.DisallowAdvertise : (FeatureAttributes.DisallowAdvertise | FeatureAttributes.UIDisallowAbsent));
      feature.CurrentFeatureState.Returns(installed ? FeatureState.Local : FeatureState.Absent);
      feature.PreviousVersionInstalled.Returns(previousVersionInstalled);
      return feature;
    }
  }
}
