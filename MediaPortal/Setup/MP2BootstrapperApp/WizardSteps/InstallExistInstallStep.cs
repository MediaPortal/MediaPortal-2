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

using MP2BootstrapperApp.ViewModels;

namespace MP2BootstrapperApp.WizardSteps
{
  public class InstallExistInstallStep : IStep
  {
    private readonly InstallWizardViewModel _viewModel;

    public InstallExistInstallStep(InstallWizardViewModel viewModel)
    {
      _viewModel = viewModel;
      _viewModel.CurrentPage = new InstallExistTypePageViewModel(_viewModel);
    }

    public void Next(Wizard wizard)
    {
      InstallExistTypePageViewModel page = _viewModel.CurrentPage as InstallExistTypePageViewModel;

      switch (page?.ActionType)
      {
        case ActionType.Update:
          wizard.Step = new UpdateStep(_viewModel);
          break;
        case ActionType.Modify:
          wizard.Step = new ModifyStep(_viewModel);
          break;
        case ActionType.Repair:
          wizard.Step = new RepairStep(_viewModel);
          break;
        case ActionType.Uninstall:
          wizard.Step = new UninstallStep(_viewModel);
          break;
      }
    }

    public void Back(Wizard wizard)
    {
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
