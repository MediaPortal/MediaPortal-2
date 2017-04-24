#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using System;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using MediaPortal.Extensions.OnlineLibraries.Wrappers;

namespace MediaPortal.Extensions.OnlineLibraries.Matchers
{
  public class SeriesOmDbMatcher : SeriesMatcher<object, string>
  {
    #region Static instance

    public static SeriesOmDbMatcher Instance
    {
      get { return ServiceRegistration.Get<SeriesOmDbMatcher>(); }
    }

    #endregion

    #region Constants

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\OmDB\");
    protected static TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromMinutes(10);

    #endregion

    #region Init

    public SeriesOmDbMatcher() : 
      base(CACHE_PATH, MAX_MEMCACHE_DURATION, false)
    {
    }

    public override bool InitWrapper(bool useHttps)
    {
      try
      {
        OmDbWrapper wrapper = new OmDbWrapper();
        if (wrapper.Init(CACHE_PATH, useHttps))
        {
          _wrapper = wrapper;
          return true;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("SeriesOmDbMatcher: Error initializing wrapper", ex);
      }
      return false;
    }

    #endregion

    #region External match storage

    public override void StoreActorMatch(PersonInfo person)
    {
    }

    public override void StoreDirectorMatch(PersonInfo person)
    {
    }

    public override void StoreWriterMatch(PersonInfo person)
    {
    }

    public override void StoreCharacterMatch(CharacterInfo character)
    {
    }

    public override void StoreCompanyMatch(CompanyInfo company)
    {
    }

    public override void StoreTvNetworkMatch(CompanyInfo company)
    {
    }

    #endregion

    #region Metadata updaters

    public override bool FindAndUpdateEpisode(EpisodeInfo episodeInfo, bool importOnly)
    {
      // Don't allow OMDB during first import cycle because it is english only
      // If it was allowed it would prevent the update of metadata with preffered language
      // during refresh cycle that also allows searches which might be needed to find metadata 
      // in the preferred language
      if (importOnly && !Primary)
        return false;

      return base.FindAndUpdateEpisode(episodeInfo, importOnly);
    }

    public override bool UpdateSeries(SeriesInfo seriesInfo, bool updateEpisodeList, bool importOnly)
    {
      // Don't allow OMDB during first import cycle because it is english only
      // If it was allowed it would prevent the update of metadata with preffered language
      // during refresh cycle that also allows searches which might be needed to find metadata 
      // in the preferred language
      if (importOnly && !Primary)
        return false;

      return base.UpdateSeries(seriesInfo, updateEpisodeList, importOnly);
    }

    public override bool UpdateSeason(SeasonInfo seasonInfo, bool importOnly)
    {
      // Don't allow OMDB during first import cycle because it is english only
      // If it was allowed it would prevent the update of metadata with preffered language
      // during refresh cycle that also allows searches which might be needed to find metadata 
      // in the preferred language
      if (importOnly && !Primary)
        return false;

      return base.UpdateSeason(seasonInfo, importOnly);
    }

    public override bool UpdateSeriesPersons(SeriesInfo seriesInfo, string occupation, bool importOnly)
    {
      return false;
    }

    public override bool UpdateSeriesCharacters(SeriesInfo seriesInfo, bool importOnly)
    {
      return false;
    }

    public override bool UpdateSeriesCompanies(SeriesInfo seriesInfo, string companyType, bool importOnly)
    {
      return false;
    }

    public override bool UpdateEpisodePersons(EpisodeInfo episodeInfo, string occupation, bool importOnly)
    {
      return false;
    }

    public override bool UpdateEpisodeCharacters(EpisodeInfo episodeInfo, bool importOnly)
    {
      return false;
    }

    #endregion

    #region Translators

    protected override bool SetSeriesId(SeriesInfo series, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        series.ImdbId = id;
        return true;
      }
      return false;
    }

    protected override bool SetSeriesId(EpisodeInfo episode, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        episode.SeriesImdbId = id;
        return true;
      }
      return false;
    }

    protected override bool GetSeriesId(SeriesInfo series, out string id)
    {
      id = null;
      if (!string.IsNullOrEmpty(series.ImdbId))
      {
        id = series.ImdbId;
        return true;
      }
      return id != null;
    }

    protected override bool GetSeriesEpisodeId(EpisodeInfo episode, out string id)
    {
      id = null;
      if (!string.IsNullOrEmpty(episode.ImdbId))
        id = episode.ImdbId;
      return id != null;
    }

    protected override bool SetSeriesEpisodeId(EpisodeInfo episode, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        episode.ImdbId = id;
        return true;
      }
      return false;
    }

    #endregion

    #region FanArt

    protected override void DownloadFanArt(FanartDownload<string> fanartDownload)
    {
      // No fanart to download
      FinishDownloadFanArt(fanartDownload);
    }

    #endregion
  }
}
