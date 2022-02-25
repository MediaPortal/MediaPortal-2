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
  public class BundlePackageFeature : IBundlePackageFeature
  {
    protected string _featureIdString;
    protected FeatureId _featureId;
    protected string _package;
    protected string _title;
    protected string _description;
    protected bool _optional;

    public BundlePackageFeature(XElement featureElement, PackageContext packageContext)
    {
      SetXmlProperties(featureElement);

      if(!Enum.TryParse(_package, out PackageId packageId) || !packageContext.TryGetPackage(packageId, out IPackage package))
        throw new InvalidOperationException($"{nameof(packageContext)} does not contain package info for feature package with id {_package}");

      SetFeatureProperties(package);
    }

    protected void SetXmlProperties(XElement featureElement)
    {
      _package = featureElement.Attribute("Package")?.Value;
      _featureIdString = featureElement.Attribute("Feature")?.Value;
      _title = featureElement.Attribute("Title")?.Value;
      _description = featureElement.Attribute("Description")?.Value;

      _featureId = Enum.TryParse(_featureIdString, out FeatureId id) ? id : FeatureId.Unknown;
    }

    protected void SetFeatureProperties(IPackage package)
    {
      _optional = package.IsFeatureOptional(_featureId);
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public FeatureId Id
    {
      get { return _featureId; }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public string Package
    {
      get { return _package; }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public string FeatureName
    {
      get { return _featureIdString; }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public string Title
    {
      get { return _title; }
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
    public bool PreviousVersionInstalled { get; set; }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public FeatureState CurrentFeatureState { get; set; }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public FeatureState RequestedFeatureState { get; set; }
  }
}
