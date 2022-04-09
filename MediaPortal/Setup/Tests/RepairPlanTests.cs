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

using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MP2BootstrapperApp.ActionPlans;
using MP2BootstrapperApp.BundlePackages;
using System.Collections.Generic;
using System.Linq;
using Tests.Mocks;
using Xunit;

namespace Tests
{
  public class RepairPlanTests
  {
    [Fact]
    void Should_RepairPlanPlannedAction_Return_Repair()
    {
      RepairPlan plan = new RepairPlan(null, new PlanContext());

      Assert.Equal(LaunchAction.Repair, plan.PlannedAction);
    }

    [Fact]
    void Should_PreserveFeatureStates()
    {
      string[] installedFeatures = new[] { FeatureId.MediaPortal_2, FeatureId.Client };
      IList<IBundlePackage> packages = MockBundlePackageFactory.CreateCurrentInstall(new[] { PackageId.MediaPortal2 }, installedFeatures);
      RepairPlan plan = new RepairPlan(installedFeatures.Where(f => f != FeatureId.MediaPortal_2), new PlanContext());

      IBundleMsiPackage featurePackage = packages.First(p => p.PackageId == PackageId.MediaPortal2) as IBundleMsiPackage;
      foreach (IBundlePackageFeature feature in featurePackage.Features)
      {
        FeatureState expectedState = installedFeatures.Contains(feature.Id) ? FeatureState.Local : FeatureState.Absent;
        FeatureState? featureState = plan.GetRequestedInstallState(feature);
        Assert.Equal(expectedState, featureState);
      }
    }

    [Fact]
    void Should_RepairPresentPackages()
    {
      PackageId[] installedPackages = new[] { PackageId.MediaPortal2, PackageId.VC2019_x86 };
      string[] installedFeatures = new[] { FeatureId.MediaPortal_2, FeatureId.Client };
      IList<IBundlePackage> packages = MockBundlePackageFactory.CreateCurrentInstall(installedPackages, installedFeatures);
      RepairPlan plan = new RepairPlan(installedFeatures.Where(f => f != FeatureId.MediaPortal_2), new PlanContext());

      foreach (IBundlePackage package in packages.Where(p => installedPackages.Contains(p.PackageId)))
      {
        RequestState? requestState = plan.GetRequestedInstallState(package);
        Assert.Equal(RequestState.Repair, requestState);
      }
    }

    [Fact]
    void Should_InstallMissingDependencyPackages()
    {
      PackageId[] installedPackages = new[] { PackageId.MediaPortal2, PackageId.VC2019_x86 };
      string[] installedFeatures = new[] { FeatureId.MediaPortal_2, FeatureId.Client };
      IList<IBundlePackage> packages = MockBundlePackageFactory.CreateCurrentInstall(installedPackages, installedFeatures);
      RepairPlan plan = new RepairPlan(installedFeatures.Where(f => f != FeatureId.MediaPortal_2), new PlanContext());

      IBundlePackage missingDependencyPackage = packages.First(p => p.PackageId == PackageId.VC2019_x64);
      RequestState? requestState = plan.GetRequestedInstallState(missingDependencyPackage);

      Assert.Equal(RequestState.Present, requestState);
    }

    [Fact]
    void Should_Not_InstallMissingNonDependencyPackages()
    {
      PackageId[] installedPackages = new[] { PackageId.MediaPortal2, PackageId.VC2019_x86 };
      string[] installedFeatures = new[] { FeatureId.MediaPortal_2, FeatureId.Client };
      IList<IBundlePackage> packages = MockBundlePackageFactory.CreateCurrentInstall(installedPackages, installedFeatures);
      RepairPlan plan = new RepairPlan(installedFeatures.Where(f => f != FeatureId.MediaPortal_2), new PlanContext());

      IBundlePackage missingNonDependencyPackage = packages.First(p => p.PackageId == PackageId.VC2013_x86);
      RequestState? requestState = plan.GetRequestedInstallState(missingNonDependencyPackage);

      Assert.Equal(RequestState.None, requestState);
    }
  }
}
