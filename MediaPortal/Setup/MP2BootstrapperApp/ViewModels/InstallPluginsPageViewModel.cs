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

using MP2BootstrapperApp.BundlePackages;
using MP2BootstrapperApp.Models;
using MP2BootstrapperApp.ViewModels.ListItems;
using MP2BootstrapperApp.WizardSteps;
using System.Collections.Generic;
using System.Linq;

namespace MP2BootstrapperApp.ViewModels
{
  public class InstallPluginsPageViewModel : AbstractSelectionViewModel<InstallPluginsStep, FeatureListItem>
  {
    public InstallPluginsPageViewModel(InstallPluginsStep step)
      : base(step)
    {
      Header = "[InstallPluginsPageView.Header]";
      SubHeader = "[InstallPluginsPageView.SubHeader]";
    }

    protected override IEnumerable<FeatureListItem> GetItems()
    {
      IBundleMsiPackage mainPackage = _step.BootstrapperApplicationModel.MainPackage;

      return _step.AvailableFeatures.Select(f =>
        new FeatureListItem
        {
          Item = f.CreateFeatureModel(mainPackage.Version, mainPackage.InstalledVersion, null, _step.GetInstallableFeatureAndRelations(f.Id)),
          Selected = _step.SelectedFeatures.Contains(f)
        });
    }

    protected override void OnItemSelectedChanged(FeatureListItem item, bool selected)
    {
      IBundlePackageFeature feature = _step.AvailableFeatures.FirstOrDefault(p => p.Id == item.Item.Id);
      if (feature != null)
      {
        // If a new feature has been selected, unselect any conflicting features
        if (selected)
          foreach (FeatureListItem conflictingItem in GetSelectedConflictingItems(feature.Id))
            conflictingItem.Selected = false;

        UpdateSelectedItems(feature, _step.SelectedFeatures, selected);
      }

      base.OnItemSelectedChanged(item, selected);
    }

    protected IEnumerable<FeatureListItem> GetSelectedConflictingItems(string featureId)
    {
      IEnumerable<string> conflicts = _step.GetConflicts(featureId);
      return Items.Where(i => i.Selected && conflicts.Contains(i.Item.Id));
    }
  }
}
