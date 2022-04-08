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

using MP2BootstrapperApp.ViewModels.ListItems;
using MP2BootstrapperApp.WizardSteps;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MP2BootstrapperApp.ViewModels
{
  public abstract class AbstractSelectionViewModel<TStep, TItem> : InstallWizardPageViewModelBase<TStep> where TStep : IStep where TItem : class, ISelectable
  {
    public AbstractSelectionViewModel(TStep step)
      : base(step)
    {
      Items = new ObservableCollection<TItem>();
      foreach (TItem item in GetItems())
      {
        item.SelectedChanged += ItemSelectedChanged;
        Items.Add(item);
      }
    }

    public ObservableCollection<TItem> Items { get; }

    protected abstract IEnumerable<TItem> GetItems();

    protected virtual void OnItemSelectedChanged(TItem item, bool selected)
    {
      RaiseButtonStateChanged();
    }

    private void ItemSelectedChanged(object sender, SelectedChangedEventArgs e)
    {
      OnItemSelectedChanged(sender as TItem, e.Selected);
    }

    /// <summary>
    /// Utility mathod that can add or remove an item from a collection of selected items depending on whether the item is selected. 
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
