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
using System.Linq;
using System.Linq.Expressions;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Filters;

namespace MediaPortal.Plugins.MP2Extended.Extensions
{
  public static class QueryableExtensionMethods
  {
    // Take a range from the list
    public static IQueryable<T> TakeRange<T>(this IQueryable<T> source, int start, int end)
    {
      // Don't compensate for a too high end value (end - start + 1 > source.Count()), as that'll execute an additional query.
      int count = end - start + 1;
      return source.Skip(start).Take(count);
    }

    // Easy aliases for ordering and sorting from a service
    public static IOrderedQueryable<TSource> OrderBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, WebSortOrder? order)
    {
      return OrderBy(source, keySelector, order.HasValue ? order.Value : WebSortOrder.Asc);
    }

    public static IOrderedQueryable<TSource> OrderBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, WebSortOrder order)
    {
      if (order == WebSortOrder.Asc)
        return Queryable.OrderBy(source, keySelector);
      return Queryable.OrderByDescending(source, keySelector);
    }

    public static IOrderedQueryable<TSource> ThenBy<TSource, TKey>(this IOrderedQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, WebSortOrder? order)
    {
      return ThenBy(source, keySelector, order.HasValue ? order.Value : WebSortOrder.Asc);
    }

    public static IOrderedQueryable<TSource> ThenBy<TSource, TKey>(this IOrderedQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, WebSortOrder order)
    {
      if (order == WebSortOrder.Asc)
        return Queryable.ThenBy(source, keySelector);
      return Queryable.ThenByDescending(source, keySelector);
    }

    // Filter the list
    public static IQueryable<T> Filter<T>(this IQueryable<T> list, string filter)
    {
      if (String.IsNullOrWhiteSpace(filter))
        return list;

      var parser = new FilterParser(filter);
      var filterInstance = parser.Parse();
      filterInstance.ExpectType(list.GetType().GetGenericArguments().Single());
      return list.Where(x => filterInstance.Matches<T>(x));
    }
  }
}