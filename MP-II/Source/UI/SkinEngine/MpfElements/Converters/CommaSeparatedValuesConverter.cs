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
using System.Collections;
using System.Globalization;
using MediaPortal.UI.SkinEngine.MarkupExtensions;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.Utilities;

namespace MediaPortal.UI.SkinEngine.MpfElements.Converters
{
  /// <summary>
  /// Value converter which converts an <see cref="IEnumerable"/> or <see cref="System.Collections.Generic.IEnumerable{T}"/>
  /// of arbitrary values to a string containing a comma-separated list of the string representations of the enumerable entries.
  /// Conversion back is supported too.
  /// </summary>
  public class CommaSeparatedValuesConverter : IValueConverter
  {
    #region IValueConverter implementation

    public bool Convert(object val, Type targetType, object parameter, CultureInfo culture, out object result)
    {
      result = null;
      if (val == null)
        return true;
      IEnumerable enumerable = val as IEnumerable;
      if (enumerable == null)
        return false;
      return TypeConverter.Convert(StringUtils.Join(", ", enumerable), targetType, out result);
    }

    public bool ConvertBack(object val, Type targetType, object parameter, CultureInfo culture, out object result)
    {
      result = null;
      if (val == null)
        return true;
      object str;
      if (!TypeConverter.Convert(val, typeof(string), out str))
        return false;
      string[] entryStrs = ((string) str).Split(',');
      Type enumerableType;
      Type entryType;
      ReflectionHelper.FindImplementedEnumerableType(targetType, out enumerableType, out entryType);
      IList resultCol = new ArrayList();
      foreach (string entryStr in entryStrs)
        if (entryType == null)
          resultCol.Add(entryStr);
        else
        {
          object convertedEntry;
          if (!TypeConverter.Convert(entryStr, entryType, out convertedEntry))
            return false;
          resultCol.Add(convertedEntry);
        }
      result = resultCol;
      return true;
    }

    #endregion
  }
}
