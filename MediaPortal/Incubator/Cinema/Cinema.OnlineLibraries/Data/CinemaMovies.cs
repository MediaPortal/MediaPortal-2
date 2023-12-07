using System.Collections.Generic;

namespace Cinema.OnlineLibraries.Data
{
  public class CinemaMovies
  {
    public Cinema Cinema { get; set; } = new Cinema();

    public List<Movie> Movies { get; set; } = new List<Movie>();
  }
}
