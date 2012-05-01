using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MovieDbLib.Data;
using System.Net;
using System.Xml;
using MovieDbLib.Exceptions;
using MovieDb;
using MovieDbLib.Xml;

namespace MovieDbLib
{
  public class MovieDbDownloader
  {
    private String m_apiKey;
    private WebClient m_webClient;
    private MovieDbXmlReader m_xmlHandler;

    public MovieDbDownloader(String _apiKey)
    {
      m_webClient = new WebClient();
      m_xmlHandler = new MovieDbXmlReader();
      m_apiKey = _apiKey;
    }

    private MovieDbMovie DownloadMovieFromUrl(String _url)
    {
      String xml = null;
      try
      {
        xml = m_webClient.DownloadString(_url);

        //extract all series the xml file contains
        List<MovieDbMovie> seriesList = m_xmlHandler.ExtractMovies(xml);

        //if a request is made on a movie id, one and only one result
        //should be returned, otherwise there obviously was an error
        if (seriesList != null && seriesList.Count == 1)
        {
          MovieDbMovie series = seriesList[0];
          return series;
        }
        else
        {
          Log.Warn("More than one series returned when trying to retrieve movie (" + _url + ")");
          return null;
        }
      }
      catch (XmlException ex)
      {
        Log.Error("Error parsing the xml file " + _url + "\n\n" + xml, ex);
        throw new MovieDbInvalidXmlException("Error parsing the xml file " + _url + "\n\n" + xml);
      }
      catch (WebException ex)
      {
        Log.Warn("Request not successfull", ex);
        if (ex.Message.Equals("The remote server returned an error: (404) Not Found."))
        {
          throw new MovieDbInvalidApiKeyException("Couldn't connect to TheMovieDb.org to retrieve " + _url +
                                               ", you may use an invalid api key  or the series doesn't exists");
        }
        else
        {
          throw new MovieDbNotAvailableException("Couldn't connect to TheMovieDb.org to retrieve " + _url +
                                              ", check your internet connection and the status of http://TheMovieDb.org");
        }
      }
    }

    public MovieDbMovie DownloadMovie(int _movieId, MovieDbLanguage _language)
    {
      //download the xml data from this request
      
      String link = MovieDbLinkCreator.CreateMovieLink(m_apiKey, _movieId, _language, false);
      return DownloadMovieFromUrl(link);
    }


    internal MovieDbMovie DownloadMovie(string _imdbId, MovieDbLanguage _language)
    {
      String link = MovieDbLinkCreator.CreateImdbLookupLink(m_apiKey, _imdbId, _language, false);
      return DownloadMovieFromUrl(link);
    }

    public List<MovieDbMovie> MovieSearch(String _movieName, MovieDbLanguage _language)
    {
      //download the xml data from this request
      String xml = "";
      String link = "";
      try
      {
        link = MovieDbLinkCreator.CreateMovieSearchLink(m_apiKey, _movieName, _language);
        xml = m_webClient.DownloadString(link);

        if (xml.Contains("Nothing found"))
        {
          return new List<MovieDbMovie>();
        }
        //extract all series the xml file contains
        List<MovieDbMovie> seriesList = m_xmlHandler.ExtractMovies(xml);

        //if a request is made on a movie id, one and only one result
        //should be returned, otherwise there obviously was an error
        if (seriesList != null)
        {
          MovieDbMovie series = seriesList[0];
          return seriesList;
        }
        else
        {
          Log.Warn("Search for " + _movieName + " returned no results");
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
          throw new MovieDbInvalidApiKeyException("Couldn't connect to TheMovieDb.org to retrieve " + _movieName +
                                               ", you may use an invalid api key  or the series doesn't exists");
        }
        else
        {
          throw new MovieDbNotAvailableException("Couldn't connect to TheMovieDb.org to retrieve " + _movieName +
                                              ", check your internet connection and the status of http://TheMovieDb.org");
        }
      }
    }

    internal List<MovieDbMovie> MovieSearchByHash(string _movieHash)
    {
      //download the xml data from this request
      String xml = "";
      String link = "";
      try
      {
        link = MovieDbLinkCreator.CreateHashLink(m_apiKey, _movieHash);
        xml = m_webClient.DownloadString(link);

        if (xml.Contains("Nothing found"))
        {
          return new List<MovieDbMovie>();
        }
        //extract all series the xml file contains
        List<MovieDbMovie> movieList = m_xmlHandler.ExtractMovies(xml);

        //if a request is made on a movie id, one and only one result
        //should be returned, otherwise there obviously was an error
        if (movieList != null)
        {
          MovieDbMovie series = movieList[0];
          return movieList;
        }
        else
        {
          Log.Warn("Search for movie hash " + _movieHash + " returned no results");
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
          throw new MovieDbInvalidApiKeyException("Couldn't connect to TheMovieDb.org to retrieve " + _movieHash +
                                               ", you may use an invalid api key  or the series doesn't exists");
        }
        else
        {
          throw new MovieDbNotAvailableException("Couldn't connect to TheMovieDb.org to retrieve " + _movieHash +
                                              ", check your internet connection and the status of http://TheMovieDb.org");
        }
      }
    }

    internal MovieDbPerson DownloadPerson(int _personId, MovieDbLanguage _language)
    {
      //download the xml data from this request
      String xml = "";
      String link = "";
      try
      {
        link = MovieDbLinkCreator.CreatePersonLink(m_apiKey, _personId, _language);
        xml = m_webClient.DownloadString(link);

        //extract all series the xml file contains
        List<MovieDbPerson> personList = m_xmlHandler.ExtractPersons(xml);

        //if a request is made on a movie id, one and only one result
        //should be returned, otherwise there obviously was an error
        if (personList != null && personList.Count == 1)
        {
          MovieDbPerson person = personList[0];
          return person;
        }
        else
        {
          Log.Warn("More than one series returned when trying to retrieve person " + _personId);
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
          throw new MovieDbInvalidApiKeyException("Couldn't connect to TheMovieDb.org to retrieve person " + _personId +
                                               ", you may use an invalid api key  or the series doesn't exists");
        }
        else
        {
          throw new MovieDbNotAvailableException("Couldn't connect to TheMovieDb.org to retrieve person " + _personId +
                                              ", check your internet connection and the status of http://TheMovieDb.org");
        }
      }
    }


    internal List<MovieDbPerson> PersonSearch(String _personName, MovieDbLanguage _language)
    {
      //download the xml data from this request
      String xml = "";
      String link = "";
      try
      {
        link = MovieDbLinkCreator.CreatePersonSearchLink(m_apiKey, _personName, _language);
        xml = m_webClient.DownloadString(link);

        if (xml.Contains("Nothing found"))
        {
          return new List<MovieDbPerson>();
        }
        //extract all series the xml file contains
        List<MovieDbPerson> personList = m_xmlHandler.ExtractPersons(xml);

        //if a request is made on a movie id, one and only one result
        //should be returned, otherwise there obviously was an error
        if (personList != null)
        {
          MovieDbPerson series = personList[0];
          return personList;
        }
        else
        {
          Log.Warn("Search for " + _personName + " returned no results");
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
          throw new MovieDbInvalidApiKeyException("Couldn't connect to TheMovieDb.org to retrieve " + _personName +
                                               ", you may use an invalid api key  or the series doesn't exists");
        }
        else
        {
          throw new MovieDbNotAvailableException("Couldn't connect to TheMovieDb.org to retrieve " + _personName +
                                              ", check your internet connection and the status of http://TheMovieDb.org");
        }
      }
    }


  }
}
