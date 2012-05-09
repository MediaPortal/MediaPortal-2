using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data.Banner;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data.Persons
{
  public class MovieDbPerson
  {
    #region private properties

    #endregion

    public MovieDbPerson()
    {

    }

    public MovieDbPerson(int id, String name)
      : this()
    {
      Name = name;
      Id = id;
    }

    public MovieDbPerson(int id, String name, String url)
      : this(id, name)
    {
      Url = url;
    }

    public MovieDbPerson(int id, String name, String url, String character)
      : this(id, name, url)
    {
      Character = character;
    }

    public override string ToString()
    {
      return Name + "(" + Id + ")";
    }

    public int Popularity { get; set; }

    public List<string> AlsoKnownAs { get; set; }

    public String AlsoKnownAsString
    {
      get
      {
        if (AlsoKnownAs != null && AlsoKnownAs.Count > 0)
        {
          StringBuilder akaBuilder = new StringBuilder();
          foreach (String s in AlsoKnownAs)
          {
            akaBuilder.Append(s);
            akaBuilder.Append(",");
          }
          akaBuilder.Remove(akaBuilder.Length - 1, 1);//remove last comma
          return akaBuilder.ToString();
        }
        return String.Empty;
      }
    }

    internal List<MovieDbPersonMovieJob> Filmography { get; set; }

    public List<MovieDbBanner> Images { get; set; }

    public string Birthplace { get; set; }

    public DateTime Birthday { get; set; }

    public int KnownMovies { get; set; }

    public string Name { get; set; }

    public string Url { get; set; }

    public int Id { get; set; }

    public string Character { get; set; }
  }
}
