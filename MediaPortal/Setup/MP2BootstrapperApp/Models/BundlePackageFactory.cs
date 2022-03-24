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
using System.Xml.Linq;

namespace MP2BootstrapperApp.Models
{
  public class BundlePackageFactory
  {
    public IBundlePackage CreatePackage(XElement packageElement)
    {
      string packageIdString = packageElement.Attribute("Package")?.Value;
      PackageId packageId = Enum.TryParse(packageIdString, out PackageId id) ? id : PackageId.Unknown;

      bool isMsiPackage = string.Equals(packageElement.Attribute("PackageType")?.Value, "Msi", StringComparison.InvariantCultureIgnoreCase);

      return isMsiPackage ? new BundleMsiPackage(packageId, packageElement) : new BundlePackage(packageId, packageElement);
    }

    public IBundlePackageFeature CreatePackageFeature(XElement featureElement)
    {
      string featureIdString = featureElement.Attribute("Feature")?.Value;
      FeatureId featureId = Enum.TryParse(featureIdString, out FeatureId fid) ? fid : FeatureId.Unknown;

      return new BundlePackageFeature(featureId, featureElement);
    }
  }
}
