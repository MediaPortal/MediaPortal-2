using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MovieDbLib.Data
{
  public class MovieDbStudios
  {
    #region private/protected fields
    private int m_studioId;
    private String m_studioUrl;
    private String m_studioName;
    #endregion

    public MovieDbStudios()
    {

    }

    public MovieDbStudios(int _id, String _name)
      : this()
    {
      m_studioId = _id;
      m_studioName = _name;
    }

    public MovieDbStudios(int _id, String _name, String _url)
      : this(_id, _name)
    {
      m_studioUrl = _url;
    }

    public String Name
    {
      get { return m_studioName; }
      set { m_studioName = value; }
    }

    public String Url
    {
      get { return m_studioUrl; }
      set { m_studioUrl = value; }
    }

    public int Id
    {
      get { return m_studioId; }
      set { m_studioId = value; }
    }
  }
}
