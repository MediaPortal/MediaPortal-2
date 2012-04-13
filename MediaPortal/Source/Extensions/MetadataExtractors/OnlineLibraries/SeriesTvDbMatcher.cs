#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Globalization;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries.TheTvDB;
using TvdbLib.Data;

namespace MediaPortal.Extensions.OnlineLibraries
{
  /// <summary>
  /// <see cref="SeriesTvDbMatcher"/> is used to look up online series information from TheTvDB.com.
  /// </summary>
  public class SeriesTvDbMatcher
  {
    public static readonly List<SeriesMatch> Matches = new List<SeriesMatch>();

    public bool TryMatch(SeriesInfo seriesInfo)
    {
      // Use cached values before doing online query
      SeriesMatch match = Matches.Find(m => m.SeriesName == seriesInfo.Series);
      if (match != null)
      {
        seriesInfo.Series = match.TvDBName;
        return true;
      }

      // Try online lookup
      TvDbWrapper tv = new TvDbWrapper();

      // Try to lookup online content in the configured language
      CultureInfo currentCulture = ServiceRegistration.Get<ILocalization>().CurrentCulture;
      tv.SetPreferredLanguage(currentCulture.TwoLetterISOLanguageName);
      tv.Init();

      List<TvdbSearchResult> series;
      if (tv.SearchSeriesUnique(seriesInfo.Series, out series))
      {
        TvdbSearchResult matchedSeries = series[0];
        TvdbSeries seriesDetail;
        if (tv.GetSeries(matchedSeries.Id, false, out seriesDetail))
        {
          // Add this match to cache
          if (Matches.All(m => m.SeriesName != seriesInfo.Series))
            Matches.Add(new SeriesMatch
                          {
                            SeriesName = seriesInfo.Series,
                            TvDBID = seriesDetail.Id,
                            TvDBName = seriesDetail.SeriesName
                          });

          // TODO: also load banners when requested first

          // Use name of online library for import into ML.
          seriesInfo.Series = seriesDetail.SeriesName;
          return true;
        }
      }
      return false;
    }
  }
}