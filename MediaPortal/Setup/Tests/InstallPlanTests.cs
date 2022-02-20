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
  public class InstallPlanTests
  {
    [Fact]
    void Should_IncludeNonOptionalFeatures()
    {
      InstallPlan plan = new InstallPlan(new[] { FeatureId.Server }, null, new PlanContext());
      IList<IBundlePackage> packages = MockBundlePackages.CreateCurrentInstall();

      plan.SetRequestedInstallStates(packages);

      IBundlePackage featurePackage = packages.First(p => p.GetId() == PackageId.MediaPortal2);
      IBundlePackageFeature nonOptionalFeature = featurePackage.Features.First(f => f.Id == FeatureId.MediaPortal_2);
      Assert.Equal(false, nonOptionalFeature.Optional);
      Assert.Equal(FeatureState.Local, nonOptionalFeature.RequestedFeatureState);
    }

    [Fact]
    void Should_IncludeOptionalFeatures_When_Selected()
    {
      InstallPlan plan = new InstallPlan(new[] { FeatureId.Server }, null, new PlanContext());
      IList<IBundlePackage> packages = MockBundlePackages.CreateCurrentInstall();

      plan.SetRequestedInstallStates(packages);

      IBundlePackage featurePackage = packages.First(p => p.GetId() == PackageId.MediaPortal2);
      IBundlePackageFeature optionalFeature = featurePackage.Features.First(f => f.Id == FeatureId.Server);
      Assert.Equal(true, optionalFeature.Optional);
      Assert.Equal(FeatureState.Local, optionalFeature.RequestedFeatureState);
    }

    [Fact]
    void Should_ExcludeOptionalFeatures_When_NotSelected()
    {
      InstallPlan plan = new InstallPlan(new[] { FeatureId.Server }, null, new PlanContext());
      IList<IBundlePackage> packages = MockBundlePackages.CreateCurrentInstall();

      plan.SetRequestedInstallStates(packages);

      IBundlePackage featurePackage = packages.First(p => p.GetId() == PackageId.MediaPortal2);
      IBundlePackageFeature optionalFeature = featurePackage.Features.First(f => f.Id == FeatureId.Client);
      Assert.Equal(true, optionalFeature.Optional);
      Assert.Equal(FeatureState.Absent, optionalFeature.RequestedFeatureState);
    }

    [Fact]
    void Should_IncludeNonOptionalPackage_When_NotExcludedByFeature()
    {
      InstallPlan plan = new InstallPlan(new[] { FeatureId.Client }, null, new PlanContext());
      IList<IBundlePackage> packages = MockBundlePackages.CreateCurrentInstall();

      plan.SetRequestedInstallStates(packages);

      IBundlePackage package = packages.First(p => p.GetId() == PackageId.VC2019_x86);
      Assert.Equal(RequestState.Present, package.RequestedInstallState);
    }

    [Fact]
    void Should_ExcludeNonOptionalPackage_When_ExcludedByFeature()
    {
      InstallPlan plan = new InstallPlan(new[] { FeatureId.Client }, null, new PlanContext());
      IList<IBundlePackage> packages = MockBundlePackages.CreateCurrentInstall();

      plan.SetRequestedInstallStates(packages);

      IBundlePackage package = packages.First(p => p.GetId() == PackageId.VC2013_x86);
      Assert.Equal(RequestState.None, package.RequestedInstallState);
    }

    [Fact]
    void Should_IncludeOptionalPackage_When_Selected_And_ExcludedByFeature()
    {
      InstallPlan plan = new InstallPlan(new[] { FeatureId.Server }, new[] { PackageId.LAVFilters }, new PlanContext());
      IList<IBundlePackage> packages = MockBundlePackages.CreateCurrentInstall();

      plan.SetRequestedInstallStates(packages);

      IBundlePackage lavPackage = packages.First(p => p.GetId() == PackageId.LAVFilters);
      Assert.Equal(RequestState.Present, lavPackage.RequestedInstallState);
    }

    [Fact]
    void Should_ExcludeOptionalPackage_When_NotSelected_And_NotExcludedByFeature()
    {
      InstallPlan plan = new InstallPlan(new[] { FeatureId.Client }, new PackageId[0], new PlanContext());
      IList<IBundlePackage> packages = MockBundlePackages.CreateCurrentInstall();

      plan.SetRequestedInstallStates(packages);

      IBundlePackage lavPackage = packages.First(p => p.GetId() == PackageId.LAVFilters);
      Assert.Equal(RequestState.None, lavPackage.RequestedInstallState);
    }

    [Fact]
    void Should_IncludeOptionalPackage_When_SelectedOptionalPackagesIsNull_And_NotExcludedByFeature()
    {
      InstallPlan plan = new InstallPlan(new[] { FeatureId.Client }, null, new PlanContext());
      IList<IBundlePackage> packages = MockBundlePackages.CreateCurrentInstall();

      plan.SetRequestedInstallStates(packages);

      IBundlePackage lavPackage = packages.First(p => p.GetId() == PackageId.LAVFilters);
      Assert.Equal(RequestState.Present, lavPackage.RequestedInstallState);
    }

    [Fact]
    void Should_ExcludeOptionalPackage_When_SelectedOptionalPackagesIsNull_And_ExcludedByFeature()
    {
      InstallPlan plan = new InstallPlan(new[] { FeatureId.Server }, null, new PlanContext());
      IList<IBundlePackage> packages = MockBundlePackages.CreateCurrentInstall();

      plan.SetRequestedInstallStates(packages);

      IBundlePackage lavPackage = packages.First(p => p.GetId() == PackageId.LAVFilters);
      Assert.Equal(RequestState.None, lavPackage.RequestedInstallState);
    }
  }
}
