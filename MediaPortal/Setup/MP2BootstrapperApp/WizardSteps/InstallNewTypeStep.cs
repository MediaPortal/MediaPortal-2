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

namespace MP2BootstrapperApp.WizardSteps
{
  public class InstallNewTypeStep : AbstractInstallStep, IStep
  {
    public InstallNewTypeStep(IBootstrapperApplicationModel bootstrapperApplicationModel)
      : base(bootstrapperApplicationModel)
    {
    }

    public InstallType InstallType { get; set; } = InstallType.ClientServer;

    public IStep Next()
    {
      ResetRequestedInstallState();
      switch (InstallType)
      {
        case InstallType.ClientServer:
          SetInstallStateForClientAndServer();
          break;
        case InstallType.Server:
          SetInstallStateForServer();
          break;
        case InstallType.Client:
          SetInstallStateForClient();
          break;
        case InstallType.Custom:
          // TODO
          break;
      }
      return new InstallOverviewStep(_bootstrapperApplicationModel);
    }

    public bool CanGoNext()
    {
      return true;
    }

    public bool CanGoBack()
    {
      return true;
    }

    private void ResetRequestedInstallState()
    {
      foreach (BundlePackage package in _bootstrapperApplicationModel.BundlePackages)
      {
        package.RequestedInstallState = RequestState.None;
      }
    }

    private void SetInstallStateForClientAndServer()
    {
      foreach (BundlePackage package in _bootstrapperApplicationModel.BundlePackages)
      {
        if (package.CurrentInstallState != PackageState.Present)
        {
          package.RequestedInstallState = RequestState.Present;
        }
      }
    }

    private void SetInstallStateForServer()
    {
      foreach (BundlePackage package in _bootstrapperApplicationModel.BundlePackages)
      {
        PackageId packageId = package.GetId();
        if (package.CurrentInstallState == PackageState.Present || packageId == PackageId.MP2Client || packageId == PackageId.LAVFilters)
        {
          continue;
        }
        package.RequestedInstallState = RequestState.Present;
      }
    }

    private void SetInstallStateForClient()
    {
      foreach (BundlePackage package in _bootstrapperApplicationModel.BundlePackages)
      {
        PackageId packageId = package.GetId();
        if (package.CurrentInstallState == PackageState.Present || packageId == PackageId.MP2Server || packageId == PackageId.VC2008SP1_x86 || packageId == PackageId.VC2010_x86 || packageId == PackageId.VC2013_x86)
        {
          continue;
        }
        package.RequestedInstallState = RequestState.Present;
      }
    }
  }
}
