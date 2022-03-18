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

using MP2BootstrapperApp.Models;

namespace MP2BootstrapperApp.WizardSteps
{
  public enum ActionType
  {
    Modify,
    Repair,
    Uninstall
  }

  public class InstallExistInstallStep : AbstractInstallStep, IStep
  {
    public InstallExistInstallStep(IBootstrapperApplicationModel bootstrapperApplicationModel)
      : base(bootstrapperApplicationModel)
    {
    }

    public ActionType ActionType { get; set; } = ActionType.Modify;

    public IStep Next()
    {
      IStep nextStep;
      switch (ActionType)
      {
        case ActionType.Modify:
          nextStep = new ModifyStep(_bootstrapperApplicationModel);
          break;
        case ActionType.Repair:
          nextStep = new RepairStep(_bootstrapperApplicationModel);
          break;
        case ActionType.Uninstall:
          nextStep = new UninstallStep(_bootstrapperApplicationModel);
          break;
        default:
          nextStep = null;
          break;
      }
      return nextStep;
    }

    public bool CanGoNext()
    {
      return true;
    }

    public bool CanGoBack()
    {
      return false;
    }
  }
}
