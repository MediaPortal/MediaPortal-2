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

namespace MP2BootstrapperApp.ActionPlans
{
  /// <summary>
  /// Interface that provides context information when planning an installation action.
  /// </summary>
  public interface IPlanContext
  {
    /// <summary>
    /// The id of the main package containing features to install.
    /// </summary>
    PackageId FeaturePackageId { get; }

    /// <summary>
    /// Gets the packages that are not required to be installed for a specified feature.
    /// </summary>
    /// <param name="feature">The feature to use when determining packages that are not required.</param>
    /// <returns>Enumeration of packages not required to be installed for the specified feature.</returns>
    IEnumerable<PackageId> GetExcludedPackagesForFeature(FeatureId feature);

    /// <summary>
    /// Gets the packages that are not required to be installed for any of the specified features.
    /// </summary>
    /// <param name="features">The features to use when determining packages that are not required.</param>
    /// <returns>Enumeration of packages not required to be installed for any of the specified features.</returns>
    IEnumerable<PackageId> GetExcludedPackagesForFeatures(IEnumerable<FeatureId> features);
  }
}
