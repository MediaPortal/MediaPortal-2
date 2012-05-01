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
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MovieDbLib.Data;
using System.IO;
using System.Xml.Linq;
using MovieDbLib.Data.Banner;
using System.Drawing;
using MovieDbLib.Data.Persons;

namespace MovieDbLib.Xml
{
  /// <summary>
  /// Writes tvdb data to xml files
  /// </summary>
  internal class MovieDbXmlWriter
  {
    /// <summary>
    /// TvdbXmlWriter constructor
    /// </summary>
    internal MovieDbXmlWriter()
    {

    }


    /// <summary>
    /// Create the movie content
    /// </summary>
    /// <param name="_movie">Movie to store</param>
    /// <returns>xml content</returns>
    internal String CreateMovieContent(MovieDbMovie _movie)
    {
      XElement xmlCat = new XElement("categories");
      if (_movie.Categories != null)
      {
        foreach (MovieDbCategory e in _movie.Categories)
        {
          xmlCat.Add(new XElement("category",
                  new XAttribute("type", e.Type.ToString()),
                  new XAttribute("url", e.Url),
                  new XAttribute("name", e.Name),
                  new XAttribute("id", e.Id)
                 ));

        }
      }

      XElement xmlStudios = new XElement("studios");
      if (_movie.Studios != null)
      {
        foreach (MovieDbStudios s in _movie.Studios)
        {
          xmlStudios.Add(new XElement("studio",
                         new XAttribute("url", s.Url),      
                         new XAttribute("name", s.Name),
                         new XAttribute("id", s.Id)
                        ));

        }
      }

      XElement xmlCountries = new XElement("countries");
      if (_movie.Countries != null)
      {
        foreach (MovieDbCountries c in _movie.Countries)
        {
          xmlCountries.Add(new XElement("country",
                           new XAttribute("url", c.Url),
                           new XAttribute("name", c.Name),
                           new XAttribute("code", c.Code),
                           new XAttribute("id", c.Id)
                           ));

        }
      }

      XElement xmlImages = new XElement("images");
      if (_movie.Banners != null)
      {
        foreach (MovieDbBanner b in _movie.Banners)
        {
          foreach (BannerSize s in b.ImageSizes.Values)
          {
            xmlImages.Add(new XElement("image",
                          new XAttribute("type", s.Type),
                          new XAttribute("size", s.Size),
                          new XAttribute("url", s.BannerPath),
                          new XAttribute("id", s.BannerId)
                          ));
          }
        }
      }

      XElement xmlCast = new XElement("cast");
      if (_movie.Cast != null)
      {
        foreach (MovieDbCast p in _movie.Cast)
        {
          xmlCast.Add(new XElement("person",
                          new XAttribute("job", p.Job),
                          new XAttribute("url", p.Url),
                          new XAttribute("name", p.Name),
                          new XAttribute("id", p.Id),
                          new XAttribute("character", p.Character)
                          ));
          
        }
      }


      XElement xml = new XElement("OpenSearchDescription");

      xml.Add(new XElement("movies",
                  new XElement("movie",
                      new XElement("popularity", _movie.Id),
                      new XElement("name", _movie.MovieName),
                      new XElement("alternative_name", _movie.AlternativeName),
                      new XElement("type", _movie.Budget),
                      new XElement("id", _movie.Id),
                      new XElement("imdb_id", _movie.ImdbId),
                      new XElement("url", _movie.Url),
                      new XElement("overview", _movie.Overview),
                      new XElement("rating", _movie.Rating),
                      new XElement("released", _movie.Released.ToShortDateString()),
                      new XElement("runtime", _movie.Runtime),
                      new XElement("budget", _movie.Rating),
                      new XElement("revenue", _movie.Revenue),
                      new XElement("homepage", _movie.Homepage),
                      new XElement("trailer", _movie.Trailer),
                      xmlCat, xmlStudios, xmlCountries, xmlImages, xmlCast
             )));



      return xml.ToString();
    }

    /// <summary>
    /// Write the series content to file
    /// </summary>
    /// <param name="_series">Series to store</param>
    /// <param name="_path">Path on disk</param>
    /// <returns>true if the file could be stored, false otherwise</returns>
    internal bool WriteMovieContent(MovieDbMovie _movie, String _path)
    {
      String fileContent = CreateMovieContent(_movie);
      try
      {
        FileInfo info = new FileInfo(_path);
        if (!info.Directory.Exists) info.Directory.Create();
        File.WriteAllText(info.FullName, fileContent);
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="_person"></param>
    /// <returns></returns>
    internal String CreatePersonContent(MovieDbPerson _person)
    {

      XElement xmlAka = new XElement("also_known_as");
      if (_person.AlsoKnownAs != null)
      {
        foreach (String aka in _person.AlsoKnownAs)
        {
          xmlAka.Add(new XElement("name", aka));

        }
      }

      XElement xmlFilmography = new XElement("filmography");
      if (_person.Filmography != null)
      {
        foreach (MovieDbPersonMovieJob j in _person.Filmography)
        {
          xmlFilmography.Add(new XElement("movie",
                           new XAttribute("job", j.JobType),
                           new XAttribute("url", j.MovieUrl),
                           new XAttribute("name", j.MovieName),
                           new XAttribute("character", j.Character != null ? j.Character : ""), 
                           new XAttribute("id", j.MovieId)
                           ));

        }
      }

      XElement xmlImages = new XElement("images");
      if (_person.Images != null)
      {
        foreach (MovieDbBanner b in _person.Images)
        {
          foreach (BannerSize s in b.ImageSizes.Values)
          {
            xmlImages.Add(new XElement("image",
                          new XAttribute("type", s.Type),
                          new XAttribute("size", s.Size),
                          new XAttribute("url", s.BannerPath),
                          new XAttribute("id", s.BannerId)
                          ));
          }
        }
      }

      XElement xml = new XElement("OpenSearchDescription");

      xml.Add(new XElement("persons",
                  new XElement("person",
                      new XElement("popularity", _person.Popularity.ToString()),
                      new XElement("name", _person.Name),
                      xmlAka,
                      new XElement("id", _person.Id),
                      new XElement("known_movies", _person.KnownMovies.ToString()),
                      new XElement("birthday", _person.Birthday.ToShortDateString()),
                      new XElement("birthplace", _person.Birthplace),
                      new XElement("url", _person.Url),
                      xmlFilmography, xmlImages
             )));



      return xml.ToString();
    }

    internal bool WritePersonContent(MovieDbPerson _person, string _path)
    {
      String fileContent = CreatePersonContent(_person);
      try
      {
        FileInfo info = new FileInfo(_path);
        if (!info.Directory.Exists) info.Directory.Create();
        File.WriteAllText(info.FullName, fileContent);
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }
  }
}
