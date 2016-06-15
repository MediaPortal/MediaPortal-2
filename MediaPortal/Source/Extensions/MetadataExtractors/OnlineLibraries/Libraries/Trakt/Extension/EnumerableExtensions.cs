using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.Extension
{
  public static class EnumerableExtensions
  {
    public static IEnumerable<T> Append<T>(this IEnumerable<T> source, params T[] tail)
    {
      return source.Concat(tail);
    }

    /// <summary>
    /// avoid null reference exception for IEnumerables that are null
    /// before converting to a list
    /// </summary>
    public static List<T> ToNullableList<T>(this IEnumerable<T> source)
    {
      if (source == null)
        return null;

      return source.ToList();
    }

    public static bool IsAny<T>(this IEnumerable<T> data)
    {
      return data != null && data.Any();
    }
  }
}