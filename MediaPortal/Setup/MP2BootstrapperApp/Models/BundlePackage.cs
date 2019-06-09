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
using System.Xml.Linq;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MP2BootstrapperApp.ChainPackages;

namespace MP2BootstrapperApp.Models
{
  /// <summary>
  /// 
  /// </summary>
  public class BundlePackage
  {
    private readonly XElement _packagElement;

    public BundlePackage(XElement packagElement)
    {
      _packagElement = packagElement;
    }

    /// <summary>
    /// 
    /// </summary>
    public PackageId Id
    {
      get { return Enum.TryParse(_packagElement.Attribute("Package")?.Value, out PackageId packageId) ? packageId : PackageId.Unknown; }
    }

    /// <summary>
    /// 
    /// </summary>
    public string Version
    {
      get { return _packagElement.Attribute("Version").Value; }
    }

    /// <summary>
    /// 
    /// </summary>
    public PackageState CurrentInstallState { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public RequestState RequestedInstallState { get; set; }
  }
}
