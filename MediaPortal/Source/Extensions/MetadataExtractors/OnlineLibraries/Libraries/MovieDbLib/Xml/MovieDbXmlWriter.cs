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
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data.Banner;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data.Persons;
using System.IO;
using System.Xml.Linq;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Xml
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
    /// <param name="movie">Movie to store</param>
    /// <returns>xml content</returns>
    internal String CreateMovieContent(MovieDbMovie movie)
    {
      XElement xmlCat = new XElement("categories");
      if (movie.Categories != null)
      {
        foreach (MovieDbCategory e in movie.Categories)
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
      if (movie.Studios != null)
      {
        foreach (MovieDbStudios s in movie.Studios)
        {
          xmlStudios.Add(new XElement("studio",
                         new XAttribute("url", s.Url),      
                         new XAttribute("name", s.Name),
                         new XAttribute("id", s.Id)
                        ));

        }
      }

      XElement xmlCountries = new XElement("countries");
      if (movie.Countries != null)
      {
        foreach (MovieDbCountries c in movie.Countries)
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
      if (movie.Banners != null)
      {
        foreach (MovieDbBanner b in movie.Banners)
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
      if (movie.Cast != null)
      {
        foreach (MovieDbCast p in movie.Cast)
        {
          xmlCast.Add(new XElement("Person",
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
                      new XElement("popularity", movie.Id),
                      new XElement("name", movie.MovieName),
                      new XElement("alternative_name", movie.AlternativeName),
                      new XElement("type", movie.Budget),
                      new XElement("id", movie.Id),
                      new XElement("imdb_id", movie.ImdbId),
                      new XElement("url", movie.Url),
                      new XElement("overview", movie.Overview),
                      new XElement("rating", movie.Rating),
                      new XElement("released", movie.Released.ToShortDateString()),
                      new XElement("runtime", movie.Runtime),
                      new XElement("budget", movie.Rating),
                      new XElement("revenue", movie.Revenue),
                      new XElement("homepage", movie.Homepage),
                      new XElement("trailer", movie.Trailer),
                      xmlCat, xmlStudios, xmlCountries, xmlImages, xmlCast
             )));



      return xml.ToString();
    }

    /// <summary>
    /// Write the movie content to file
    /// </summary>
    /// <param name="movie">Movie</param>
    /// <param name="path">Path on disk</param>
    /// <returns>true if the file could be stored, false otherwise</returns>
    internal bool WriteMovieContent(MovieDbMovie movie, String path)
    {
      String fileContent = CreateMovieContent(movie);
      try
      {
        FileInfo info = new FileInfo(path);
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
    /// <param name="person"></param>
    /// <returns></returns>
    internal String CreatePersonContent(MovieDbPerson person)
    {

      XElement xmlAka = new XElement("also_known_as");
      if (person.AlsoKnownAs != null)
      {
        foreach (String aka in person.AlsoKnownAs)
        {
          xmlAka.Add(new XElement("name", aka));

        }
      }

      XElement xmlFilmography = new XElement("filmography");
      if (person.Filmography != null)
      {
        foreach (MovieDbPersonMovieJob j in person.Filmography)
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
      if (person.Images != null)
      {
        foreach (MovieDbBanner b in person.Images)
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
                  new XElement("Person",
                      new XElement("popularity", person.Popularity.ToString()),
                      new XElement("name", person.Name),
                      xmlAka,
                      new XElement("id", person.Id),
                      new XElement("known_movies", person.KnownMovies.ToString()),
                      new XElement("birthday", person.Birthday.ToShortDateString()),
                      new XElement("birthplace", person.Birthplace),
                      new XElement("url", person.Url),
                      xmlFilmography, xmlImages
             )));



      return xml.ToString();
    }

    internal bool WritePersonContent(MovieDbPerson person, string path)
    {
      String fileContent = CreatePersonContent(person);
      try
      {
        FileInfo info = new FileInfo(path);
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
