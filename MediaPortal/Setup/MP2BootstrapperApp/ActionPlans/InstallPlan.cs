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

using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MP2BootstrapperApp.BundlePackages;
using System.Collections.Generic;

namespace MP2BootstrapperApp.ActionPlans
{
  /// <summary>
  /// Implementation of <see cref="IPlan"/> that can request the install of specified features, specified optional packages, and their dependencies.
  /// </summary>
  public class InstallPlan : SimplePlan
  {
    protected ISet<string> _plannedFeatures;
    protected ISet<PackageId> _plannedOptionalPackages;
    protected ISet<PackageId> _excludedPackages;
    protected IPlanContext _planContext;

    /// <summary>
    /// Creates a new instance of <see cref="InstallPlan"/>.
    /// </summary>
    /// <param name="plannedFeatures">The features to install.</param>
    /// <param name="plannedOptionalPackages">The optional packages to install, or <c>null</c> if not explicitly selecting optional packages.</param>
    /// <param name="planContext">The context to use when determining the appropriate dependencies to install.</param>
    public InstallPlan(IEnumerable<string> plannedFeatures, IEnumerable<PackageId> plannedOptionalPackages, IPlanContext planContext)
      : base(LaunchAction.Install)
    {
      _plannedFeatures = plannedFeatures != null ? new HashSet<string>(plannedFeatures) : new HashSet<string>();
      _plannedOptionalPackages = plannedOptionalPackages != null ? new HashSet<PackageId>(plannedOptionalPackages) : null;
      _planContext = planContext;
    }

    public IEnumerable<string> PlannedFeatures
    {
      get { return _plannedFeatures; }
    }

    public virtual void PlanPackage(PackageId packageId)
    {
      if (_plannedOptionalPackages == null)
        _plannedOptionalPackages = new HashSet<PackageId>();
      _plannedOptionalPackages.Add(packageId);
    }

    public virtual void RemoveFeature(string feature)
    {
      _plannedFeatures.Remove(feature);
      // Re-evaluate excluded packages the next time it's needed
      _excludedPackages = null;
    }

    public virtual void PlanFeature(string feature)
    {
      _plannedFeatures.Add(feature);
      // Re-evaluate excluded packages the next time it's needed
      _excludedPackages = null;
    }

    public override RequestState? GetRequestedInstallState(IBundlePackage package)
    {
      return ShouldInstallPackage(package) ? RequestState.Present : RequestState.None;
    }

    public override FeatureState? GetRequestedInstallState(IBundlePackageFeature feature)
    {
      return ShouldInstallFeature(feature) ? FeatureState.Local : FeatureState.Absent;
    }

    /// <summary>
    /// Determines whether a package should be installed.
    /// </summary>
    /// <param name="package">The package to check.</param>
    /// <returns><c>true</c> if the package should be installed.</returns>
    protected bool ShouldInstallPackage(IBundlePackage package)
    {
      // Get the packages that are not dependencies of the features to install.
      if (_excludedPackages == null)
        _excludedPackages = new HashSet<PackageId>(_planContext.GetExcludedPackagesForFeatures(_plannedFeatures));

      // If package is already present, then no need to install.
      if (package.CurrentInstallState == PackageState.Present)
        return false;

      // Don't install packages when the install condition evaluated to false
      if (!package.EvaluatedInstallCondition)
        return false;

      // If optional packages are being explicitly planned, only install this optional package if explicitly planned.
      if (!package.Vital && _plannedOptionalPackages != null)
        return _plannedOptionalPackages.Contains(package.PackageId);

      // Else install unless explcitly excluded.
      return !_excludedPackages.Contains(package.PackageId);
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
      return feature.Attributes.HasFlag(FeatureAttributes.UIDisallowAbsent) || _plannedFeatures.Count == 0 || _plannedFeatures.Contains(feature.Id);
    }
  }
}
