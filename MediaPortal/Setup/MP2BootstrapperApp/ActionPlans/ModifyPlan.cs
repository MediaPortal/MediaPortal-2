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
  /// Implementation of <see cref="IPlan"/> that can modify the install of specified features, specified optional packages, and their dependencies.
  /// </summary>
  public class ModifyPlan : InstallPlan
  {
    /// <summary>
    /// Creates a new instance of <see cref="ModifyPlan"/>.
    /// </summary>
    /// <param name="plannedFeatures">The features to install.</param>
    /// <param name="plannedOptionalPackages">The optional packages to install, or <c>null</c> if not explicitly selecting optional packages.</param>
    /// <param name="planContext">The context to use when determining the appropriate dependencies to install.</param>
    public ModifyPlan(IEnumerable<FeatureId> plannedFeatures, IEnumerable<PackageId> plannedOptionalPackages, IPlanContext planContext)
      : base(plannedFeatures, plannedOptionalPackages, planContext)
    {
      _plannedAction = LaunchAction.Modify;
    }

    public override RequestState? GetRequestedInstallState(IBundlePackage package)
    {
      // The base implementation returns a RequestState of None for already installed packages,
      // which when modifying an existing install will mean that the main MP2 package gets a
      // request state of None. However this means that the underlying engine will skip the package and
      // won't request the modified state of the features, which may have changed, so override it so that
      // the main package is always requested present when modifying.
      if (package.PackageId == _planContext.FeaturePackageId)
        return RequestState.Present;
      else
        return base.GetRequestedInstallState(package);
    }
  }
}
