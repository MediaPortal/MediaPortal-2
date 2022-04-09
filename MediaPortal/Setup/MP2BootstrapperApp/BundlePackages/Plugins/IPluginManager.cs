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
using System.Collections.Generic;

namespace MP2BootstrapperApp.BundlePackages.Plugins
{
  /// <summary>
  /// Interface for a class that can instantiate and manage <see cref="IPluginDescriptor"/>s.
  /// </summary>
  public interface IPluginManager
  {
    /// <summary>
    /// Collection of all plugin descriptors managed by this class.
    /// </summary>
    IReadOnlyCollection<IPluginDescriptor> PluginDescriptors { get; }

    /// <summary>
    /// Gets the plugins that can be installed based on the features that are planned to be installed.
    /// </summary>
    /// <param name="plan">The installation plan specifying which features are currently being installed.</param>
    /// <param name="bundleFeatures">All features available in the bundle.</param>
    /// <returns>List of <see cref="IPluginDescriptor"/> containing all plugins that can be installed.</returns>
    IList<IPluginDescriptor> GetAvailablePlugins(IPlan plan, IEnumerable<IBundlePackageFeature> bundleFeatures);

    /// <summary>
    /// Gets the available plugins that are either installed or should be installed by default, minus any conflicting plugins.
    /// </summary>
    /// <param name="plan">The installation plan specifying which features are currently being installed.</param>
    /// <param name="bundleFeatures">All features available in the bundle.</param>    /// 
    /// <returns>List of <see cref="IPluginDescriptor"/> containing all plugins that are currently installed or should be installed by default.</returns>
    IList<IPluginDescriptor> GetInstalledOrDefaultAvailablePlugins(IPlan plan, IEnumerable<IBundlePackageFeature> bundleFeatures);
  }
}
