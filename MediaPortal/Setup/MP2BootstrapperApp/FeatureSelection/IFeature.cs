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

using MP2BootstrapperApp.ChainPackages;
using MP2BootstrapperApp.Models;
using System.Collections.Generic;

namespace MP2BootstrapperApp.FeatureSelection
{
  public interface IFeature
  {
    /// <summary>
    /// Gets the features to exclude based on the selected features.
    /// </summary>
    ISet<string> ExcludeFeatures { get; }

    /// <summary>
    /// Gets the packages to exclude based on the selected features.
    /// </summary>
    ISet<PackageId> ExcludePackages { get; }

    /// <summary>
    /// Sets the install state and feature state of the packages based on the selected features.
    /// </summary>
    /// <param name="bundlePackages">The packages to update.</param>
    void SetInstallState(IEnumerable<IBundlePackage> bundlePackages);
  }
}
