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