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
using MP2BootstrapperApp.ChainPackages;
using MP2BootstrapperApp.Models;
using System.Collections.Generic;
using System.Linq;

namespace MP2BootstrapperApp.ActionPlans
{
  /// <summary>
  /// Implementation of <see cref="IPlan"/> that can request the repair of specified features, and either repair their dependencies or reinstall missing dependencies.
  /// </summary>
  public class RepairPlan : IPlan
  {
    protected IPlanContext _planContext;

    /// <summary>
    /// Creates a new instance of <see cref="RepairPlan"/>.
    /// </summary>
    /// <param name="planContext">The context to use when determining the appropriate dependencies to repair or reinstall.</param>
    public RepairPlan(IPlanContext planContext)
    {
      _planContext = planContext;
    }

    public void SetRequestedInstallStates(IEnumerable<IBundlePackage> packages)
    {
      // Get the currently installed features.
      IBundlePackage featurePackage = packages.FirstOrDefault(p => p.GetId() == _planContext.FeaturePackageId);
      IEnumerable<string> installedFeatures = featurePackage?.Features.Where(f => f.CurrentFeatureState == FeatureState.Local).Select(f => f.FeatureName);

      // Get the packages that are not required to be installed.
      ISet<PackageId> excludedPackages = new HashSet<PackageId>(_planContext.GetExcludedPackagesForFeatures(installedFeatures));

      foreach (IBundlePackage package in packages)
      {
        PackageId packageId = package.GetId();
        // If a package is installed, then request a repair.
        if (package.CurrentInstallState == PackageState.Present)
        {
          package.RequestedInstallState = RequestState.Repair;
        }
        // If not installed and not required, then do nothing
        else if (excludedPackages.Contains(packageId) || package.Optional)
        {
          package.RequestedInstallState = RequestState.None;
        }
        // Else, required package is not installed, so [re]install
        else
        {
          package.RequestedInstallState = RequestState.Present;
        }

        // Ensure that features keep their current installation state.
        if (packageId == _planContext.FeaturePackageId)
        {
          foreach (IBundlePackageFeature feature in package.Features)
            feature.RequestedFeatureState = feature.CurrentFeatureState;
        }
      }
    }
  }
}
