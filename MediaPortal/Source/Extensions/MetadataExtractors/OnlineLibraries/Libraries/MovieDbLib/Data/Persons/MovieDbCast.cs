using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MovieDbLib.Data.Banner;

namespace MovieDbLib.Data.Persons
{
  public class MovieDbCast
  {
        #region private properties
    private int m_personId;
    private String m_url;
    private String m_name;
    private String m_job;
    private String m_character;
    private double m_popularity;
    private List<String> m_alsoKnownAs;
    private int m_knownMovies;
    private DateTime m_birthday;
    private String m_birthplace;
    private List<MovieDbPersonMovieJob> m_filmography;
    private List<MovieDbBanner> m_images;


    #endregion

    public MovieDbCast()
    {

    }

    public MovieDbCast(int _id, String _name)
      : this()
    {
      m_name = _name;
      m_personId = _id;
    }

    public MovieDbCast(int _id, String _name, String _url, String _job)
      : this(_id, _name)
    {
      m_url = _url;
      m_job = _job;
    }

    public MovieDbCast(int _id, String _name, String _url, String _job, String _character)
      : this(_id, _name, _url, _job)
    {
      m_character = _character;
    }

    public override string ToString()
    {
      return m_name + "(" + m_personId + ") - " + m_job;
    }

    public List<MovieDbBanner> Images
    {
      get { return m_images; }
      set { m_images = value; }
    }

    public String Name
    {
      get { return m_name; }
      set { m_name = value; }
    }

    public String Url
    {
      get { return m_url; }
      set { m_url = value; }
    }

    public int Id
    {
      get { return m_personId; }
      set { m_personId = value; }
    }

    public String Character
    {
      get { return m_character; }
      set { m_character = value; }
    }

    public String Job
    {
      get { return m_job; }
      set { m_job = value; }
    }
  }
}
