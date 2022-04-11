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
using MP2BootstrapperApp.BundlePackages.Features;
using System.Collections.Generic;
using System.Linq;
using Tests.Mocks;
using Xunit;

namespace Tests
{
  public class PluginInstallationTests
  {
    [Theory]
    [InlineData(new[] { FeatureId.Client }, new[] { FeatureId.SlimTvServiceClient })]
    [InlineData(new[] { FeatureId.Server }, new[] { FeatureId.SlimTvService3, FeatureId.SlimTvService35 })]
    [InlineData(new[] { FeatureId.Client, FeatureId.Server }, new[] { FeatureId.SlimTvService3, FeatureId.SlimTvService35 })]
    [InlineData(new[] { FeatureId.LogCollector }, new string[0])]
    void Should_OnlyReturnPluginFeaturesWhereParentFeatureIsBeingInstalledAndNotIncludedWithRelatedFeature(string[] plannedParents, string[] expectedFeatures)
    {
      IBundleMsiPackage featurePackage = MockBundlePackageFactory.CreateCurrentInstall().First(p => p.PackageId == PackageId.MediaPortal2) as IBundleMsiPackage;

      IEnumerable<string> availableFeatures = FeatureUtils.GetSelectableChildFeatures(plannedParents, featurePackage.Features).Select(f => f.Id);
      
      Assert.Equal(expectedFeatures.OrderBy(f => f), availableFeatures.OrderBy(f => f));
    }

    [Theory]
    [InlineData(new[] { FeatureId.Client, FeatureId.Server }, FeatureId.SlimTvService3, new[] { FeatureId.SlimTvService3, FeatureId.SlimTvServiceClient })]
    [InlineData(new[] { FeatureId.Server }, FeatureId.SlimTvService3, new[] { FeatureId.SlimTvService3 })]
    [InlineData(new[] { FeatureId.Client }, FeatureId.SlimTvService3, new string[0])]
    void Should_OnlyInstallRelatedPluginFeaturesWhenParentFeatureIsBeingInstalled(string[] plannedParents, string installingFeature, string[] expectedInstallableFeatures)
    {
      IBundleMsiPackage featurePackage = MockBundlePackageFactory.CreateCurrentInstall().First(p => p.PackageId == PackageId.MediaPortal2) as IBundleMsiPackage;
      IBundlePackageFeature feature = featurePackage.Features.First(f => f.Id == installingFeature);

      ICollection<IBundlePackageFeature> installableFeatures = FeatureUtils.GetInstallableFeatureAndRelations(feature, plannedParents, featurePackage.Features);

      Assert.Equal(expectedInstallableFeatures.OrderBy(f => f), installableFeatures.Select(f => f.Id).OrderBy(f => f));
    }

    [Theory]
    [InlineData(new[] { FeatureId.Client, FeatureId.Server, FeatureId.SlimTvService35 }, FeatureId.SlimTvService3)]
    void Should_NotInstallPluginFeatureWhenConflictingFeatureIsBeingInstalled(string[] plannedFeatures, string conflictingfeature)
    {
      IBundleMsiPackage featurePackage = MockBundlePackageFactory.CreateCurrentInstall().First(p => p.PackageId == PackageId.MediaPortal2) as IBundleMsiPackage;

      IEnumerable<string> conflicts = FeatureUtils.GetConflicts(conflictingfeature, plannedFeatures, featurePackage.Features);

      Assert.True(conflicts.Any());
    }
  }
}
