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
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data.Comparer;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data
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
    private int _id;
    private String _seriesName;
    private List<String> _actors;
    private DayOfWeek? _airsDayOfWeek;
    private String _airsTime;
    private String _contentRating;
    private DateTime _firstAired;
    private List<String> _genre;
    private String _imdbId;
    private TvdbLanguage _language;
    private String _network;
    private String _overview;
    private double _rating;
    private double _runtime;
    private int _tvDotComId;
    private String _status;
    private String _bannerPath;
    private String _fanartPath;
    private String _posterPath;
    private DateTime _lastUpdated;
    private String _zap2itId;
    private bool _episodesLoaded;
    private List<TvdbEpisode> _episodes;
    #endregion

    /// <summary>
    /// TvdbSeriesFields constructor
    /// </summary>
    public TvdbSeriesFields()
    {
      _episodes = new List<TvdbEpisode>();
      _episodesLoaded = false;
    }

    /// <summary>
    /// Returns a short description of the episode (e.g. 1x20 Episodename)
    /// </summary>
    /// <returns>short description of the episode</returns>
    public override string ToString()
    {
      return _seriesName + "[" + _language.Abbriviation + "]";
    }

    /// <summary>
    /// List of episodes for this translation
    /// </summary>
    public List<TvdbEpisode> Episodes
    {
      get { return _episodes; }
      set { _episodes = value; }
    }

    /// <summary>
    /// <para>Gets the episodes for the given Season in the given order (aired or dvd). Absolute is also possible but makes no sense since
    /// there are no seasons with absoulte ordering. Use GetEpisodesAbsoluteOrder() instead.</para>
    /// 
    /// <para>For more information on episode ordering <see href="http://thetvdb.com/wiki/index.php/Category:Episodes">thetvdb wiki</see></para>
    /// </summary>
    /// <returns>List of episodes</returns>
    public List<TvdbEpisode> GetEpisodes(int season, TvdbEpisode.EpisodeOrdering order)
    {
      List<TvdbEpisode> episodes = new List<TvdbEpisode>();
      _episodes.ForEach(delegate(TvdbEpisode e) { if (e.SeasonNumber == season) episodes.Add(e); });

      switch (order)
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
      _episodes.ForEach(episodes.Add);
      episodes.Sort(new EpisodeComparerAbsolute());
      return episodes;
    }

    /// <summary>
    /// Is the episode info loaded
    /// </summary>
    public bool EpisodesLoaded
    {
      get { return _episodesLoaded; }
      set { _episodesLoaded = value; }
    }

    /// <summary>
    /// Series Id
    /// </summary>
    public int Id
    {
      get { return _id; }
      set { _id = value; }
    }

    /// <summary>
    /// Series Name
    /// </summary>
    public String SeriesName
    {
      get { return _seriesName; }
      set { _seriesName = value; }
    }

    /// <summary>
    /// Series network
    /// </summary>
    public String Network
    {
      get { return _network; }
      set { _network = value; }
    }

    /// <summary>
    /// The language of the series
    /// </summary>
    public TvdbLanguage Language
    {
      get { return _language; }
      set { _language = value; }
    }

    /// <summary>
    /// Content-Rating of the series
    /// </summary>
    public string ContentRating
    {
      get { return _contentRating; }
      set { _contentRating = value; }
    }

    /// <summary>
    /// Zap2it Id of the series
    /// </summary>
    public String Zap2itId
    {
      get { return _zap2itId; }
      set { _zap2itId = value; }
    }

    /// <summary>
    /// When was the series updated the last time
    /// </summary>
    public DateTime LastUpdated
    {
      get { return _lastUpdated; }
      set { _lastUpdated = value; }
    }

    /// <summary>
    /// Path to the primary fanart banner
    /// </summary>
    public String FanartPath
    {
      get { return _fanartPath; }
      set { _fanartPath = value; }
    }

    /// <summary>
    /// Path to primary banner
    /// </summary>
    public String BannerPath
    {
      get { return _bannerPath; }
      set { _bannerPath = value; }
    }

    /// <summary>
    /// Path to the primary Poster
    /// </summary>
    public String PosterPath
    {
      get { return _posterPath; }
      set { _posterPath = value; }
    }

    /// <summary>
    /// Status of the show
    /// </summary>
    public String Status
    {
      get { return _status; }
      set { _status = value; }
    }

    /// <summary>
    /// Tv.com id of the series
    /// </summary>
    public int TvDotComId
    {
      get { return _tvDotComId; }
      set { _tvDotComId = value; }
    }

    /// <summary>
    /// Runtime of the show
    /// </summary>
    public double Runtime
    {
      get { return _runtime; }
      set { _runtime = value; }
    }

    /// <summary>
    /// Rating of the series
    /// </summary>
    public double Rating
    {
      get { return _rating; }
      set { _rating = value; }
    }

    /// <summary>
    /// Overview of the series
    /// </summary>
    public String Overview
    {
      get { return _overview; }
      set { _overview = value; }
    }

    /// <summary>
    /// Imdb Id of the series
    /// </summary>
    public String ImdbId
    {
      get { return _imdbId; }
      set { _imdbId = value; }
    }

    /// <summary>
    /// List of the series' genres
    /// </summary>
    public List<String> Genre
    {
      get { return _genre; }
      set { _genre = value; }
    }

    /// <summary>
    /// The Date the series was first aired
    /// </summary>
    public DateTime FirstAired
    {
      get { return _firstAired; }
      set { _firstAired = value; }
    }

    /// <summary>
    /// At which time does the series air
    /// </summary>
    public String AirsTime
    {
      get { return _airsTime; }
      set { _airsTime = value; }
    }

    /// <summary>
    /// At which day of the week does the series air
    /// </summary>
    public DayOfWeek? AirsDayOfWeek
    {
      get { return _airsDayOfWeek; }
      set { _airsDayOfWeek = value; }
    }

    /// <summary>
    /// List of actors that appear in this series
    /// </summary>
    public List<String> Actors
    {
      get { return _actors; }
      set { _actors = value; }
    }

    /// <summary>
    /// Update all fields of the object with the given information
    /// </summary>
    /// <param name="fields">the fields for the update</param>
    /// <param name="replaceEpisodes">Should the episodes be replaced or kept</param>
    internal void UpdateTvdbFields(TvdbSeriesFields fields, bool replaceEpisodes)
    {
      //Update series details
      Id = fields.Id;
      Actors = fields.Actors;
      AirsDayOfWeek = fields.AirsDayOfWeek;
      AirsTime = fields.AirsTime;
      ContentRating = fields.ContentRating;
      FirstAired = fields.FirstAired;
      Genre = fields.Genre;
      ImdbId = fields.ImdbId;
      Language = fields.Language;
      Network = fields.Network;
      Overview = fields.Overview;
      Rating = fields.Rating;
      Runtime = fields.Runtime;
      TvDotComId = fields.TvDotComId;
      SeriesName = fields.SeriesName;
      Status = fields.Status;
      BannerPath = fields.BannerPath;
      FanartPath = fields.FanartPath;
      PosterPath = fields.PosterPath;
      LastUpdated = fields.LastUpdated;
      Zap2itId = fields.Zap2itId;

      if (replaceEpisodes)
      {
        if (Episodes != null && fields.EpisodesLoaded)
        {
          //check for each episode if episode images have been loaded... 
          //if yes -> copy image
          foreach (TvdbEpisode f in fields.Episodes)
          {
            foreach (TvdbEpisode e in Episodes)
            {
              if (e.Id == f.Id && e.Banner != null && e.Banner.IsLoaded)
              {
                f.Banner = e.Banner;
                break;
              }
            }
          }
        }
        EpisodesLoaded = fields.EpisodesLoaded;
        if (Episodes == null)
          Episodes = new List<TvdbEpisode>();
        else
          Episodes.Clear();
        Episodes.AddRange(fields.Episodes);
      }
    }
  }
}
