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

using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using System;
using System.Globalization;
using System.Xml.Linq;

namespace MP2BootstrapperApp.BundlePackages
{
  public class BundlePackageFeature : IBundlePackageFeature
  {
    protected string _featureId;
    protected string _package;
    protected string _title;
    protected string _description;
    protected long _installedSize;
    protected FeatureAttributes _attributes;

    public BundlePackageFeature(XElement featureElement)
    {
      SetXmlProperties(featureElement);
    }

    protected void SetXmlProperties(XElement featureElement)
    {
      _featureId = featureElement.Attribute("Feature")?.Value;
      _package = featureElement.Attribute("Package")?.Value;
      _title = featureElement.Attribute("Title")?.Value;
      _description = featureElement.Attribute("Description")?.Value;
      _installedSize = long.TryParse(featureElement.Attribute("Size")?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long installedSize) ? installedSize : 0;
      _attributes = Enum.TryParse(featureElement.Attribute("Attributes")?.Value, out FeatureAttributes attributes) ? attributes : FeatureAttributes.None;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public string Id
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
    public long InstalledSize
    {
      get { return _installedSize; }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public FeatureAttributes Attributes
    {
      get { return _attributes; }
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
