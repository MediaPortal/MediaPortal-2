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
using MP2BootstrapperApp.Models;
using WixToolset.Mba.Core;

namespace MP2BootstrapperApp.WizardSteps
{
  public class UninstallStep : AbstractInstallStep, IStep
  {
    /// <summary>
    /// The uninstall confirmation step. May be reached from the repair/modify/uninstall step or shown initially on startup if the setup
    /// was launched with the uninstall action.
    /// </summary>
    /// <param name="bootstrapperApplicationModel">The bootstrapper model used to plan the uninstall action.</param>
    public UninstallStep(IBootstrapperApplicationModel bootstrapperApplicationModel)
      : base(bootstrapperApplicationModel)
    {
    }
    
    public IStep Next()
    {
      _bootstrapperApplicationModel.PlanAction(new SimplePlan(LaunchAction.Uninstall));
      _bootstrapperApplicationModel.LogMessage(LogLevel.Standard, "starting uninstallation");
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
