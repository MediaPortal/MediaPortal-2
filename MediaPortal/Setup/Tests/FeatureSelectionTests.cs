using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MP2BootstrapperApp.ChainPackages;
using MP2BootstrapperApp.ActionPlans;
using MP2BootstrapperApp.Models;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Tests
{
  public class FeatureSelectionTests
  {
    [Fact]
    void Should_IncludeNonOptionalFeatures()
    {
      InstallPlan plan = new InstallPlan(new[] { FeatureId.Server }, null, new PlanContext());
      IList<IBundlePackage> packages = CreateTestBundlePackages();

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
      IList<IBundlePackage> packages = CreateTestBundlePackages();

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
      IList<IBundlePackage> packages = CreateTestBundlePackages();

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
      IList<IBundlePackage> packages = CreateTestBundlePackages();

      plan.SetRequestedInstallStates(packages);

      IBundlePackage package = packages.First(p => p.GetId() == PackageId.VC2019_x86);
      Assert.Equal(RequestState.Present, package.RequestedInstallState);
    }

    [Fact]
    void Should_ExcludeNonOptionalPackage_When_ExcludedByFeature()
    {
      InstallPlan plan = new InstallPlan(new[] { FeatureId.Client }, null, new PlanContext());
      IList<IBundlePackage> packages = CreateTestBundlePackages();

      plan.SetRequestedInstallStates(packages);

      IBundlePackage package = packages.First(p => p.GetId() == PackageId.VC2013_x86);
      Assert.Equal(RequestState.None, package.RequestedInstallState);
    }

    [Fact]
    void Should_IncludeOptionalPackage_When_Selected_And_ExcludedByFeature()
    {
      InstallPlan plan = new InstallPlan(new[] { FeatureId.Server }, new[] { PackageId.LAVFilters }, new PlanContext());
      IList<IBundlePackage> packages = CreateTestBundlePackages();

      plan.SetRequestedInstallStates(packages);

      IBundlePackage lavPackage = packages.First(p => p.GetId() == PackageId.LAVFilters);
      Assert.Equal(RequestState.Present, lavPackage.RequestedInstallState);
    }

    [Fact]
    void Should_ExcludeOptionalPackage_When_NotSelected_And_NotExcludedByFeature()
    {
      InstallPlan plan = new InstallPlan(new[] { FeatureId.Client }, new PackageId[0], new PlanContext());
      IList<IBundlePackage> packages = CreateTestBundlePackages();

      plan.SetRequestedInstallStates(packages);

      IBundlePackage lavPackage = packages.First(p => p.GetId() == PackageId.LAVFilters);
      Assert.Equal(RequestState.None, lavPackage.RequestedInstallState);
    }

    [Fact]
    void Should_IncludeOptionalPackage_When_SelectedOptionalPackagesIsNull_And_NotExcludedByFeature()
    {
      InstallPlan plan = new InstallPlan(new[] { FeatureId.Client }, null, new PlanContext());
      IList<IBundlePackage> packages = CreateTestBundlePackages();

      plan.SetRequestedInstallStates(packages);

      IBundlePackage lavPackage = packages.First(p => p.GetId() == PackageId.LAVFilters);
      Assert.Equal(RequestState.Present, lavPackage.RequestedInstallState);
    }

    [Fact]
    void Should_ExcludeOptionalPackage_When_SelectedOptionalPackagesIsNull_And_ExcludedByFeature()
    {
      InstallPlan plan = new InstallPlan(new[] { FeatureId.Server }, null, new PlanContext());
      IList<IBundlePackage> packages = CreateTestBundlePackages();

      plan.SetRequestedInstallStates(packages);

      IBundlePackage lavPackage = packages.First(p => p.GetId() == PackageId.LAVFilters);
      Assert.Equal(RequestState.None, lavPackage.RequestedInstallState);
    }

    IList<IBundlePackage> CreateTestBundlePackages()
    {
      List<IBundlePackage> packages = new List<IBundlePackage>();
      foreach (PackageId packageId in Enum.GetValues(typeof(PackageId)))
      {
        if (packageId == PackageId.Unknown)
          continue;

        IBundlePackage package = Substitute.For<IBundlePackage>();
        package.GetId().Returns(packageId);
        package.Optional.Returns(packageId == PackageId.LAVFilters);

        if (packageId == PackageId.MediaPortal2)
        {
          List<IBundlePackageFeature> features = new List<IBundlePackageFeature>();
          foreach (FeatureId featureId in Enum.GetValues(typeof(FeatureId)))
          {
            if (featureId == FeatureId.Unknown)
              continue;
            IBundlePackageFeature feature = Substitute.For<IBundlePackageFeature>();
            feature.Id.Returns(featureId);
            feature.Optional.Returns(featureId != FeatureId.MediaPortal_2);
            features.Add(feature);
          }
          package.Features.Returns(features);
        }
        packages.Add(package);
      }
      return packages;
    }
  }
}
