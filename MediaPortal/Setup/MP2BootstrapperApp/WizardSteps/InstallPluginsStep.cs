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
using MP2BootstrapperApp.BundlePackages;
using MP2BootstrapperApp.BundlePackages.Plugins;
using MP2BootstrapperApp.Models;
using System.Collections.Generic;
using System.Linq;

namespace MP2BootstrapperApp.WizardSteps
{
  /// <summary>
  /// Install step that provides optional plugins that can be individually selected for installation.
  /// </summary>
  public class InstallPluginsStep : AbstractInstallStep, IStep
  {
    protected InstallPlan _installPlan;
    protected ICollection<IBundlePackageFeature> _allFeatures;
    protected bool _showCustomPropertiesStepNext;

    public InstallPluginsStep(IBootstrapperApplicationModel bootstrapperApplicationModel, InstallPlan installPlan, bool showCustomPropertiesStepNext)
      : base(bootstrapperApplicationModel)
    {
      _installPlan = installPlan;
      _showCustomPropertiesStepNext = showCustomPropertiesStepNext;
      _allFeatures = _bootstrapperApplicationModel.MainPackage.Features;
      AvailablePlugins = bootstrapperApplicationModel.PluginManager.GetAvailablePlugins(_installPlan, _allFeatures);
      SelectedPlugins = bootstrapperApplicationModel.PluginManager.GetInstalledOrDefaultAvailablePlugins(_installPlan, _allFeatures);
    }

    /// <summary>
    /// All plugins available for installation.
    /// </summary>
    public ICollection<IPluginDescriptor> AvailablePlugins { get; }

    /// <summary>
    /// Plugins that have been selected for installation.
    /// </summary>
    public ICollection<IPluginDescriptor> SelectedPlugins { get; protected set; }

    /// <summary>
    /// Gets all features that can be installed for the specified plugin.
    /// </summary>
    /// <param name="plugin">The plugin to get the features of.</param>
    /// <returns>Enumeration of <see cref="IBundlePackageFeature"/> that can be installed.</returns>
    public IEnumerable<IBundlePackageFeature> GetAvailableFeaturesForPlugin(IPluginDescriptor plugin)
    {
      return plugin.GetInstallableFeatures(_installPlan, _allFeatures);
    }

    public bool CanGoBack()
    {
      return true;
    }

    public bool CanGoNext()
    {
      return true;
    }

    public IStep Next()
    {
      List<string> pluginsPlanned = new List<string>();
      foreach (IPluginDescriptor plugin in SelectedPlugins)
      {
        IEnumerable<string> conflictingPlugins = pluginsPlanned.Where(p => plugin.ConflictsWith(p));
        if (conflictingPlugins.Any())
        {
          // The view model should prevent multiple conflicting plugins being selected, but just in case log the conflict and skip installation
          _bootstrapperApplicationModel.LogMessage(LogLevel.Error, $"Skipping conflicting plugin, '{plugin.Id}' conflicts with '{string.Join(",", conflictingPlugins.ToArray())}'");
          continue;
        }

        foreach (IBundlePackageFeature feature in plugin.GetInstallableFeatures(_installPlan, _allFeatures))
          _installPlan.PlanFeature(feature.Id);

        pluginsPlanned.Add(plugin.Id);
      }

      if (_showCustomPropertiesStepNext)
        return new InstallCustomPropertiesStep(_bootstrapperApplicationModel, _installPlan);
      else
        return new InstallOverviewStep(_bootstrapperApplicationModel, _installPlan);
    }
  }
}
