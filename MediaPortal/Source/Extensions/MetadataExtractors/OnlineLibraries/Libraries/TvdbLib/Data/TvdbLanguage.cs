/*
 *   TvdbLib: A library to retrieve information and media from http://thetvdb.com
 * 
 *   Copyright (C) 2008  Benjamin Gmeiner
 * 
 *   This program is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace TvdbLib.Data
{
  /// <summary>
  /// Baseclass for a tvdb language
  /// </summary>
  [Serializable]
  public class TvdbLanguage
  {
    /// <summary>
    /// The default language (which is English)
    /// Id:           7
    /// Abbriviation: en
    /// Name:         English
    /// 
    /// </summary>
    public static TvdbLanguage DefaultLanguage = new TvdbLanguage(7, "English", "en");

    /// <summary>
    /// language valid for all available languages
    /// Id:           7
    /// Abbriviation: en
    /// Name:         English
    /// 
    /// </summary>
    public static TvdbLanguage UniversalLanguage = new TvdbLanguage(99, "Universal", "all");

    #region private properties
    private String m_name;
    private String m_abbriviation;
    private int m_id;
    #endregion

    /// <summary>
    /// TvdbLanguage constructor
    /// </summary>
    public TvdbLanguage():this(-99, "", "")
    {

    }

    /// <summary>
    /// TvdbLanguage constructor
    /// </summary>
    /// <param name="_id">Id of language</param>
    /// <param name="_name">Name of language (e.g. English)</param>
    /// <param name="_abbr">Abbriviation of language (e.g. en)</param>
    public TvdbLanguage(int _id, String _name, String _abbr)
    {
      m_id = _id;
      m_name = _name;
      m_abbriviation = _abbr;
    }

    /// <summary>
    /// Id of the language
    /// </summary>
    public int Id
    {
      get { return m_id; }
      set { m_id = value; }
    }

    /// <summary>
    /// Abbriviation of the series
    /// </summary>
    public String Abbriviation
    {
      get { return m_abbriviation; }
      set { m_abbriviation = value; }
    }

    /// <summary>
    /// Name of the series
    /// </summary>
    public String Name
    {
      get { return m_name; }
      set { m_name = value; }
    }

    /// <summary>
    /// Returns String that describes the language in the format "Name (Abbriviation)"
    /// </summary>
    /// <returns>String representing this object (e.g. "English (en)")</returns>
    public override string ToString()
    {
      return m_name + "(" + m_abbriviation + ")";
      //return base.ToString();
    }

    /// <summary>
    /// Overrides the equals Method to ensure a valid comparison of two language objects. The
    /// comparison currently matches abbriviation only.
    /// </summary>
    /// <param name="_compare">object to compare with</param>
    /// <returns>True if the two language objects are the same, false otherwise</returns>
    public override bool Equals(object _compare)
    {
      if (_compare != null && _compare.GetType() == typeof(TvdbLanguage) &&
          this.Abbriviation.Equals(((TvdbLanguage)_compare).Abbriviation))
      {
        return true;
      }
      return false;
    }

    

    /// <summary>
    /// Returns a hash code of the object
    /// </summary>
    /// <returns>Hash code</returns>
    public override int GetHashCode()
    {
      return base.GetHashCode();
    }

    /// <summary>
    /// Overrides the equality operator to ensure a valid comparison of two language objects. The
    /// comparison currently matches abbriviation only.
    /// </summary>
    /// <param name="a">First language object</param>
    /// <param name="b">Second language object</param>
    /// <returns>True if the two language objects are the same, false otherwise</returns>
    public static bool operator ==(TvdbLanguage a, TvdbLanguage b)
    {
      if (((object)a) == null || ((object)b) == null) return false; 
      if (a.Abbriviation.Equals(b.Abbriviation)) return true;
      return false;
    }

    /// <summary>
    /// Overrides the inequality operator to ensure a valid comparison of two language objects. The
    /// comparison currently matches abbriviation only.
    /// </summary>
    /// <param name="a">First language object</param>
    /// <param name="b">Second language object</param>
    /// <returns>True if the two language objects are the same, false otherwise</returns>
    public static bool operator !=(TvdbLanguage a, TvdbLanguage b)
    {
      if (((object)a) == null && ((object)b) == null) return false; 
      if (((object)a) == null || ((object)b) == null) return true; 

      if (!a.Abbriviation.Equals(b.Abbriviation)) return true;
      return false;
    }
  }
}
