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
using System.Collections.Generic;
using System.Linq;

namespace MP2BootstrapperApp.BundlePackages.Plugins
{
  /// <summary>
  /// Class that groups a collection of related features into a discrete plugin and can determine which of the plugin features can be installed.
  /// </summary>
  /// <remarks>
  /// A plugin may be made up of both client and server features, on a client or server only install only the client or server features respectively
  /// should be installed, these features can be determined by calling <see cref="GetFeaturesWhereParentIsBeingInstalled"/>.
  /// </remarks>
  public class PluginBase
  {
    protected string _id;
    protected string _name;
    protected IReadOnlyCollection<string> _featureIds;
    protected ISet<string> _conflictingPluginIds;

    /// <summary>
    /// Creates a new instance of a plugin definition.
    /// </summary>
    /// <param name="id">The unique indentifier of the plugin.</param>
    /// <param name="name">The human readable name of the plugin.</param>
    /// <param name="featureIds">Ids of the features that are included in the plugin.</param>
    /// <param name="conflictingPluginIds">Ids of the plugins that this plugin conflicts with.</param>
    public PluginBase(string id, string name, IEnumerable<string> featureIds, IEnumerable<string> conflictingPluginIds)
    {
      _id = id;
      _name = name;
      _featureIds = featureIds != null ? new HashSet<string>(featureIds) : new HashSet<string>();
      _conflictingPluginIds = conflictingPluginIds != null ? new HashSet<string>(conflictingPluginIds) : new HashSet<string>();
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
    /// The ids of the features contained in this plugin. 
    /// </summary>
    public IReadOnlyCollection<string> FeatureIds
    {
      get { return _featureIds; }
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
    /// Gets the ids of the features in this plugin that can be installed, based on whether their parent features are being installed.
    /// </summary>
    /// <param name="plan">The current installation plan.</param>
    /// <param name="bundleFeatures">Enumeration of all features in the installation bundle.</param>
    /// <returns>List of feature ids contained in this plugin where the parent feature is being installed.</returns>
    public IList<string> GetFeaturesWhereParentIsBeingInstalled(IPlan plan, IEnumerable<IBundlePackageFeature> bundleFeatures)
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
      return _featureIds.Select(id => bundleFeatures.FirstOrDefault(f => f.Id == id)).Where(f => f != null);
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
      return feature != null && (pluginFeaturesPlanned.Contains(feature.Id) || plan.GetRequestedInstallState(feature) == FeatureState.Local);
    }
  }
}
