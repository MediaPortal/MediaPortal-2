using System.Collections.Generic;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data.Comparer
{
  /// <summary>
  /// Compare episodes according to their default episode numbers (aired)
  /// </summary>
  internal class EpisodeComparerAired: IComparer<TvdbEpisode>
  {
    public int Compare(TvdbEpisode x, TvdbEpisode y)
    {
      return x.EpisodeNumber - y.EpisodeNumber;
    }
  }

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
      return x.AbsoluteNumber - y.AbsoluteNumber;
    }
  }
}
