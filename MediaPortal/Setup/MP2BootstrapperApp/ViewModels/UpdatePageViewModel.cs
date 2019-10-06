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

using System.Collections.ObjectModel;
using MP2BootstrapperApp.Models;

namespace MP2BootstrapperApp.ViewModels
{
  public class UpdatePageViewModel : InstallWizardPageViewModelBase
  {
    public UpdatePageViewModel(InstallWizardViewModel viewModel)
    {
      viewModel.Header = "Update MediaPortal 2";
      viewModel.ButtonNextContent = "Next";
      viewModel.ButtonBackContent = "Back";
      viewModel.ButtonCancelContent = "Abort";
      Packages = new ObservableCollection<Package>();
      foreach (BundlePackage package in viewModel.BundlePackages)
      {
        Packages.Add(new Package
        {
          BundleVersion = package.GetVersion().ToString(),
          InstalledVersion = package.InstalledVersion.ToString(),
          ImagePath = @"..\resources\" + package.GetId() + ".png",
          Name = package.Id,
          PackageState = package.CurrentInstallState
        });
      }
    }
    
    public ObservableCollection<Package> Packages { get; }
  }
}
