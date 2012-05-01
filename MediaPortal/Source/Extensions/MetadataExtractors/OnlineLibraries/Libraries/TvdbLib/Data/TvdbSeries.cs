/*
 *   TvdbLib: A library to retrieve information and media from http://thetvdb.com
 * 
 *   Copyright (C) 2008  Benjamin Gmeiner, BSc.
 * 
 *   This program is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TvdbLib.Data.Banner;
using TvdbLib.Data;

namespace TvdbLib.Data
{
  /// <summary>
  /// Series class holds all the info that can be retrieved from http://thetvdb.com.  <br/>
  /// <br/>
  /// Those are as follows:<br/>
  /// <br/>
  ///  - Base information: <br/>
  ///  <code>
  ///    <Series>
  ///       <id>73739</id>
  ///       <Actors>|Malcolm David Kelley|Jorge Garcia|Maggie Grace|...|</Actors>
  ///       <Airs_DayOfWeek>Thursday</Airs_DayOfWeek>
  ///       <Airs_Time>9:00 PM</Airs_Time>
  ///       <ContentRating>TV-14</ContentRating>
  ///       <FirstAired>2004-09-22</FirstAired>
  ///       <Genre>|Action and Adventure|Drama|Science-Fiction|</Genre>
  ///       <IMDB_ID>tt0411008</IMDB_ID>
  ///       <Language>en</Language>
  ///       <Network>ABC</Network>
  ///       <Overview>After Oceanic Air flight 815...</Overview>
  ///       <Rating>8.9</Rating>
  ///       <Runtime>60</Runtime>
  ///       <SeriesID>24313</SeriesID>
  ///       <SeriesName>Lost</SeriesName>
  ///       <Status>Continuing</Status>
  ///       <banner>graphical/24313-g2.jpg</banner>
  ///       <fanart>fanart/original/73739-1.jpg</fanart>
  ///       <lastupdated>1205694666</lastupdated>
  ///       <zap2it_id>SH672362</zap2it_id>
  ///    </Series>
  ///  </code>
  ///  - Banner information <br/>
  ///  - Episode information <br/>
  ///  - Extended actor information <br/>
  ///  <br/>
  /// Each of those can be downloaded seperately. If the information is downloaded as 
  /// zipped file, everything is downloaded at once
  /// </summary>
  [Serializable]
  public class TvdbSeries : TvdbSeriesFields
  {
    #region private properties
    private Dictionary<TvdbLanguage, TvdbSeriesFields> m_seriesTranslations;
    #endregion

    /// <summary>
    /// Basic constructor for the TvdbSeries class
    /// </summary>
    public TvdbSeries()
    {
      m_banners = new List<TvdbBanner>();
      m_bannersLoaded = false;
      m_tvdbActorsLoaded = false;

    }

    /// <summary>
    /// Create a series object with all the information contained in the TvdbSeriesFields object
    /// </summary>
    /// <param name="_fields"></param>
    internal TvdbSeries(TvdbSeriesFields _fields)
      : this()
    {
      AddLanguage(_fields);
      SetLanguage(_fields.Language);
      //UpdateTvdbFields(_fields, true);
    }


    internal void SetEpisodes(List<TvdbEpisode> _episodes)
    {
      foreach (KeyValuePair<TvdbLanguage, TvdbSeriesFields> kvp in m_seriesTranslations)
      {
        if (kvp.Key.Abbriviation.Equals(this.Language.Abbriviation))
        {
          kvp.Value.EpisodesLoaded = true;
          kvp.Value.Episodes.Clear();
          kvp.Value.Episodes.AddRange(_episodes);
        }
      }

      this.EpisodesLoaded = true;
      this.Episodes.Clear();
      this.Episodes.AddRange(_episodes);
    }

    /// <summary>
    /// Add a new language to the series
    /// </summary>
    /// <param name="_fields"></param>
    internal void AddLanguage(TvdbSeriesFields _fields)
    {
      if (m_seriesTranslations == null)
      {
        m_seriesTranslations = new Dictionary<TvdbLanguage, TvdbSeriesFields>();
      }

      //delete translation if it already exists and overwrite it with a new one
      if (m_seriesTranslations.ContainsKey(_fields.Language))
      {
        m_seriesTranslations.Remove(_fields.Language);
      }
      /*foreach (KeyValuePair<TvdbLanguage, TvdbSeriesFields> kvp in m_seriesTranslations)
      {
        if (kvp.Key == _fields.Language)
        {
          m_seriesTranslations.Remove(kvp.Key);
        }
      }*/

      m_seriesTranslations.Add(_fields.Language, _fields);
    }

    /// <summary>
    /// Set the language of the series to one of the languages that have
    /// already been loaded
    /// </summary>
    /// <param name="_language">The new language for this series</param>
    /// <returns>true if success, false otherwise</returns>
    public bool SetLanguage(TvdbLanguage _language)
    {
      return SetLanguage(_language.Abbriviation);
    }

    /// <summary>
    /// Set the language of the series to one of the languages that have
    /// already been loaded
    /// </summary>
    /// <param name="_language">The new language abbriviation for this series</param>
    /// <returns>true if success, false otherwise</returns>
    public bool SetLanguage(String _language)
    {
      foreach (KeyValuePair<TvdbLanguage, TvdbSeriesFields> kvp in m_seriesTranslations)
      {
        if (kvp.Key.Abbriviation.Equals(_language))
        {
          this.UpdateTvdbFields(kvp.Value, true);
          return true;
        }
      }
      return false;
    }


    /// <summary>
    /// Get all languages that have already been loaded for this series
    /// </summary>
    /// <returns>List of all translations that are loaded for this series</returns>
    public List<TvdbLanguage> GetAvailableLanguages()
    {
      if (m_seriesTranslations != null)
      {
        return m_seriesTranslations.Keys.ToList();
      }
      else
      {
        return null;
      }
    }

    /// <summary>
    /// Get all available Translations
    /// </summary>
    internal Dictionary<TvdbLanguage, TvdbSeriesFields> SeriesTranslations
    {
      get { return m_seriesTranslations; }
      set { m_seriesTranslations = value; }
    }

    #region user properties
    private bool m_isFavorite;

    /// <summary>
    /// Is the series a favorite
    /// </summary>
    public bool IsFavorite
    {
      get { return m_isFavorite; }
      set { m_isFavorite = value; }
    }

    #endregion

    #region tvdb properties
    /// <summary>
    /// Returns the genre string in the format | genre1 | genre2 | genre3 |
    /// </summary>
    public String GenreString
    {
      get
      {
        if (Genre == null || Genre.Count == 0) return "";
        StringBuilder retString = new StringBuilder();
        retString.Append("|");
        foreach (String s in Genre)
        {
          retString.Append(s);
          retString.Append("|");
        }
        return retString.ToString();
      }
    }

    /// <summary>
    /// Formatted String of actors that appear during this episode in the 
    /// format | actor1 | actor2 | actor3 |
    /// </summary>
    public String ActorsString
    {
      get
      {
        if (Actors == null || Actors.Count == 0) return "";
        StringBuilder retString = new StringBuilder();
        retString.Append("|");
        foreach (String s in Actors)
        {
          retString.Append(s);
          retString.Append("|");
        }
        return retString.ToString();
      }
    }

    #endregion

    #region banners

    //all banners
    private List<TvdbBanner> m_banners;
    private bool m_bannersLoaded;

    /// <summary>
    /// returns a list of all banners for this series
    /// </summary>
    public List<TvdbBanner> Banners
    {
      get { return m_banners; }
      set
      {
        m_banners = value;
        m_bannersLoaded = true;
      }
    }

    /// <summary>
    /// Is the banner info loaded
    /// </summary>
    public bool BannersLoaded
    {
      get { return m_bannersLoaded; }
      set { m_bannersLoaded = value; }
    }

    /// <summary>
    /// returns a list of all series banners for this series
    /// </summary>
    public List<TvdbSeriesBanner> SeriesBanners
    {
      get
      {
        List<TvdbSeriesBanner> retList = new List<TvdbSeriesBanner>();
        foreach (TvdbBanner b in Banners)
        {
          if (b.GetType() == typeof(TvdbSeriesBanner))
          {
            retList.Add((TvdbSeriesBanner)b);
          }
        }
        return retList;
      }
    }

    /// <summary>
    /// Returns a list of all season banners for this series
    /// </summary>
    public List<TvdbSeasonBanner> SeasonBanners
    {
      get
      {
        List<TvdbSeasonBanner> retList = new List<TvdbSeasonBanner>();
        foreach (TvdbBanner b in Banners)
        {
          if (b.GetType() == typeof(TvdbSeasonBanner))
          {
            retList.Add((TvdbSeasonBanner)b);
          }
        }
        return retList;
      }
    }

    /// <summary>
    /// Returns a list of all season banners for this series
    /// </summary>
    public List<TvdbPosterBanner> PosterBanners
    {
      get
      {
        List<TvdbPosterBanner> retList = new List<TvdbPosterBanner>();
        foreach (TvdbBanner b in Banners)
        {
          if (b.GetType() == typeof(TvdbPosterBanner))
          {
            retList.Add((TvdbPosterBanner)b);
          }
        }
        return retList;
      }
    }

    /// <summary>
    /// Returns a list of all fanart banners for this series
    /// </summary>
    public List<TvdbFanartBanner> FanartBanners
    {
      get
      {
        List<TvdbFanartBanner> retList = new List<TvdbFanartBanner>();
        foreach (TvdbBanner b in Banners)
        {
          if (b.GetType() == typeof(TvdbFanartBanner))
          {
            retList.Add((TvdbFanartBanner)b);
          }
        }
        return retList;
      }
    }

    #endregion

    #region episodes

    /// <summary>
    /// Return a list of episodes for the given season
    /// </summary>
    /// <param name="_season">Season for which episodes should be returned</param>
    /// <returns>List of episodes for the given season</returns>
    public List<TvdbEpisode> GetEpisodes(int _season)
    {
      List<TvdbEpisode> retList = new List<TvdbEpisode>();
      if (Episodes != null && Episodes.Count > 0 && EpisodesLoaded)
      {
        foreach (TvdbEpisode e in Episodes)
        {
          if (e.SeasonNumber == _season)
          {
            retList.Add(e);
          }
        }
      }
      return retList;
    }

    /// <summary>
    /// How many Season does the series have
    /// </summary>
    public int NumSeasons
    {
      get
      {
        if (Episodes != null && EpisodesLoaded && Episodes.Count > 0)
        {
          int maxSeason = 0;
          foreach (TvdbEpisode e in Episodes)
          {
            if (e.SeasonNumber > maxSeason)
            {
              maxSeason++;
            }
          }
          return maxSeason;
        }
        else
        {
          return 0;
        }
      }
    }


    #endregion

    #region Actors
    //Actor Information
    private List<TvdbActor> m_tvdbActors;
    private bool m_tvdbActorsLoaded;

    /// <summary>
    /// List of loaded tvdb actors
    /// </summary>
    public List<TvdbActor> TvdbActors
    {
      get { return m_tvdbActors; }
      set
      {
        m_tvdbActorsLoaded = true;
        m_tvdbActors = value;
      }
    }

    /// <summary>
    /// Is the actor info loaded
    /// </summary>
    public bool TvdbActorsLoaded
    {
      get { return m_tvdbActorsLoaded; }
      set { m_tvdbActorsLoaded = value; }
    }
    #endregion

    /// <summary>
    /// returns SeriesName (SeriesId)
    /// </summary>
    /// <returns>String representing this series</returns>
    public override string ToString()
    {
      return SeriesName + "(" + Id + ")";
    }


    /// <summary>
    /// Uptdate the info of the current series with the updated one
    /// </summary>
    /// <param name="_series">TvdbSeries object</param>
    protected void UpdateSeriesInfo(TvdbSeries _series)
    {
      this.Actors = _series.Actors;
      this.AirsDayOfWeek = _series.AirsDayOfWeek;
      this.AirsTime = _series.AirsTime;
      this.BannerPath = _series.BannerPath;
      this.Banners = _series.Banners;
      this.ContentRating = _series.ContentRating;
      this.FanartPath = _series.FanartPath;
      this.FirstAired = _series.FirstAired;
      this.Genre = _series.Genre;
      this.Id = _series.Id;
      this.ImdbId = _series.ImdbId;
      this.Language = _series.Language;
      this.LastUpdated = _series.LastUpdated;
      this.Network = _series.Network;
      this.Overview = _series.Overview;
      this.Rating = _series.Rating;
      this.Runtime = _series.Runtime;
      this.SeriesName = _series.SeriesName;
      this.Status = _series.Status;
      this.TvDotComId = _series.TvDotComId;
      this.Zap2itId = _series.Zap2itId;

      if (_series.EpisodesLoaded)
      {//check if the old series has any images loaded already -> if yes, save them
        if (this.EpisodesLoaded)
        {
          foreach (TvdbEpisode oe in this.Episodes)
          {
            foreach (TvdbEpisode ne in _series.Episodes)
            {
              if (oe.SeasonNumber == ne.SeasonNumber &&
                  oe.EpisodeNumber == ne.EpisodeNumber)
              {
                if (oe.Banner != null && oe.Banner.IsLoaded)
                {
                  ne.Banner = oe.Banner;
                }
              }
            }
          }
        }

        this.Episodes.Clear();
        this.Episodes.AddRange(_series.Episodes);
      }

      if (_series.TvdbActorsLoaded)
      {//check if the old series has any images loaded already -> if yes, save them
        if (this.TvdbActorsLoaded)
        {
          foreach (TvdbActor oa in this.TvdbActors)
          {
            foreach (TvdbActor na in _series.TvdbActors)
            {
              if (oa.Id == na.Id)
              {
                if (oa.ActorImage != null && oa.ActorImage.IsLoaded)
                {
                  na.ActorImage = oa.ActorImage;
                }
              }
            }
          }
        }
        this.TvdbActors = _series.TvdbActors;
      }

      if (_series.BannersLoaded)
      {
        //check if the old series has any images loaded already -> if yes, save them
        if (this.BannersLoaded)
        {
          foreach (TvdbBanner ob in this.Banners)
          {
            foreach (TvdbBanner nb in _series.Banners)
            {
              if (ob.BannerPath.Equals(nb.BannerPath))//I have to check for the banner path since the Update file doesn't include IDs
              {
                if (ob.BannerImage != null && ob.IsLoaded)
                {
                  nb.BannerImage = ob.BannerImage;
                }

                if (ob.GetType() == typeof(TvdbFanartBanner))
                {
                  TvdbFanartBanner newFaBanner = (TvdbFanartBanner)nb;
                  TvdbFanartBanner oldFaBanner = (TvdbFanartBanner)ob;

                  if (oldFaBanner.ThumbImage != null && oldFaBanner.IsThumbLoaded)
                  {
                    newFaBanner.ThumbImage = oldFaBanner.ThumbImage;
                  }

                  if (oldFaBanner.ThumbImage != null && oldFaBanner.IsVignetteLoaded)
                  {
                    newFaBanner.VignetteImage = oldFaBanner.VignetteImage;
                  }
                }
              }
            }
          }
        }
        this.Banners = _series.Banners;
      }
    }


  }
}
