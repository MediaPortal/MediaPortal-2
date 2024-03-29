﻿#region Copyright (C) 2007-2021 Team MediaPortal

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

using MP2BootstrapperApp.BundlePackages;
using MP2BootstrapperApp.Models;
using System.Collections.Generic;
using WixToolset.Dtf.WindowsInstaller;
using WixToolset.Mba.Core;

namespace MP2BootstrapperApp.WizardSteps
{
  /// <summary>
  /// Base class for a package selection step that provides optional packages and features that can be indiviually selected for installation.
  /// </summary>
  public abstract class AbstractPackageSelectionStep : AbstractInstallStep, IStep
  {
    public AbstractPackageSelectionStep(IBootstrapperApplicationModel bootstrapperApplicationModel)
      : base(bootstrapperApplicationModel)
    {
      AvailableFeatures = GetSelectableFeatures();
      AvailablePackages = GetSelectablePackages(bootstrapperApplicationModel.BundlePackages);
      SelectedFeatures = new List<IBundlePackageFeature>();
      SelectedPackages = new List<IBundlePackage>();
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
    /// All packages available for installation.
    /// </summary>
    public ICollection<IBundlePackage> AvailablePackages { get; }

    /// <summary>
    /// Packages selected for installation.
    /// </summary>
    public ICollection<IBundlePackage> SelectedPackages { get; protected set; }

    public abstract IStep Next();

    public virtual bool CanGoNext()
    {
      return SelectedFeatures.Count > 0;
    }

    public virtual bool CanGoBack()
    {
      return true;
    }

    protected ICollection<IBundlePackageFeature> GetSelectableFeatures()
    {
      ICollection<IBundlePackageFeature> selectableFeatures = new List<IBundlePackageFeature>();

      // Get the top-level optional features, namely Client, Server, ServiceMonitor and Log Collector
      foreach (IBundlePackageFeature feature in _bootstrapperApplicationModel.MainPackage.Features)
      {
        if (!feature.Attributes.HasFlag(FeatureAttributes.UIDisallowAbsent) && feature.Parent == FeatureId.MediaPortal_2)
          selectableFeatures.Add(feature);
      }
      return selectableFeatures;
    }

    protected ICollection<IBundlePackage> GetSelectablePackages(IEnumerable<IBundlePackage> bundlePackages)
    {
      ICollection<IBundlePackage> selectablePackages = new List<IBundlePackage>();

      // Optional packages, currently this is only the LAV Filters package
      foreach (IBundlePackage bundlePackage in bundlePackages)
      {
        if (!bundlePackage.Vital && bundlePackage.CurrentInstallState != PackageState.Present)
          selectablePackages.Add(bundlePackage);
      }
      return selectablePackages;
    }
  }
}
