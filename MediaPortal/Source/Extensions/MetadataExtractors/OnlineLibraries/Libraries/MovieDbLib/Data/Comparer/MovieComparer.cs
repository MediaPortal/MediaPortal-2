using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MovieDbLib.Data.Comparer
{
  /// <summary>
  /// Compare episodes according to their default episode numbers (aired)
  /// </summary>
  internal class MovieComparerAired: IComparer<MovieDbMovie>
  {
    public int Compare(MovieDbMovie x, MovieDbMovie y)
    {
      return (int)(x.Released - y.Released).TotalMilliseconds;
    }
  }

  /*
  /// <summary>
  /// Compare episodes according to their dvd episode number
  /// </summary>
  internal class EpisodeComparerDvd : IComparer<TvdbEpisode>
  {
    public int Compare(TvdbEpisode x, TvdbEpisode y)
    {
      return (int)(x.DvdEpisodeNumber - y.DvdEpisodeNumber);
    }
  }

  /// <summary>
  /// Compare episodes according to their absolute episode number
  /// </summary>
  internal class EpisodeComparerAbsolute : IComparer<TvdbEpisode>
  {
    public int Compare(TvdbEpisode x, TvdbEpisode y)
    {
      return (int)(x.AbsoluteNumber - y.AbsoluteNumber);
    }
  }
   * */
}
