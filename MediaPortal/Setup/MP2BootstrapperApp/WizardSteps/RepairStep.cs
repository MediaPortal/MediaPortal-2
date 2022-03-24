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

using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MP2BootstrapperApp.ActionPlans;
using MP2BootstrapperApp.BundlePackages;
using MP2BootstrapperApp.Models;
using System.Collections.Generic;
using System.Linq;

namespace MP2BootstrapperApp.WizardSteps
{
  public class RepairStep : AbstractInstallStep, IStep
  {
    public RepairStep(IBootstrapperApplicationModel bootstrapperApplicationModel)
      : base(bootstrapperApplicationModel)
    {
    }

    public IStep Next()
    {
      PlanContext planContext = new PlanContext();
      // Get the currently installed features.
      IBundleMsiPackage featurePackage = _bootstrapperApplicationModel.BundlePackages.FirstOrDefault(p => p.PackageId == planContext.FeaturePackageId) as IBundleMsiPackage;
      IEnumerable<FeatureId> installedFeatures = featurePackage?.Features.Where(f => !f.Attributes.HasFlag(FeatureAttributes.UIDisallowAbsent) && f.CurrentFeatureState == FeatureState.Local).Select(f => f.Id);

      RepairPlan plan = new RepairPlan(installedFeatures, planContext);

      _bootstrapperApplicationModel.PlanAction(plan);
      _bootstrapperApplicationModel.LogMessage(LogLevel.Standard, "Starting repair");
      return new InstallationInProgressStep(_bootstrapperApplicationModel);
    }

    public bool CanGoNext()
    {
      return true;
    }

    public bool CanGoBack()
    {
      return true;
    }
  }
}
