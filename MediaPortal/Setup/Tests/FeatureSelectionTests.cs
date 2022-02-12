using MP2BootstrapperApp.ChainPackages;
using MP2BootstrapperApp.FeatureSelection;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Tests
{
  public class FeatureSelectionTests
  {
    #region Should_ExcludeFeature_Test_Cases

    public static IEnumerable<object[]> Should_ExcludeFeature_Test_Cases()
    {
      yield return new object[] { new ClientFeature(), new[] { FeatureId.Server, FeatureId.LogCollector, FeatureId.ServiceMonitor } };
      yield return new object[] { new ServerFeature(), new[] { FeatureId.Client, FeatureId.LogCollector, FeatureId.ServiceMonitor } };
      yield return new object[] { new LogCollectorFeature(), new[] { FeatureId.Client, FeatureId.Server, FeatureId.ServiceMonitor } };
      yield return new object[] { new ServiceMonitorFeature(), new[] { FeatureId.Client, FeatureId.Server, FeatureId.LogCollector } };

      yield return new object[] { new CombinedFeatures(new IFeature[] { new ClientFeature() }), new[] { FeatureId.Server, FeatureId.LogCollector, FeatureId.ServiceMonitor } };
      yield return new object[] { new CombinedFeatures(new IFeature[] { new ClientFeature(), new ServiceMonitorFeature() }), new[] { FeatureId.Server, FeatureId.LogCollector } };
      yield return new object[] { new CombinedFeatures(new IFeature[] { new ClientFeature(), new LogCollectorFeature() }), new[] { FeatureId.Server, FeatureId.ServiceMonitor } };
      yield return new object[] { new CombinedFeatures(new IFeature[] { new ClientFeature(), new ServiceMonitorFeature(), new LogCollectorFeature() }), new[] { FeatureId.Server } };

      yield return new object[] { new CombinedFeatures(new IFeature[] { new ServerFeature() }), new[] { FeatureId.Client, FeatureId.LogCollector, FeatureId.ServiceMonitor } };
      yield return new object[] { new CombinedFeatures(new IFeature[] { new ServerFeature(), new ServiceMonitorFeature() }), new[] { FeatureId.Client, FeatureId.LogCollector } };
      yield return new object[] { new CombinedFeatures(new IFeature[] { new ServerFeature(), new LogCollectorFeature() }), new[] { FeatureId.Client, FeatureId.ServiceMonitor } };
      yield return new object[] { new CombinedFeatures(new IFeature[] { new ServerFeature(), new ServiceMonitorFeature(), new LogCollectorFeature() }), new[] { FeatureId.Client } };

      yield return new object[] { new CombinedFeatures(new IFeature[] { new ClientFeature(), new ServerFeature() }), new[] { FeatureId.LogCollector, FeatureId.ServiceMonitor } };
      yield return new object[] { new CombinedFeatures(new IFeature[] { new ClientFeature(), new ServerFeature(), new ServiceMonitorFeature() }), new[] { FeatureId.LogCollector } };
      yield return new object[] { new CombinedFeatures(new IFeature[] { new ClientFeature(), new ServerFeature(), new LogCollectorFeature() }), new[] { FeatureId.ServiceMonitor } };
      yield return new object[] { new CombinedFeatures(new IFeature[] { new ClientFeature(), new ServerFeature(), new ServiceMonitorFeature(), new LogCollectorFeature() }), new string[0] };

      yield return new object[] { new CombinedFeatures(new IFeature[] { new ServiceMonitorFeature() }), new[] { FeatureId.Client, FeatureId.Server, FeatureId.LogCollector } };
      yield return new object[] { new CombinedFeatures(new IFeature[] { new LogCollectorFeature() }), new[] { FeatureId.Client, FeatureId.Server, FeatureId.ServiceMonitor } };
      yield return new object[] { new CombinedFeatures(new IFeature[] { new ServiceMonitorFeature(), new LogCollectorFeature() }), new[] { FeatureId.Client, FeatureId.Server } };
    }

    #endregion

    [Theory]
    [MemberData(nameof(Should_ExcludeFeature_Test_Cases))]
    void Should_ExcludeFeature_When_FeatureSelectionDoesNotIncludeFeature(IFeature featureSelection, string[] expectedExcludedFeatures)
    {
      Assert.Equal(expectedExcludedFeatures.OrderBy(f => f), featureSelection.ExcludeFeatures.OrderBy(f => f));
    }

    #region Should_ExcludePackage_Test_Cases

    public static IEnumerable<object[]> Should_ExcludePackage_Test_Cases()
    {
      yield return new object[] { new ClientFeature(), new[] { PackageId.VC2008SP1_x86, PackageId.VC2010_x86, PackageId.VC2013_x86 } };
      yield return new object[] { new ServerFeature(), new[] { PackageId.LAVFilters } };
      yield return new object[] { new LogCollectorFeature(), new[] { PackageId.VC2008SP1_x86, PackageId.VC2010_x86, PackageId.VC2013_x86, PackageId.LAVFilters } };
      yield return new object[] { new ServiceMonitorFeature(), new[] { PackageId.VC2008SP1_x86, PackageId.VC2010_x86, PackageId.VC2013_x86, PackageId.LAVFilters } };

      yield return new object[] { new CombinedFeatures(new IFeature[] { new ClientFeature() }), new[] { PackageId.VC2008SP1_x86, PackageId.VC2010_x86, PackageId.VC2013_x86 } };
      yield return new object[] { new CombinedFeatures(new IFeature[] { new ClientFeature(), new ServiceMonitorFeature() }), new[] { PackageId.VC2008SP1_x86, PackageId.VC2010_x86, PackageId.VC2013_x86 } };
      yield return new object[] { new CombinedFeatures(new IFeature[] { new ClientFeature(), new LogCollectorFeature() }), new[] { PackageId.VC2008SP1_x86, PackageId.VC2010_x86, PackageId.VC2013_x86 } };
      yield return new object[] { new CombinedFeatures(new IFeature[] { new ClientFeature(), new ServiceMonitorFeature(), new LogCollectorFeature() }), new[] { PackageId.VC2008SP1_x86, PackageId.VC2010_x86, PackageId.VC2013_x86 } };

      yield return new object[] { new CombinedFeatures(new IFeature[] { new ServerFeature() }), new[] { PackageId.LAVFilters } };
      yield return new object[] { new CombinedFeatures(new IFeature[] { new ServerFeature(), new ServiceMonitorFeature() }), new[] { PackageId.LAVFilters } };
      yield return new object[] { new CombinedFeatures(new IFeature[] { new ServerFeature(), new LogCollectorFeature() }), new[] { PackageId.LAVFilters } };
      yield return new object[] { new CombinedFeatures(new IFeature[] { new ServerFeature(), new ServiceMonitorFeature(), new LogCollectorFeature() }), new[] { PackageId.LAVFilters } };

      yield return new object[] { new CombinedFeatures(new IFeature[] { new ClientFeature(), new ServerFeature() }), new PackageId[0] };
      yield return new object[] { new CombinedFeatures(new IFeature[] { new ClientFeature(), new ServerFeature(), new ServiceMonitorFeature() }), new PackageId[0] };
      yield return new object[] { new CombinedFeatures(new IFeature[] { new ClientFeature(), new ServerFeature(), new LogCollectorFeature() }), new PackageId[0] };
      yield return new object[] { new CombinedFeatures(new IFeature[] { new ClientFeature(), new ServerFeature(), new ServiceMonitorFeature(), new LogCollectorFeature() }), new PackageId[0] };

      yield return new object[] { new CombinedFeatures(new IFeature[] { new ServiceMonitorFeature() }), new[] { PackageId.VC2008SP1_x86, PackageId.VC2010_x86, PackageId.VC2013_x86, PackageId.LAVFilters } };
      yield return new object[] { new CombinedFeatures(new IFeature[] { new LogCollectorFeature() }), new[] { PackageId.VC2008SP1_x86, PackageId.VC2010_x86, PackageId.VC2013_x86, PackageId.LAVFilters } };
      yield return new object[] { new CombinedFeatures(new IFeature[] { new ServiceMonitorFeature(), new LogCollectorFeature() }), new[] { PackageId.VC2008SP1_x86, PackageId.VC2010_x86, PackageId.VC2013_x86, PackageId.LAVFilters } };
    }

    #endregion

    [Theory]
    [MemberData(nameof(Should_ExcludePackage_Test_Cases))]
    void Should_ExcludePackage_When_FeatureSelectionDoesNotIncludePackage(IFeature featureSelection, PackageId[] expectedExcludedPackages)
    {
      Assert.Equal(expectedExcludedPackages.OrderBy(f => f), featureSelection.ExcludePackages.OrderBy(f => f));
    }
  }
}
