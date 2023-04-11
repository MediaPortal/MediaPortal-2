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
using MP2BootstrapperApp.Models;

namespace MP2BootstrapperApp.WizardSteps
{
  public class InstallOverviewStep : AbstractInstallStep, IStep
  {
    protected IPlan _actionPlan;

    public InstallOverviewStep(IBootstrapperApplicationModel bootstrapperApplicationModel, IPlan actionPlan)
      : base(bootstrapperApplicationModel)
    {
      _actionPlan = actionPlan;
    }

    public IPlan ActionPlan
    {
      get { return _actionPlan; }
    }

    public IStep Next()
    {
      _bootstrapperApplicationModel.PlanAction(_actionPlan);
      _bootstrapperApplicationModel.LogMessage(LogLevel.Standard, "starting installation");
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
