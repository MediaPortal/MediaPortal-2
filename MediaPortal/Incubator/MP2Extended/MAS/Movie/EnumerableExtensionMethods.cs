#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Extensions;

namespace MediaPortal.Plugins.MP2Extended.MAS.Movie
{
  internal static class IEnumerableExtensionMethods
  {
    public static IEnumerable<T> SortWebMovieBasic<T>(this IEnumerable<T> list, WebSortField? sortInput, WebSortOrder? orderInput) where T : WebMovieBasic
    {
      switch (sortInput)
      {
        case WebSortField.Title:
          return list.OrderBy(x => x.Title, orderInput);
        case WebSortField.DateAdded:
          return list.OrderBy(x => x.DateAdded, orderInput);
        case WebSortField.Year:
          return list.OrderBy(x => x.Year, orderInput);
        case WebSortField.Genre:
          return list.OrderBy(x => x.Genres, orderInput);
        case WebSortField.Rating:
          return list.OrderBy(x => x.Rating, orderInput);
        case WebSortField.Type:
          return list.OrderBy(x => x.Type, orderInput);
        default:
          return list.OrderBy(x => x.Title, orderInput);
      }
    }

    public static IEnumerable<T> SortWebMovieDetailed<T>(this IEnumerable<T> list, WebSortField? sortInput, WebSortOrder? orderInput) where T : WebMovieDetailed
    {
      switch (sortInput)
      {
        case WebSortField.Title:
          return list.OrderBy(x => x.Title, orderInput);
        case WebSortField.DateAdded:
          return list.OrderBy(x => x.DateAdded, orderInput);
        case WebSortField.Year:
          return list.OrderBy(x => x.Year, orderInput);
        case WebSortField.Genre:
          return list.OrderBy(x => x.Genres, orderInput);
        case WebSortField.Rating:
          return list.OrderBy(x => x.Rating, orderInput);
        case WebSortField.Type:
          return list.OrderBy(x => x.Type, orderInput);
        default:
          return list.OrderBy(x => x.Title, orderInput);
      }
    }
  }
}
