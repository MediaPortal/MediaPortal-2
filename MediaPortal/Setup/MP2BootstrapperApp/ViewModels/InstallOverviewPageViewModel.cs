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
using MP2BootstrapperApp.FeatureSelection;
using MP2BootstrapperApp.Models;
using MP2BootstrapperApp.WizardSteps;
using System.Collections.ObjectModel;

namespace MP2BootstrapperApp.ViewModels
{
  public class InstallOverviewPageViewModel : InstallWizardPageViewModelBase
  {
    public InstallOverviewPageViewModel(InstallOverviewStep step)
      : base(step)
    {
      Header = "Overview";
      ButtonNextContent = "Install";

      Packages = new ObservableCollection<string>();
      foreach (var package in step.BootstrapperApplicationModel.BundlePackages)
      {
        if (package.RequestedInstallState == RequestState.Present)
        {
          if (package.GetId() == PackageId.MediaPortal2)
          {
            foreach (string featureName in FeatureId.All)
            {
              if (package.Features.TryGetValue(featureName, out IBundlePackageFeature feature) && feature.RequestedFeatureState != FeatureState.Absent)
              {
                Packages.Add(@"..\resources\" + featureName + ".png");
              }
            }
          }
          else
          {
            Packages.Add(@"..\resources\" + package.GetId() + ".png");
          }
        }
      }
    }

    public ObservableCollection<string> Packages { get; }
  }
}
