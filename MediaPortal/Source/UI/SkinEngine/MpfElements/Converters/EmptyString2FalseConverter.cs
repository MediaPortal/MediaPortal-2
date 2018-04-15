#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Globalization;
using MediaPortal.UI.SkinEngine.Xaml;

namespace MediaPortal.UI.SkinEngine.MpfElements.Converters
{
  /// <summary>
  /// Converter which converts each string which is not empty to <c>true</c>. If the given string is empty or <c>null</c>,
  /// the conversion result is <c>false</c>.
  /// </summary>
  public class EmptyString2FalseConverter : AbstractSingleDirectionConverter
  {
    #region IValueConverter implementation

    public override bool Convert(object val, Type targetType, object parameter, CultureInfo culture, out object result)
    {
      if (targetType == typeof(bool))
      {
        string str = val as string;
        result = !string.IsNullOrEmpty(str);
        return true;
      }
      return TypeConverter.Convert(val, targetType, out result);
    }

    #endregion
  }
}
