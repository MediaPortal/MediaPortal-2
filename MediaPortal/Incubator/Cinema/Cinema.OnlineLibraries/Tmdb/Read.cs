using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Cinema.OnlineLibraries.Tmdb.Data;

namespace Cinema.OnlineLibraries.Tmdb
{
  public class Read
  {
    private static string _key = "b209a38e9f0d3b955b25027010174a8a";

    public static string TvdbId(string imdbId)
    {
      string url = "https://api.themoviedb.org/3/find/" + imdbId + "?api_key=" + _key + "&external_source=imdb_id";

      var ret = Helper.Http.Request(url);
      if (Helper.Http.StatusCode != HttpStatusCode.OK)
      {
        var d = "";
      }

      if (ret != "")
      {
        var f = Helper.Json.Deserialize<Find>(ret);
        if (f != null && f.MovieResults != null)
        {
          if (f.MovieResults.Count > 0)
          {
            return f.MovieResults[0].Id;
          }
        }
      }
      
      return "";
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tvdbId"></param>
    /// <param name="language">ISO 639-1 value</param>
    /// <returns></returns>
    public static Movie Movie(string tvdbId, string language = "en-US")
    {
      Movie movie = new Movie();
      string url = "https://api.themoviedb.org/3/movie/" + tvdbId + "?api_key=" + _key + "&language=" + language + "&append_to_response=videos";

      var ret = Helper.Http.Request(url);
      if (ret != "")
      {
        var f = Helper.Json.Deserialize<Movie>(ret);
        if (f != null)
        {
            return f;
        }
      }

      return movie;
    }
  }
}
