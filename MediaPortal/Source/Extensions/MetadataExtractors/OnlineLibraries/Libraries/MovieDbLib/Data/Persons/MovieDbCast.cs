using System;
using System.Collections.Generic;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data.Banner;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data.Persons
{
  public class MovieDbCast
  {
    public MovieDbCast()
    {

    }

    public MovieDbCast(int id, String name)
      : this()
    {
      Name = name;
      Id = id;
    }

    public MovieDbCast(int id, String name, String url, String job)
      : this(id, name)
    {
      Url = url;
      Job = job;
    }

    public MovieDbCast(int id, String name, String url, String job, String character)
      : this(id, name, url, job)
    {
      Character = character;
    }

    public override string ToString()
    {
      return Name + "(" + Id + ") - " + Job;
    }

    public List<MovieDbBanner> Images { get; set; }

    public string Name { get; set; }

    public string Url { get; set; }

    public int Id { get; set; }

    public string Character { get; set; }

    public string Job { get; set; }
  }
}
