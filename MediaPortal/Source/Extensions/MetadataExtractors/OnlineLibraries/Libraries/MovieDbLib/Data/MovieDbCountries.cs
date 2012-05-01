using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MovieDbLib.Data
{
  public class MovieDbCountries
  {
    #region private/protected fields
    private int m_id;
    private String m_name;
    private String m_code;
    private String m_url;
    #endregion

    public MovieDbCountries()
    {

    }

    public MovieDbCountries(int _id, String _name, String _code, String _url)
      : this()
    {

    }

    public int Id
    {
      get { return m_id; }
      set { m_id = value; }
    }

    public String Name
    {
      get { return m_name; }
      set { m_name = value; }
    }

    public String Code
    {
      get { return m_code; }
      set { m_code = value; }
    }

    public String Url
    {
      get { return m_url; }
      set { m_url = value; }
    }

  }
}
