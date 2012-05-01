using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MovieDbLib.Data
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
    private CategoryTypes m_type;
    private String m_name;
    private String m_url;
    private int m_id;

    #endregion

    public override string ToString()
    {
      return m_type.ToString() + ": " + m_name;
    }

    /// <summary>
    /// Name of property
    /// </summary>
    public String Name
    {
      get { return m_name; }
      set { m_name = value; }
    }

    /// <summary>
    /// Type of property
    /// </summary>
    public CategoryTypes Type
    {
      get { return m_type; }
      set { m_type = value; }
    }

    public int Id
    {
      get { return m_id; }
      set { m_id = value; }
    }

    public String Url
    {
      get { return m_url; }
      set { m_url = value; }
    }
  }
}
