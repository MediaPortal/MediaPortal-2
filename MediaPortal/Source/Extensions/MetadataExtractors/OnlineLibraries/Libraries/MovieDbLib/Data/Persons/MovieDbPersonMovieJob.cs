using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MovieDbLib.Data.Persons
{
  class MovieDbPersonMovieJob
  {
    #region private/protected fields
    private String m_jobType;
    private int m_movieId;
    private String m_movieName;
    private String m_movieUrl;
    private String m_character;
    #endregion

    public MovieDbPersonMovieJob()
    {

    }

    public MovieDbPersonMovieJob(String _jobType, int _movieId)
      : this()
    {
      m_jobType = _jobType;
      m_movieId = _movieId;
    }

    public MovieDbPersonMovieJob(String _jobType, int _movieId, String _movieName)
      : this(_jobType, _movieId)
    {
      m_movieName = _movieName;
    }

    public MovieDbPersonMovieJob(String _jobType, int _movieId, String _movieName, String _character, String _url)
      : this(_jobType, _movieId, _movieName)
    {
      m_movieUrl = _url;
    }

    public String Character
    {
      get { return m_character; }
      set { m_character = value; }
    }

    public String MovieName
    {
      get { return m_movieName; }
      set { m_movieName = value; }
    }

    public String MovieUrl
    {
      get { return m_movieUrl; }
      set { m_movieUrl = value; }
    }

    public int MovieId
    {
      get { return m_movieId; }
      set { m_movieId = value; }
    }

    public String JobType
    {
      get { return m_jobType; }
      set { m_jobType = value; }
    }
  }
}
