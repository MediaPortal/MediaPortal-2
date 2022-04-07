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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace MP2BootstrapperApp.BundlePackages
{
  public class BundlePackageFactory
  {
    public IList<IBundlePackage> CreatePackagesFromXmlString(string xml)
    {
      XNamespace manifestNamespace = "http://schemas.microsoft.com/wix/2010/BootstrapperApplicationData";
      const string bootstrapperApplicationData = "BootstrapperApplicationData";

      XDocument xDoc = XDocument.Parse(xml);
      XElement bundleManifestData = xDoc.Element(manifestNamespace + bootstrapperApplicationData);

      const string wixMbaPrereqInfo = "WixMbaPrereqInformation";
      IList<string> mbaPrereqPackages = bundleManifestData?.Descendants(manifestNamespace + wixMbaPrereqInfo)
        .Select(x => x.Attribute("PackageId")?.Value)
        .ToList();

      const string wixPackageProperties = "WixPackageProperties";
      IList<IBundlePackage> packages = bundleManifestData?.Descendants(manifestNamespace + wixPackageProperties)
        .Where(x => mbaPrereqPackages.All(preReq => preReq != x.Attribute("Package")?.Value))
        .Select(x => CreatePackage(x)).ToList();

      const string wixPackageFeatureInfo = "WixPackageFeatureInfo";
      IEnumerable<IBundlePackageFeature> features = bundleManifestData?.Descendants(manifestNamespace + wixPackageFeatureInfo)
        .Select(x => CreatePackageFeature(x));
      foreach (IBundlePackageFeature feature in features)
      {
        IBundleMsiPackage parent = packages.FirstOrDefault(p => p.Id == feature.Package) as IBundleMsiPackage;
        if (parent != null)
          parent.Features.Add(feature);
      }

      return packages;
    }

    public virtual IBundlePackage CreatePackage(XElement packageElement)
    {
      bool isMsiPackage = string.Equals(packageElement.Attribute("PackageType")?.Value, "Msi", StringComparison.InvariantCultureIgnoreCase);
      return isMsiPackage ? new BundleMsiPackage(packageElement) : new BundlePackage(packageElement);
    }

    public virtual IBundlePackageFeature CreatePackageFeature(XElement featureElement)
    {
      return new BundlePackageFeature(featureElement);
    }
  }
}
