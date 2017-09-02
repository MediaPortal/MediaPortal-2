using System.Collections.Generic;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Extensions;

namespace MediaPortal.Plugins.MP2Extended.MAS.TvShow
{
  internal static class IEnumerableExtensionMethods
  {
    public static IEnumerable<T> SortWebTVShowBasic<T>(this IEnumerable<T> list, WebSortField? sortInput, WebSortOrder? orderInput) where T : WebTVShowBasic
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
        default:
          return list.OrderBy(x => x.Title, orderInput);
      }
    }

    public static IEnumerable<T> SortWebTVEpisodeBasic<T>(this IEnumerable<T> list, WebSortField? sortInput, WebSortOrder? orderInput) where T : WebTVEpisodeBasic
    {
      switch (sortInput)
      {
        case WebSortField.Title:
          return list.OrderBy(x => x.Title, orderInput);
        case WebSortField.DateAdded:
          return list.OrderBy(x => x.DateAdded, orderInput);
        case WebSortField.Rating:
          return list.OrderBy(x => x.Rating, orderInput);
        case WebSortField.TVDateAired:
          return list.OrderBy(x => x.FirstAired, orderInput);
        default:
          return list.OrderBy(x => x.Title, orderInput);
      }
    }

    public static IEnumerable<T> SortWebTVEpisodeDetailed<T>(this IEnumerable<T> list, WebSortField? sortInput, WebSortOrder? orderInput) where T : WebTVEpisodeDetailed
    {
      switch (sortInput)
      {
        case WebSortField.Title:
          return list.OrderBy(x => x.Title, orderInput);
        case WebSortField.DateAdded:
          return list.OrderBy(x => x.DateAdded, orderInput);
        case WebSortField.Rating:
          return list.OrderBy(x => x.Rating, orderInput);
        case WebSortField.TVEpisodeNumber:
          return list.OrderBy(x => x.EpisodeNumber, orderInput);
        case WebSortField.TVDateAired:
          return list.OrderBy(x => x.FirstAired, orderInput);
        case WebSortField.Type:
          return list.OrderBy(x => x.Type, orderInput);
        default:
          return list.OrderBy(x => x.Title, orderInput);
      }
    }

    public static IEnumerable<T> SortWebGenre<T>(this IEnumerable<T> list, WebSortField? sortInput, WebSortOrder? orderInput) where T : WebGenre
    {
      switch (sortInput)
      {
        case WebSortField.Title:
        default:
          return list.OrderBy(x => x.Title, orderInput);
      }
    }

    public static IEnumerable<T> SortWebTVSeasonBasic<T>(this IEnumerable<T> list, WebSortField? sortInput, WebSortOrder? orderInput) where T : WebTVSeasonBasic
    {
      switch (sortInput)
      {
        case WebSortField.Title:
          return list.OrderBy(x => x.Title, orderInput);
        case WebSortField.DateAdded:
          return list.OrderBy(x => x.DateAdded, orderInput);
        case WebSortField.Year:
          return list.OrderBy(x => x.Year, orderInput);
        case WebSortField.TVSeasonNumber:
          return list.OrderBy(x => x.SeasonNumber, orderInput);
        default:
          return list.OrderBy(x => x.Title, orderInput);
      }
    }

    public static IEnumerable<T> SortWebTVSeasonDetailed<T>(this IEnumerable<T> list, WebSortField? sortInput, WebSortOrder? orderInput) where T : WebTVSeasonDetailed
    {
      switch (sortInput)
      {
        case WebSortField.Title:
          return list.OrderBy(x => x.Title, orderInput);
        case WebSortField.DateAdded:
          return list.OrderBy(x => x.DateAdded, orderInput);
        case WebSortField.Year:
          return list.OrderBy(x => x.Year, orderInput);
        case WebSortField.TVSeasonNumber:
          return list.OrderBy(x => x.SeasonNumber, orderInput);
        default:
          return list.OrderBy(x => x.Title, orderInput);
      }
    }
  }
}