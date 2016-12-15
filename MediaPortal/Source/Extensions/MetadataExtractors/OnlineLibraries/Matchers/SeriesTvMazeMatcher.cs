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
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvMazeV1.Data;
using MediaPortal.Extensions.OnlineLibraries.Wrappers;

namespace MediaPortal.Extensions.OnlineLibraries.Matchers
{
  public class SeriesTvMazeMatcher : SeriesMatcher<TvMazeImageCollection, string>
  {
    #region Static instance

    public static SeriesTvMazeMatcher Instance
    {
      get { return ServiceRegistration.Get<SeriesTvMazeMatcher>(); }
    }

    #endregion

    #region Constants

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\TvMaze\");
    protected static TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromMinutes(10);

    #endregion

    #region Init

    public SeriesTvMazeMatcher() : 
      base(CACHE_PATH, MAX_MEMCACHE_DURATION, true)
    {
    }

    public override bool InitWrapper(bool useHttps)
    {
      try
      {
        TvMazeWrapper wrapper = new TvMazeWrapper();
        if (wrapper.Init(CACHE_PATH))
        {
          _wrapper = wrapper;
          return true;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("SeriesTvMazeMatcher: Error initializing wrapper", ex);
      }
      return false;
    }

    #endregion

    #region External match storage

    public override void StoreCompanyMatch(CompanyInfo company)
    {
    }

    public override void StoreTvNetworkMatch(CompanyInfo company)
    {
    }

    #endregion

    #region Metadata updaters

    public override bool UpdateSeason(SeasonInfo seasonInfo, bool importOnly)
    {
      return false;
    }

    public override bool UpdateSeriesCompanies(SeriesInfo seriesInfo, string companyType, bool importOnly)
    {
      return false;
    }

    #endregion

    #region Translators

    protected override bool SetSeriesId(SeriesInfo series, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        series.TvMazeId = Convert.ToInt32(id);
        return true;
      }
      return false;
    }

    protected override bool SetSeriesId(EpisodeInfo episode, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        episode.SeriesTvMazeId = Convert.ToInt32(id);
        return true;
      }
      return false;
    }

    protected override bool GetSeriesId(SeriesInfo series, out string id)
    {
      id = null;
      if (series.TvMazeId > 0)
        id = series.TvMazeId.ToString();
      return id != null;
    }

    protected override bool GetSeriesEpisodeId(EpisodeInfo episode, out string id)
    {
      id = null;
      if (episode.TvMazeId > 0)
        id = episode.TvMazeId.ToString();
      return id != null;
    }

    protected override bool SetSeriesEpisodeId(EpisodeInfo episode, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        episode.TvMazeId = Convert.ToInt32(id);
        return true;
      }
      return false;
    }

    protected override bool GetCompanyId(CompanyInfo company, out string id)
    {
      id = null;
      if (company.TvMazeId > 0)
        id = company.TvMazeId.ToString();
      return id != null;
    }

    protected override bool SetCompanyId(CompanyInfo company, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        company.TvMazeId = Convert.ToInt32(id);
        return true;
      }
      return false;
    }

    protected override bool GetCharacterId(CharacterInfo character, out string id)
    {
      id = null;
      if (character.TvMazeId > 0)
        id = character.TvMazeId.ToString();
      return id != null;
    }

    protected override bool SetCharacterId(CharacterInfo character, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        character.TvMazeId = Convert.ToInt32(id);
        return true;
      }
      return false;
    }

    protected override bool GetPersonId(PersonInfo person, out string id)
    {
      id = null;
      if (person.TvMazeId > 0)
        id = person.TvMazeId.ToString();
      return id != null;
    }

    protected override bool SetPersonId(PersonInfo person, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        person.TvMazeId = Convert.ToInt32(id);
        return true;
      }
      return false;
    }

    #endregion
  }
}
