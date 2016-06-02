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
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data
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
    /// Abbreviation: en
    /// Name:         English
    /// 
    /// </summary>
    public static TvdbLanguage DefaultLanguage = new TvdbLanguage(7, "English", "en");

    /// <summary>
    /// language valid for all available languages
    /// Id:           7
    /// Abbreviation: en
    /// Name:         English
    /// 
    /// </summary>
    public static TvdbLanguage UniversalLanguage = new TvdbLanguage(99, "Universal", "all");

    #region private properties

    #endregion

    /// <summary>
    /// TvdbLanguage constructor
    /// </summary>
    public TvdbLanguage()
      : this(Util.NO_VALUE, "", "")
    {

    }

    /// <summary>
    /// TvdbLanguage constructor
    /// </summary>
    /// <param name="id">Id of language</param>
    /// <param name="name">Name of language (e.g. English)</param>
    /// <param name="abbr">Abbreviation of language (e.g. en)</param>
    public TvdbLanguage(int id, String name, String abbr)
    {
      Id = id;
      Name = name;
      Abbriviation = abbr;
    }

    /// <summary>
    /// Id of the language
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Abbreviation of the series
    /// </summary>
    public string Abbriviation { get; set; }

    /// <summary>
    /// Name of the series
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Returns String that describes the language in the format "Name (Abbreviation)"
    /// </summary>
    /// <returns>String representing this object (e.g. "en")</returns>
    public override string ToString()
    {
      return Abbriviation;
      //return base.ToString();
    }

    /// <summary>
    /// Overrides the equals Method to ensure a valid comparison of two language objects. The
    /// comparison currently matches abbriviation only.
    /// </summary>
    /// <param name="compare">object to compare with</param>
    /// <returns>True if the two language objects are the same, false otherwise</returns>
    public override bool Equals(object compare)
    {
      if (compare != null && compare.GetType() == typeof(TvdbLanguage) &&
          Abbriviation.Equals(((TvdbLanguage)compare).Abbriviation))
      {
        return true;
      }
      return false;
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
