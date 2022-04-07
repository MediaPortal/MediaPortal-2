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
using System.Collections.Generic;
using System.Linq;

namespace MP2BootstrapperApp.ActionPlans
{
  /// <summary>
  /// Implementation of <see cref="IPlanContext"/> that provides context for an installation of MediaPortal 2.
  /// </summary>
  public class PlanContext : IPlanContext
  {
    // Packages not required for each feature, implemented like this, rather than specifying the required packages, so that
    // a new package added later will be installed by default rather than having to be explcitly added here.
    protected static readonly IDictionary<string, PackageId[]> _excludedPackages = new Dictionary<string, PackageId[]>
    {
      { FeatureId.Client, new[]{ PackageId.VC2008SP1_x86, PackageId.VC2010_x86, PackageId.VC2013_x86 } },
      { FeatureId.Server, new[]{ PackageId.LAVFilters } },
      { FeatureId.ServiceMonitor, new[]{ PackageId.VC2008SP1_x86, PackageId.VC2010_x86, PackageId.VC2013_x86, PackageId.LAVFilters } },
      { FeatureId.LogCollector, new[]{ PackageId.VC2008SP1_x86, PackageId.VC2010_x86, PackageId.VC2013_x86, PackageId.LAVFilters } }
    };

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public PackageId FeaturePackageId
    {
      get { return PackageId.MediaPortal2; }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    /// <param name="feature"><inheritdoc/></param>
    /// <returns><inheritdoc/></returns>
    public IEnumerable<PackageId> GetExcludedPackagesForFeature(string feature)
    {
      return _excludedPackages.TryGetValue(feature, out PackageId[] packageIds) ? packageIds : new PackageId[0];
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    /// <param name="features"><inheritdoc/></param>
    /// <returns><inheritdoc/></returns>
    public IEnumerable<PackageId> GetExcludedPackagesForFeatures(IEnumerable<string> features)
    {
      if (features == null || features.Count() == 0)
        return new List<PackageId>();
      return features.Select(f => GetExcludedPackagesForFeature(f)).Aggregate((p1, p2) => p1.Intersect(p2));
    }
  }
}
