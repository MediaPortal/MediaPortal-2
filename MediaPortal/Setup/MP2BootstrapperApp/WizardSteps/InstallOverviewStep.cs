﻿#region Copyright (C) 2007-2017 Team MediaPortal

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
  public class InstallOverviewStep : IStep
  {
    private readonly InstallWizardViewModel _viewModel;
    private readonly Logger _logger;

    public InstallOverviewStep(InstallWizardViewModel wizardViewModel, Logger logger)
    {
      _viewModel = wizardViewModel;
      _logger = logger;
    }

    public void Next(Wizard wizard)
    {
      _viewModel.Install();
      wizard.Step = new InstallationInProgressStep();
      _viewModel.CurrentPage = new InstallationInProgressPageViewModel(_viewModel);
    }

    public void Back(Wizard wizard)
    {
      wizard.Step = new InstallNewTypeStep(_viewModel, _logger);
      _viewModel.CurrentPage = new InstallNewTypePageViewModel(_viewModel);
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
