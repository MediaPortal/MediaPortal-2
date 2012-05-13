using System;
using System.Collections.Generic;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data.Persons;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Exceptions;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Xml;
using System.Net;
using System.Xml;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib
{
  public class MovieDbDownloader
  {
    private readonly String _apiKey;
    private readonly WebClient _webClient;
    private readonly MovieDbXmlReader _xmlHandler;

    public MovieDbDownloader(String apiKey)
    {
      _webClient = new WebClient();
      _xmlHandler = new MovieDbXmlReader();
      _apiKey = apiKey;
    }

    private MovieDbMovie DownloadMovieFromUrl(String url)
    {
      String xml = null;
      try
      {
        xml = _webClient.DownloadString(url);

        //extract all series the xml file contains
        List<MovieDbMovie> seriesList = _xmlHandler.ExtractMovies(xml);

        //if a request is made on a movie id, one and only one result
        //should be returned, otherwise there obviously was an error
        if (seriesList != null && seriesList.Count == 1)
        {
          MovieDbMovie series = seriesList[0];
          return series;
        }
        Log.Warn("More than one series returned when trying to retrieve movie (" + url + ")");
        return null;
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + url + "\n\n" + xml, ex);
        throw new MovieDbInvalidXmlException("Error parsing the xml file " + url + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        Log.Warn("Request not successfull", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new MovieDbInvalidApiKeyException("Couldn't connect to TheMovieDb.org to retrieve " + url +
                                               ", you may use an invalid api key  or the series doesn't exists");
        }
        else
        {
          throw new MovieDbNotAvailableException("Couldn't connect to TheMovieDb.org to retrieve " + url +
                                              ", check your internet connection and the status of http://TheMovieDb.org");
        }
      }
    }

    public MovieDbMovie DownloadMovie(int movieId, MovieDbLanguage language)
    {
      //download the xml data from this request
      
      String link = MovieDbLinkCreator.CreateMovieLink(_apiKey, movieId, language, false);
      return DownloadMovieFromUrl(link);
    }


    internal MovieDbMovie DownloadMovie(string imdbId, MovieDbLanguage language)
    {
      String link = MovieDbLinkCreator.CreateImdbLookupLink(_apiKey, imdbId, language, false);
      return DownloadMovieFromUrl(link);
    }

    public List<MovieDbMovie> MovieSearch(String movieName, MovieDbLanguage language)
    {
      //download the xml data from this request
      String xml = "";
      String link = "";
      try
      {
        link = MovieDbLinkCreator.CreateMovieSearchLink(_apiKey, movieName, language);
        xml = _webClient.DownloadString(link);

        if (xml.Contains("Nothing found"))
        {
          return new List<MovieDbMovie>();
        }
        //extract all series the xml file contains
        List<MovieDbMovie> moviesList = _xmlHandler.ExtractMovies(xml);

        //if a request is made on a movie id, one and only one result
        //should be returned, otherwise there obviously was an error
        if (moviesList != null)
        {
          MovieDbMovie movie = moviesList[0];
          return moviesList;
        }
        else
        {
          Log.Warn("Search for " + movieName + " returned no results");
          return null;
        }
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new MovieDbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        Log.Warn("Request not successfull", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new MovieDbInvalidApiKeyException("Couldn't connect to TheMovieDb.org to retrieve " + movieName +
                                               ", you may use an invalid api key  or the series doesn't exists");
        }
        else
        {
          throw new MovieDbNotAvailableException("Couldn't connect to TheMovieDb.org to retrieve " + movieName +
                                              ", check your internet connection and the status of http://TheMovieDb.org");
        }
      }
    }

    internal List<MovieDbMovie> MovieSearchByHash(string movieHash)
    {
      //download the xml data from this request
      String xml = "";
      String link = "";
      try
      {
        link = MovieDbLinkCreator.CreateHashLink(_apiKey, movieHash);
        xml = _webClient.DownloadString(link);

        if (xml.Contains("Nothing found"))
        {
          return new List<MovieDbMovie>();
        }
        //extract all series the xml file contains
        List<MovieDbMovie> movieList = _xmlHandler.ExtractMovies(xml);

        //if a request is made on a movie id, one and only one result
        //should be returned, otherwise there obviously was an error
        if (movieList != null)
        {
          return movieList;
        }
        Log.Warn("Search for movie hash " + movieHash + " returned no results");
        return null;
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new MovieDbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        Log.Warn("Request not successfull", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new MovieDbInvalidApiKeyException("Couldn't connect to TheMovieDb.org to retrieve " + movieHash +
                                               ", you may use an invalid api key  or the series doesn't exists");
        }
        else
        {
          throw new MovieDbNotAvailableException("Couldn't connect to TheMovieDb.org to retrieve " + movieHash +
                                              ", check your internet connection and the status of http://TheMovieDb.org");
        }
      }
    }

    internal MovieDbPerson DownloadPerson(int personId, MovieDbLanguage language)
    {
      //download the xml data from this request
      String xml = "";
      String link = "";
      try
      {
        link = MovieDbLinkCreator.CreatePersonLink(_apiKey, personId, language);
        xml = _webClient.DownloadString(link);

        //extract all series the xml file contains
        List<MovieDbPerson> personList = _xmlHandler.ExtractPersons(xml);

        //if a request is made on a movie id, one and only one result
        //should be returned, otherwise there obviously was an error
        if (personList != null && personList.Count == 1)
        {
          MovieDbPerson person = personList[0];
          return person;
        }
        Log.Warn("More than one series returned when trying to retrieve Person " + personId);
        return null;
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new MovieDbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        Log.Warn("Request not successfull", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new MovieDbInvalidApiKeyException("Couldn't connect to TheMovieDb.org to retrieve Person " + personId +
                                               ", you may use an invalid api key  or the series doesn't exists");
        }
        throw new MovieDbNotAvailableException("Couldn't connect to TheMovieDb.org to retrieve Person " + personId +
          ", check your internet connection and the status of http://TheMovieDb.org");
      }
    }


    internal List<MovieDbPerson> PersonSearch(String personName, MovieDbLanguage language)
    {
      //download the xml data from this request
      String xml = "";
      String link = "";
      try
      {
        link = MovieDbLinkCreator.CreatePersonSearchLink(_apiKey, personName, language);
        xml = _webClient.DownloadString(link);

        if (xml.Contains("Nothing found"))
        {
          return new List<MovieDbPerson>();
        }
        //extract all series the xml file contains
        List<MovieDbPerson> personList = _xmlHandler.ExtractPersons(xml);

        //if a request is made on a movie id, one and only one result
        //should be returned, otherwise there obviously was an error
        if (personList != null)
        {
          MovieDbPerson series = personList[0];
          return personList;
        }
        else
        {
          Log.Warn("Search for " + personName + " returned no results");
          return null;
        }
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + link + "\n\n" + xml, ex);
        throw new MovieDbInvalidXmlException("Error parsing the xml file " + link + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        Log.Warn("Request not successfull", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new MovieDbInvalidApiKeyException("Couldn't connect to TheMovieDb.org to retrieve " + personName +
                                               ", you may use an invalid api key  or the series doesn't exists");
        }
        throw new MovieDbNotAvailableException("Couldn't connect to TheMovieDb.org to retrieve " + personName +
          ", check your internet connection and the status of http://TheMovieDb.org");
      }
    }


  }
}
