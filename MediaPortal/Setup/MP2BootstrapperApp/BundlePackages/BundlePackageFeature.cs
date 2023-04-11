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
using MP2BootstrapperApp.BundlePackages.Features;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;

namespace MP2BootstrapperApp.BundlePackages
{
  public class BundlePackageFeature : IBundlePackageFeature
  {
    protected string _featureId;
    protected string _package;
    protected string _parent;
    protected int _installLevel;
    protected string _title;
    protected string _description;
    protected long _installedSize;
    protected FeatureAttributes _attributes;

    protected ICollection<string> _relatedFeatures;
    protected ICollection<string> _conflictingFeatures;

    public BundlePackageFeature(XElement featureElement, IFeatureMetadata featureMetadata)
    {
      SetXmlProperties(featureElement);
      SetMetadataProperties(featureMetadata);
    }

    protected void SetXmlProperties(XElement featureElement)
    {
      _featureId = featureElement.Attribute("Feature")?.Value;
      _package = featureElement.Attribute("Package")?.Value;
      _parent = featureElement.Attribute("Parent")?.Value ?? string.Empty;
      _installLevel = int.TryParse(featureElement.Attribute("Level")?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int level) ? level : 1;
      _title = featureElement.Attribute("Title")?.Value;
      _description = featureElement.Attribute("Description")?.Value;
      _installedSize = long.TryParse(featureElement.Attribute("Size")?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long installedSize) ? installedSize : 0;
      _attributes = Enum.TryParse(featureElement.Attribute("Attributes")?.Value, out FeatureAttributes attributes) ? attributes : FeatureAttributes.None;
    }

    protected void SetMetadataProperties(IFeatureMetadata metadata)
    {
      if (metadata != null)
      {
        _relatedFeatures = metadata.RelatedFeatures;
        _conflictingFeatures = metadata.ConflictingFeatures;
      }
      else
      {
        _relatedFeatures = new List<string>();
        _conflictingFeatures = new List<string>();
      }
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
    public string Parent
    {
      get { return _parent; }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public int InstallLevel
    {
      get { return _installLevel; }
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
    public ICollection<string> RelatedFeatures
    {
      get { return _relatedFeatures; }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public ICollection<string> ConflictingFeatures
    {
      get { return _conflictingFeatures; }
    }
  }
}
