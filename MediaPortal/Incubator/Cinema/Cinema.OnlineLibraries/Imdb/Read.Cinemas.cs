using System.Collections.Generic;
using System.Threading.Tasks;
using Cinema.OnlineLibraries.Helper;

namespace Cinema.OnlineLibraries.Imdb
{
  public partial class Read
  {
    public static List<Data.Cinema> Cinemas(string postalCode, string country)
    {
      var list = new List<Data.Cinema>();

      var url = "https://www.imdb.com/showtimes/" + country + "/" + postalCode + "?ref_=shlc_sh";

      var doc = Http.GetHtmlDocument(url);
      var cinemaNodes = doc.DocumentNode.SelectNodes("//*[@itemtype=\"http://schema.org/MovieTheater\"]");

      if (cinemaNodes != null)
        foreach (var item in cinemaNodes)
        {
          var cinema = new Data.Cinema();

          var p1 = item.SelectSingleNode(".//*[@itemprop=\"url\"]");
          if (p1 != null) cinema.Name = p1.InnerText.Trim();

          var p2 = item.SelectSingleNode(".//*[@class=\"favorite_toggle favorite_off\"]");
          if (p2 != null) cinema.Id = p2.GetAttributeValue("data-cinemaid", "");

          var p3 = item.SelectSingleNode(".//*[@itemprop=\"streetAddress\"]");
          if (p3 != null) cinema.Address = p3.InnerText.Trim();

          var p4 = item.SelectSingleNode(".//*[@itemprop=\"addressLocality\"]");
          if (p4 != null) cinema.Locality = p4.InnerText.Trim();

          var p5 = item.SelectSingleNode(".//*[@itemprop=\"addressRegion\"]");
          if (p5 != null) cinema.Region = p5.InnerText.Trim();

          var p6 = item.SelectSingleNode(".//*[@itemprop=\"postalCode\"]");
          if (p6 != null) cinema.PostalCode = p6.InnerText.Trim();

          var p7 = item.SelectSingleNode(".//*[@itemprop=\"telephone\"]");
          if (p7 != null) cinema.Phone = p7.InnerText.Trim();

          list.Add(cinema);
        }

      return list;
    }
  }
}
