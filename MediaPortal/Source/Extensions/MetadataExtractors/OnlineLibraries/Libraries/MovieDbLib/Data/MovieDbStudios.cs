using System;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data
{
  public class MovieDbStudios
  {
    #region private/protected fields

    #endregion

    public MovieDbStudios()
    {

    }

    public MovieDbStudios(int id, String name)
      : this()
    {
      Id = id;
      Name = name;
    }

    public MovieDbStudios(int id, String name, String url)
      : this(id, name)
    {
      Url = url;
    }

    public string Name { get; set; }

    public string Url { get; set; }

    public int Id { get; set; }
  }
}
