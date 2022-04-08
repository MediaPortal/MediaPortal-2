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
using MP2BootstrapperApp.WizardSteps;
using System.Collections.Generic;
using System.Linq;

namespace MP2BootstrapperApp.ViewModels
{
  public abstract class AbstractPackageSelectionViewModel<T> : AbstractSelectionViewModel<T, SelectablePackageViewModel> where T : AbstractPackageSelectionStep
  {
    public AbstractPackageSelectionViewModel(T step)
      : base(step)
    {
    }

    protected override IEnumerable<SelectablePackageViewModel> GetItems()
    {
      List<SelectablePackageViewModel> items = new List<SelectablePackageViewModel>();
      // Create list items for each feature
      foreach (IBundlePackageFeature feature in _step.AvailableFeatures)
      {
        IBundlePackage parentPackage = _step.BootstrapperApplicationModel.BundlePackages.FirstOrDefault(p => p.Id == feature.Package);
        if (parentPackage != null)
        {

          SelectableFeatureViewModel featureItem = new SelectableFeatureViewModel
          {
            Item = CreatePackageFeature(parentPackage, feature),
            Selected = _step.SelectedFeatures.Contains(feature)
          };
          items.Add(featureItem);
        }
      }

      // Create list items for each package
      foreach (IBundlePackage package in _step.AvailablePackages)
      {
        SelectablePackageViewModel packageItem = new SelectablePackageViewModel
        {
          Item = CreatePackage(package),
          Selected = _step.SelectedPackages.Contains(package)
        };
        items.Add(packageItem);
      }

      return items;
    }

    protected override void OnItemSelectedChanged(SelectablePackageViewModel item, bool selected)
    {
      if (item is SelectableFeatureViewModel featureViewModel)
      {
        IBundlePackageFeature feature = _step.AvailableFeatures.FirstOrDefault(f => f.Id == featureViewModel.Item.Id);
        if (feature != null)
          UpdateSelectedItems(feature, _step.SelectedFeatures, selected);
      }
      else
      {
        IBundlePackage package = _step.AvailablePackages.FirstOrDefault(p => p.Id == item.Item.Id);
        if (package != null)
          UpdateSelectedItems(package, _step.SelectedPackages, selected);
      }

      // Updates the Next button state
      base.OnItemSelectedChanged(item, selected);
    }
  }
}
