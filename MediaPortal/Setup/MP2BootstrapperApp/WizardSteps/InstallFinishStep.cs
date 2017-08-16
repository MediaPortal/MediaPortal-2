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

using System.Diagnostics;
using System.ServiceProcess;
using MP2BootstrapperApp.Models;
using MP2BootstrapperApp.ViewModels;

namespace MP2BootstrapperApp.WizardSteps
{
  public class InstallFinishStep : IStep
  {
    private readonly InstallWizardViewModel _viewModel;

    public InstallFinishStep(InstallWizardViewModel wizardViewModel)
    {
      _viewModel = wizardViewModel;
    }

    public void Next(Wizard wizard)
    {
      InstallFinishPageViewModel page = _viewModel.CurrentPage as InstallFinishPageViewModel;

      if (page != null)
      {
        if (page.StartMp2Server)
        {
          StartServer();
        }

        if (page.StartMp2Client)
        {
          StartClient();
        }

        if (page.StartMp2ServiceMonitor)
        {
          StartServiceMonitor();
        }

        if (page.StartMp2TvServerConfiguration)
        {
          StartTvConfiguration();
        }
      }

      _viewModel.CloseWizard();
    }

    public void Back(Wizard wizard)
    {
      // not allowed
    }

    public bool CanGoNext()
    {
      return true;
    }

    public bool CanGoBack()
    {
      return false;
    }

    private void StartTvConfiguration()
    {
      
    }

    private void StartServiceMonitor()
    {
    }

    private void StartClient()
    {
      
    }

    private void StartServer()
    {
    }
  }
}
