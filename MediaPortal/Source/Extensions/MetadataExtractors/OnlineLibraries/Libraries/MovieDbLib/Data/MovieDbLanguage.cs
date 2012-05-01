using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MovieDbLib.Data
{
  public class MovieDbLanguage
  {
    #region private properties
    private String m_abbriviation;
    private String m_name;
    private int m_id;
    #endregion

    public MovieDbLanguage(int _id, String _abbriviation, String _name)
      : this()
    {
      m_id = _id;
      m_abbriviation = _abbriviation;
      m_name = _name;
    }

    public MovieDbLanguage()
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

    public String Abbriviation
    {
      get { return m_abbriviation; }
      set { m_abbriviation = value; }
    }
  }
}
