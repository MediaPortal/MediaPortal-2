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

using MP2BootstrapperApp.ChainPackages;
using MP2BootstrapperApp.Models;
using MP2BootstrapperApp.WizardSteps;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Tests.Mocks;
using Xunit;

namespace Tests
{
  public class WizardTests
  {
    [Fact]
    void Should_IncludeOptionalPackagesInCustomStep_If_Not_Installed()
    {
      IList<IBundlePackage> packages = MockBundlePackages.CreateCurrentInstall();
      IBootstrapperApplicationModel applicationModel = Substitute.For<IBootstrapperApplicationModel>();
      applicationModel.BundlePackages.Returns(new ReadOnlyCollection<IBundlePackage>(packages));

      InstallCustomStep customStep = new InstallCustomStep(applicationModel);

      IBundlePackage availablePackage = customStep.AvailablePackages.FirstOrDefault(p => p.PackageId == PackageId.LAVFilters);

      Assert.NotNull(availablePackage);
    }

    [Fact]
    void Should_Not_IncludeOptionalPackagesInCustomStep_If_Installed()
    {
      IList<IBundlePackage> packages = MockBundlePackages.CreateCurrentInstall(new[] { PackageId.LAVFilters });
      IBootstrapperApplicationModel applicationModel = Substitute.For<IBootstrapperApplicationModel>();
      applicationModel.BundlePackages.Returns(new ReadOnlyCollection<IBundlePackage>(packages));
      
      InstallCustomStep customStep = new InstallCustomStep(applicationModel);

      IBundlePackage availablePackage = customStep.AvailablePackages.FirstOrDefault(p => p.PackageId == PackageId.LAVFilters);

      Assert.Null(availablePackage);
    }

    [Fact]
    void Should_SelectInstalledFeaturesInCustomStep_If_PreviousVersionInstalled()
    {
      IList<IBundlePackage> packages = MockBundlePackages.CreatePreviousInstall(new Version(1, 0), new[] { PackageId.MediaPortal2 }, new[] { FeatureId.Server });
      IBootstrapperApplicationModel applicationModel = Substitute.For<IBootstrapperApplicationModel>();
      applicationModel.BundlePackages.Returns(new ReadOnlyCollection<IBundlePackage>(packages));

      InstallCustomStep customStep = new InstallCustomStep(applicationModel);

      IBundlePackageFeature selectedFeature = customStep.SelectedFeatures.Single();

      Assert.Equal(FeatureId.Server, selectedFeature.Id);
    }

    [Fact]
    void Should_SelectAllFeaturesInCustomStep_If_PreviousVersionNotInstalled()
    {
      IList<IBundlePackage> packages = MockBundlePackages.CreatePreviousInstall(new Version(1, 0), null, null);
      IBootstrapperApplicationModel applicationModel = Substitute.For<IBootstrapperApplicationModel>();
      applicationModel.BundlePackages.Returns(new ReadOnlyCollection<IBundlePackage>(packages));

      InstallCustomStep customStep = new InstallCustomStep(applicationModel);

      IEnumerable<FeatureId> expectedFeatures = new[] { FeatureId.Client, FeatureId.Server, FeatureId.ServiceMonitor, FeatureId.LogCollector };
      IEnumerable<FeatureId> actualFeatures = customStep.SelectedFeatures.Select(f => f.Id).OrderBy(f => f);

      Assert.Equal(expectedFeatures, actualFeatures);
    }

    [Fact]
    void Should_SelectAllOptionalPackagesInCustomStep_If_PreviousVersionNotInstalled()
    {
      IList<IBundlePackage> packages = MockBundlePackages.CreatePreviousInstall(new Version(1, 0), null, null);
      IBootstrapperApplicationModel applicationModel = Substitute.For<IBootstrapperApplicationModel>();
      applicationModel.BundlePackages.Returns(new ReadOnlyCollection<IBundlePackage>(packages));

      InstallCustomStep customStep = new InstallCustomStep(applicationModel);

      IEnumerable<PackageId> expectedPackages = new[] { PackageId.LAVFilters };
      IEnumerable<PackageId> actualPackages = customStep.SelectedPackages.Select(p => p.PackageId);

      Assert.Equal(expectedPackages, actualPackages);
    }

    [Fact]
    void Should_SelectOptionalPackagesInCustomStep_If_PreviousVersionInstalled()
    {
      IList<IBundlePackage> packages = MockBundlePackages.CreatePreviousInstall(new Version(1, 0), new[] { PackageId.MediaPortal2, PackageId.LAVFilters }, new[] { FeatureId.Server });
      IBootstrapperApplicationModel applicationModel = Substitute.For<IBootstrapperApplicationModel>();
      applicationModel.BundlePackages.Returns(new ReadOnlyCollection<IBundlePackage>(packages));

      InstallCustomStep customStep = new InstallCustomStep(applicationModel);

      IBundlePackage selectedPackage = customStep.SelectedPackages.FirstOrDefault(p => p.PackageId == PackageId.LAVFilters);

      Assert.NotNull(selectedPackage);
    }

    [Fact]
    void Should_Not_SelectOptionalPackagesInCustomStep_If_PreviousVersionNotInstalled()
    {
      IList<IBundlePackage> packages = MockBundlePackages.CreatePreviousInstall(new Version(1, 0), new[] { PackageId.MediaPortal2 }, new[] { FeatureId.Server });

      IBootstrapperApplicationModel applicationModel = Substitute.For<IBootstrapperApplicationModel>();
      applicationModel.BundlePackages.Returns(new ReadOnlyCollection<IBundlePackage>(packages));

      InstallCustomStep customStep = new InstallCustomStep(applicationModel);

      IBundlePackage selectedPackage = customStep.SelectedPackages.FirstOrDefault(p => p.PackageId == PackageId.LAVFilters);

      Assert.Null(selectedPackage);
    }

    [Theory]
    // All labeled parameters should be removed
    [InlineData("Removing applications Application: [1], Command line: [2]", "Removing applications")]
    // Unlabelled parameters should be removed, keeping preceeding word
    [InlineData("Generating script operations for action: [1]", "Generating script operations for action")]
    // Strings with no parameter should be left unchanged
    [InlineData("Uninstalling Windows Firewall configuration", "Uninstalling Windows Firewall configuration")]
    void Should_ProgressMessageParametersBeRemoved(string message, string expected)
    {
      InstallationInProgressStep step = new InstallationInProgressStep(null);

      string actual = step.ParseActionMessage(message);

      Assert.Equal(expected, actual);
    }
  }
}
