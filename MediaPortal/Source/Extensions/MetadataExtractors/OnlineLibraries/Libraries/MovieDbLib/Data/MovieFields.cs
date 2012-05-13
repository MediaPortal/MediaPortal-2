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
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data.Banner;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data.Persons;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data
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
  ///       <fanart>fanart/Original/73739-1.jpg</fanart>
  ///       <lastupdated>1205694666</lastupdated>
  ///       <zap2it_id>SH672362</zap2it_id>
  /// </summary>
  [Serializable]
  public class MovieFields
  {
    #region private fields

    #endregion

    public MovieFields()
    {
      _banners = new List<MovieDbBanner>();
      BannersLoaded = false;
    }

    /// <summary>
    /// Returns a short description of the episode (e.g. 1x20 Episodename)
    /// </summary>
    /// <returns>short description of the episode</returns>
    public override string ToString()
    {
      return MovieName + " [" + Language.Abbreviation + "]";
    }

    /// <summary>
    /// Series Id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Series Name
    /// </summary>
    public string MovieName { get; set; }

    public int Popularity { get; set; }


    public MovieDbLanguage Language { get; set; }

    public List<MovieDbCountries> Countries { get; set; }

    public List<MovieDbStudios> Studios { get; set; }

    public List<MovieDbCategory> Categories { get; set; }

    public string Trailer { get; set; }

    public string Homepage { get; set; }

    public long Revenue { get; set; }

    public long Budget { get; set; }

    public int Runtime { get; set; }

    public DateTime Released { get; set; }

    public double Rating { get; set; }

    public string Overview { get; set; }


    public string Url { get; set; }

    public string ImdbId { get; set; }

    public string AlternativeName { get; set; }

    #region banners

    //all banners
    private List<MovieDbBanner> _banners;

    /// <summary>
    /// returns a list of all banners for this series
    /// </summary>
    public List<MovieDbBanner> Banners
    {
      get { return _banners; }
      set
      {
        _banners = value;
        BannersLoaded = true;
      }
    }

    public List<MovieDbPoster> Posters
    {
      get
      {
        return _banners.OfType<MovieDbPoster>().ToList();
      }
    }

    public List<MovieDbBackdrop> Backdrops
    {
      get
      {
        return _banners.OfType<MovieDbBackdrop>().ToList();
      }
    }

    /// <summary>
    /// Is the banner info loaded
    /// </summary>
    public bool BannersLoaded { get; set; }

    #endregion

    #region Cast

    public List<MovieDbCast> Cast { get; set; }

    #endregion

    /// <summary>
    /// Update all fields of the object with the given information
    /// </summary>
    /// <param name="fields">the fields for the update</param>
    internal void UpdateTvdbFields(MovieFields fields)
    {
      //Update series details
      Popularity = fields.Popularity;
      MovieName = fields.MovieName;
      AlternativeName = fields.AlternativeName;
      Id = fields.Id;
      ImdbId = fields.ImdbId;
      Url = fields.Url;
      Overview = fields.Overview;
      Rating = fields.Rating;
      Released = fields.Released;
      Runtime = fields.Runtime;
      Budget = fields.Budget;
      Revenue = fields.Revenue;
      Homepage = fields.Homepage;
      Trailer = fields.Trailer;
      Banners = fields.Banners;
      Cast = fields.Cast;
      Categories = fields.Categories;
      Countries = fields.Countries;
      Studios = fields.Studios;
    }
  }
}
