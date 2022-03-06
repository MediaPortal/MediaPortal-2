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
using MP2BootstrapperApp.Models;
using System.Collections.Generic;

namespace MP2BootstrapperApp.ActionPlans
{
  /// <summary>
  /// Interface for a class that can get package install states and install variables for a particular planned action.
  /// </summary>
  public interface IPlan
  {
    /// <summary>
    /// Gets the action that should be planned.
    /// </summary>
    LaunchAction PlannedAction { get; }

    /// <summary>
    /// Gets the requested install state of a package.
    /// </summary>
    /// <param name="package">The package to get the requested install state of.</param>
    /// <returns>The requested <see cref="RequestState"/> of the package or <c>null</c> if the default <see cref="RequestState"/> should be used.</returns>
    RequestState? GetRequestedInstallState(IBundlePackage package);

    /// <summary>
    /// Gets the requested install state of a feature.
    /// </summary>
    /// <param name="feature">The feature to get the requested install state of. </param>
    /// <returns>The requested <see cref="FeatureState"/> of the feature or <c>null</c> if the default <see cref="FeatureState"/> should be used.</returns>
    FeatureState? GetRequestedInstallState(IBundlePackageFeature feature);

    /// <summary>
    /// Sets a variable that will be passed to the installation engine when this plan is actioned.
    /// </summary>
    /// <param name="name">The name of the variable.</param>
    /// <param name="value">The value of the variable.</param>
    void SetVariable(string name, object value);

    /// <summary>
    /// Gets all variables that have been set by a call to <see cref="SetVariable(string, object)"/>.
    /// </summary>
    /// <returns>Enumeration of variables.</returns>
    IEnumerable<KeyValuePair<string, object>> GetVariables();
  }
}
