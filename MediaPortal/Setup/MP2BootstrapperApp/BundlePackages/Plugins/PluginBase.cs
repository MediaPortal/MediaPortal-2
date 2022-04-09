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
using MP2BootstrapperApp.ActionPlans;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MP2BootstrapperApp.BundlePackages.Plugins
{
  /// <summary>
  /// Class that groups a collection of related features into a discrete plugin and can determine which of the plugin features can be installed.
  /// </summary>
  /// <remarks>
  /// A plugin may be made up of both client and server features, on a client or server only install only the client or server features respectively
  /// should be installed, these features can be determined by calling <see cref="GetInstallableFeatures"/>.
  /// </remarks>
  public class PluginBase
  {
    protected string _id;
    protected string _name;
    protected string _mainPluginFeature;
    protected IReadOnlyCollection<string> _pluginFeatures;
    protected IReadOnlyCollection<string> _excludedParentFeatures;
    protected ISet<string> _conflictingPluginIds;

    /// <summary>
    /// Creates a new instance of a plugin definition.
    /// </summary>
    /// <param name="id">The unique indentifier of the plugin.</param>
    /// <param name="name">The human readable name of the plugin.</param>
    /// <param name="mainPluginFeature">Id of the main feature for this plugin, this will be used to determine the current installation state of this plugin and whether the plugin can be installed.</param>
    /// <param name="optionalPluginFeatures">Ids of optional features that will be installed if their parent features are also being installed.</param>
    /// <param name="excludedParentFeatures">Ids of any parent features that should not be installed to allow this plugin to be installed.</param>
    /// <param name="conflictingPluginIds">Ids of the plugins that this plugin conflicts with.</param>
    /// 
    public PluginBase(string id, string name, string mainPluginFeature, IEnumerable<string> optionalPluginFeatures, IEnumerable<string> excludedParentFeatures, IEnumerable<string> conflictingPluginIds)
    {
      if (mainPluginFeature == null)
        throw new ArgumentNullException($"{nameof(mainPluginFeature)} cannot be null");

      _id = id;
      _name = name;
      _mainPluginFeature = mainPluginFeature;
      _excludedParentFeatures = excludedParentFeatures != null ? new HashSet<string>(excludedParentFeatures) : new HashSet<string>();
      _conflictingPluginIds = conflictingPluginIds != null ? new HashSet<string>(conflictingPluginIds) : new HashSet<string>();

      HashSet<string> pluginFeatures = optionalPluginFeatures != null ? new HashSet<string>(optionalPluginFeatures) : new HashSet<string>();
      pluginFeatures.Add(mainPluginFeature);
      _pluginFeatures = pluginFeatures;
    }

    /// <summary>
    /// The unique identifier for this plugin.
    /// </summary>
    public string Id
    {
      get { return _id; }
    }

    /// <summary>
    /// The human readable name of this plugin.
    /// </summary>
    public string Name
    {
      get { return _name; }
    }

    /// <summary>
    /// The id of the main feature that is required to be installed by this plugin. 
    /// </summary>
    public string MainPluginFeature
    {
      get { return _mainPluginFeature; }
    }

    /// <summary>
    /// The ids of all features that can be installed by this plugin. 
    /// </summary>
    public IReadOnlyCollection<string> PluginFeatures
    {
      get { return _pluginFeatures; }
    }

    /// <summary>
    /// The ids of any features that should prevent this plugin being installed. 
    /// </summary>
    public IReadOnlyCollection<string> ExcludedParentFeatures
    {
      get { return _excludedParentFeatures; }
    }

    /// <summary>
    /// Determines whether this plugin conflicts with the plugin with the given id.
    /// </summary>
    /// <param name="pluginId">Id of the plugin to determine whether this plugin conflicts with.</param>
    /// <returns><c>true</c> if this plugin conflicts with the specified plugin; else <c>false</c>.</returns>
    public bool ConflictsWith(string pluginId)
    {
      return _conflictingPluginIds.Contains(pluginId);
    }

    /// <summary>
    /// Determines whether this plugin can be installed.
    /// </summary>
    /// <param name="plan">The current installation plan.</param>
    /// <param name="bundleFeatures">Enumeration of all features in the installation bundle.</param>
    /// <returns><c>true</c> if all necessary parent features can be installed and no excluded parent features are being installed; else <c>false</c>.</returns>
    public bool CanPluginBeInstalled(IPlan plan, IEnumerable<IBundlePackageFeature> bundleFeatures)
    {
      return _excludedParentFeatures.All(f => !IsFeatureBeingInstalled(GetFeature(f, bundleFeatures), plan, null)) && GetInstallableFeatures(plan, bundleFeatures).Contains(_mainPluginFeature);
    }

    /// <summary>
    /// Gets the ids of the features in this plugin that can be installed, based on whether their parent features are being installed.
    /// </summary>
    /// <param name="plan">The current installation plan.</param>
    /// <param name="bundleFeatures">Enumeration of all features in the installation bundle.</param>
    /// <returns>List of feature ids contained in this plugin where the parent feature is being installed.</returns>
    public IList<string> GetInstallableFeatures(IPlan plan, IEnumerable<IBundlePackageFeature> bundleFeatures)
    {
      var features = GetPluginFeatures(bundleFeatures);
      // Features are hierarchical, we only want features where the parent feature is also being installed.
      // This plugin might contain features that are children of other features in this plugin so on the
      // initial pass those child features will fail the check because we haven't determined that their parent
      // feature can be installed yet, so keep track of any plugin features that can be installed and check
      // against this on subsequent iterations until no more installable features are found.
      List<string> allPlannedFeatures = new List<string>();
      while (true)
      {
        List<string> plannedFeatures = features.Where(f => !allPlannedFeatures.Contains(f.Id) && IsFeatureBeingInstalled(GetFeature(f.Parent, bundleFeatures), plan, allPlannedFeatures)).Select(f => f.Id).ToList();
        if (plannedFeatures.Count == 0)
          break;
        allPlannedFeatures.AddRange(plannedFeatures);
      }
      return allPlannedFeatures;
    }

    /// <summary>
    /// Gets an enumeration of <see cref="IBundlePackageFeature"/> that are included in this plugin.
    /// </summary>
    /// <param name="bundleFeatures">Enumeration of all features in the installation bundle.</param>
    /// <returns>Enumeration of <see cref="IBundlePackageFeature"/> that are included in this plugin.</returns>
    protected IEnumerable<IBundlePackageFeature> GetPluginFeatures(IEnumerable<IBundlePackageFeature> bundleFeatures)
    {
      return _pluginFeatures.Select(id => bundleFeatures.FirstOrDefault(f => f.Id == id)).Where(f => f != null);
    }

    /// <summary>
    /// Gets the <see cref="IBundlePackageFeature"/> with a specified id from an enumeration of features.
    /// </summary>
    /// <param name="id">The id of the feature to get.</param>
    /// <param name="bundleFeatures">Enumeration of all features in the installation bundle.</param>
    /// <returns>The feature with the specified id; else <c>null</c>.</returns>
    protected IBundlePackageFeature GetFeature(string id, IEnumerable<IBundlePackageFeature> bundleFeatures)
    {
      return bundleFeatures.FirstOrDefault(f => f.Id == id);
    }

    /// <summary>
    /// Determines whether a feature is being planned for installation by checking the
    /// current installation plan and the list of plugin features that will be planned.
    /// </summary>
    /// <param name="feature">The feature to check.</param>
    /// <param name="plan">The current installation plan.</param>
    /// <param name="pluginFeaturesPlanned">List of features in this plugin that will be installed.</param>
    /// <returns><c>true</c> if the specified feature is planned for installation; else <c>false</c>.</returns>
    protected bool IsFeatureBeingInstalled(IBundlePackageFeature feature, IPlan plan, IList<string> pluginFeaturesPlanned)
    {
      return feature != null && ((pluginFeaturesPlanned?.Contains(feature.Id) ?? false) || plan.GetRequestedInstallState(feature) == FeatureState.Local);
    }
  }
}
