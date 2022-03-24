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

namespace MP2BootstrapperApp.WizardSteps
{
  public enum InstallType
  {
    ClientServer,
    Client,
    Server,
    Custom
  }

  public class InstallNewTypeStep : AbstractInstallStep, IStep
  {
    public InstallNewTypeStep(IBootstrapperApplicationModel bootstrapperApplicationModel)
      : base(bootstrapperApplicationModel)
    {
    }

    public InstallType InstallType { get; set; } = InstallType.ClientServer;

    public IStep Next()
    {
      IEnumerable<FeatureId> features;
      switch (InstallType)
      {
        case InstallType.ClientServer:
          features = new FeatureId[] { FeatureId.Client, FeatureId.Server, FeatureId.ServiceMonitor, FeatureId.LogCollector };
          break;
        case InstallType.Server:
          features = new FeatureId[] { FeatureId.Server, FeatureId.ServiceMonitor, FeatureId.LogCollector };
          break;
        case InstallType.Client:
          features = new FeatureId[] { FeatureId.Client, FeatureId.ServiceMonitor, FeatureId.LogCollector };
          break;
        //case InstallType.Custom:
        default:
          return new InstallCustomStep(_bootstrapperApplicationModel);
      }

      InstallPlan plan = new InstallPlan(features, null, new PlanContext());
      return new InstallOverviewStep(_bootstrapperApplicationModel, plan);
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
