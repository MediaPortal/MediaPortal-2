/*
 *   TvdbLib: A library to retrieve information and media from http://thetvdb.com
 * 
 *   Copyright (C) 2008  Benjamin Gmeiner
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
using TvdbLib.Data.Comparer;

namespace TvdbLib.Data
{
  /// <summary>
  /// This class represents all fields that are available on http://thetvdb.com and
  /// a list of episodefields. This is used for localised series information.
  /// 
  /// These are as follows:
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
  /// </summary>
  [Serializable]
  public class TvdbSeriesFields
  {
    #region private fields
    private int m_id;
    private String m_seriesName;
    private List<String> m_actors;
    private DayOfWeek? m_airsDayOfWeek;
    private String m_airsTime;
    private String m_contentRating;
    private DateTime m_firstAired;
    private List<String> m_genre;
    private String m_imdbId;
    private TvdbLanguage m_language;
    private String m_network;
    private String m_overview;
    private double m_rating;
    private double m_runtime;
    private int m_tvDotComId;
    private String m_status;
    private String m_bannerPath;
    private String m_fanartPath;
    private String m_posterPath;
    private DateTime m_lastUpdated;
    private String m_zap2itId;
    private bool m_episodesLoaded;
    private List<TvdbEpisode> m_episodes = null;
    #endregion

    /// <summary>
    /// TvdbSeriesFields constructor
    /// </summary>
    public TvdbSeriesFields()
    {
      m_episodes = new List<TvdbEpisode>();
      m_episodesLoaded = false;
    }

    /// <summary>
    /// Returns a short description of the episode (e.g. 1x20 Episodename)
    /// </summary>
    /// <returns>short description of the episode</returns>
    public override string ToString()
    {
      return m_seriesName + "[" + m_language.Abbriviation + "]";;
    }

    /// <summary>
    /// List of episodes for this translation
    /// </summary>
    public List<TvdbEpisode> Episodes
    {
      get { return m_episodes; }
      set { m_episodes = value; }
    }

    /// <summary>
    /// <para>Gets the episodes for the given season in the given order (aired or dvd). Absolute is also possible but makes no sense since
    /// there are no seasons with absoulte ordering. Use GetEpisodesAbsoluteOrder() instead.</para>
    /// 
    /// <para>For more information on episode ordering <see href="http://thetvdb.com/wiki/index.php/Category:Episodes">thetvdb wiki</see></para>
    /// </summary>
    /// <returns>List of episodes</returns>
    public List<TvdbEpisode> GetEpisodes(int _season, TvdbEpisode.EpisodeOrdering _order)
    {
      List<TvdbEpisode> episodes = new List<TvdbEpisode>();
      m_episodes.ForEach(delegate(TvdbEpisode e) { if (e.SeasonNumber == _season) episodes.Add(e); });

      switch (_order)
      {
        case TvdbEpisode.EpisodeOrdering.DefaultOrder:
          episodes.Sort(new EpisodeComparerAired());
          break;
        case TvdbEpisode.EpisodeOrdering.DvdOrder:
          episodes.Sort(new EpisodeComparerDvd());
          break;
        case TvdbEpisode.EpisodeOrdering.AbsoluteOrder:
          episodes.Sort(new EpisodeComparerAbsolute());
          break;
      }
      return episodes;
    }

    /// <summary>
    /// Returns all episodes in the absolute order
    /// </summary>
    /// <returns>List of episodes</returns>
    public List<TvdbEpisode> GetEpisodesAbsoluteOrder()
    {
      List<TvdbEpisode> episodes = new List<TvdbEpisode>();
      m_episodes.ForEach(delegate(TvdbEpisode e) { episodes.Add(e); });
      episodes.Sort(new EpisodeComparerAbsolute());
      return episodes;
    }

    /// <summary>
    /// Is the episode info loaded
    /// </summary>
    public bool EpisodesLoaded
    {
      get { return m_episodesLoaded; }
      set { m_episodesLoaded = value; }
    }

    /// <summary>
    /// Series Id
    /// </summary>
    public int Id
    {
      get { return m_id; }
      set { m_id = value; }
    }

    /// <summary>
    /// Series Name
    /// </summary>
    public String SeriesName
    {
      get { return m_seriesName; }
      set { m_seriesName = value; }
    }

    /// <summary>
    /// Series network
    /// </summary>
    public String Network
    {
      get { return m_network; }
      set { m_network = value; }
    }

    /// <summary>
    /// The language of the series
    /// </summary>
    public TvdbLanguage Language
    {
      get { return m_language; }
      set { m_language = value; }
    }

    /// <summary>
    /// Content-Rating of the series
    /// </summary>
    public string ContentRating
    {
      get { return m_contentRating; }
      set { m_contentRating = value; }
    }

    /// <summary>
    /// Zap2it Id of the series
    /// </summary>
    public String Zap2itId
    {
      get { return m_zap2itId; }
      set { m_zap2itId = value; }
    }

    /// <summary>
    /// When was the series updated the last time
    /// </summary>
    public DateTime LastUpdated
    {
      get { return m_lastUpdated; }
      set { m_lastUpdated = value; }
    }

    /// <summary>
    /// Path to the primary fanart banner
    /// </summary>
    public String FanartPath
    {
      get { return m_fanartPath; }
      set { m_fanartPath = value; }
    }

    /// <summary>
    /// Path to primary banner
    /// </summary>
    public String BannerPath
    {
      get { return m_bannerPath; }
      set { m_bannerPath = value; }
    }

    /// <summary>
    /// Path to the primary poster
    /// </summary>
    public String PosterPath
    {
      get { return m_posterPath; }
      set { m_posterPath = value; }
    }

    /// <summary>
    /// Status of the show
    /// </summary>
    public String Status
    {
      get { return m_status; }
      set { m_status = value; }
    }

    /// <summary>
    /// Tv.com id of the series
    /// </summary>
    public int TvDotComId
    {
      get { return m_tvDotComId; }
      set { m_tvDotComId = value; }
    }

    /// <summary>
    /// Runtime of the show
    /// </summary>
    public double Runtime
    {
      get { return m_runtime; }
      set { m_runtime = value; }
    }

    /// <summary>
    /// Rating of the series
    /// </summary>
    public double Rating
    {
      get { return m_rating; }
      set { m_rating = value; }
    }

    /// <summary>
    /// Overview of the series
    /// </summary>
    public String Overview
    {
      get { return m_overview; }
      set { m_overview = value; }
    }

    /// <summary>
    /// Imdb Id of the series
    /// </summary>
    public String ImdbId
    {
      get { return m_imdbId; }
      set { m_imdbId = value; }
    }

    /// <summary>
    /// List of the series' genres
    /// </summary>
    public List<String> Genre
    {
      get { return m_genre; }
      set { m_genre = value; }
    }

    /// <summary>
    /// The Date the series was first aired
    /// </summary>
    public DateTime FirstAired
    {
      get { return m_firstAired; }
      set { m_firstAired = value; }
    }

    /// <summary>
    /// At which time does the series air
    /// </summary>
    public String AirsTime
    {
      get { return m_airsTime; }
      set { m_airsTime = value; }
    }

    /// <summary>
    /// At which day of the week does the series air
    /// </summary>
    public DayOfWeek? AirsDayOfWeek
    {
      get { return m_airsDayOfWeek; }
      set { m_airsDayOfWeek = value; }
    }

    /// <summary>
    /// List of actors that appear in this series
    /// </summary>
    public List<String> Actors
    {
      get { return m_actors; }
      set { m_actors = value; }
    }

    /// <summary>
    /// Update all fields of the object with the given information
    /// </summary>
    /// <param name="_fields">the fields for the update</param>
    /// <param name="_replaceEpisodes">Should the episodes be replaced or kept</param>
    internal void UpdateTvdbFields(TvdbSeriesFields _fields, bool _replaceEpisodes)
    {
      //Update series details
      this.Id = _fields.Id;
      this.Actors = _fields.Actors;
      this.AirsDayOfWeek = _fields.AirsDayOfWeek;
      this.AirsTime = _fields.AirsTime;
      this.ContentRating = _fields.ContentRating;
      this.FirstAired = _fields.FirstAired;
      this.Genre = _fields.Genre;
      this.ImdbId = _fields.ImdbId;
      this.Language = _fields.Language;
      this.Network = _fields.Network;
      this.Overview = _fields.Overview;
      this.Rating = _fields.Rating;
      this.Runtime = _fields.Runtime;
      this.TvDotComId = _fields.TvDotComId;
      this.SeriesName = _fields.SeriesName;
      this.Status = _fields.Status;
      this.BannerPath = _fields.BannerPath;
      this.FanartPath = _fields.FanartPath;
      this.PosterPath = _fields.PosterPath;
      this.LastUpdated = _fields.LastUpdated;
      this.Zap2itId = _fields.Zap2itId;

      if (_replaceEpisodes)
      {
        if (this.Episodes != null && _fields.EpisodesLoaded)
        {
          //check for each episode if episode images have been loaded... 
          //if yes -> copy image
          foreach (TvdbEpisode f in _fields.Episodes)
          {
            foreach (TvdbEpisode e in this.Episodes)
            {
              if (e.Id == f.Id && e.Banner != null && e.Banner.IsLoaded)
              {
                f.Banner = e.Banner;
                break;
              }
            }
          }
        }
        this.EpisodesLoaded = _fields.EpisodesLoaded;
        this.Episodes.Clear();
        this.Episodes.AddRange(_fields.Episodes);
      }
    }
  }
}
