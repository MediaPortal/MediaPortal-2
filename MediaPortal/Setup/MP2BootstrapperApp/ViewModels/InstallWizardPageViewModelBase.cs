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
using MP2BootstrapperApp.Models;
using MP2BootstrapperApp.WizardSteps;
using System;

namespace MP2BootstrapperApp.ViewModels
{
  public abstract class InstallWizardPageViewModelBase<T> : PageViewModelBase where T : IStep
  {
    protected T _step;

    public InstallWizardPageViewModelBase(T step)
    {
      _step = step;
    }

    protected Package CreatePackage(IBundlePackage bundlePackage, RequestState? requestState = null)
    {
      return new Package
      {
        BundleVersion = bundlePackage.Version,
        InstalledVersion = bundlePackage.InstalledVersion,
        ImagePath = @"..\resources\" + bundlePackage.PackageId + ".png",
        Id = bundlePackage.Id,
        DisplayName = bundlePackage.DisplayName,
        Description = bundlePackage.Description,
        LocalizedDescription = $"[PackageDescription.{bundlePackage.Id}]",
        InstalledSize = bundlePackage.InstalledSize,
        PackageState = bundlePackage.CurrentInstallState,
        RequestState = requestState ?? RequestState.None
      };
    }

    protected Package CreatePackageFeature(IBundlePackage bundlePackage, IBundlePackageFeature feature, FeatureState? featureState = null)
    {
      // Convert the FeatureState into a corresponding RequestState for display purposes.
      // There isn't a one-to-one mapping because feature states always have to be explicitly set, in our case either to Present or Absent,
      // so there's no equivalent of RequestState.None which is used to indicate that the state of a package won't change. Instead we have
      // to determine this depending on whether a current/previous version of the feature is currently installed.
      RequestState requestState;
      if (!featureState.HasValue)
        requestState = RequestState.None;
      // For features requested absent, if no current/previous version is installed then nothing will change, so use RequestState.None,
      // else if the feature is installed then it will be removed, so use RequestState.Absent.
      else if (featureState == FeatureState.Absent)
        requestState = (feature.PreviousVersionInstalled || feature.CurrentFeatureState == FeatureState.Local) ? RequestState.Absent : RequestState.None;
      // For features requested present, if the current version is already installed then nothing will change, so use RequestState.None,
      // else the feature will be installed/updated, so use RequestState.Present
      else
        requestState = feature.CurrentFeatureState == FeatureState.Local ? RequestState.None : RequestState.Present;

      return new Package
      {
        BundleVersion = bundlePackage.Version,
        InstalledVersion = (feature.PreviousVersionInstalled || feature.CurrentFeatureState == FeatureState.Local) ? bundlePackage.InstalledVersion : new Version(),
        ImagePath = @"..\resources\" + feature.FeatureName + ".png",
        Id = feature.FeatureName,
        DisplayName = feature.Title,
        Description = feature.Description,
        LocalizedDescription = $"[FeatureDescription.{feature.FeatureName}]",
        InstalledSize = feature.InstalledSize,
        PackageState = feature.CurrentFeatureState == FeatureState.Local ? PackageState.Present : PackageState.Absent,
        RequestState = requestState
      };
    }
  }
}
