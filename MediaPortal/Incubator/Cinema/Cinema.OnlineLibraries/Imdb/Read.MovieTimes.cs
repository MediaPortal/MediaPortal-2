using Cinema.OnlineLibraries.Helper;
using System.Collections.Generic;
using Cinema.OnlineLibraries.Data;

namespace Cinema.OnlineLibraries.Imdb
{
  public partial class Read
  {
    public static List<List<MovieTime>> MovieTimesWeek(string cinemaId, string postalCode, string country)
    {
      List<List<MovieTime>> list = new List<List<MovieTime>>();

      var days = Help.GetDays(7);

      foreach (var d in days)
      {
        Eventhandler.Instance.NewMessageReceived("[D]" + d);

        list.Add(MovieTimes(cinemaId, postalCode, country, d));
      }

      return list;
    }

    public static List<MovieTime> MovieTimes(string cinemaId, string postalCode, string country, string day)
    {
      var movieTimes = new List<MovieTime>();

      var url = "https://www.imdb.com/showtimes/cinema/" + country + "/" + cinemaId + "/" + country + "/" + postalCode + "/" + day + "?ref_=shtt_tny_th";

      var doc = Http.GetHtmlDocument(url);
      var liste = doc.DocumentNode.SelectSingleNode("//*[@class=\"list detail\"]");

      var movieNodes = liste.SelectNodes(".//*[@itemtype=\"http://schema.org/Movie\"]");
      if (movieNodes != null)
      {
        foreach (var item in movieNodes)
        {
          MovieTime mt = new MovieTime();
          mt.Day = day;

          var p1 = item.SelectSingleNode(".//a");
          if (p1 != null)
          {
            var tt_url = p1.GetAttributeValue("href", "").Replace("/showtimes/title/", "");
            mt.Movie.ImdbId = tt_url.Substring(0, tt_url.IndexOf("/"));
          }

          var p2 = item.SelectSingleNode(".//*[@itemprop=\"image\"]");
          if (p2 != null) mt.Movie.CoverUrl = p2.GetAttributeValue("src", "");

          var p3 = item.SelectSingleNode(".//*[@itemprop=\"url\"]");
          if (p3 != null) mt.Movie.Title = p3.InnerText.Trim();

          var p4 = item.SelectSingleNode(".//*[@itemprop=\"contentRating\"]");
          if (p4 != null)
          {
            var p41 = p4.SelectSingleNode(".//img");
            if (p41 != null) mt.Movie.Age = p41.GetAttributeValue("title", "");
          }

          var p5 = item.SelectSingleNode(".//*[@itemprop=\"duration\"]");
          if (p5 != null) mt.Movie.Runtime = p5.InnerText.Trim();

          var p6 = item.SelectSingleNode(".//*[@itemprop=\"ratingValue\"]");
          if (p6 != null) mt.Movie.UserRating = p6.InnerText.Trim();

          var showTimes = item.SelectSingleNode(".//*[@class=\"info\"]");
          Imdb.Showtime st = null;
          foreach (var cn in showTimes.ChildNodes)
          {
            if (cn.Name == "h5")
            {
              st = new Imdb.Showtime();
              st.Typ = cn.InnerText.Replace(":&nbsp;", "");
            }
            if (cn.Name == "div" && st != null)
            {
              st.Time = cn.InnerText.Replace("\n", "").Trim();
              mt.Showtimes.Add(st);
              st = null;
            }
          }

          Eventhandler.Instance.NewMessageReceived("[M]" + mt.Movie.Title);

          movieTimes.Add(mt);
        }
      }

      return movieTimes;
    }
  }
}
