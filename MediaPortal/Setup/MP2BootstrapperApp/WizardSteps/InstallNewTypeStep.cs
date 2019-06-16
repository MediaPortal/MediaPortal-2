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

using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MP2BootstrapperApp.ChainPackages;
using MP2BootstrapperApp.Models;
using MP2BootstrapperApp.ViewModels;

namespace MP2BootstrapperApp.WizardSteps
{
  public class InstallNewTypeStep : IStep
  {
    private readonly InstallWizardViewModel _viewModel;
    private readonly Logger _logger;

    public InstallNewTypeStep(InstallWizardViewModel viewModel, Logger logger)
    {
      _viewModel = viewModel;
      _logger = logger;
      _viewModel.CurrentPage = new InstallNewTypePageViewModel(_viewModel);
      foreach (BundlePackage package in _viewModel.BundlePackages)
      {
        package.RequestedInstallState = RequestState.None;
      }
    }

    public void Next(Wizard wizard)
    {
      InstallNewTypePageViewModel page = _viewModel.CurrentPage as InstallNewTypePageViewModel;

      switch (page?.InstallType)
      {
        case InstallType.ClientServer:
          SetInstallStateForClientAndServer(wizard);
          break;
        case InstallType.Server:
          SetInstallStateForServer(wizard);
          break;
        case InstallType.Client:
          SetInstallStateForClient(wizard);
          break;
        case InstallType.Custom:
          // TODO
          break;
      }
    }

    public void Back(Wizard wizard)
    {
      wizard.Step = new InstallWelcomeStep(_viewModel, _logger);
    }

    public bool CanGoNext()
    {
      return true;
    }

    public bool CanGoBack()
    {
      return true;
    }
    
    private void SetInstallStateForClientAndServer(Wizard wizard)
    {
      foreach (BundlePackage package in _viewModel.BundlePackages)
      {
        if (package.CurrentInstallState != PackageState.Present)
        {
          package.RequestedInstallState = RequestState.Present;
        }
      }
      MoveToOverview(wizard);
    }
    
    private void MoveToOverview(Wizard wizard)
    {
      wizard.Step = new InstallOverviewStep(_viewModel, _logger);
    }

    private void SetInstallStateForServer(Wizard wizard)
    {
      foreach (BundlePackage package in _viewModel.BundlePackages)
      {
        if (package.CurrentInstallState == PackageState.Present || package.GetId() == PackageId.MP2Client || package.GetId() == PackageId.LAVFilters)
        {
          continue;
        }
        package.RequestedInstallState = RequestState.Present;
      }
      MoveToOverview(wizard);
    }

    private void SetInstallStateForClient(Wizard wizard)
    {
      foreach (BundlePackage package in _viewModel.BundlePackages)
      {
        if (package.CurrentInstallState == PackageState.Present || package.GetId() == PackageId.MP2Server)
        {
          continue;
        }
        package.RequestedInstallState = RequestState.Present;
      }
      MoveToOverview(wizard);
    }
  }
}
