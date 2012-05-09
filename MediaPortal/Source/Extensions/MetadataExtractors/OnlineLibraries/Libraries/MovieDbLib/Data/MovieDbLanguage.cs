using System;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data
{
  public class MovieDbLanguage
  {
    #region private properties

    #endregion

    public MovieDbLanguage(int id, String abbriviation, String name)
      : this()
    {
      Id = id;
      Abbriviation = abbriviation;
      Name = name;
    }

    public MovieDbLanguage()
    {

    }

    public int Id { get; set; }

    public string Name { get; set; }

    public string Abbriviation { get; set; }
  }
}
