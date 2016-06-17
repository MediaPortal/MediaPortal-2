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
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data.Banner;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data
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
  ///       <RatingCount>8</RatingCount>
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
    private Dictionary<TvdbLanguage, TvdbSeriesFields> _seriesTranslations = new Dictionary<TvdbLanguage, TvdbSeriesFields>();
    #endregion

    /// <summary>
    /// Basic constructor for the TvdbSeries class
    /// </summary>
    public TvdbSeries()
    {
      _banners = new List<TvdbBanner>();
      _bannersLoaded = false;
      _tvdbActorsLoaded = false;

    }

    /// <summary>
    /// Create a series object with all the information contained in the TvdbSeriesFields object
    /// </summary>
    /// <param name="fields"></param>
    internal TvdbSeries(TvdbSeriesFields fields)
      : this()
    {
      AddLanguage(fields);
      SetLanguage(fields.Language);
      //UpdateTvdbFields(_fields, true);
    }


    internal void SetEpisodes(List<TvdbEpisode> episodes)
    {
      foreach (KeyValuePair<TvdbLanguage, TvdbSeriesFields> kvp in _seriesTranslations)
      {
        if (kvp.Key.Abbriviation.Equals(Language.Abbriviation))
        {
          kvp.Value.EpisodesLoaded = true;
          kvp.Value.Episodes.Clear();
          kvp.Value.Episodes.AddRange(episodes);
        }
      }

      EpisodesLoaded = true;
      Episodes.Clear();
      Episodes.AddRange(episodes);
    }

    /// <summary>
    /// Add a new language to the series
    /// </summary>
    /// <param name="fields"></param>
    internal void AddLanguage(TvdbSeriesFields fields)
    {
      if (_seriesTranslations == null)
        _seriesTranslations = new Dictionary<TvdbLanguage, TvdbSeriesFields>();

      //delete translation if it already exists and overwrite it with a new one
      if (_seriesTranslations.ContainsKey(fields.Language))
        _seriesTranslations.Remove(fields.Language);

      /*foreach (KeyValuePair<TvdbLanguage, TvdbSeriesFields> kvp in _seriesTranslations)
      {
        if (kvp.Key == _fields.Language)
        {
          _seriesTranslations.Remove(kvp.Key);
        }
      }*/

      _seriesTranslations.Add(fields.Language, fields);
    }

    /// <summary>
    /// Set the language of the series to one of the languages that have
    /// already been loaded
    /// </summary>
    /// <param name="language">The new language for this series</param>
    /// <returns>true if success, false otherwise</returns>
    public bool SetLanguage(TvdbLanguage language)
    {
      return SetLanguage(language.Abbriviation);
    }

    /// <summary>
    /// Set the language of the series to one of the languages that have
    /// already been loaded
    /// </summary>
    /// <param name="language">The new language abbriviation for this series</param>
    /// <returns>true if success, false otherwise</returns>
    public bool SetLanguage(String language)
    {
      foreach (KeyValuePair<TvdbLanguage, TvdbSeriesFields> kvp in _seriesTranslations.Where(kvp => kvp.Key.Abbriviation.Equals(language)))
      {
        UpdateTvdbFields(kvp.Value, true);
        return true;
      }
      return false;
    }


    /// <summary>
    /// Get all languages that have already been loaded for this series
    /// </summary>
    /// <returns>List of all translations that are loaded for this series</returns>
    public List<TvdbLanguage> GetAvailableLanguages()
    {
      return _seriesTranslations != null ? _seriesTranslations.Keys.ToList() : null;
    }

    /// <summary>
    /// Get all available Translations
    /// </summary>
    internal Dictionary<TvdbLanguage, TvdbSeriesFields> SeriesTranslations
    {
      get { return _seriesTranslations; }
      set { _seriesTranslations = value; }
    }

    #region user properties
    private bool _isFavorite;

    /// <summary>
    /// Is the series a favorite
    /// </summary>
    public bool IsFavorite
    {
      get { return _isFavorite; }
      set { _isFavorite = value; }
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
    private List<TvdbBanner> _banners;
    private bool _bannersLoaded;

    /// <summary>
    /// returns a list of all banners for this series
    /// </summary>
    public List<TvdbBanner> Banners
    {
      get { return _banners; }
      set
      {
        _banners = value;
        _bannersLoaded = true;
      }
    }

    /// <summary>
    /// Is the banner info loaded
    /// </summary>
    public bool BannersLoaded
    {
      get { return _bannersLoaded; }
      set { _bannersLoaded = value; }
    }

    /// <summary>
    /// returns a list of all series banners for this series
    /// </summary>
    public List<TvdbSeriesBanner> SeriesBanners
    {
      get
      {
        return Banners.OfType<TvdbSeriesBanner>().ToList();
      }
    }

    /// <summary>
    /// Returns a list of all Season banners for this series
    /// </summary>
    public List<TvdbSeasonBanner> SeasonBanners
    {
      get
      {
        return Banners.OfType<TvdbSeasonBanner>().ToList();
      }
    }

    /// <summary>
    /// Returns a list of all Season banners for this series
    /// </summary>
    public List<TvdbPosterBanner> PosterBanners
    {
      get
      {
        return Banners.OfType<TvdbPosterBanner>().ToList();
      }
    }

    /// <summary>
    /// Returns a list of all fanart banners for this series
    /// </summary>
    public List<TvdbFanartBanner> FanartBanners
    {
      get
      {
        return Banners.OfType<TvdbFanartBanner>().ToList();
      }
    }

    #endregion

    #region episodes

    /// <summary>
    /// Return a list of episodes for the given Season
    /// </summary>
    /// <param name="season">Season for which episodes should be returned</param>
    /// <returns>List of episodes for the given Season</returns>
    public List<TvdbEpisode> GetEpisodes(int season)
    {
      List<TvdbEpisode> retList = new List<TvdbEpisode>();
      if (Episodes != null && Episodes.Count > 0 && EpisodesLoaded)
        retList.AddRange(Episodes.Where(e => e.SeasonNumber == season));
      return retList;
    }

    /// <summary>
    /// How many Season does the series have
    /// </summary>
    public int NumSeasons
    {
      get
      {
        int maxSeason = 0;
        if (Episodes != null && EpisodesLoaded && Episodes.Count > 0)
        {
          foreach (TvdbEpisode e in Episodes)
          {
            if (e.SeasonNumber > maxSeason)
              maxSeason++;
          }
        }
        return maxSeason;
      }
    }


    #endregion

    #region Actors
    //Actor Information
    private List<TvdbActor> _tvdbActors;
    private bool _tvdbActorsLoaded;

    /// <summary>
    /// List of loaded tvdb actors
    /// </summary>
    public List<TvdbActor> TvdbActors
    {
      get { return _tvdbActors; }
      set
      {
        _tvdbActorsLoaded = true;
        _tvdbActors = value;
      }
    }

    /// <summary>
    /// Is the actor info loaded
    /// </summary>
    public bool TvdbActorsLoaded
    {
      get { return _tvdbActorsLoaded; }
      set { _tvdbActorsLoaded = value; }
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
    /// Update the info of the current series with the updated one
    /// </summary>
    /// <param name="series">TvdbSeries object</param>
    protected void UpdateSeriesInfo(TvdbSeries series)
    {
      Actors = series.Actors;
      AirsDayOfWeek = series.AirsDayOfWeek;
      AirsTime = series.AirsTime;
      BannerPath = series.BannerPath;
      Banners = series.Banners;
      ContentRating = series.ContentRating;
      FanartPath = series.FanartPath;
      FirstAired = series.FirstAired;
      Genre = series.Genre;
      Id = series.Id;
      ImdbId = series.ImdbId;
      Language = series.Language;
      LastUpdated = series.LastUpdated;
      Network = series.Network;
      Overview = series.Overview;
      Rating = series.Rating;
      Runtime = series.Runtime;
      SeriesName = series.SeriesName;
      Status = series.Status;
      TvDotComId = series.TvDotComId;
      Zap2itId = series.Zap2itId;

      if (series.EpisodesLoaded)
      {//check if the old series has any images loaded already -> if yes, save them
        if (EpisodesLoaded)
        {
          foreach (TvdbEpisode oe in Episodes)
          {
            foreach (TvdbEpisode ne in series.Episodes)
            {
              if (oe.SeasonNumber == ne.SeasonNumber &&
                  oe.EpisodeNumber == ne.EpisodeNumber)
              {
                if (oe.Banner != null && oe.Banner.IsLoaded)
                  ne.Banner = oe.Banner;
              }
            }
          }
        }

        Episodes.Clear();
        Episodes.AddRange(series.Episodes);
      }

      if (series.TvdbActorsLoaded)
      {//check if the old series has any images loaded already -> if yes, save them
        if (TvdbActorsLoaded)
        {
          foreach (TvdbActor oa in TvdbActors)
          {
            foreach (TvdbActor na in series.TvdbActors)
            {
              if (oa.Id == na.Id)
                if (oa.ActorImage != null && oa.ActorImage.IsLoaded)
                  na.ActorImage = oa.ActorImage;
            }
          }
        }
        TvdbActors = series.TvdbActors;
      }

      if (series.BannersLoaded)
      {
        //check if the old series has any images loaded already -> if yes, save them
        if (BannersLoaded)
        {
          foreach (TvdbBanner ob in Banners)
          {
            foreach (TvdbBanner nb in series.Banners)
            {
              if (ob.BannerPath.Equals(nb.BannerPath))//I have to check for the banner path since the Update file doesn't include IDs
              {
                if (ob.BannerImage != null && ob.IsLoaded)
                  nb.BannerImage = ob.BannerImage;

                if (ob.GetType() == typeof(TvdbFanartBanner))
                {
                  TvdbFanartBanner newFaBanner = (TvdbFanartBanner)nb;
                  TvdbFanartBanner oldFaBanner = (TvdbFanartBanner)ob;

                  if (oldFaBanner.ThumbImage != null && oldFaBanner.IsThumbLoaded)
                    newFaBanner.ThumbImage = oldFaBanner.ThumbImage;

                  if (oldFaBanner.ThumbImage != null && oldFaBanner.IsVignetteLoaded)
                    newFaBanner.VignetteImage = oldFaBanner.VignetteImage;
                }
              }
            }
          }
        }
        Banners = series.Banners;
      }
    }
  }
}
