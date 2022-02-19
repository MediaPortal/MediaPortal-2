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
  public class RepairPlanTests
  {
    [Fact]
    void Should_PreserveFeatureStates()
    {
      RepairPlan plan = new RepairPlan(new PlanContext());
      FeatureId[] installedFeatures = new[] { FeatureId.MediaPortal_2, FeatureId.Client };
      IList<IBundlePackage> packages = CreateTestBundlePackages(new[] { PackageId.MediaPortal2 }, installedFeatures);

      plan.SetRequestedInstallStates(packages);

      IBundlePackage featurePackage = packages.First(p => p.GetId() == PackageId.MediaPortal2);      
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
      IList<IBundlePackage> packages = CreateTestBundlePackages(installedPackages, installedFeatures);

      plan.SetRequestedInstallStates(packages);

      foreach (IBundlePackage package in packages.Where(p => installedPackages.Contains(p.GetId())))
        Assert.Equal(RequestState.Repair, package.RequestedInstallState);
    }

    [Fact]
    void Should_InstallMissingDependencyPackages()
    {
      RepairPlan plan = new RepairPlan(new PlanContext());

      PackageId[] installedPackages = new[] { PackageId.MediaPortal2, PackageId.VC2019_x86 };
      FeatureId[] installedFeatures = new[] { FeatureId.MediaPortal_2, FeatureId.Client };
      IList<IBundlePackage> packages = CreateTestBundlePackages(installedPackages, installedFeatures);

      plan.SetRequestedInstallStates(packages);

      IBundlePackage missingDependencyPackage = packages.First(p => p.GetId() == PackageId.VC2019_x64);
      Assert.Equal(RequestState.Present, missingDependencyPackage.RequestedInstallState);
    }

    [Fact]
    void Should_Not_InstallMissingNonDependencyPackages()
    {
      RepairPlan plan = new RepairPlan(new PlanContext());

      PackageId[] installedPackages = new[] { PackageId.MediaPortal2, PackageId.VC2019_x86 };
      FeatureId[] installedFeatures = new[] { FeatureId.MediaPortal_2, FeatureId.Client };
      IList<IBundlePackage> packages = CreateTestBundlePackages(installedPackages, installedFeatures);

      plan.SetRequestedInstallStates(packages);

      IBundlePackage missingNonDependencyPackage = packages.First(p => p.GetId() == PackageId.VC2013_x86);
      Assert.Equal(RequestState.None, missingNonDependencyPackage.RequestedInstallState);
    }

    IList<IBundlePackage> CreateTestBundlePackages(IEnumerable<PackageId> installedPackages = null, IEnumerable<FeatureId> installedFeatures = null)
    {
      if (installedPackages == null)
        installedPackages = new PackageId[0];
      if (installedFeatures == null)
        installedFeatures = new FeatureId[0];

      List<IBundlePackage> packages = new List<IBundlePackage>();
      foreach (PackageId packageId in Enum.GetValues(typeof(PackageId)))
      {
        if (packageId == PackageId.Unknown)
          continue;

        IBundlePackage package = Substitute.For<IBundlePackage>();
        package.GetId().Returns(packageId);
        package.Optional.Returns(packageId == PackageId.LAVFilters);
        package.CurrentInstallState.Returns(installedPackages.Contains(packageId) ? PackageState.Present : PackageState.Absent);

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
            FeatureState currentState = (!feature.Optional || installedFeatures.Contains(featureId)) ? FeatureState.Local : FeatureState.Absent;
            feature.CurrentFeatureState.Returns(currentState);
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
