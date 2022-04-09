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
using MP2BootstrapperApp.Models;
using System.Collections.Generic;
using System.Linq;

namespace MP2BootstrapperApp.WizardSteps
{
  /// <summary>
  /// Custom install step that provides optional packages and features that can be individually selected for installation.
  /// </summary>
  public class InstallCustomStep : AbstractPackageSelectionStep
  {
    public InstallCustomStep(IBootstrapperApplicationModel bootstrapperApplicationModel)
      : base(bootstrapperApplicationModel)
    {
      SetSelectedPackages();
    }

    public override IStep Next()
    {
      IEnumerable<string> features = SelectedFeatures.Select(f => f.Id);
      IEnumerable<PackageId> packages = SelectedPackages.Select(p => p.PackageId);

      InstallPlan plan = new InstallPlan(features, packages, new PlanContext());

      if (_bootstrapperApplicationModel.PluginManager.GetAvailablePlugins(plan, _bootstrapperApplicationModel.MainPackage.Features).Any())
        return new InstallCustomPluginsStep(_bootstrapperApplicationModel, plan);
      else
        return new InstallCustomPropertiesStep(_bootstrapperApplicationModel, plan);
    }

    protected void SetSelectedPackages()
    {
      bool isPreviousVersionInstalled = AvailableFeatures.Any(f => f.PreviousVersionInstalled);
      if (isPreviousVersionInstalled)
      {
        // If a previous installation exists, initially select the previously installed features and packages
        SelectedFeatures = AvailableFeatures.Where(f => f.PreviousVersionInstalled).ToList();
        SelectedPackages = AvailablePackages.Where(p => p.InstalledVersion != null).ToList();
      }
      else
      {
        // Else initially select all features and packages
        SelectedFeatures = new List<IBundlePackageFeature>(AvailableFeatures);
        SelectedPackages = new List<IBundlePackage>(AvailablePackages);
      }
    }
  }
}
