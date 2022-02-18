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

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MP2BootstrapperApp.ChainPackages;

namespace MP2BootstrapperApp.Models
{
  /// <summary>
  /// 
  /// </summary>
  public class BundlePackage : IBundlePackage
  {
    private readonly XElement _packageElement;
    private readonly ICollection<IBundlePackageFeature> _features;

    public BundlePackage(XElement packageElement)
    {
      _packageElement = packageElement;
      _features = new List<IBundlePackageFeature>();
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public PackageId GetId()
    {
      return Enum.TryParse(_packageElement.Attribute("Package")?.Value, out PackageId packageId) ? packageId : PackageId.Unknown;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public string Id
    {
      get
      {
        PackageId id = Enum.TryParse(_packageElement.Attribute("Package")?.Value, out PackageId packageId) ? packageId : PackageId.Unknown;
        return id.ToString();
      }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Version GetVersion()
    {
      return Version.TryParse(_packageElement.Attribute("Version")?.Value, out Version result) ? result : new Version();
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public string DisplayName
    {
      get { return _packageElement.Attribute("DisplayName")?.Value; }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public string Description
    {
      get { return _packageElement.Attribute("Description")?.Value; }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public bool Optional { get; set; }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Version InstalledVersion { get; set; }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public PackageState CurrentInstallState { get; set; }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public RequestState RequestedInstallState { get; set; }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public ICollection<IBundlePackageFeature> Features
    {
      get { return _features; }
    }
  }
}
