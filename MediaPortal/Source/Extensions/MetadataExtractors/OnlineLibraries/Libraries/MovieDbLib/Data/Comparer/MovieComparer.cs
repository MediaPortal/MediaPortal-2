using System;
using System.Collections.Generic;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data.Comparer
{
  /// <summary>
  /// Compare episodes according to their default episode numbers (aired)
  /// </summary>
  internal class MovieComparerAired: IComparer<MovieDbMovie>
  {
    public int Compare(MovieDbMovie x, MovieDbMovie y)
    {
      return DateTime.Compare(x.Released, y.Released);
    }
  }
}
