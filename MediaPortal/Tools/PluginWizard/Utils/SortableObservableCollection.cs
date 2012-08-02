#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MP2_PluginWizard.Utils
{

  public class SortableObservableCollection<T> : ObservableCollection<T>
  {
    public SortableObservableCollection()
    {
    }

    public SortableObservableCollection(List<T> list)
      : base(list)
    {
    }

    public SortableObservableCollection(IEnumerable<T> collection)
      : base(collection)
    {
    }

    public void Sort<TKey>(Func<T, TKey> keySelector, System.ComponentModel.ListSortDirection direction)
    {
      switch (direction)
      {
        case System.ComponentModel.ListSortDirection.Ascending:
          {
            ApplySort(Items.OrderBy(keySelector));
            break;
          }
        case System.ComponentModel.ListSortDirection.Descending:
          {
            ApplySort(Items.OrderByDescending(keySelector));
            break;
          }
      }
    }

    public void Sort<TKey>(Func<T, TKey> keySelector, IComparer<TKey> comparer)
    {
      ApplySort(Items.OrderBy(keySelector, comparer));
    }

    private void ApplySort(IEnumerable<T> sortedItems)
    {
      var sortedItemsList = sortedItems.ToList();

      foreach (var item in sortedItemsList)
        Move(IndexOf(item), sortedItemsList.IndexOf(item));
    }
  }

}
