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

using System.Collections.Generic;

namespace MP2BootstrapperApp.BundlePackages.Features
{
  /// <summary>
  /// Base implementaion of <see cref="IFeatureMetadata"/>. Features that require a <see cref="IFeatureMetadata"/>
  /// should derive from this class and provide a parameterless public constructor so they can be instantiated by reflection.
  /// </summary>
  public abstract class AbstractFeatureMetadata : IFeatureMetadata
  {
    /// <summary>
    /// Creates an instance of a class that implements <see cref="IFeatureMetadata"/>.
    /// </summary>
    /// <param name="feature">Id of feature that this metadata belongs to.
    /// <param name="relatedFeatures">Ids of any related features that should optionally be installed alongside this feature.</param>
    /// <param name="conflictingFeatures">Any features that conflict with the feature with the id specified in <paramref name="feature"/>.</param>
    public AbstractFeatureMetadata(string feature, IEnumerable<string> relatedFeatures, IEnumerable<string> conflictingFeatures)
    {
      Feature = feature;
      RelatedFeatures = relatedFeatures != null ? new List<string>(relatedFeatures) : new List<string>();
      ConflictingFeatures = conflictingFeatures != null ? new List<string>(conflictingFeatures) : new List<string>();
    }

    public string Feature { get; }

    public ICollection<string> RelatedFeatures { get; }

    public ICollection<string> ConflictingFeatures { get; }
  }
}
