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
  /// Implementation of <see cref="IPluginManager"/> that loads <see cref="IPluginDescriptor"/>s from an <see cref="IPluginLoader"/> implementation.
  /// </summary>
  public class PluginManager : IPluginManager
  {
    protected IReadOnlyCollection<IPluginDescriptor> _pluginDescriptors;

    public PluginManager(IPluginLoader pluginLoader)
    {
      _pluginDescriptors = pluginLoader.GetPluginDescriptors().ToList().AsReadOnly();
    }

    public IReadOnlyCollection<IPluginDescriptor> PluginDescriptors
    {
      get { return _pluginDescriptors; }
    }

    public IList<IPluginDescriptor> GetAvailablePlugins(IPlan plan, IEnumerable<IBundlePackageFeature> bundleFeatures)
    {
      return PluginDescriptors.Where(p => p.CanPluginBeInstalled(plan, bundleFeatures)).ToList();
    }

    public IList<IPluginDescriptor> GetInstalledOrDefaultAvailablePlugins(IPlan plan, IEnumerable<IBundlePackageFeature> bundleFeatures)
    {
      IList<IPluginDescriptor> availablePlugins = GetAvailablePlugins(plan, bundleFeatures);

      // First add plugins where the main feature is currently installed, these get priority when resolving conflicts
      List<IPluginDescriptor> previousOrDefaultPlugins = new List<IPluginDescriptor>(
        availablePlugins.Where(p => ContainsInstalledMainFeature(bundleFeatures, p)).OrderBy(p => !p.IsDefault)
      );

      // Then add plugins with any feature installed, these get priority over default plugins when resolving conflicts
      previousOrDefaultPlugins.AddRange(availablePlugins.Where(
        p => !previousOrDefaultPlugins.Contains(p) && ContainsInstalledPluginFeature(bundleFeatures, p)).OrderBy(p => !p.IsDefault)
      );

      // Finally add default plugins, these have the lowest priority
      previousOrDefaultPlugins.AddRange(
        availablePlugins.Where(p => !previousOrDefaultPlugins.Contains(p) && p.IsDefault)
      );

      // Remove any conflicting plugins, plugins earlier in the list have priority over later plugins when resolving conflicts
      for (int i = 0; i < previousOrDefaultPlugins.Count; i++)
      {
        IPluginDescriptor current = previousOrDefaultPlugins[i];
        foreach (IPluginDescriptor conflict in previousOrDefaultPlugins.Where(p => current.ConflictsWith(p)).ToArray())
          previousOrDefaultPlugins.Remove(conflict);
      }

      return previousOrDefaultPlugins;
    }

    protected static bool ContainsInstalledMainFeature(IEnumerable<IBundlePackageFeature> features, IPluginDescriptor plugin)
    {
      IBundlePackageFeature mainFeature = features.FirstOrDefault(f => f.Id == plugin.MainPluginFeature);
      return mainFeature != null && IsFeatureInstalled(mainFeature);
    }

    protected static bool ContainsInstalledPluginFeature(IEnumerable<IBundlePackageFeature> features, IPluginDescriptor plugin)
    {
      return features.Any(f => plugin.PluginFeatures.Contains(f.Id) && IsFeatureInstalled(f));
    }

    protected static bool IsFeatureInstalled(IBundlePackageFeature feature)
    {
      return feature.PreviousVersionInstalled || feature.CurrentFeatureState == FeatureState.Local;
    }
  }
}
