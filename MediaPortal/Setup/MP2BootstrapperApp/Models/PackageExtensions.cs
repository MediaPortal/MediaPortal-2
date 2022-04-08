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
using MP2BootstrapperApp.BundlePackages.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MP2BootstrapperApp.Models
{
  public static class PackageExtensions
  {
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

    public static Package CreateFeatureModel(this IBundlePackageFeature feature, Version bundleVersion, Version installedVersion, FeatureState? featureState = null)
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
        InstalledSize = feature.InstalledSize,
        PackageState = feature.CurrentFeatureState == FeatureState.Local ? PackageState.Present : PackageState.Absent,
        RequestState = requestState
      };
    }

    public static Package CreatePluginModel(this PluginBase plugin, IEnumerable<IBundlePackageFeature> features, Version bundleVersion, Version installedVersion, FeatureState? featureState = null)
    {
      IBundlePackageFeature feature = features.FirstOrDefault(f => f.PreviousVersionInstalled || f.CurrentFeatureState == FeatureState.Local);
      if (feature == null)
        feature = features.FirstOrDefault();

      if (feature == null)
        throw new InvalidOperationException($"Cannot create PluginModel for plugin {plugin.Id}, the bundle package does not contain any of the plugin features.");

      RequestState requestState = GetFeatureRequestState(feature, featureState);

      return new Package
      {
        BundleVersion = bundleVersion,
        InstalledVersion = (feature.PreviousVersionInstalled || feature.CurrentFeatureState == FeatureState.Local) ? installedVersion : new Version(),
        ImagePath = @"..\resources\Plugins\" + plugin.Id + ".png",
        Id = plugin.Id,
        DisplayName = plugin.Name,
        LocalizedDescription = $"[PluginDescription.{plugin.Id}]",
        InstalledSize = features.Sum(f => f.InstalledSize),
        PackageState = feature.CurrentFeatureState == FeatureState.Local ? PackageState.Present : PackageState.Absent,
        RequestState = requestState
      };
    }

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
