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

using MP2BootstrapperApp.ChainPackages;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace MP2BootstrapperApp.Models
{
  /// <summary>
  /// 
  /// </summary>
  public class BundleMsiPackage : BundlePackage, IBundleMsiPackage
  {
    protected readonly ICollection<IBundlePackageFeature> _features;
    protected Guid _productCode;
    protected Guid _upgradeCode;

    public BundleMsiPackage(PackageId packageId, XElement packageElement, IPackage package)
      : base(packageId, packageElement, package)
    {
      _features = new List<IBundlePackageFeature>();
    }

    protected override void SetXmlProperties(XElement packageElement)
    {
      base.SetXmlProperties(packageElement);
      _productCode = Guid.TryParse(packageElement.Attribute("ProductCode")?.Value, out Guid productCode) ? productCode : Guid.Empty;
      _upgradeCode = Guid.TryParse(packageElement.Attribute("UpgradeCode")?.Value, out Guid upgradeCode) ? upgradeCode : Guid.Empty;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public ICollection<IBundlePackageFeature> Features
    {
      get { return _features; }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Guid ProductCode
    {
      get { return _productCode; }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Guid UpgradeCode
    {
      get { return _upgradeCode; }
    }
  }
}
