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
  /// Implementation of <see cref="IPlan"/> that can modify the existing install of specified features, specified optional packages, and their dependencies.
  /// </summary>
  public class ModifyPlan : IPlan
  {
    protected ISet<FeatureId> _plannedFeatures;
    protected ISet<PackageId> _plannedOptionalPackages;
    protected IPlanContext _planContext;

    /// <summary>
    /// Creates a new instance of <see cref="ModifyPlan"/>.
    /// </summary>
    /// <param name="plannedFeatures">The features to modify.</param>
    /// <param name="plannedOptionalPackages">The optional packages to modify, or <c>null</c> if not explicitly modifying optional packages.</param>
    /// <param name="planContext">The context to use when determining the appropriate dependencies to install.</param>
    public ModifyPlan(IEnumerable<FeatureId> plannedFeatures, IEnumerable<PackageId> plannedOptionalPackages, IPlanContext planContext)
    {
      _plannedFeatures = plannedFeatures != null ? new HashSet<FeatureId>(plannedFeatures) : new HashSet<FeatureId>();
      _plannedOptionalPackages = plannedOptionalPackages != null ? new HashSet<PackageId>(plannedOptionalPackages) : null;
      _planContext = planContext;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    /// <param name="packages"><inheritdoc/></param>
    public void SetRequestedInstallStates(IEnumerable<IBundlePackage> packages)
    {
      IEnumerable<FeatureId> currentlyInstalledFeatures =
        packages.FirstOrDefault(p => p.PackageId == _planContext.FeaturePackageId)?.Features
        .Where(f => f.CurrentFeatureState == FeatureState.Local).Select(f => f.Id);

      // Get the packages that are not dependencies of the features currently installed.
      HashSet<PackageId> existingExcludedPackages = new HashSet<PackageId>(_planContext.GetExcludedPackagesForFeatures(currentlyInstalledFeatures));
      // Get the packages that are not dependencies of the features to install.
      HashSet<PackageId> modifiedExcludedPackages = new HashSet<PackageId>(_planContext.GetExcludedPackagesForFeatures(_plannedFeatures));

      foreach (IBundlePackage package in packages)
      {
        package.RequestedInstallState = ShouldInstallPackage(package, existingExcludedPackages, modifiedExcludedPackages) ? RequestState.Present : RequestState.None;

        if (package.PackageId == _planContext.FeaturePackageId)
        {
          foreach (IBundlePackageFeature feature in package.Features)
            feature.RequestedFeatureState = ShouldInstallFeature(feature) ? FeatureState.Local : FeatureState.Absent;
        }
      }
    }

    /// <summary>
    /// Determines whether a package should be installed.
    /// </summary>
    /// <param name="package">The package to check.</param>
    /// <param name="existingExcludedPackages">The ids of packages that should not be installed with the existing features.</param>
    /// <param name="modifiedExcludedPackages">The ids of packages that should not be installed with the modified features.</param>
    /// <returns><c>true</c> if the package should be installed.</returns>
    protected bool ShouldInstallPackage(IBundlePackage package, ISet<PackageId> existingExcludedPackages, ISet<PackageId> modifiedExcludedPackages)
    {
      // If package is already present, then no need to install.
      if (package.CurrentInstallState == PackageState.Present)
        return false;

      // If optional packages are being explicitly planned, only install this optional package if explicitly planned.
      if (package.Optional && _plannedOptionalPackages != null)
        return _plannedOptionalPackages.Contains(package.PackageId);

      // Else install if previously excluded and not currently excluded.
      return existingExcludedPackages.Contains(package.PackageId) && !modifiedExcludedPackages.Contains(package.PackageId);
    }

    /// <summary>
    /// Determines whether a feature should be installed.
    /// </summary>
    /// <param name="feature">The feature to check.</param>
    /// <returns><c>true</c> if the feature should be planned for installation.</returns>
    protected bool ShouldInstallFeature(IBundlePackageFeature feature)
    {
      // Non-optional features should always be installed. For optional features, either install all
      // if no features are explicitly planned, else just install the planned features.
      return !feature.Optional || _plannedFeatures.Count == 0 || _plannedFeatures.Contains(feature.Id);
    }
  }
}
