using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MovieDbLib.Data.Persons;
using MovieDbLib.Data.Banner;

namespace MovieDbLib.Data
{
  public class MovieDbPerson
  {
    #region private properties
    private int m_Id;
    private String m_url;
    private String m_name;
    private String m_character;
    private int m_popularity;
    private List<String> m_alsoKnownAs;
    private int m_knownMovies;
    private DateTime m_birthday;
    private String m_birthplace;
    private List<MovieDbPersonMovieJob> m_filmography;
    private List<MovieDbBanner> m_images;


    #endregion

    public MovieDbPerson()
    {

    }

    public MovieDbPerson(int _id, String _name)
      : this()
    {
      m_name = _name;
      m_Id = _id;
    }

    public MovieDbPerson(int _id, String _name, String _url)
      : this(_id, _name)
    {
      m_url = _url;
    }

    public MovieDbPerson(int _id, String _name, String _url, String _character)
      : this(_id, _name, _url)
    {
      m_character = _character;
    }

    public override string ToString()
    {
      return m_name + "(" + m_Id + ")";
    }

    public int Popularity
    {
      get { return m_popularity; }
      set { m_popularity = value; }
    }

    public List<String> AlsoKnownAs
    {
      get { return m_alsoKnownAs; }
      set { m_alsoKnownAs = value; }
    }

    public String AlsoKnownAsString
    {
      get
      {
        if (m_alsoKnownAs != null && m_alsoKnownAs.Count > 0)
        {
          StringBuilder akaBuilder = new StringBuilder();
          foreach (String s in m_alsoKnownAs)
          {
            akaBuilder.Append(s);
            akaBuilder.Append(",");
          }
          akaBuilder.Remove(akaBuilder.Length - 1, 1);//remove last comma
          return akaBuilder.ToString();
        }
        else
        {
          return String.Empty;
        }
      }
    }

    internal List<MovieDbPersonMovieJob> Filmography
    {
      get { return m_filmography; }
      set { m_filmography = value; }
    }

    public List<MovieDbBanner> Images
    {
      get { return m_images; }
      set { m_images = value; }
    }

    public String Birthplace
    {
      get { return m_birthplace; }
      set { m_birthplace = value; }
    }

    public DateTime Birthday
    {
      get { return m_birthday; }
      set { m_birthday = value; }
    }

    public int KnownMovies
    {
      get { return m_knownMovies; }
      set { m_knownMovies = value; }
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
      get { return m_Id; }
      set { m_Id = value; }
    }

    public String Character
    {
      get { return m_character; }
      set { m_character = value; }
    }
  }
}
