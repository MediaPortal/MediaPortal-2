#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using MP2BootstrapperApp.BundlePackages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MP2BootstrapperApp.Models
{
  public static class PackageExtensions
  {
    /// <summary>
    /// Creates a model for displaying a package in a view.
    /// </summary>
    /// <param name="bundlePackage">The package to create a model for.</param>
    /// <param name="requestState">Optional requested install state of the package.</param>
    /// <returns>A <see cref="Package"/> model containing details of the package.</returns>
    public static Package CreatePackageModel(this IBundlePackage bundlePackage, RequestState? requestState = null)
    {
      return new Package
      {
        BundleVersion = bundlePackage.Version,
        InstalledVersion = bundlePackage.InstalledVersion ?? new Version(),
        ImagePath = @"..\resources\Packages\" + bundlePackage.PackageId + ".png",
        Id = bundlePackage.Id,
        DisplayName = bundlePackage.DisplayName,
        Description = bundlePackage.Description,
        LocalizedDescription = $"[PackageDescription.{bundlePackage.Id}]",
        InstalledSize = bundlePackage.InstalledSize,
        PackageState = bundlePackage.CurrentInstallState,
        RequestState = requestState ?? RequestState.None
      };
    }

    /// <summary>
    /// Creates a model for displaying a feature in a view.
    /// </summary>
    /// <param name="feature">The feature to create a model for.</param>
    /// <param name="bundleVersion">The bundled version of the feature.</param>
    /// <param name="installedVersion">The currently installed verion of the feature.</param>
    /// <param name="featureState">Optional requested install state of the feature.</param>
    /// <param name="allFeaturesToBeInstalled">Enumeration of all features that will be installed if this feature is installed, inclusive of the feature itself, will be used to determine the installed size.</param>
    /// <returns>A <see cref="Package"/> model containing details of the feature.</returns>
    public static Package CreateFeatureModel(this IBundlePackageFeature feature, Version bundleVersion, Version installedVersion, FeatureState? featureState = null, IEnumerable<IBundlePackageFeature> allFeaturesToBeInstalled = null)
    {
      RequestState requestState = GetFeatureRequestState(feature, featureState);

      return new Package
      {
        BundleVersion = bundleVersion,
        InstalledVersion = (feature.PreviousVersionInstalled || feature.CurrentFeatureState == FeatureState.Local) ? installedVersion : new Version(),
        ImagePath = @"..\resources\Features\" + feature.Id + ".png",
        Id = feature.Id,
        DisplayName = feature.Title,
        Description = feature.Description,
        LocalizedDescription = $"[FeatureDescription.{feature.Id}]",
        InstalledSize = allFeaturesToBeInstalled == null ? feature.InstalledSize : allFeaturesToBeInstalled.Sum(f => f.InstalledSize),
        PackageState = feature.CurrentFeatureState == FeatureState.Local ? PackageState.Present : PackageState.Absent,
        RequestState = requestState
      };
    }

    /// <summary>
    /// Converts a <see cref="FeatureState"/> to a corresponding <see cref="RequestState"/>.
    /// </summary>
    /// <param name="feature">The feature to get the request state of.</param>
    /// <param name="featureState">The features requested feature state.</param>
    /// <returns><see cref="RequestState"/> that corresponds to the feature's request state.</returns>
    public static RequestState GetFeatureRequestState(IBundlePackageFeature feature, FeatureState? featureState = null)
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

      return requestState;
    }
  }
}
