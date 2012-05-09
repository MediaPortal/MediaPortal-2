using System;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data.Persons
{
  class MovieDbPersonMovieJob
  {
    #region private/protected fields

    #endregion

    public MovieDbPersonMovieJob()
    {

    }

    public MovieDbPersonMovieJob(String jobType, int movieId)
      : this()
    {
      JobType = jobType;
      MovieId = movieId;
    }

    public MovieDbPersonMovieJob(String jobType, int movieId, String movieName)
      : this(jobType, movieId)
    {
      MovieName = movieName;
    }

    public MovieDbPersonMovieJob(String jobType, int movieId, String movieName, String character, String url)
      : this(jobType, movieId, movieName)
    {
      Character = character;
      MovieUrl = url;
    }

    public string Character { get; set; }

    public string MovieName { get; set; }

    public string MovieUrl { get; set; }

    public int MovieId { get; set; }

    public string JobType { get; set; }
  }
}
