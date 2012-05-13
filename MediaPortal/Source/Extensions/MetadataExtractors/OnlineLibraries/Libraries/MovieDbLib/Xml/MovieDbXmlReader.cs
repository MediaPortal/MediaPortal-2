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
using System.Xml.Linq;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data.Banner;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data.Persons;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Xml
{
  /// <summary>
  /// Class for parsing the xml info from thetvdb
  /// </summary>
  internal class MovieDbXmlReader
  {
    /// <summary>
    /// Base constructor for a TvdbXmlReader class
    /// </summary>
    internal MovieDbXmlReader()
    {

    }


    /// <summary>
    /// Extract a list of series in the format:
    /// <![CDATA[
    /// <?xml version="1.0" encoding="UTF-8" ?>
    /// <Data>
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
    ///       <fanart>fanart/Original/73739-1.jpg</fanart>
    ///       <lastupdated>1205694666</lastupdated>
    ///       <zap2it_id>SH672362</zap2it_id>
    ///    </Series>
    /// </Data>
    /// ]]>
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    internal List<MovieDbMovie> ExtractMovies(String data)
    {
      List<MovieFields> moviedbInfo = ExtractMovieFields(data);
      List<MovieDbMovie> retList = new List<MovieDbMovie>();
      foreach (MovieFields m in moviedbInfo)
      {
        MovieDbMovie series = new MovieDbMovie(m);

        /*if (!series.BannerPath.Equals(""))
        {
          series.Banners.Add(new TvdbSeriesBanner(series.Id, series.BannerPath, series.Language, TvdbSeriesBanner.Type.graphical));
        }

        if (!series.FanartPath.Equals(""))
        {
          series.Banners.Add(new TvdbFanartBanner(series.Id, series.FanartPath, series.Language));
        }

        if (!series.PosterPath.Equals(""))
        {
          series.Banners.Add(new TvdbPosterBanner(series.Id, series.PosterPath, series.Language));
        }*/
        retList.Add(series);
      }
      return retList;
    }

    /// <summary>
    /// Extract all the series fields that are available on thetvdb
    /// <![CDATA[
    /// <?xml version="1.0" encoding="UTF-8" ?>
    /// <Data>
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
    ///       <fanart>fanart/Original/73739-1.jpg</fanart>
    ///       <lastupdated>1205694666</lastupdated>
    ///       <zap2it_id>SH672362</zap2it_id>
    ///    </Series>
    ///  
    /// </Data>
    /// ]]>
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    internal List<MovieFields> ExtractMovieFields(String data)
    {
      //Stopwatch watch = new Stopwatch();
      //watch.Start();
      List<MovieFields> retList = new List<MovieFields>();
      XDocument xml = XDocument.Parse(data);

      var allMovies = from movie in xml.Descendants("movie")
                      select new
                      {
                        popularity = movie.Elements("popularity").Count() == 1 ?
                                     movie.Element("popularity").Value : null,
                        name = movie.Elements("name").Count() == 1 ?
                               movie.Element("name").Value : null,
                        alternative_name = movie.Elements("alternative_name").Count() == 1 ?
                                           movie.Element("alternative_name").Value : null,
                        type = movie.Elements("type").Count() == 1 ?
                               movie.Element("type").Value : null,
                        id = movie.Elements("id").Count() == 1 ?
                             movie.Element("id").Value : null,
                        imdb_id = movie.Elements("imdb_id").Count() == 1 ?
                                  movie.Element("imdb_id").Value : null,
                        url = movie.Elements("url").Count() == 1 ?
                              movie.Element("url").Value : null,
                        overview = movie.Elements("overview").Count() == 1 ?
                                   movie.Element("overview").Value : null,
                        rating = movie.Elements("rating").Count() == 1 ?
                                 movie.Element("rating").Value : null,
                        released = movie.Elements("released").Count() == 1 ?
                                   movie.Element("released").Value : null,
                        runtime = movie.Elements("runtime").Count() == 1 ?
                                  movie.Element("runtime").Value : null,
                        budget = movie.Elements("budget").Count() == 1 ?
                                 movie.Element("budget").Value : null,
                        revenue = movie.Elements("revenue").Count() == 1 ?
                                  movie.Element("revenue").Value : null,
                        homepage = movie.Elements("homepage").Count() == 1 ?
                                   movie.Element("homepage").Value : null,
                        trailer = movie.Elements("trailer").Count() == 1 ?
                                  movie.Element("trailer").Value : null,
                        categories = movie.Elements("categories").Count() == 1 ?
                                    (from image in movie.Elements("categories").Descendants("category")
                                     select new
                                      {
                                        type = image.Attributes("type").Count() == 1 ?
                                               image.Attribute("type").Value : null,
                                        name = image.Attributes("name").Count() == 1 ?
                                               image.Attribute("name").Value : null,
                                        url = image.Attributes("url").Count() == 1 ?
                                              image.Attribute("url").Value : null,
                                        id = image.Attributes("id").Count() == 1 ?
                                             image.Attribute("id").Value : null
                                      }
                                    ).ToList() : null,
                        studios = movie.Elements("studios").Count() == 1 ?
                                    (from image in movie.Elements("studios").Descendants("studio")
                                     select new
                                     {
                                       name = image.Attributes("name").Count() == 1 ?
                                              image.Attribute("name").Value : null,
                                       url = image.Attributes("url").Count() == 1 ?
                                             image.Attribute("url").Value : null
                                     }
                                    ).ToList() : null,
                        countries = movie.Elements("countries").Count() == 1 ?
                                  (from image in movie.Elements("countries").Descendants("country")
                                   select new
                                   {
                                     name = image.Attributes("name").Count() == 1 ?
                                            image.Attribute("name").Value : null,
                                     url = image.Attributes("url").Count() == 1 ?
                                           image.Attribute("url").Value : null,
                                     code = image.Attributes("code").Count() == 1 ?
                                           image.Attribute("code").Value : null
                                   }
                                  ).ToList() : null,
                        images = movie.Elements("images").Count() == 1 ?
                                  (from image in movie.Elements("images").Descendants("image")
                                   select new
                                   {
                                     type = image.Attributes("type").Count() == 1 ?
                                            image.Attribute("type").Value : null,
                                     size = image.Attributes("size").Count() == 1 ?
                                            image.Attribute("size").Value : null,
                                     url = image.Attributes("url").Count() == 1 ?
                                           image.Attribute("url").Value : null,
                                     id = image.Attributes("id").Count() == 1 ?
                                          image.Attribute("id").Value : null
                                   }
                                  ).ToList() : null,
                        cast = movie.Elements("cast").Count() == 1 ?
                                 (from image in movie.Elements("cast").Descendants("person")
                                  select new
                                  {
                                    character = image.Attributes("character").Count() == 1 ?
                                                image.Attribute("character").Value : null,
                                    job = image.Attributes("job").Count() == 1 ?
                                          image.Attribute("job").Value : null,
                                    url = image.Attributes("url").Count() == 1 ?
                                          image.Attribute("url").Value : null,
                                    id = image.Attributes("id").Count() == 1 ?
                                         image.Attribute("id").Value : null,
                                    name = image.Attributes("name").Count() == 1 ?
                                         image.Attribute("name").Value : null
                                  }
                                 ).ToList() : null,
                      };


      foreach (var m in allMovies)
      {
        MovieFields movie = new MovieFields
          {
            Language = MovieDbLanguage.DefaultLanguage,
            Popularity = Util.Int32Parse(m.popularity),
            Id = Util.Int32Parse(m.id),
            MovieName = m.name,
            AlternativeName = m.alternative_name,
            ImdbId = m.imdb_id,
            Url = m.url,
            Overview = m.overview,
            Rating = Util.DoubleParse(m.rating),
            Released = Util.ParseDateTime(m.released),
            Runtime = Util.Int32Parse(m.runtime),
            Budget = Util.LongParse(m.budget),
            Revenue = Util.LongParse(m.revenue),
            Homepage = m.homepage,
            Trailer = m.trailer
          };


        if (m.categories != null)
        {
          movie.Categories = new List<MovieDbCategory>();
          foreach (var c in m.categories)
          {
            MovieDbCategory cat = new MovieDbCategory();
            String url = c.url;
            int id = Util.Int32Parse(url.Substring(url.LastIndexOf("/") + 1));
            if (id != Util.NO_VALUE)
            {
              cat.Id = id;
              switch (c.type.ToLower())
              {
                case "genre":
                  cat.Type = MovieDbCategory.CategoryTypes.Genre;
                  break;
                default:
                  cat.Type = MovieDbCategory.CategoryTypes.Unknown;
                  throw new Exception("unknown category tpye");
              }
              cat.Name = c.name;
              cat.Url = c.url;
              movie.Categories.Add(cat);
            }
            else
            {
              Log.Warn("Error adding category (id=" + c.id + ", name=" + c.name + ") to cast");
            }
          }
        }

        if (m.studios != null)
        {
          movie.Studios = new List<MovieDbStudios>();
          foreach (var c in m.studios)
          {
            MovieDbStudios cat = new MovieDbStudios();
            String url = c.url;
            int id = Util.Int32Parse(url.Substring(url.LastIndexOf("/") + 1));
            if (id != Util.NO_VALUE)
            {
              cat.Id = id;
              cat.Name = c.name;
              cat.Url = c.url;
              movie.Studios.Add(cat);
            }
            else
            {
              Log.Warn("Error adding category (url=" + c.url + "name=" + c.name + ") to cast");
            }
          }
        }

        if (m.countries != null)
        {
          movie.Countries = new List<MovieDbCountries>();
          foreach (var c in m.countries)
          {
            MovieDbCountries country = new MovieDbCountries();
            String url = c.url;
            int id = Util.Int32Parse(url.Substring(url.LastIndexOf("/") + 1));
            if (id != Util.NO_VALUE)
            {
              country.Id = id;
              country.Name = c.name;
              country.Url = c.url;
              country.Code = c.code;
              movie.Countries.Add(country);
            }
            else
            {
              Log.Warn("Error adding category (url=" + c.url + "name=" + c.name + ") to cast");
            }
          }
        }

        if (m.cast != null)
        {
          movie.Cast = new List<MovieDbCast>();
          foreach (var p in m.cast)
          {
            int id = Util.Int32Parse(p.id);
            if (id != Util.NO_VALUE)
            {
              MovieDbCast cast = new MovieDbCast(id, p.name, p.url, p.job, p.character);
              movie.Cast.Add(cast);
            }
            else
            {
              Log.Warn("Error adding Person (id=" + p.id + ", name=" + p.name + ") to cast");
            }
          }
        }
        //parse and add banners to movie
        #region banners
        if (m.images != null)
        {
          Dictionary<string, List<String[]>> bannerList = new Dictionary<string, List<string[]>>();
          foreach (var i in m.images)
          {
            string id = i.id;
            if (!string.IsNullOrEmpty(id))
            {
              if (!bannerList.ContainsKey(id))
              {
                bannerList.Add(id, new List<String[]>());
              }
              bannerList[id].Add(new String[] { i.type, i.size, i.url });
            }
          }
          movie.Banners = new List<MovieDbBanner>();
          foreach (KeyValuePair<string, List<String[]>> kvp in bannerList)
          {
            MovieDbBanner banner = MovieDbBanner.CreateBanner(movie.Id, kvp.Key, kvp.Value);
            if (banner != null)
            {
              movie.Banners.Add(banner);
            }
          }
        }
        #endregion
        if (movie.Id != Util.NO_VALUE) retList.Add(movie);
      }

      //watch.Stop();
      //Log.Debug("Extracted " + retList.Count + " series in " + watch.ElapsedMilliseconds + " milliseconds");
      return retList;
    }

    internal List<MovieDbPerson> ExtractPersons(String data)
    {
      //Stopwatch watch = new Stopwatch();
      //watch.Start();
      List<MovieDbPerson> retList = new List<MovieDbPerson>();
      XDocument xml = XDocument.Parse(data);

      var allPersons = from movie in xml.Descendants("Person")
                       select new
                       {
                         popularity = movie.Elements("popularity").Count() == 1 ?
                                      movie.Element("popularity").Value : null,
                         name = movie.Elements("name").Count() == 1 ?
                                movie.Element("name").Value : null,
                         known_movies = movie.Elements("known_movies").Count() == 1 ?
                                            movie.Element("known_movies").Value : null,
                         birthday = movie.Elements("birthday").Count() == 1 ?
                                    movie.Element("birthday").Value : null,
                         id = movie.Elements("id").Count() == 1 ?
                              movie.Element("id").Value : null,
                         birthplace = movie.Elements("birthplace").Count() == 1 ?
                                   movie.Element("birthplace").Value : null,
                         url = movie.Elements("url").Count() == 1 ?
                               movie.Element("url").Value : null,
                         filmography = movie.Elements("filmography").Count() == 1 ?
                                       (from subelement in movie.Elements("filmography").Descendants("movie")
                                        select new
                                        {
                                          job = subelement.Attributes("job").Count() == 1 ?
                                                subelement.Attribute("job").Value : null,
                                          name = subelement.Attributes("name").Count() == 1 ?
                                                 subelement.Attribute("name").Value : null,
                                          url = subelement.Attributes("url").Count() == 1 ?
                                                subelement.Attribute("url").Value : null,
                                          character = subelement.Attributes("character").Count() == 1 ?
                                                      subelement.Attribute("character").Value : null,
                                          id = subelement.Attributes("id").Count() == 1 ?
                                               subelement.Attribute("id").Value : null
                                        }
                                       ).ToList() : null,
                         also_known_as = movie.Elements("also_known_as").Count() == 1 ?
                                     (from subelement in movie.Elements("also_known_as").Descendants("name")
                                      select new
                                      {
                                        alias = subelement.Value
                                      }
                                     ).ToList() : null,
                         images = movie.Elements("images").Count() == 1 ?
                                   (from image in movie.Elements("images").Descendants("image")
                                    select new
                                    {
                                      type = image.Attributes("type").Count() == 1 ?
                                             image.Attribute("type").Value : null,
                                      size = image.Attributes("size").Count() == 1 ?
                                             image.Attribute("size").Value : null,
                                      url = image.Attributes("url").Count() == 1 ?
                                            image.Attribute("url").Value : null,
                                      id = image.Attributes("id").Count() == 1 ?
                                           image.Attribute("id").Value : null
                                    }
                                   ).ToList() : null
                       };


      foreach (var p in allPersons)
      {
        MovieDbPerson person = new MovieDbPerson();
        person.Popularity = Util.Int32Parse(p.popularity);
        person.Id = Util.Int32Parse(p.id);
        person.Name = p.name;
        person.KnownMovies = Util.Int32Parse(p.known_movies);
        person.Birthday = Util.ParseDateTime(p.birthday);
        person.Birthplace = p.birthplace;
        person.Url = p.url;

        if (p.also_known_as != null)
        {
          person.AlsoKnownAs = new List<string>();
          foreach (var aka in p.also_known_as)
          {
            person.AlsoKnownAs.Add(aka.alias);
          }
        }

        if (p.filmography != null)
        {
          person.Filmography = new List<MovieDbPersonMovieJob>();
          foreach (var m in p.filmography)
          {
            person.Filmography.Add(new MovieDbPersonMovieJob(m.job, Util.Int32Parse(m.id), m.name,
                                                             m.character, m.url));
          }
        }

        //parse and add banners to movie
        #region banners
        if (p.images != null)
        {
          Dictionary<string, List<String[]>> bannerList = new Dictionary<string, List<string[]>>();
          foreach (var i in p.images)
          {
            string id = i.id;
            if (!string.IsNullOrEmpty(id))
            {
              if (!bannerList.ContainsKey(id))
              {
                bannerList.Add(id, new List<String[]>());
              }
              bannerList[id].Add(new String[] { i.type, i.size, i.url });
            }
          }
          person.Images = new List<MovieDbBanner>();
          foreach (KeyValuePair<string, List<String[]>> kvp in bannerList)
          {
            MovieDbBanner banner = MovieDbBanner.CreateBanner(person.Id, kvp.Key, kvp.Value);
            if (banner != null)
            {
              person.Images.Add(banner);
            }
          }
        }
        #endregion
        if (person.Id != Util.NO_VALUE) retList.Add(person);
      }

      //watch.Stop();
      //Log.Debug("Extracted " + retList.Count + " series in " + watch.ElapsedMilliseconds + " milliseconds");
      return retList;
    }
  }
}
