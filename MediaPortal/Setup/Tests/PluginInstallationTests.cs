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
    [InlineData(new[] { FeatureId.Server }, new string[0])]
    [InlineData(new[] { FeatureId.Client, FeatureId.Server }, new[] { FeatureId.SlimTvServiceClient })]
    [InlineData(new[] { FeatureId.LogCollector }, new string[0])]
    void Should_OnlyReturnPluginFeaturesWhereParentFeatureIsBeingInstalledAndNotIncludedWithRelatedFeature(string[] plannedParents, string[] expectedFeatures)
    {
      IBundleMsiPackage featurePackage = MockBundlePackageFactory.CreateCurrentInstall().First(p => p.PackageId == PackageId.MediaPortal2) as IBundleMsiPackage;
      IEnumerable<IBundlePackageFeature> plannedFeatures = featurePackage.Features.Where(f => plannedParents.Contains(f.Id));

      IEnumerable<string> availableFeatures = FeatureUtils.GetSelectableChildFeatures(plannedFeatures, featurePackage.Features).Select(f => f.Id);

      Assert.Equal(expectedFeatures.OrderBy(f => f), availableFeatures.OrderBy(f => f));
    }

    // History: the TV layers used to drive this test class hard. SlimTv.Service3/.Service35 were the
    // only features declaring *related* features (they pulled the TV Service Client in), and
    // SlimTv.TvMosaicClient was the only one declaring a *conflict* with the TV Service Client. Both
    // layers have been removed - the services in 2026-10 together with the TV engine, and TvMosaic
    // right after - so no shipped feature declares relations or conflicts anymore.
    //
    // That is an invariant now, and Should_NotDeclareRelationsOrConflictsWithoutATest guards it: if a
    // feature starts declaring relations again, this test says "go and cover it", because the
    // FeatureUtils code paths below would otherwise silently lose their only product fixture.
    [Fact]
    void Should_NotDeclareRelationsOrConflictsWithoutATest()
    {
      IFeatureMetadata[] declaring = new AssemblyFeatureMetadataProvider().GetMetadata()
          .Where(m => m.RelatedFeatures.Count > 0 || m.ConflictingFeatures.Count > 0)
          .ToArray();

      Assert.True(declaring.Length == 0,
          "Feature(s) " + string.Join(", ", declaring.Select(m => m.Feature)) + " declare related or conflicting" +
          " features. Since the TV layers were removed no shipped feature does - which means" +
          " FeatureUtils.GetInstallableFeatureAndRelations and FeatureUtils.GetConflicts have no product fixture left." +
          " If you re-introduce a relation or conflict, add theory rows for it here (git history of this file has the" +
          " SlimTv.Service3/.Service35 and SlimTv.TvMosaicClient rows that used to cover both paths) instead of" +
          " letting the code path go untested again.");
    }

    [Theory]
    [InlineData(new[] { FeatureId.Client }, FeatureId.SlimTvServiceClient, new[] { FeatureId.SlimTvServiceClient })]
    [InlineData(new[] { FeatureId.Server }, FeatureId.SlimTvServiceClient, new string[0])]
    void Should_OnlyInstallRelatedPluginFeaturesWhenParentFeatureIsBeingInstalled(string[] plannedParents, string installingFeature, string[] expectedInstallableFeatures)
    {
      IBundleMsiPackage featurePackage = MockBundlePackageFactory.CreateCurrentInstall().First(p => p.PackageId == PackageId.MediaPortal2) as IBundleMsiPackage;
      IEnumerable<IBundlePackageFeature> plannedFeatures = featurePackage.Features.Where(f => plannedParents.Contains(f.Id));
      IBundlePackageFeature feature = featurePackage.Features.First(f => f.Id == installingFeature);

      ICollection<IBundlePackageFeature> installableFeatures = FeatureUtils.GetInstallableFeatureAndRelations(feature, plannedFeatures, featurePackage.Features);

      Assert.Equal(expectedInstallableFeatures.OrderBy(f => f), installableFeatures.Select(f => f.Id).OrderBy(f => f));
    }
  }
}
