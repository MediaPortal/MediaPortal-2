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

using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MP2BootstrapperApp.BundlePackages;
using System.Collections.Generic;

namespace MP2BootstrapperApp.ActionPlans
{
  /// <summary>
  /// Implementation of <see cref="IPlan"/> that can request the repair of specified features, and either repair their dependencies or reinstall missing dependencies.
  /// </summary>
  public class RepairPlan : SimplePlan
  {
    protected ISet<PackageId> _excludedPackages;
    protected IPlanContext _planContext;

    /// <summary>
    /// Creates a new instance of <see cref="RepairPlan"/>.
    /// </summary>
    /// <param name="installedFeatures">The currently installed features.</param>
    /// <param name="planContext">The context to use when determining the appropriate dependencies to repair or reinstall.</param>
    public RepairPlan(IEnumerable<FeatureId> installedFeatures, IPlanContext planContext)
      : base(LaunchAction.Repair)
    {
      _planContext = planContext;

      // Get the packages that are not required to be installed.
      _excludedPackages = new HashSet<PackageId>(planContext.GetExcludedPackagesForFeatures(installedFeatures));
    }

    public override RequestState? GetRequestedInstallState(IBundlePackage package)
    {
      // If a package is installed, then request a repair.
      if (package.CurrentInstallState == PackageState.Present)
      {
        return RequestState.Repair;
      }
      // If not installed and not required, then do nothing
      else if (_excludedPackages.Contains(package.PackageId) || !package.Vital || !package.EvaluatedInstallCondition)
      {
        return RequestState.None;
      }
      // Else, required package is not installed, so [re]install
      else
      {
        return RequestState.Present;
      }
    }

    public override FeatureState? GetRequestedInstallState(IBundlePackageFeature feature)
    {
      // Ensure that features keep their current installation state.
      return feature.CurrentFeatureState;
    }
  }
}
