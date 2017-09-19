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
  public class InstallModifyStep : IStep
  {
    private InstallWizardViewModel _viewModel;

    public InstallModifyStep(InstallWizardViewModel wizardViewModel)
    {
      _viewModel = wizardViewModel;
    }

    public void Next(Wizard wizard)
    {
      

      throw new System.NotImplementedException();
    }

    public void Back(Wizard wizard)
    {
      throw new System.NotImplementedException();
    }

    public bool CanGoNext()
    {
      throw new System.NotImplementedException();
    }

    public bool CanGoBack()
    {
      throw new System.NotImplementedException();
    }
  }
}
