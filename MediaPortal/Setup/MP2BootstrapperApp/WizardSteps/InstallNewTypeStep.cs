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
using System.Collections.ObjectModel;

namespace MP2BootstrapperApp.WizardSteps
{
  public class InstallNewTypeStep : AbstractInstallStep, IStep
  {
    public InstallNewTypeStep(ReadOnlyCollection<BundlePackage> bundlePackages)
      : base(bundlePackages)
    {
      foreach (BundlePackage package in bundlePackages)
      {
        package.RequestedInstallState = RequestState.None;
      }
    }

    public InstallType InstallType { get; set; } = InstallType.ClientServer;

    public IStep Next()
    {
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
      return new InstallOverviewStep(_bundlePackages);
    }

    public bool CanGoNext()
    {
      return true;
    }

    public bool CanGoBack()
    {
      return true;
    }

    private void SetInstallStateForClientAndServer()
    {
      foreach (BundlePackage package in _bundlePackages)
      {
        if (package.CurrentInstallState != PackageState.Present)
        {
          package.RequestedInstallState = RequestState.Present;
        }
      }
    }

    private void SetInstallStateForServer()
    {
      foreach (BundlePackage package in _bundlePackages)
      {
        if (package.CurrentInstallState == PackageState.Present || package.GetId() == PackageId.MP2Client || package.GetId() == PackageId.LAVFilters)
        {
          continue;
        }
        package.RequestedInstallState = RequestState.Present;
      }
    }

    private void SetInstallStateForClient()
    {
      foreach (BundlePackage package in _bundlePackages)
      {
        if (package.CurrentInstallState == PackageState.Present || package.GetId() == PackageId.MP2Server)
        {
          continue;
        }
        package.RequestedInstallState = RequestState.Present;
      }
    }
  }
}
