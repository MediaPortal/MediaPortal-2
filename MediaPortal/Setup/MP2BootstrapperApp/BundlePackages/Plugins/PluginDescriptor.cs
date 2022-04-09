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
  /// Base implementation of <see cref="IPluginDescriptor"/>.
  /// </summary>
  public abstract class PluginDescriptor : IPluginDescriptor
  {
    protected string _id;
    protected string _name;
    protected string _sortName;
    protected bool _isDefault;
    protected string _mainPluginFeature;
    protected IReadOnlyCollection<string> _pluginFeatures;
    protected IReadOnlyCollection<string> _excludedParentFeatures;
    protected ISet<string> _conflictingPluginIds;

    /// <summary>
    /// Creates a new instance of a plugin descriptor.
    /// </summary>
    /// <param name="id">The unique indentifier of the plugin.</param>
    /// <param name="name">The human readable name of the plugin.</param>
    /// <param name="sortName">A string to use to order this plugin in relation to others.</param>
    /// <param name="isDefault">Whether this plugin should be installed by default and preferred over any conflicting plugins.</param>
    /// <param name="mainPluginFeature">Id of the main feature for this plugin, this will be used to determine the current installation state of this plugin and whether the plugin can be installed.</param>
    /// <param name="optionalPluginFeatures">Ids of optional features that will be installed if their parent features are also being installed.</param>
    /// <param name="excludedParentFeatures">Ids of any parent features that should not be installed to allow this plugin to be installed.</param>
    /// <param name="conflictingPluginIds">Ids of the plugins that this plugin conflicts with.</param>
    /// 
    public PluginDescriptor(string id, string name, string sortName, bool isDefault, string mainPluginFeature, IEnumerable<string> optionalPluginFeatures, IEnumerable<string> excludedParentFeatures, IEnumerable<string> conflictingPluginIds)
    {
      if (mainPluginFeature == null)
        throw new ArgumentNullException($"{nameof(mainPluginFeature)} cannot be null");

      _id = id;
      _name = name;
      _sortName = sortName;
      _isDefault = isDefault;
      _mainPluginFeature = mainPluginFeature;
      _excludedParentFeatures = excludedParentFeatures != null ? new HashSet<string>(excludedParentFeatures) : new HashSet<string>();
      _conflictingPluginIds = conflictingPluginIds != null ? new HashSet<string>(conflictingPluginIds) : new HashSet<string>();

      HashSet<string> pluginFeatures = optionalPluginFeatures != null ? new HashSet<string>(optionalPluginFeatures) : new HashSet<string>();
      pluginFeatures.Add(mainPluginFeature);
      _pluginFeatures = pluginFeatures;
    }

    public string Id
    {
      get { return _id; }
    }

    public string Name
    {
      get { return _name; }
    }

    public string SortName
    {
      get { return _sortName; }
    }

    public bool IsDefault
    {
      get { return _isDefault; }
    }

    public string MainPluginFeature
    {
      get { return _mainPluginFeature; }
    }

    public IReadOnlyCollection<string> PluginFeatures
    {
      get { return _pluginFeatures; }
    }

    public IReadOnlyCollection<string> ExcludedParentFeatures
    {
      get { return _excludedParentFeatures; }
    }

    public bool ConflictsWith(IPluginDescriptor plugin, bool checkBothDirections = true)
    {
      if (plugin == null)
        return false;
      return _conflictingPluginIds.Contains(plugin.Id) || (checkBothDirections && plugin.ConflictsWith(this, false));
    }

    public bool CanPluginBeInstalled(IPlan plan, IEnumerable<IBundlePackageFeature> bundleFeatures)
    {
      return _excludedParentFeatures.All(f => !IsFeatureBeingInstalled(f, plan, bundleFeatures, null)) && GetInstallableFeatures(plan, bundleFeatures).Any(f => f.Id == _mainPluginFeature);
    }

    public IList<IBundlePackageFeature> GetInstallableFeatures(IPlan plan, IEnumerable<IBundlePackageFeature> bundleFeatures)
    {
      var features = GetPluginFeatures(bundleFeatures);
      // Features are hierarchical, we only want features where the parent feature is also being installed.
      // This plugin might contain features that are children of other features in this plugin so on the
      // initial pass those child features will fail the check because we haven't determined that their parent
      // feature can be installed yet, so keep track of any plugin features that can be installed and check
      // against this on subsequent iterations until no more installable features are found.
      List<IBundlePackageFeature> allInstallableFeatures = new List<IBundlePackageFeature>();
      while (true)
      {
        // Installable features are features where their parent is also being installed
        List<IBundlePackageFeature> installableFeatures = features.Where(f => !allInstallableFeatures.Contains(f) && IsFeatureBeingInstalled(f.Parent, plan, bundleFeatures, allInstallableFeatures)).ToList();
        if (installableFeatures.Count == 0)
          break;
        allInstallableFeatures.AddRange(installableFeatures);
      }
      return allInstallableFeatures;
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
    /// Determines whether a feature is being planned for installation by checking the
    /// current installation plan and the list of plugin features that will be planned.
    /// </summary>
    /// <param name="featureId">The id of the feature to check.</param>
    /// <param name="plan">The current installation plan.</param>
    /// <param name="bundleFeatures">Enumeration of all features in the installation bundle.</param>
    /// <param name="pluginFeaturesPlanned">List of features in this plugin that will be installed.</param>
    /// <returns><c>true</c> if the specified feature is planned for installation; else <c>false</c>.</returns>
    protected bool IsFeatureBeingInstalled(string featureId, IPlan plan, IEnumerable<IBundlePackageFeature> bundleFeatures, IList<IBundlePackageFeature> pluginFeaturesPlanned)
    {
      IBundlePackageFeature feature = bundleFeatures.FirstOrDefault(f => f.Id == featureId);
      return feature != null && ((pluginFeaturesPlanned?.Contains(feature) ?? false) || plan.GetRequestedInstallState(feature) == FeatureState.Local);
    }
  }
}
