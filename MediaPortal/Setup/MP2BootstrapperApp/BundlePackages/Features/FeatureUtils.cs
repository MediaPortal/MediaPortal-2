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
using System.Linq;

namespace MP2BootstrapperApp.BundlePackages.Features
{
  /// <summary>
  /// Utility methods for determining which features to display and install.
  /// </summary>
  public static class FeatureUtils
  {
    /// <summary>
    /// Gets a collection of features that can be selected for installation based on whether their parent feature is being installed and the feature
    /// will not be automatically installed as a related feature of another installable feature.
    /// </summary>
    /// <param name="currentlyInstallingFeatures">Enumeration of features that are currently planned to be installed.</param>
    /// <param name="allFeatures">All features available in the setup bundle.</param>
    /// <returns>Collection of features that can be installed.</returns>
    public static ICollection<IBundlePackageFeature> GetSelectableChildFeatures(IEnumerable<string> currentlyInstallingFeatures, IEnumerable<IBundlePackageFeature> allFeatures)
    {
      List<IBundlePackageFeature> installableFeatures = new List<IBundlePackageFeature>();

      // Get all features that are children of the main package features where their parent feature is planned for installation
      IList<IBundlePackageFeature> installablefeatures = GetInstallableChildFeatures(currentlyInstallingFeatures, allFeatures);
      foreach (IBundlePackageFeature installableFeature in installablefeatures)
      {
        // See if the feature is included as a related feature of another feature
        IBundlePackageFeature relatedFeature = allFeatures.FirstOrDefault(feature => feature.RelatedFeatures.Contains(installableFeature.Id) && installablefeatures.Any(f => f.Id == feature.Id));
        // If the feature is included with a related feature, don't add it, it will be automatically installed alongside that feature
        if (relatedFeature == null)
          installableFeatures.Add(installableFeature);
      }
      return installableFeatures;
    }

    /// <summary>
    /// Gets a collection containing the specified feature and all related features that will be automatically installed alongside it.
    /// </summary>
    /// <param name="feature">The main feature to be installed.</param>
    /// <param name="currentlyInstallingFeatures">Enumeration of feature ids that are currently planned to be installed.</param>
    /// <param name="allFeatures">All features available in the setup bundle.</param>
    /// <returns>Collection containing the specified feature and all related features that will be automatically installed alongside it;
    /// or an empty collection if the main feature cannot be installed.</returns>
    public static ICollection<IBundlePackageFeature> GetInstallableFeatureAndRelations(IBundlePackageFeature feature, IEnumerable<string> currentlyInstallingFeatures, IEnumerable<IBundlePackageFeature> allFeatures)
    {
      List<IBundlePackageFeature> installableFeatures = new List<IBundlePackageFeature>();
      // If the main feature's parent is not being installed then don't bother checking related features either
      if (!currentlyInstallingFeatures.Contains(feature.Parent))
        return installableFeatures;

      installableFeatures.Add(feature);

      // Find related features that should be automatically installed alongside the main feature and add any where their parent is being installed
      if (feature.RelatedFeatures != null)
      {
        IEnumerable<IBundlePackageFeature> relatedFeatures = feature.RelatedFeatures.Select(id => allFeatures.FirstOrDefault(f => f.Id == id)).Where(f => f != null);
        foreach (IBundlePackageFeature relatedFeature in relatedFeatures.Where(f => currentlyInstallingFeatures.Contains(f.Parent)))
          installableFeatures.Add(relatedFeature);
      }

      return installableFeatures;
    }

    /// <summary>
    /// Gets a collection of feature ids that conflict with the current feature.
    /// </summary>
    /// <param name="featureId">The feature to check for conflicts with.</param>
    /// <param name="possibleConflictingFeatureIds">Enumeration of features to check for conflicting features.</param>
    /// <param name="allFeatures">All features available in the setup bundle.</param>
    /// <returns>Collection of features that conflict with the current feature.</returns>
    public static ICollection<string> GetConflicts(string featureId, IEnumerable<string> possibleConflictingFeatureIds, IEnumerable<IBundlePackageFeature> allFeatures)
    {
      HashSet<string> conflictingFeatureIds = new HashSet<string>();
      // Get the conflicts defined for the feature and see if any are contained in possibleConflictingFeatureIds
      ICollection<string> featureconflicts = allFeatures.FirstOrDefault(f => f.Id == featureId)?.ConflictingFeatures;
      if (featureconflicts != null)
        foreach (string conflictingFeature in featureconflicts.Where(c => possibleConflictingFeatureIds.Contains(c)))
          conflictingFeatureIds.Add(conflictingFeature);

      // Inverse check, see if the feature is defined as a conflict by any of the features contained in possibleConflictingFeatureIds
      foreach (IBundlePackageFeature conflictingFeature in allFeatures.Where(f => f.ConflictingFeatures.Contains(featureId) && possibleConflictingFeatureIds.Contains(f.Id)))
        conflictingFeatureIds.Add(conflictingFeature.Id);

      return conflictingFeatureIds;
    }

    /// <summary>
    /// Gets all features that are children of the features currently planned for installation.
    /// </summary>
    /// <param name="currentlyInstallingFeatures">Enumeration of features that are currently planned to be installed.</param>
    /// <param name="allFeatures">All features available in the setup bundle.</param>
    /// <returns>Collection of features that are children of the features currently planned for installation.</returns>
    static IList<IBundlePackageFeature> GetInstallableChildFeatures(IEnumerable<string> currentlyInstallingFeatures, IEnumerable<IBundlePackageFeature> allFeatures)
    {
      return allFeatures.Where(f => !string.IsNullOrEmpty(f.Parent) && f.Parent != FeatureId.MediaPortal_2 && currentlyInstallingFeatures.Contains(f.Parent)).ToList();
    }
  }
}
