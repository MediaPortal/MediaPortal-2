using System.Collections.Generic;

namespace Cinema.OnlineLibraries.Data
{
  public class Movie
  {
    public string Title { get; set; } = string.Empty;
    public string ImdbId { get; set; } = string.Empty;
    public string TmdbId { get; set; } = string.Empty;
    public string Release { get; set; } = string.Empty;
    public string Runtime { get; set; } = string.Empty;
    public string Genres { get; set; } = string.Empty;
    public string Age { get; set; } = string.Empty;
    public string CoverUrl { get; set; } = string.Empty;
    public string Fanart { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string UserRating { get; set; } = string.Empty;
    public string UserRatingScaled { get; set; } = string.Empty;

    public List<Trailer> Trailer { get; set; }

    public List<Showtime> Showtimes { get; set; } = new List<Showtime>();
  }
}
