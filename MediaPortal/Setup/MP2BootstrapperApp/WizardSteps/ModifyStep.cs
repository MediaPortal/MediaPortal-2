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
using MP2BootstrapperApp.BundlePackages.Features;
using MP2BootstrapperApp.Models;
using System.Collections.Generic;
using System.Linq;

namespace MP2BootstrapperApp.WizardSteps
{
  public class ModifyStep : AbstractPackageSelectionStep
  {
    public ModifyStep(IBootstrapperApplicationModel bootstrapperApplicationModel)
      : base(bootstrapperApplicationModel)
    {
      // Initially select the currently installed features
      SelectedFeatures = AvailableFeatures.Where(f => f.CurrentFeatureState == FeatureState.Local).ToList();
    }

    public override IStep Next()
    {
      IEnumerable<string> features = SelectedFeatures.Select(f => f.Id);
      IEnumerable<PackageId> packages = SelectedPackages.Select(p => p.PackageId);

      ModifyPlan plan = new ModifyPlan(features, packages, new PlanContext());

      if (FeatureUtils.GetSelectableChildFeatures(plan.PlannedFeatures, _bootstrapperApplicationModel.MainPackage.Features).Any())
        return new InstallPluginsStep(_bootstrapperApplicationModel, plan, false);
      else
        return new InstallOverviewStep(_bootstrapperApplicationModel, plan);
    }
  }
}
