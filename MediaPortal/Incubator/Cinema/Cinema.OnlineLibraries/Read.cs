using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cinema.OnlineLibraries.Data;
using Cinema.OnlineLibraries.Helper;
using Cinema.OnlineLibraries.Tmdb.Data;
using Movie = Cinema.OnlineLibraries.Data.Movie;

namespace Cinema.OnlineLibraries
{
  public class Read
  {
    public static List<CinemaMovies> MoviesForAllDaysAndCinemas(string language, string country, string postalCode, List<Data.Cinema> cinemas)
    {
      List<CinemaMovies> moviesForAllDays = new List<CinemaMovies>();

      foreach (var cinema in cinemas)
      {
        CinemaMovies movies = new CinemaMovies();
        movies.Cinema = cinema;

        Eventhandler.Instance.NewMessageReceived("[C]" + cinema.Name);

        var allMovieTimes = Imdb.Read.MovieTimesWeek(cinema.Id, postalCode, country);

        List<Movie> sortedMovies = SortMovieTimes(allMovieTimes);

        var showTimeMovies = AddShowtimes(sortedMovies, allMovieTimes);
        
        movies.Movies.AddRange(GetMoviedetailsFromTmdb(showTimeMovies, language));

        moviesForAllDays.Add(movies);
      }

      return moviesForAllDays;
    }

    //private static async Task<List<Movie>> ImdbMoviesOfTheWeek(string country, string postalCode)
    //{
    //  List<string> ids = new List<string>();
    //  List<Movie> imdbMovies = new List<Movie>();

    //  try
    //  {
    //    var days = Helper.Help.GetDays(7);
    //    foreach (var day in days)
    //    {
    //      var currentMovies = await Imdb.Read.Movies(country, postalCode, day);

    //      foreach (var cm in currentMovies)
    //      {
    //        if (!ids.Contains(cm.ImdbId))
    //        {
    //          ids.Add(cm.ImdbId);
    //          imdbMovies.Add(cm);
    //        }
    //      }
    //    }
    //  }
    //  catch (Exception e)
    //  {
    //    ExceptionHandler.Instance.NewExceptionReceived(e);
    //  }

    //  return imdbMovies;
    //}

    private static List<Movie> SortMovieTimes(List<List<MovieTime>> allMovieTimes)
    {
      List<Movie> list = new List<Movie>();
      List<string> ids = new List<string>();

      try
      {
        foreach (var day in allMovieTimes)
        {
          foreach (var movie in day)
          {
            if (!ids.Contains(movie.Movie.ImdbId))
            {
              ids.Add(movie.Movie.ImdbId);
              list.Add(movie.Movie);
            }
          }
        }
      }
      catch (Exception e)
      {
        ExceptionHandler.Instance.NewExceptionReceived(e);
      }
      
      return list;
    }

    private static List<Movie> AddShowtimes(List<Movie> sortedMovies, List<List<MovieTime>> allMovieTimes)
    {
      List<Movie> list = new List<Movie>();

      try
      {
        foreach (var mt in sortedMovies)
        {
          mt.Showtimes = Showtimes(allMovieTimes, mt.ImdbId);
          list.Add(mt);
        }
      }
      catch (Exception e)
      {
        ExceptionHandler.Instance.NewExceptionReceived(e);
      }

      return list;
    }

    private static List<Movie> GetMoviedetailsFromTmdb(List<Movie> imdbMovies, string language)
    {
      List<Movie> movies = new List<Movie>();

      try
      {
        foreach (var movie in imdbMovies)
        {
          Movie m = new Movie();
          m.ImdbId = movie.ImdbId;
          m.TmdbId = Tmdb.Read.TvdbId(m.ImdbId);

          var tmdbMovie = Tmdb.Read.Movie(m.TmdbId, language);
          m.Title = tmdbMovie.Title;
          m.Release = tmdbMovie.ReleaseDate.ToString("yyyy-MM-dd");
          m.Runtime = tmdbMovie.Runtime;
          m.Genres = Genres(tmdbMovie.Genres);
          m.Age = movie.Age;
          m.CoverUrl = "https://image.tmdb.org/t/p/original" + tmdbMovie.PosterPath;
          m.Fanart = "https://image.tmdb.org/t/p/original" + tmdbMovie.BackdropPath;
          m.Language = tmdbMovie.OriginalLanguage;
          m.Description = tmdbMovie.Overview;
          m.Country = Countrys(tmdbMovie.ProductionCountries);
          m.Trailer = Trailer(tmdbMovie.Videos);
          m.Showtimes = movie.Showtimes;
          m.UserRating = movie.UserRating;
          m.UserRatingScaled = Help.UserRatingFromString(movie.UserRating.Replace(".", ","));
          movies.Add(m);
        }
      }
      catch (Exception e)
      {
        ExceptionHandler.Instance.NewExceptionReceived(e);
      }

      return movies;
    }

    private static List<Showtime> Showtimes(List<List<MovieTime>> movies, string imdbId)
    {
      var list = new List<Showtime>();

      try
      {
        foreach (var day in movies)
        {
          foreach (var movie in day)
          {
            if (movie.Movie.ImdbId == imdbId)
            {
              string times = "";

              foreach (var st in movie.Showtimes)
              {
                var t = st.Time;
                var tt = t.Split('|');
                bool am = false;

                foreach (var ttt in tt)
                {
                  if (ttt.Trim().Length > 5 || am == true)
                  {
                    if (ttt.Trim().Contains("am"))
                    {
                      am = true;
                    }
                    else
                    {
                      am = false;
                    }
                    DateTime d = DateTime.Parse(ttt.Trim());
                    times += d.ToString("HH:mm") + "   ";
                  }
                  else
                  {
                    DateTime d = DateTime.Parse(ttt.Trim()).AddHours(12);
                    times += d.ToString("HH:mm") + "   ";
                    am = false;
                  }

                }
              }

              list.Add(new Showtime() { Day = movie.Day, Showtimes = times });
            }
          }
        }
      }
      catch (Exception e)
      {
        ExceptionHandler.Instance.NewExceptionReceived(e);
      }

      return list;
    }

    private static List<Trailer> Trailer(Videos videos)
    {
      List<Trailer> trailer = new List<Trailer>();

      try
      {
        if (videos != null)
        {
          if (videos.Results != null)
          {
            foreach (var video in videos.Results)
            {
              trailer.Add(new Trailer(video.Name, video.Key));
            }
          }
        }
      }
      catch (Exception e)
      {
        ExceptionHandler.Instance.NewExceptionReceived(e);
      }

      return trailer;
    }

    private static string Genres(List<Genre> genres)
    {
      try
      {
        string ret = genres.Aggregate("", (current, g) => current + (g.Name + ","));

        return ret.Length > 1 ? ret.Substring(0, ret.Length - 1) : "";
      }
      catch (Exception e)
      {
        ExceptionHandler.Instance.NewExceptionReceived(e);
      }

      return "";
    }

    private static string Countrys(List<ProductionCountry> countrys)
    {
      try
      {
        string ret = countrys.Aggregate("", (current, g) => current + (g.Name + ","));

        return ret.Length > 1 ? ret.Substring(0, ret.Length - 1) : "";
      }
      catch (Exception e)
      {
        ExceptionHandler.Instance.NewExceptionReceived(e);
      }

      return "";
    }
  }
}
