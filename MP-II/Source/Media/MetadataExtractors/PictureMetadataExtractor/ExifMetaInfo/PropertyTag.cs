#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace Media.Importers.PictureImporter
{
  class PropertyTag
  {
    private static System.Text.ASCIIEncoding _encoding = new System.Text.ASCIIEncoding();
    ///<summary>Transformes value of the entry byte array to apropriate .NET Framework type.</summary>
    /// <param name="aPropertyItem"></param>
    public static Object getValue(uint entryType, byte[] entryData)
    {
      if (entryData == null) return null;
      switch ((PropertyTagType)entryType)
      {
        case PropertyTagType.Byte:
          {
            if (entryData.Length == 1) return entryData[0];
            return entryData;
          }
        case PropertyTagType.ASCII:
          {
            string tmpstring = _encoding.GetString(entryData, 0, entryData.Length - 1);
            if (tmpstring.IndexOf("\0") != -1) tmpstring = tmpstring.Remove(tmpstring.IndexOf("\0")); // try to clean string values with attached zero bytes
            return tmpstring;
          }
        case PropertyTagType.Short:
          return BitConverter.ToUInt16(entryData,0);

        case PropertyTagType.Long:
          return BitConverter.ToUInt32(entryData, 0);

        case PropertyTagType.Rational:
          {
            ulong FirstLong = BitConverter.ToUInt32(entryData, 0);
            ulong SecondLong = BitConverter.ToUInt32(entryData, 4);
            return new Fraction(FirstLong, SecondLong);
          }
        case PropertyTagType.SLONG:
          return BitConverter.ToInt32(entryData, 0);

        case PropertyTagType.Undefined:
          {
            if (entryData.Length == 1) return entryData[0];
            return entryData;
          }
        case PropertyTagType.SRational:
          {
            long FirstLong = BitConverter.ToInt32(entryData, 0);
            long SecondLong = BitConverter.ToInt32(entryData, 4);
            return new Fraction(FirstLong, SecondLong);
          }
        default:
          {
            if (entryData.Length == 1) return entryData[0];
            return entryData;
          }
      }
    }

  }
}
