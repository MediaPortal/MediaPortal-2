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

using MP2BootstrapperApp.ActionPlans;
using MP2BootstrapperApp.ChainPackages;
using MP2BootstrapperApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MP2BootstrapperApp.WizardSteps
{
  /// <summary>
  /// Custom install step that provides optional packages and features that can be indiviually selected for installation.
  /// </summary>
  public class InstallCustomStep : AbstractInstallStep, IStep
  {
    private static readonly Version ZERO_VERSION = new Version();

    public InstallCustomStep(IBootstrapperApplicationModel bootstrapperApplicationModel)
      : base(bootstrapperApplicationModel)
    {
      AvailableFeatures = GetSelectableFeatures(bootstrapperApplicationModel.BundlePackages);
      AvailablePackages = GetSelectablePackages(bootstrapperApplicationModel.BundlePackages);

      bool isPreviousVersionInstalled = AvailableFeatures.Any(f => f.PreviousVersionInstalled);
      if (isPreviousVersionInstalled)
      {
        // If a previous installation exists, initially select the previously installed features and packages
        SelectedFeatures = AvailableFeatures.Where(f => f.PreviousVersionInstalled).ToList();
        SelectedPackages = AvailablePackages.Where(p => p.InstalledVersion != ZERO_VERSION).ToList();
      }
      else
      {
        // Else initially select all features and packages
        SelectedFeatures = new List<IBundlePackageFeature>(AvailableFeatures);
        SelectedPackages = new List<IBundlePackage>(AvailablePackages);
      }
    }

    /// <summary>
    /// All features available for installation.
    /// </summary>
    public ICollection<IBundlePackageFeature> AvailableFeatures { get; }

    /// <summary>
    /// Features that have been selected for installation.
    /// </summary>
    public ICollection<IBundlePackageFeature> SelectedFeatures { get; }

    /// <summary>
    /// All packages available for installation.
    /// </summary>
    public ICollection<IBundlePackage> AvailablePackages { get; }

    /// <summary>
    /// Packages selected for installation.
    /// </summary>
    public ICollection<IBundlePackage> SelectedPackages { get; }

    public IStep Next()
    {
      IEnumerable<string> features = SelectedFeatures.Select(f => f.FeatureName);
      IEnumerable<PackageId> packages = SelectedPackages.Select(p => p.GetId());

      InstallPlan plan = new InstallPlan(features, packages, new PlanContext());
      plan.SetRequestedInstallStates(_bootstrapperApplicationModel.BundlePackages);

      return new InstallOverviewStep(_bootstrapperApplicationModel);
    }

    public bool CanGoNext()
    {
      return SelectedFeatures.Count > 0;
    }

    public bool CanGoBack()
    {
      return true;
    }

    protected ICollection<IBundlePackageFeature> GetSelectableFeatures(IEnumerable<IBundlePackage> bundlePackages)
    {
      ICollection<IBundlePackageFeature> selectableFeatures = new List<IBundlePackageFeature>();

      // Only features in the main MP2 package should be selectable
      IBundlePackage mainPackage = bundlePackages.FirstOrDefault(p => p.GetId() == PackageId.MediaPortal2);
      if (mainPackage == null)
      {
        return selectableFeatures;
      }

      // Get the optional features, namely Client, Server, ServiceMonitor and Log Collector
      foreach (IBundlePackageFeature feature in mainPackage.Features)
      {
        if (feature.Optional)
          selectableFeatures.Add(feature);
      }
      return selectableFeatures;
    }

    protected ICollection<IBundlePackage> GetSelectablePackages(IEnumerable<IBundlePackage> bundlePackages)
    {
      ICollection<IBundlePackage> selectablePackages = new List<IBundlePackage>();

      // Optional packages, currently this is only the LAV Filters package
      foreach (IBundlePackage bundlePackage in _bootstrapperApplicationModel.BundlePackages)
      {
        if (bundlePackage.Optional)
          selectablePackages.Add(bundlePackage);
      }
      return selectablePackages;
    }
  }
}
