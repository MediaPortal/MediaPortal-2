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
using System.Linq;

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

      IFeatureSelection featureSelection = null;
      switch (InstallType)
      {
        case InstallType.ClientServer:
          featureSelection = new ClientServer();
          break;
        case InstallType.Server:
          featureSelection = new Server();
          break;
        case InstallType.Client:
          featureSelection = new Client();
          break;
        case InstallType.Custom:
          // TODO
          break;
      }

      if (featureSelection != null)
        SetInstallType(featureSelection);

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

    private void SetInstallType(IFeatureSelection featureSelection)
    {
      foreach (BundlePackage package in _bootstrapperApplicationModel.BundlePackages)
      {
        PackageId packageId = package.GetId();
        if (package.CurrentInstallState != PackageState.Present && !featureSelection.ExcludePackages.Contains(packageId))
        {
          package.RequestedInstallState = RequestState.Present;
        }

        if (package.GetId() == PackageId.MediaPortal2)
        {
          foreach (var feature in package.FeatureStates.Keys.ToList())
          {
            package.FeatureStates[feature] = featureSelection.ExcludeFeatures.Contains(feature) ? FeatureState.Absent : FeatureState.Local;
          }
        }
      }
    }
  }
}
