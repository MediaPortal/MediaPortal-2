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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace MP2BootstrapperApp.ViewModels
{
  public abstract class AbstractPackageSelectionViewModel<T> : InstallWizardPageViewModelBase<T> where T : AbstractPackageSelectionStep
  {
    public AbstractPackageSelectionViewModel(T step)
      : base(step)
    {
      Items = new ObservableCollection<SelectablePackageViewModel>();
      AddItems(step);
    }

    public ObservableCollection<SelectablePackageViewModel> Items { get; }

    protected void AddItems(AbstractPackageSelectionStep step)
    {
      // Create list items for each feature
      foreach (IBundlePackageFeature feature in step.AvailableFeatures)
      {
        IBundlePackage parentPackage = step.BootstrapperApplicationModel.BundlePackages.FirstOrDefault(p => p.Id == feature.Package);
        if (parentPackage != null)
        {

          SelectableFeatureViewModel featureItem = new SelectableFeatureViewModel
          {
            Package = CreatePackageFeature(parentPackage, feature),
            Selected = step.SelectedFeatures.Contains(feature)
          };
          featureItem.PropertyChanged += ItemPropertyChanged;
          Items.Add(featureItem);
        }
      }

      // Create list items for each package
      foreach (IBundlePackage package in step.AvailablePackages)
      {
        SelectablePackageViewModel packageItem = new SelectablePackageViewModel
        {
          Package = CreatePackage(package),
          Selected = step.SelectedPackages.Contains(package)
        };
        packageItem.PropertyChanged += ItemPropertyChanged;
        Items.Add(packageItem);
      }
    }

    private void ItemPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName == nameof(SelectablePackageViewModel.Selected))
      {
        if (sender is SelectableFeatureViewModel featureViewModel)
        {
          IBundlePackageFeature feature = _step.AvailableFeatures.FirstOrDefault(f => f.FeatureName == featureViewModel.Package.Id);
          if (feature != null)
          {
            UpdateSelectedItems(feature, _step.SelectedFeatures, featureViewModel.Selected);
          }
        }
        else if (sender is SelectablePackageViewModel packageViewModel)
        {
          IBundlePackage package = _step.AvailablePackages.FirstOrDefault(p => p.Id == packageViewModel.Package.Id);
          if (package != null)
          {
            UpdateSelectedItems(package, _step.SelectedPackages, packageViewModel.Selected);
          }
        }

        RaiseButtonStateChanged();
      }
    }

    /// <summary>
    /// Adds or removes an item from a collection of selected items depending on whether the item is selected. 
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <param name="item">The item that's selected state has changed.</param>
    /// <param name="selectedItems">The collection of selected items to update.</param>
    /// <param name="selected">Whether the item is selected.</param>
    protected void UpdateSelectedItems<T1>(T1 item, ICollection<T1> selectedItems, bool selected)
    {
      if (selected)
      {
        if (!selectedItems.Contains(item))
        {
          selectedItems.Add(item);
        }
      }
      else
      {
        selectedItems.Remove(item);
      }
    }
  }
}
