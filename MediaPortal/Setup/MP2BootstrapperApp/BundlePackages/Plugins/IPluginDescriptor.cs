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
  /// Interface for a class that combines a collection of related features into a discrete plugin and can determine which of the plugin features can be installed.
  /// </summary>
  /// <remarks>
  /// A plugin is described as a main feature, which is required to be installed and will be used to determine the current installation state of the plugin,
  /// and zero or more optional features that may be installed if their parent features are also being installed. E.g. a plugin might consist of a main server
  /// feature and optional client features, in which case the server must be being installed to allow the plugin to be installed and the client features will
  /// only be installed if the client is also being installed. The features that will be installed can be determined by calling <see cref="GetInstallableFeatures"/>.
  /// </remarks>
  public interface IPluginDescriptor
  {
    /// <summary>
    /// The unique identifier for this plugin.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// The human readable name of this plugin.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// A string that can be used to sort this plugin in relation to others for display.
    /// </summary>
    string SortName { get; }

    /// <summary>
    /// Whether this plugin should be installed by default and preferred over any conflicting plugins.
    /// </summary>
    bool IsDefault { get; }

    /// <summary>
    /// The id of the main feature that is required to be installed by this plugin. 
    /// </summary>
    string MainPluginFeature { get; }

    /// <summary>
    /// The ids of all features that can be installed by this plugin. 
    /// </summary>
    IReadOnlyCollection<string> PluginFeatures { get; }

    /// <summary>
    /// The ids of any features that should prevent this plugin being installed. 
    /// </summary>
    IReadOnlyCollection<string> ExcludedParentFeatures { get; }

    /// <summary>
    /// Determines whether this plugin conflicts with the specified other plugin.
    /// </summary>
    /// <param name="plugin">Plugin to check for conflicts with.</param>
    /// <param name="checkBothDirections">Whether to also inverse the check to see if the other plugin defines a conflict with this plugin.</param>
    /// <returns><c>true</c> if this plugin conflicts with the specified plugin; else <c>false</c>.</returns>
    bool ConflictsWith(IPluginDescriptor plugin, bool checkBothDirections = true);

    /// <summary>
    /// Determines whether this plugin can be installed.
    /// </summary>
    /// <param name="plan">The current installation plan.</param>
    /// <param name="bundleFeatures">Enumeration of all features in the installation bundle.</param>
    /// <returns><c>true</c> if the main feature can be installed and no excluded parent features are being installed; else <c>false</c>.</returns>
    bool CanPluginBeInstalled(IPlan plan, IEnumerable<IBundlePackageFeature> bundleFeatures);

    /// <summary>
    /// Gets the the features in this plugin that can be installed, based on whether their parent features are being installed.
    /// </summary>
    /// <param name="plan">The current installation plan.</param>
    /// <param name="bundleFeatures">Enumeration of all features in the installation bundle.</param>
    /// <returns>List of feature contained in this plugin where the parent feature is being installed.</returns>
    IList<IBundlePackageFeature> GetInstallableFeatures(IPlan plan, IEnumerable<IBundlePackageFeature> bundleFeatures);
  }
}
