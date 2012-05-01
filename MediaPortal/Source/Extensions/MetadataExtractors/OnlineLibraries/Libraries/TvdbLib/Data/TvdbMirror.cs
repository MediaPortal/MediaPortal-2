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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data
{
  /// <summary>
  /// Baseclass for a tvdb mirror. A mirror is defined in the tvdb xml by:
  /// <![CDATA[
  /// <?xml version="1.0" encoding="UTF-8" ?>
  /// <Mirrors>
  ///  <Mirror>
  ///    <id>1</id>
  ///    <mirrorpath>http://thetvdb.com</mirrorpath>
  ///    <typemask>7</typemask>
  ///  </Mirror>
  /// </Mirrors>
  /// ]]>
  /// </summary>
  [Serializable]
  [Obsolete("Not used any more, however if won't delete the class since it could be useful at some point")]
  public class TvdbMirror
  {
    #region private properties

    #endregion

    /// <summary>
    /// TvdbMirror constructor
    /// </summary>
    public TvdbMirror()
      : this(Util.NO_VALUE, null, Util.NO_VALUE)
    {
    }

    /// <summary>
    /// TvdbMirror constructor
    /// </summary>
    /// <param name="id">Id of the mirror</param>
    /// <param name="mirror">Url to the mirror</param>
    /// <param name="typeMask">Typemask of the mirror, see property "TypeMask"</param>
    public TvdbMirror(int id, Uri mirror, int typeMask)
    {
      Id = id;
      MirrorPath = mirror;
      TypeMask = typeMask;
    }

    /// <summary>
    /// Id of the mirror
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The value of typemask is the sum of whichever file types that mirror holds:
    /// 1 xml files
    /// 2 banner files
    /// 4 zip files
    /// So, a mirror that has a typemask of 5 would hold XML and ZIP files, but no banner files. 
    /// </summary>
    public int TypeMask { get; set; }

    ///<summary>
    /// Returns true if the mirror offers images for downloading, false otherwise
    ///</summary>
    public bool OffersImages
    {
      get
      {
        //todo: proper way to calculate capabilities of a mirror
        return (TypeMask == 2 || TypeMask == 6 || TypeMask == 5 || TypeMask == 7);
      }
    }

    ///<summary>
    /// Returns true if the mirror offers xml files for downloading, false otherwise
    ///</summary>
    public bool OffersXml
    {      
      get
      {
        //all uneven TypeMasks indicate the mirror offers xml files
        return ((TypeMask%2) == 1);
      }
    }

    ///<summary>
    /// Returns true if the mirror offers zipped downloads, false otherwise
    ///</summary>
    public bool OffersZip
    {
     get
     {
       //if typemask is 4 or higher, it has to contain zipped files (1+2=3 ;))
       return (TypeMask >= 4);
     }

    }

    /// <summary>
    /// Path to the mirror
    /// </summary>
    public Uri MirrorPath { get; set; }
  }
}
