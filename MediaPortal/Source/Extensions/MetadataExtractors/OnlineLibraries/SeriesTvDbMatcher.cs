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
using System.Xml.Serialization;
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
    public const string SETTINGS_MATCHES = @"C:\ProgramData\Team MediaPortal\MP2-Client\TvDB\Matches.xml";

    static SeriesTvDbMatcher()
    {
      List<SeriesMatch> savedList = Settings.Load<List<SeriesMatch>>(SETTINGS_MATCHES);
      if (savedList != null)
        savedList.ForEach(Matches.Add);
    }

    public bool TryGetTvDbId(string seriesName, out int tvDbId)
    {
      SeriesMatch match = TryMatch(seriesName);
      if (match != null)
      {
        tvDbId = match.TvDBID;
        return true;
      }
      tvDbId = 0;
      return false;
    }

    public bool TryMatch(SeriesInfo seriesInfo)
    {
      // Use cached values before doing online query
      SeriesMatch match = TryMatch(seriesInfo.Series);
      if (match != null)
      {
        seriesInfo.Series = match.TvDBName;
        return true;
      }
      return false;
    }

    protected SeriesMatch TryMatch(string seriesName)
    {
      // Use cached values before doing online query
      SeriesMatch match = Matches.Find(m => m.SeriesName == seriesName);
      if (match != null)
        return match;

      // Try online lookup
      TvDbWrapper tv = new TvDbWrapper();

      // Try to lookup online content in the configured language
      CultureInfo currentCulture = ServiceRegistration.Get<ILocalization>().CurrentCulture;
      tv.SetPreferredLanguage(currentCulture.TwoLetterISOLanguageName);
      tv.Init();

      List<TvdbSearchResult> series;
      if (tv.SearchSeriesUnique(seriesName, out series))
      {
        TvdbSearchResult matchedSeries = series[0];
        TvdbSeries seriesDetail;
        if (tv.GetSeries(matchedSeries.Id, false, out seriesDetail))
        {
          // Add this match to cache
          SeriesMatch onlineMatch = new SeriesMatch
          {
            SeriesName = seriesName,
            TvDBID = seriesDetail.Id,
            TvDBName = seriesDetail.SeriesName
          };

          if (Matches.All(m => m.SeriesName != seriesName))
            Matches.Add(onlineMatch);

          // Save cache
          Settings.Save(SETTINGS_MATCHES, Matches);

          // TODO: also load banners when requested first
          return onlineMatch;
        }
      }
      return null;
    }
  }
}