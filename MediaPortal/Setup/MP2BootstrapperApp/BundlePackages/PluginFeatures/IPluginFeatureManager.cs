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

namespace MP2BootstrapperApp.BundlePackages.PluginFeatures
{
  /// <summary>
  /// Interface for a class that manages known implementations of <see cref="IPluginFeatureDescriptor"/> to determine which features should be displayed and installed.
  /// </summary>
  public interface IPluginFeatureManager
  {
    /// <summary>
    /// Gets a collection of plugin features that can be installed based on the features that are currently planned
    /// to be installed, typically this will be all features where their parent feature is being installed and the feature
    /// will not be automatically installed as a related feature of another installable feature.
    /// </summary>
    /// <param name="currentlyInstallingFeatures">Enumeration of features that are currently planned to be installed.</param>
    /// <param name="allFeatures">All features available in the setup bundle.</param>
    /// <returns>Collection of features that can be installed.</returns>
    ICollection<IBundlePackageFeature> GetInstallableFeatures(IEnumerable<string> currentlyInstallingFeatures, IEnumerable<IBundlePackageFeature> allFeatures);

    /// <summary>
    /// Gets a collection containing the specified feature and all related features that will be automatically installed alongside it.
    /// </summary>
    /// <param name="featureId">The main feature to be installed.</param>
    /// <param name="currentlyInstallingFeatures">Enumeration of features that are currently planned to be installed.</param>
    /// <param name="allFeatures">All features available in the setup bundle.</param>
    /// <returns>Collection containing the specified feature and all related features that will be automatically installed alongside it;
    /// or an empty collection if the main featire cannot be installed.</returns>
    ICollection<string> GetInstallableFeatureAndRelations(string featureId, IEnumerable<string> currentlyInstallingFeatures, IEnumerable<IBundlePackageFeature> allFeatures);

    /// <summary>
    /// Gets a collection of features that conflict with the current feature.
    /// </summary>
    /// <param name="featureId">The feature to check for conflicts with.</param>
    /// <param name="possibleConflictingFeatureIds">Enumeration of features to check for conflicting features.</param>
    /// <returns>Collection of features that conflict with the current feature.</returns>
    ICollection<string> GetConflicts(string featureId, IEnumerable<string> possibleConflictingFeatureIds);
  }
}
