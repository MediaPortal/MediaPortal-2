#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Linq;
using System.Web.Mvc;

namespace MediaPortal.PackageServer.Utility.Extensions
{
  /// <summary>
  ///   Extension methods on IEnumerable.
  /// </summary>
  public static class SelectListExtensions
  {
    /// <summary>
    ///   Converts the source sequence into an IEnumerable of SelectListItem
    /// </summary>
    /// <param name = "items">Source sequence</param>
    /// <param name = "nameSelector">Lambda that specifies the name for the SelectList items</param>
    /// <param name = "valueSelector">Lambda that specifies the value for the SelectList items</param>
    /// <returns>IEnumerable of SelectListItem</returns>
    public static IEnumerable<SelectListItem> ToSelectList<TItem, TValue>(this IEnumerable<TItem> items, Func<TItem, TValue> valueSelector, Func<TItem, string> nameSelector)
    {
      return items.ToSelectList(valueSelector, nameSelector, x => false);
    }

    /// <summary>
    ///   Converts the source sequence into an IEnumerable of SelectListItem
    /// </summary>
    /// <param name = "items">Source sequence</param>
    /// <param name = "nameSelector">Lambda that specifies the name for the SelectList items</param>
    /// <param name = "valueSelector">Lambda that specifies the value for the SelectList items</param>
    /// <param name = "selectedItems">Those items that should be selected</param>
    /// <returns>IEnumerable of SelectListItem</returns>
    public static IEnumerable<SelectListItem> ToSelectList<TItem, TValue>(this IEnumerable<TItem> items, Func<TItem, TValue> valueSelector, Func<TItem, string> nameSelector, IEnumerable<TValue> selectedItems)
    {
      return items.ToSelectList(valueSelector, nameSelector, x => selectedItems != null && selectedItems.Contains(valueSelector(x)));
    }

    /// <summary>
    ///   Converts the source sequence into an IEnumerable of SelectListItem
    /// </summary>
    /// <param name = "items">Source sequence</param>
    /// <param name = "nameSelector">Lambda that specifies the name for the SelectList items</param>
    /// <param name = "valueSelector">Lambda that specifies the value for the SelectList items</param>
    /// <param name = "selectedValueSelector">Lambda that specifies whether the item should be selected</param>
    /// <returns>IEnumerable of SelectListItem</returns>
    public static IEnumerable<SelectListItem> ToSelectList<TItem, TValue>(this IEnumerable<TItem> items, Func<TItem, TValue> valueSelector, Func<TItem, string> nameSelector, Func<TItem, bool> selectedValueSelector)
    {
      return from item in items
        let value = valueSelector(item)
        select new SelectListItem
        {
          Text = nameSelector(item),
          Value = value.ToString(),
          Selected = selectedValueSelector(item)
        };
    }
  }
}