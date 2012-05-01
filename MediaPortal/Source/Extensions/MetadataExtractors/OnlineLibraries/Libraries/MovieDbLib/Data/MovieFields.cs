/*
 *   MovieDbLib: A library to retrieve information and media from http://TheMovieDb.org
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
using MovieDbLib.Data.Comparer;
using MovieDbLib.Data.Banner;
using MovieDbLib.Data.Persons;

namespace MovieDbLib.Data
{
  /// <summary>
  /// This class represents all fields that are available on http://TheMovieDb.org and
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
  public class MovieFields
  {
    #region private fields
    private int m_id;
    private String m_name;
    private String m_alternativeName;
    private String m_imdbId;
    private String m_url;
    private String m_overview;
    private double m_rating;
    private DateTime m_released;
    private int m_runtime;
    private int m_budget;
    private int m_revenue;
    private String m_homepage;
    private String m_trailer;
    private int m_popularity;

    private List<MovieDbCast> m_cast;
    private List<MovieDbCategory> m_categories;
    private List<MovieDbStudios> m_studios;
    private List<MovieDbCountries> m_countries;
    private MovieDbLanguage m_language;
    #endregion

    public MovieFields()
    {
      m_banners = new List<MovieDbBanner>();
      m_bannersLoaded = false;
    }

    /// <summary>
    /// Returns a short description of the episode (e.g. 1x20 Episodename)
    /// </summary>
    /// <returns>short description of the episode</returns>
    public override string ToString()
    {
      return m_name + "[" + m_language.Abbriviation + "]";;
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
    public String MovieName
    {
      get { return m_name; }
      set { m_name = value; }
    }

    public int Popularity
    {
      get { return m_popularity; }
      set { m_popularity = value; }
    }


    public MovieDbLanguage Language
    {
      get { return m_language; }
      set { m_language = value; }
    }

    public List<MovieDbCountries> Countries
    {
      get { return m_countries; }
      set { m_countries = value; }
    }

    public List<MovieDbStudios> Studios
    {
      get { return m_studios; }
      set { m_studios = value; }
    }

    public List<MovieDbCategory> Categories
    {
      get { return m_categories; }
      set { m_categories = value; }
    }

    public String Trailer
    {
      get { return m_trailer; }
      set { m_trailer = value; }
    }

    public String Homepage
    {
      get { return m_homepage; }
      set { m_homepage = value; }
    }

    public int Revenue
    {
      get { return m_revenue; }
      set { m_revenue = value; }
    }

    public int Budget
    {
      get { return m_budget; }
      set { m_budget = value; }
    }

    public int Runtime
    {
      get { return m_runtime; }
      set { m_runtime = value; }
    }

    public DateTime Released
    {
      get { return m_released; }
      set { m_released = value; }
    }

    public double Rating
    {
      get { return m_rating; }
      set { m_rating = value; }
    }

    public String Overview
    {
      get { return m_overview; }
      set { m_overview = value; }
    }


    public String Url
    {
      get { return m_url; }
      set { m_url = value; }
    }

    public String ImdbId
    {
      get { return m_imdbId; }
      set { m_imdbId = value; }
    }

    public String AlternativeName
    {
      get { return m_alternativeName; }
      set { m_alternativeName = value; }
    }


    #region banners

    //all banners
    private List<MovieDbBanner> m_banners;
    private bool m_bannersLoaded;

    /// <summary>
    /// returns a list of all banners for this series
    /// </summary>
    public List<MovieDbBanner> Banners
    {
      get { return m_banners; }
      set
      {
        m_banners = value;
        m_bannersLoaded = true;
      }
    }

    public List<MovieDbPoster> Posters
    {
      get
      {
        List<MovieDbPoster> posters = new List<MovieDbPoster>();
        foreach (MovieDbBanner b in m_banners)
        {
          if (b.GetType() == typeof(MovieDbPoster))
          {
            posters.Add((MovieDbPoster)b);
          }
        }
        return posters;
      }
    }

    public List<MovieDbBackdrop> Backdrops
    {
      get
      {
        List<MovieDbBackdrop> posters = new List<MovieDbBackdrop>();
        foreach (MovieDbBanner b in m_banners)
        {
          if (b.GetType() == typeof(MovieDbBackdrop))
          {
            posters.Add((MovieDbBackdrop)b);
          }
        }
        return posters;
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

    #endregion

    #region Cast
    public List<MovieDbCast> Cast
    {
      get { return m_cast; }
      set { m_cast = value; }
    }
    #endregion

    /// <summary>
    /// Update all fields of the object with the given information
    /// </summary>
    /// <param name="_fields">the fields for the update</param>
    /// <param name="_replaceEpisodes">Should the episodes be replaced or kept</param>
    internal void UpdateTvdbFields(MovieFields _fields, bool _replaceEpisodes)
    {
      //Update series details
      this.Popularity = _fields.Popularity;
      this.MovieName = _fields.MovieName;
      this.AlternativeName = _fields.AlternativeName;
      this.Id = _fields.Id;
      this.ImdbId = _fields.ImdbId;
      this.Url = _fields.Url;
      this.Overview = _fields.Overview;
      this.Rating = _fields.Rating;
      this.Released = _fields.Released;
      this.Runtime = _fields.Runtime;
      this.Budget = _fields.Budget;
      this.Revenue = _fields.Revenue;
      this.Homepage = _fields.Homepage;
      this.Trailer = _fields.Trailer;
      this.Banners = _fields.Banners;
      this.Cast = _fields.Cast;
      this.Categories = _fields.Categories;
      this.Countries = _fields.Countries;
      this.Studios = _fields.Studios;

    }
  }
}
