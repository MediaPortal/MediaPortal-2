using System.Collections.Generic;

namespace Cinema.OnlineLibraries.Data
{
  public class MovieTime
  {
    public string Day { get; set; } = string.Empty;

    public Movie Movie { get; set; } = new Movie();

    public List<Imdb.Showtime> Showtimes { get; set; } = new List<Imdb.Showtime>();
  }
}
