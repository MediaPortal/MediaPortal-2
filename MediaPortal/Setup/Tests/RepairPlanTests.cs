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

using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MP2BootstrapperApp.ActionPlans;
using MP2BootstrapperApp.ChainPackages;
using MP2BootstrapperApp.Models;
using System.Collections.Generic;
using System.Linq;
using Tests.Mocks;
using Xunit;

namespace Tests
{
  public class RepairPlanTests
  {
    [Fact]
    void Should_PreserveFeatureStates()
    {
      RepairPlan plan = new RepairPlan(new PlanContext());
      FeatureId[] installedFeatures = new[] { FeatureId.MediaPortal_2, FeatureId.Client };
      IList<IBundlePackage> packages = MockBundlePackages.CreateCurrentInstall(new[] { PackageId.MediaPortal2 }, installedFeatures);

      plan.SetRequestedInstallStates(packages);

      IBundleMsiPackage featurePackage = packages.First(p => p.PackageId == PackageId.MediaPortal2) as IBundleMsiPackage;
      foreach (IBundlePackageFeature feature in featurePackage.Features)
      {
        FeatureState expectedState = installedFeatures.Contains(feature.Id) ? FeatureState.Local : FeatureState.Absent;
        Assert.Equal(expectedState, feature.RequestedFeatureState);
      }
    }

    [Fact]
    void Should_RepairPresentPackages()
    {
      RepairPlan plan = new RepairPlan(new PlanContext());

      PackageId[] installedPackages = new[] { PackageId.MediaPortal2, PackageId.VC2019_x86 };
      FeatureId[] installedFeatures = new[] { FeatureId.MediaPortal_2, FeatureId.Client };
      IList<IBundlePackage> packages = MockBundlePackages.CreateCurrentInstall(installedPackages, installedFeatures);

      plan.SetRequestedInstallStates(packages);

      foreach (IBundlePackage package in packages.Where(p => installedPackages.Contains(p.PackageId)))
        Assert.Equal(RequestState.Repair, package.RequestedInstallState);
    }

    [Fact]
    void Should_InstallMissingDependencyPackages()
    {
      RepairPlan plan = new RepairPlan(new PlanContext());

      PackageId[] installedPackages = new[] { PackageId.MediaPortal2, PackageId.VC2019_x86 };
      FeatureId[] installedFeatures = new[] { FeatureId.MediaPortal_2, FeatureId.Client };
      IList<IBundlePackage> packages = MockBundlePackages.CreateCurrentInstall(installedPackages, installedFeatures);

      plan.SetRequestedInstallStates(packages);

      IBundlePackage missingDependencyPackage = packages.First(p => p.PackageId == PackageId.VC2019_x64);
      Assert.Equal(RequestState.Present, missingDependencyPackage.RequestedInstallState);
    }

    [Fact]
    void Should_Not_InstallMissingNonDependencyPackages()
    {
      RepairPlan plan = new RepairPlan(new PlanContext());

      PackageId[] installedPackages = new[] { PackageId.MediaPortal2, PackageId.VC2019_x86 };
      FeatureId[] installedFeatures = new[] { FeatureId.MediaPortal_2, FeatureId.Client };
      IList<IBundlePackage> packages = MockBundlePackages.CreateCurrentInstall(installedPackages, installedFeatures);

      plan.SetRequestedInstallStates(packages);

      IBundlePackage missingNonDependencyPackage = packages.First(p => p.PackageId == PackageId.VC2013_x86);
      Assert.Equal(RequestState.None, missingNonDependencyPackage.RequestedInstallState);
    }
  }
}
