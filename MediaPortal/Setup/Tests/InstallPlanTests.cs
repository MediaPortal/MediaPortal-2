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
using MP2BootstrapperApp.ChainPackages;
using MP2BootstrapperApp.Models;
using System.Collections.Generic;
using System.Linq;
using Tests.Mocks;
using Xunit;

namespace Tests
{
  public class InstallPlanTests
  {
    [Fact]
    void Should_InstallPlanPlannedAction_Return_Install()
    {
      InstallPlan plan = new InstallPlan(null, null, new PlanContext());

      Assert.Equal(LaunchAction.Install, plan.PlannedAction);
    }

    [Fact]
    void Should_ModifyPlanPlannedAction_Return_Modify()
    {
      ModifyPlan plan = new ModifyPlan(null, null, new PlanContext());

      Assert.Equal(LaunchAction.Modify, plan.PlannedAction);
    }

    [Fact]
    void Should_IncludeNonOptionalFeatures()
    {
      InstallPlan plan = new InstallPlan(new[] { FeatureId.Server }, null, new PlanContext());
      IList<IBundlePackage> packages = MockBundlePackages.CreateCurrentInstall();
      IBundleMsiPackage featurePackage = packages.First(p => p.PackageId == PackageId.MediaPortal2) as IBundleMsiPackage;
      IBundlePackageFeature nonOptionalFeature = featurePackage.Features.First(f => f.Id == FeatureId.MediaPortal_2);

      FeatureState? featureState = plan.GetRequestedInstallState(nonOptionalFeature);

      Assert.False(nonOptionalFeature.Optional);
      Assert.Equal(FeatureState.Local, featureState);
    }

    [Fact]
    void Should_IncludeOptionalFeatures_When_Selected()
    {
      InstallPlan plan = new InstallPlan(new[] { FeatureId.Server }, null, new PlanContext());
      IList<IBundlePackage> packages = MockBundlePackages.CreateCurrentInstall();
      IBundleMsiPackage featurePackage = packages.First(p => p.PackageId == PackageId.MediaPortal2) as IBundleMsiPackage;
      IBundlePackageFeature optionalFeature = featurePackage.Features.First(f => f.Id == FeatureId.Server);

      FeatureState? featureState = plan.GetRequestedInstallState(optionalFeature);

      Assert.True(optionalFeature.Optional);
      Assert.Equal(FeatureState.Local, featureState);
    }

    [Fact]
    void Should_ExcludeOptionalFeatures_When_NotSelected()
    {
      InstallPlan plan = new InstallPlan(new[] { FeatureId.Server }, null, new PlanContext());
      IList<IBundlePackage> packages = MockBundlePackages.CreateCurrentInstall();
      IBundleMsiPackage featurePackage = packages.First(p => p.PackageId == PackageId.MediaPortal2) as IBundleMsiPackage;
      IBundlePackageFeature optionalFeature = featurePackage.Features.First(f => f.Id == FeatureId.Client);

      FeatureState? featureState = plan.GetRequestedInstallState(optionalFeature);

      Assert.True(optionalFeature.Optional);
      Assert.Equal(FeatureState.Absent, featureState);
    }

    [Fact]
    void Should_IncludeNonOptionalPackage_When_NotExcludedByFeature()
    {
      InstallPlan plan = new InstallPlan(new[] { FeatureId.Client }, null, new PlanContext());
      IList<IBundlePackage> packages = MockBundlePackages.CreateCurrentInstall();
      IBundlePackage package = packages.First(p => p.PackageId == PackageId.VC2019_x86);

      RequestState? requestState = plan.GetRequestedInstallState(package);

      Assert.Equal(RequestState.Present, requestState);
    }

    [Fact]
    void Should_ExcludeNonOptionalPackage_When_ExcludedByFeature()
    {
      InstallPlan plan = new InstallPlan(new[] { FeatureId.Client }, null, new PlanContext());
      IList<IBundlePackage> packages = MockBundlePackages.CreateCurrentInstall();
      IBundlePackage package = packages.First(p => p.PackageId == PackageId.VC2013_x86);

      RequestState? requestState = plan.GetRequestedInstallState(package);

      Assert.Equal(RequestState.None, requestState);
    }

    [Fact]
    void Should_IncludeOptionalPackage_When_Selected_And_ExcludedByFeature()
    {
      InstallPlan plan = new InstallPlan(new[] { FeatureId.Server }, new[] { PackageId.LAVFilters }, new PlanContext());
      IList<IBundlePackage> packages = MockBundlePackages.CreateCurrentInstall();
      IBundlePackage lavPackage = packages.First(p => p.PackageId == PackageId.LAVFilters);

      RequestState? requestState = plan.GetRequestedInstallState(lavPackage);

      Assert.Equal(RequestState.Present, requestState);
    }

    [Fact]
    void Should_ExcludeOptionalPackage_When_NotSelected_And_NotExcludedByFeature()
    {
      InstallPlan plan = new InstallPlan(new[] { FeatureId.Client }, new PackageId[0], new PlanContext());
      IList<IBundlePackage> packages = MockBundlePackages.CreateCurrentInstall();
      IBundlePackage lavPackage = packages.First(p => p.PackageId == PackageId.LAVFilters);

      RequestState? requestState = plan.GetRequestedInstallState(lavPackage);

      Assert.Equal(RequestState.None, requestState);
    }

    [Fact]
    void Should_IncludeOptionalPackage_When_SelectedOptionalPackagesIsNull_And_NotExcludedByFeature()
    {
      InstallPlan plan = new InstallPlan(new[] { FeatureId.Client }, null, new PlanContext());
      IList<IBundlePackage> packages = MockBundlePackages.CreateCurrentInstall();
      IBundlePackage lavPackage = packages.First(p => p.PackageId == PackageId.LAVFilters);

      RequestState? requestState = plan.GetRequestedInstallState(lavPackage);

      Assert.Equal(RequestState.Present, requestState);
    }

    [Fact]
    void Should_ExcludeOptionalPackage_When_SelectedOptionalPackagesIsNull_And_ExcludedByFeature()
    {
      InstallPlan plan = new InstallPlan(new[] { FeatureId.Server }, null, new PlanContext());
      IList<IBundlePackage> packages = MockBundlePackages.CreateCurrentInstall();
      IBundlePackage lavPackage = packages.First(p => p.PackageId == PackageId.LAVFilters);

      RequestState? requestState = plan.GetRequestedInstallState(lavPackage);

      Assert.Equal(RequestState.None, requestState);
    }
  }
}
