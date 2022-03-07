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
using System;
using System.Xml.Linq;

namespace MP2BootstrapperApp.Models
{
  /// <summary>
  /// 
  /// </summary>
  public class BundlePackage : IBundlePackage
  {
    protected string _packageIdString;
    protected PackageId _packageId;
    protected Version _version;
    protected string _displayName;
    protected string _description;

    protected bool _optional;
    protected bool _is64Bit;
    protected Version _installedVersion;

    public BundlePackage(PackageId packageId, XElement packageElement, IPackage package)
    {
      _packageId = packageId;

      SetXmlProperties(packageElement);

      if (package != null)
        SetPackageProperties(package);
    }

    protected virtual void SetXmlProperties(XElement packageElement)
    {
      _packageIdString = packageElement.Attribute("Package")?.Value;
      _displayName = packageElement.Attribute("DisplayName")?.Value;
      _description = packageElement.Attribute("Description")?.Value;

      _version = Version.TryParse(packageElement.Attribute("Version")?.Value, out Version result) ? result : new Version();

    }

    protected virtual void SetPackageProperties(IPackage package)
    {
      _optional = package.IsOptional;
      _is64Bit = package.Is64Bit;
      _installedVersion = package.GetInstalledVersion();
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public PackageId PackageId
    {
      get { return _packageId; }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public string Id
    {
      get { return _packageIdString; }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Version Version
    {
      get { return _version; }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public string DisplayName
    {
      get { return _displayName; }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public string Description
    {
      get { return _description; }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public bool Optional
    {
      get { return _optional; }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public bool Is64Bit
    {
      get { return _is64Bit; }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Version InstalledVersion
    {
      get { return _installedVersion; }
      set { _installedVersion = value; }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public PackageState CurrentInstallState { get; set; }
  }
}
