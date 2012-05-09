/*
 *   MovieDbLib: A library to retrieve information and media from http://TheMovieDb.org
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
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data.Persons;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data
{
  [Serializable]
  public class MovieDbMovie : MovieFields
  {
    #region private properties

    #endregion

    /// <summary>
    /// Basic constructor for the TvdbSeries class
    /// </summary>
    public MovieDbMovie()
    {
    }

    /// <summary>
    /// Create a series object with all the information contained in the TvdbSeriesFields object
    /// </summary>
    /// <param name="fields"></param>
    internal MovieDbMovie(MovieFields fields)
      : this()
    {
      AddLanguage(fields);
      SetLanguage(fields.Language);
      //UpdateTvdbFields(_fields, true);
    }

    /// <summary>
    /// Add a new language to the series
    /// </summary>
    /// <param name="fields"></param>
    internal void AddLanguage(MovieFields fields)
    {
      if (SeriesTranslations == null)
      {
        SeriesTranslations = new Dictionary<MovieDbLanguage, MovieFields>();
      }

      //delete translation if it already exists and overwrite it with a new one
      if (SeriesTranslations.ContainsKey(fields.Language))
      {
        SeriesTranslations.Remove(fields.Language);
      }
      /*foreach (KeyValuePair<TvdbLanguage, TvdbSeriesFields> kvp in _seriesTranslations)
      {
        if (kvp.Key == _fields.Language)
        {
          _seriesTranslations.Remove(kvp.Key);
        }
      }*/

      SeriesTranslations.Add(fields.Language, fields);
    }

    /// <summary>
    /// Set the language of the series to one of the languages that have
    /// already been loaded
    /// </summary>
    /// <param name="language">The new language for this series</param>
    /// <returns>true if success, false otherwise</returns>
    public bool SetLanguage(MovieDbLanguage language)
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
      foreach (KeyValuePair<MovieDbLanguage, MovieFields> kvp in SeriesTranslations)
      {
        if (!kvp.Key.Abbriviation.Equals(language)) 
          continue;

        UpdateTvdbFields(kvp.Value, true);
        return true;
      }
      return false;
    }


    /// <summary>
    /// Get all languages that have already been loaded for this series
    /// </summary>
    /// <returns>List of all translations that are loaded for this series</returns>
    public List<MovieDbLanguage> GetAvailableLanguages()
    {
      return SeriesTranslations != null ? SeriesTranslations.Keys.ToList() : null;
    }

    /// <summary>
    /// Get all available Translations
    /// </summary>
    internal Dictionary<MovieDbLanguage, MovieFields> SeriesTranslations { get; set; }

    #region Actors

    //Actor Information
    private List<MovieDbPerson> _persons;

    /// <summary>
    /// List of loaded tvdb actors
    /// </summary>
    public List<MovieDbPerson> Persons
    {
      get { return _persons; }
      set
      {
        PersonsLoaded = true;
        _persons = value;
      }
    }

    /// <summary>
    /// Is the actor info loaded
    /// </summary>
    public bool PersonsLoaded { get; set; }

    #endregion

    /// <summary>
    /// returns SeriesName (SeriesId)
    /// </summary>
    /// <returns>String representing this series</returns>
    public override string ToString()
    {
      return MovieName + "(" + Id + ")";
    }

    /// <summary>
    /// Uptdate the info of the current series with the updated one
    /// </summary>
    /// <param name="movie">TvdbSeries object</param>
    protected void UpdateMovieInfo(MovieDbMovie movie)
    {
     /* this.Actors = movie.Actors;
      this.AirsDayOfWeek = movie.AirsDayOfWeek;
      this.AirsTime = movie.AirsTime;
      this.BannerPath = movie.BannerPath;
      this.Banners = movie.Banners;
      this.ContentRating = movie.ContentRating;
      this.FanartPath = movie.FanartPath;
      this.FirstAired = movie.FirstAired;
      this.Genre = movie.Genre;
      this.Id = movie.Id;
      this.ImdbId = movie.ImdbId;
      this.Language = movie.Language;
      this.LastUpdated = movie.LastUpdated;
      this.Network = movie.Network;
      this.Overview = movie.Overview;
      this.Rating = movie.Rating;
      this.Runtime = movie.Runtime;
      this.MovieName = movie.MovieName;
      this.Status = movie.Status;
      this.TvDotComId = movie.TvDotComId;
      this.Zap2itId = movie.Zap2itId;

      if (movie.EpisodesLoaded)
      {//check if the old series has any images loaded already -> if yes, save them
        if (this.EpisodesLoaded)
        {
          foreach (TvdbEpisode oe in this.Episodes)
          {
            foreach (TvdbEpisode ne in movie.Episodes)
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

        this.Episodes = movie.Episodes;
      }

      if (movie.PersonsLoaded)
      {//check if the old series has any images loaded already -> if yes, save them
        if (this.PersonsLoaded)
        {
          foreach (TvdbActor oa in this.Persons)
          {
            foreach (TvdbActor na in movie.Persons)
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
        this.Persons = movie.Persons;
      }

      if (movie.BannersLoaded)
      {
        //check if the old series has any images loaded already -> if yes, save them
        if (this.BannersLoaded)
        {
          foreach (TvdbBanner ob in this.Banners)
          {
            foreach (TvdbBanner nb in movie.Banners)
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
        this.Banners = movie.Banners;
      }/*/
    }
  }
}
