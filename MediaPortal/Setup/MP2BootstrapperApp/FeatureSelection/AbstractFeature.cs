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
using System.Collections.Generic;

namespace MP2BootstrapperApp.FeatureSelection
{
  public abstract class AbstractFeature : IFeature
  {
    protected ISet<PackageId> _excludePackages;
    protected ISet<string> _excludeFeatures;

    public ISet<PackageId> ExcludePackages
    {
      get { return _excludePackages ?? new HashSet<PackageId>(); }
    }

    public ISet<string> ExcludeFeatures
    {
      get { return _excludeFeatures ?? new HashSet<string>(); }
    }

    public void SetInstallState(IEnumerable<IBundlePackage> bundlePackages)
    {
      foreach (IBundlePackage package in bundlePackages)
      {
        PackageId packageId = package.GetId();
        if (package.CurrentInstallState != PackageState.Present && !ExcludePackages.Contains(packageId))
        {
          package.RequestedInstallState = RequestState.Present;
        }
        else
        {
          package.RequestedInstallState = RequestState.None;
        }

        if (packageId == PackageId.MediaPortal2)
        {
          foreach (var feature in package.Features.Values)
          {
            feature.RequestedFeatureState = ExcludeFeatures.Contains(feature.FeatureName) ? FeatureState.Absent : FeatureState.Local;
          }
        }
      }
    }
  }
}
