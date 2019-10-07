#region Copyright (C) 2011-2013 MPExtended

// Copyright (C) 2011-2013 MPExtended Developers, http://www.mpextended.com/
// 
// MPExtended is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MPExtended is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MPExtended. If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Filters;

namespace MediaPortal.Plugins.MP2Extended.Extensions
{
  public static class EnumerableExtensionMethods
  {
    // Take a range from the list
    public static IEnumerable<T> TakeRange<T>(this IEnumerable<T> source, int start, int end)
    {
      int count = Math.Min(end - start + 1, source.Count() - start);

      if (source is List<T>)
        return ((List<T>)source).GetRange(start, count);
      return source.Skip(start).Take(count);
    }

    // Easy aliases for ordering and sorting from a service
    public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, WebSortOrder? order)
    {
      return OrderBy(source, keySelector, order.HasValue ? order.Value : WebSortOrder.Asc);
    }

    public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, WebSortOrder order)
    {
      if (order == WebSortOrder.Asc)
        return Enumerable.OrderBy(source, keySelector);
      return Enumerable.OrderByDescending(source, keySelector);
    }

    public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, WebSortOrder? order)
    {
      return ThenBy(source, keySelector, order.HasValue ? order.Value : WebSortOrder.Asc);
    }

    public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, WebSortOrder order)
    {
      if (order == WebSortOrder.Asc)
        return Enumerable.ThenBy(source, keySelector);
      return Enumerable.ThenByDescending(source, keySelector);
    }

    // Filter the list
    public static IEnumerable<T> Filter<T>(this IEnumerable<T> list, string filter)
    {
      if (String.IsNullOrWhiteSpace(filter))
        return list;

      var parser = new FilterParser(filter);
      var filterInstance = parser.Parse();
      filterInstance.ExpectType(list.GetType().GetGenericArguments().Single());
      return list.Where(x => filterInstance.Matches<T>(x));
    }

    // Easy alias for creating a dictionary
    public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> list)
    {
      return list.ToDictionary(x => x.Key, x => x.Value);
    }
  }
}