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

using MP2BootstrapperApp.ActionPlans;
using MP2BootstrapperApp.BundlePackages;
using MP2BootstrapperApp.BundlePackages.Plugins;
using System.Collections.Generic;
using System.Linq;
using Tests.Mocks;
using Xunit;

namespace Tests
{
  public class PluginInstallationTests
  {
    [Fact]
    void Should_OnlyIncludeFeaturesWhereParentFeatureIsBeingInstalled()
    {
      InstallPlan plan = new InstallPlan(new[] { FeatureId.Server }, null, new PlanContext());
      IBundleMsiPackage featurePackage = TestBundlePackageFactory.CreateCurrentInstall().First(p => p.PackageId == PackageId.MediaPortal2) as IBundleMsiPackage;

      IPluginDescriptor tvService3 = new TvService3();

      IEnumerable<string> plannedFeatures = tvService3.GetInstallableFeatures(plan, featurePackage.Features).Select(f => f.Id);
      Assert.Equal(new[] { FeatureId.SlimTvService3 }, plannedFeatures);
    }

    [Theory]
    // Should select TV Server 3 if installing server and previous install was client only with native TV provider
    [InlineData(new object[] { new[] { FeatureId.Client, FeatureId.Server }, new[] { FeatureId.Client, FeatureId.SlimTvClient, FeatureId.SlimTvNativeProvider }, new[] { PluginId.TvService3 } })]
    // Should select TV Server 3 if installing server and previous install included server with Tv Service 3
    [InlineData(new object[] { new[] { FeatureId.Server }, new[] { FeatureId.Server, FeatureId.SlimTvService3 }, new[] { PluginId.TvService3 } })]
    [InlineData(new object[] { new[] { FeatureId.Client, FeatureId.Server }, new[] { FeatureId.Server, FeatureId.SlimTvService3 }, new[] { PluginId.TvService3 } })]
    [InlineData(new object[] { new[] { FeatureId.Client, FeatureId.Server }, new[] { FeatureId.Client, FeatureId.SlimTvClient, FeatureId.SlimTvNativeProvider, FeatureId.Server, FeatureId.SlimTvService3 }, new[] { PluginId.TvService3 } })]
    // Should select TV Server 3.5 if installing server and previous install included server and TV Service 3.5
    [InlineData(new object[] { new[] { FeatureId.Server }, new[] { FeatureId.Server, FeatureId.SlimTvService35 }, new[] { PluginId.TvService35 } })]
    [InlineData(new object[] { new[] { FeatureId.Client, FeatureId.Server }, new[] { FeatureId.Server, FeatureId.SlimTvService35 }, new[] { PluginId.TvService35 } })]
    [InlineData(new object[] { new[] { FeatureId.Client, FeatureId.Server }, new[] { FeatureId.Client, FeatureId.SlimTvClient, FeatureId.SlimTvNativeProvider, FeatureId.Server, FeatureId.SlimTvService35 }, new[] { PluginId.TvService35 } })]
    // Should select TV Server client if only installing client and previous install included native TV provider
    [InlineData(new object[] { new[] { FeatureId.Client }, new[] { FeatureId.Client, FeatureId.SlimTvClient, FeatureId.SlimTvNativeProvider }, new[] { PluginId.TvServiceClient } })]
    [InlineData(new object[] { new[] { FeatureId.Client }, new[] { FeatureId.Client, FeatureId.Server, FeatureId.SlimTvClient, FeatureId.SlimTvNativeProvider, FeatureId.SlimTvService3 }, new[] { PluginId.TvServiceClient } })]
    void Should_SelectBestAvailablePluginBasedonPreviousInstallation(string[] plannedFeatures, string[] previouslyInstalledFeatures, string[] expectedPlugin)
    {
      InstallPlan plan = new InstallPlan(plannedFeatures, null, new PlanContext());
      IList<IBundlePackage> bundlePackages = TestBundlePackageFactory.CreatePreviousInstall(new System.Version(1, 0), new[] { PackageId.MediaPortal2 }, previouslyInstalledFeatures); ;
      IBundleMsiPackage featurePackage = bundlePackages.First(p => p.PackageId == PackageId.MediaPortal2) as IBundleMsiPackage;

      PluginManager pluginManager = new PluginManager(new MockPluginLoader());
      IEnumerable<string> plugins = pluginManager.GetInstalledOrDefaultAvailablePlugins(plan, featurePackage.Features).Select(p => p.Id);

      Assert.Equal(expectedPlugin, plugins);
    }

    [Theory]
    // Should select TV Server 3 if installing server and previous install did not include any TV provider
    [InlineData(new object[] { new[] { FeatureId.Server }, new string[] { }, new[] { PluginId.TvService3 } })]
    [InlineData(new object[] { new[] { FeatureId.Client, FeatureId.Server }, new[] { FeatureId.Client, FeatureId.Server }, new[] { PluginId.TvService3 } })]
    // Should select TV Server Client if installing client and previous install did not include any TV provider
    [InlineData(new object[] { new[] { FeatureId.Client }, new string[] { }, new[] { PluginId.TvServiceClient } })]
    [InlineData(new object[] { new[] { FeatureId.Client }, new[] { FeatureId.Client, FeatureId.Server }, new[] { PluginId.TvServiceClient } })]
    [InlineData(new object[] { new[] { FeatureId.Client }, new[] { FeatureId.Server, FeatureId.SlimTvService3 }, new[] { PluginId.TvServiceClient } })]
    void Should_SelectBestAvailableDefaultPluginBasedonPreviousInstallation(string[] plannedFeatures, string[] previouslyInstalledFeatures, string[] expectedPlugin)
    {
      InstallPlan plan = new InstallPlan(plannedFeatures, null, new PlanContext());
      IList<IBundlePackage> bundlePackages = TestBundlePackageFactory.CreatePreviousInstall(new System.Version(1, 0), new[] { PackageId.MediaPortal2 }, previouslyInstalledFeatures); ;
      IBundleMsiPackage featurePackage = bundlePackages.First(p => p.PackageId == PackageId.MediaPortal2) as IBundleMsiPackage;

      PluginManager pluginManager = new PluginManager(new MockPluginLoader());
      IEnumerable<string> plugins = pluginManager.GetInstalledOrDefaultAvailablePlugins(plan, featurePackage.Features).Select(p => p.Id);

      Assert.Equal(expectedPlugin, plugins);
    }
  }
}
