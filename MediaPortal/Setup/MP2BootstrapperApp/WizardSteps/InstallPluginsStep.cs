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
using MP2BootstrapperApp.BundlePackages;
using MP2BootstrapperApp.BundlePackages.Features;
using MP2BootstrapperApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using WixToolset.Mba.Core;

namespace MP2BootstrapperApp.WizardSteps
{
  /// <summary>
  /// Install step that provides optional plugins that can be individually selected for installation.
  /// </summary>
  public class InstallPluginsStep : AbstractInstallStep, IStep
  {
    protected InstallPlan _installPlan;
    protected ICollection<IBundlePackageFeature> _initiallyPlannedFeatures;
    protected ICollection<IBundlePackageFeature> _allFeatures;
    protected bool _showCustomPropertiesStepNext;

    public InstallPluginsStep(IBootstrapperApplicationModel bootstrapperApplicationModel, InstallPlan installPlan, bool showCustomPropertiesStepNext)
      : base(bootstrapperApplicationModel)
    {
      _installPlan = installPlan;
      _allFeatures = _bootstrapperApplicationModel.MainPackage.Features;
      _initiallyPlannedFeatures = _installPlan.GetPlannedFeatures(_allFeatures).ToList();
      _showCustomPropertiesStepNext = showCustomPropertiesStepNext;

      AvailableFeatures = FeatureUtils.GetSelectableChildFeatures(_initiallyPlannedFeatures, _allFeatures);
      SelectedFeatures = GetInitiallySelectedFeatures();
    }

    protected ICollection<IBundlePackageFeature> GetInitiallySelectedFeatures()
    {
      List<IBundlePackageFeature> installedFeatures = AvailableFeatures.Where(f => f.PreviousVersionInstalled || f.CurrentFeatureState == FeatureState.Local).ToList();
      if (installedFeatures.Count > 0)
        return installedFeatures;
      return AvailableFeatures.Where(f => f.InstallLevel == 1).ToList();
    }

    /// <summary>
    /// All features available for installation.
    /// </summary>
    public ICollection<IBundlePackageFeature> AvailableFeatures { get; }

    /// <summary>
    /// Features that have been selected for installation.
    /// </summary>
    public ICollection<IBundlePackageFeature> SelectedFeatures { get; protected set; }

    /// <summary>
    /// Gets all features that will be installed with the specified feature.
    /// </summary>
    /// <param name="feature">The main feature.</param>
    /// <returns>Enumeration of <see cref="IBundlePackageFeature"/> that will be installed.</returns>
    public IEnumerable<IBundlePackageFeature> GetInstallableFeatureAndRelations(IBundlePackageFeature feature)
    {
      return FeatureUtils.GetInstallableFeatureAndRelations(feature, _initiallyPlannedFeatures, _allFeatures);
    }

    public IEnumerable<string> GetConflicts(string featureId)
    {
      IBundlePackageFeature feature = AvailableFeatures.FirstOrDefault(f => f.Id == featureId) ?? throw new InvalidOperationException($"Feature with id {featureId} is not contained in {nameof(AvailableFeatures)}");
      return FeatureUtils.GetConflicts(feature, AvailableFeatures).Select(f => f.Id);
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
      List<IBundlePackageFeature> plannedFeatures = new List<IBundlePackageFeature>(_initiallyPlannedFeatures);
      foreach (IBundlePackageFeature feature in SelectedFeatures)
      {
        ICollection<IBundlePackageFeature> installableFeatures = FeatureUtils.GetInstallableFeatureAndRelations(feature, plannedFeatures, _allFeatures);
        foreach (IBundlePackageFeature installableFeature in installableFeatures)
          if (!FeatureUtils.GetConflicts(installableFeature, plannedFeatures).Any())
            plannedFeatures.Add(installableFeature);
      }

      _installPlan.PlanChildFeatures(plannedFeatures.Except(_initiallyPlannedFeatures).Select(f => f.Id));

      if (_showCustomPropertiesStepNext)
        return new InstallCustomPropertiesStep(_bootstrapperApplicationModel, _installPlan);
      else
        return new InstallOverviewStep(_bootstrapperApplicationModel, _installPlan);
    }
  }
}
