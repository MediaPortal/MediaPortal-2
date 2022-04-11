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

namespace MP2BootstrapperApp.BundlePackages.PluginFeatures
{
  /// <summary>
  /// Implementation of <see cref="IPluginFeatureManager"/> that manages the <see cref="IPluginFeatureDescriptor"/>s provided by a <see cref="IPluginFeatureDescriptorProvider"/>.
  /// </summary>
  public class PluginFeatureManager : IPluginFeatureManager
  {
    ICollection<IPluginFeatureDescriptor> _pluginFeatureDescriptors;

    public PluginFeatureManager(IPluginFeatureDescriptorProvider featureDescriptorProvider)
    {
      _pluginFeatureDescriptors = new List<IPluginFeatureDescriptor>(featureDescriptorProvider.GetDescriptors());
    }

    public ICollection<IBundlePackageFeature> GetInstallableFeatures(IEnumerable<string> currentlyInstallingFeatures, IEnumerable<IBundlePackageFeature> allFeatures)
    {
      List<IBundlePackageFeature> availableFeatures = new List<IBundlePackageFeature>();

      // Get all features that are children of the main package features where their parent feature is planned for installation
      IList<IBundlePackageFeature> installablefeatures = GetInstallableChildFeatures(currentlyInstallingFeatures, allFeatures);
      foreach (IBundlePackageFeature feature in installablefeatures)
      {
        // See if the feature is included as a related feature of another feature
        IPluginFeatureDescriptor relatedDescriptor = _pluginFeatureDescriptors.FirstOrDefault(d => d.RelatedFeatures.Contains(feature.Id) && installablefeatures.Any(f => f.Id == d.Feature));
        // If the feature is included with a related feature, don't add it, it will be automatically installed alongside that feature
        if (relatedDescriptor == null)
          availableFeatures.Add(feature);
      }
      return availableFeatures;
    }

    public ICollection<string> GetInstallableFeatureAndRelations(string featureId, IEnumerable<string> currentlyInstallingFeatures, IEnumerable<IBundlePackageFeature> allFeatures)
    {
      List<string> installableFeatures = new List<string>();
      // If the main feature's parent is not being installed then don't bother checking related features either
      if (!IsParentFeatureBeingInstalled(featureId, currentlyInstallingFeatures, allFeatures))
        return installableFeatures;

      installableFeatures.Add(featureId);

      // Find related features that should be automatically installed alongside the main feature and add any where their parent is being installed
      IPluginFeatureDescriptor featureDescriptor = _pluginFeatureDescriptors.FirstOrDefault(d => d.Feature == featureId);
      if (featureDescriptor != null)
        foreach (string relatedFeature in featureDescriptor.RelatedFeatures.Where(f => IsParentFeatureBeingInstalled(f, currentlyInstallingFeatures, allFeatures)))
          installableFeatures.Add(relatedFeature);

      return installableFeatures;
    }

    public ICollection<string> GetConflicts(string featureId, IEnumerable<string> possibleConflictingFeatureIds)
    {
      HashSet<string> conflictingFeatureIds = new HashSet<string>();
      // Get the conflicts defined for the feature and see if any are contained in possibleConflictingFeatureIds
      IPluginFeatureDescriptor featureDescriptor = _pluginFeatureDescriptors.FirstOrDefault(d => d.Feature == featureId);
      if (featureDescriptor != null)
        foreach (string conflictingFeature in featureDescriptor.ConflictingFeatures.Where(c => possibleConflictingFeatureIds.Contains(c)))
          conflictingFeatureIds.Add(conflictingFeature);

      // Inverse check, see if the feature is defined as a conflict by any of the features contained in possibleConflictingFeatureIds
      foreach (IPluginFeatureDescriptor conflictingDescriptor in _pluginFeatureDescriptors.Where(d => d.ConflictingFeatures.Contains(featureId) && possibleConflictingFeatureIds.Contains(d.Feature)))
        conflictingFeatureIds.Add(conflictingDescriptor.Feature);

      return conflictingFeatureIds;
    }

    /// <summary>
    /// Gets all features that are children of the features currently planned for installation.
    /// </summary>
    /// <param name="currentlyInstallingFeatures">Enumeration of features that are currently planned to be installed.</param>
    /// <param name="allFeatures">All features available in the setup bundle.</param>
    /// <returns>Collection of features that are children of the features currently planned for installation.</returns>
    protected IList<IBundlePackageFeature> GetInstallableChildFeatures(IEnumerable<string> currentlyInstallingFeatures, IEnumerable<IBundlePackageFeature> allFeatures)
    {
      return allFeatures.Where(f => !string.IsNullOrEmpty(f.Parent) && f.Parent != FeatureId.MediaPortal_2 && currentlyInstallingFeatures.Contains(f.Parent)).ToList();
    }

    /// <summary>
    /// Determines whether the parent of the feature with the specified id is currently planned for installation.
    /// </summary>
    /// <param name="id">Id of the feature to check if the parent feature is currently planned for installation.</param>
    /// <param name="currentlyInstallingFeatures">Enumeration of features that are currently planned to be installed.</param>
    /// <param name="allFeatures">All features available in the setup bundle.</param>
    /// <returns></returns>
    protected bool IsParentFeatureBeingInstalled(string id, IEnumerable<string> currentlyInstallingFeatures, IEnumerable<IBundlePackageFeature> allFeatures)
    {
      string featureParent = allFeatures.FirstOrDefault(f => f.Id == id)?.Parent;
      return featureParent != null && currentlyInstallingFeatures.Contains(featureParent);
    }
  }
}
