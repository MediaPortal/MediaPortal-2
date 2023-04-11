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
  /// Interface for a class that provides additional metadata for a feature.
  /// </summary>  /// 
  /// <remarks>
  /// Windows installer packages do not have a concept of mutually exclusive features nor do they allow
  /// sibling features to be installed automatically when a certain feature is being installed, but we have
  /// these requirements in the MediaPortal setup, e.g. when installing TV Server 3, TV Service 3.5 should
  /// not also be installed, and in a single-seat install both the client and server features should be
  /// installed together as a single selectable unit. This interface defines those additional requirements.<br/>
  /// For simple features which don't have any of the above requirements an <see cref="IFeatureMetadata"/>
  /// is not required.
  /// </remarks>
  public interface IFeatureMetadata
  {
    /// <summary>
    /// Id of the feature that this metadata belongs to.
    /// </summary>
    string Feature { get; }

    /// <summary>
    /// Ids of any related features that should optionally be installed alongside this feature. These features
    /// will only be installed if their main parent feature is being installed and they do not conflict with any
    /// other features that are being installed, e.g. if this contains client features and the client is not being
    /// installed, then those features will not be installed.
    /// </summary>
    ICollection<string> RelatedFeatures { get; }

    /// <summary>
    /// Any features that conflict with the feature with the id specified in <see cref="Feature"/>, conflicting features
    /// will not be selectable or installed alongside each other.
    /// </summary>
    ICollection<string> ConflictingFeatures { get; }
  }
}
