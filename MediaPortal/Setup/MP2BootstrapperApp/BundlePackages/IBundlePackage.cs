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

/// <summary>
/// Interface for a package included in the installation bundle.
/// </summary>
namespace MP2BootstrapperApp.BundlePackages
{
  public interface IBundlePackage
  {
    /// <summary>
    /// Gets the id of the package as <see cref="PackageId"/>.
    /// </summary>
    PackageId PackageId { get; }

    /// <summary>
    /// Gets the bundled version of the package.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// Gets the id of the package as <see cref="string"/>.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// The display name of the package.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// The description of the package.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// The installled size of the package in bytes.
    /// </summary>
    long InstalledSize { get; }

    /// <summary>
    /// The install condition specified for the package.
    /// </summary>
    string InstallCondition { get; }

    /// <summary>
    /// Whether this package is vital or can be optionally not installed.
    /// </summary>
    bool Vital { get; }

    /// <summary>
    /// Gets the currently installed version of the package.
    /// </summary>
    Version InstalledVersion { get; set; }

    /// <summary>
    /// The evaluated value of the install condition specified for the package, if no install condition was specified this should default to <c>true</c>.
    /// </summary>
    bool EvaluatedInstallCondition { get; set; }

    /// <summary>
    /// Gets or sets the current install state of the package.
    /// </summary>
    PackageState CurrentInstallState { get; set; }
  }
}
