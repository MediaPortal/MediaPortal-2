namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbLib.Data
{
  public class MovieDbCategory
  {
    public enum CategoryTypes { Unknown = 0, Genre = 1 };
    public enum MovieGenres//not used atm
    {
      ActionFilm = 0,
      Adventure = 1,
      Animation = 2,
      Comedy = 3,
      Crime = 4,
      Disaster = 5,
      Documentary = 6,
      Drama = 7,
      Eastern = 8,
      Erotic = 9,
      Fantasy = 10,
      Historical = 11,
      Horror = 12,
      Musical = 13,
      Mystery = 14,
      RoadMovie = 15,
      ScienceFiction = 16,
      Sport = 17,
      Thriller = 18,
      Western = 19,
    };

    #region private/protected fields

    #endregion

    public override string ToString()
    {
      return Type.ToString() + ": " + Name;
    }

    /// <summary>
    /// Name of property
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Type of property
    /// </summary>
    public CategoryTypes Type { get; set; }

    public int Id { get; set; }

    public string Url { get; set; }
  }
}
