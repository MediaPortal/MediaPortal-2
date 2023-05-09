#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using MediaPortal.UI.SkinEngine.MarkupExtensions;
using System;
using System.Globalization;

namespace MediaPortal.UI.SkinEngine.MpfElements.Converters
{
  /// <summary>
  /// Converts a localized string representation of a double into a double. 
  /// </summary>
  /// <remarks>
  /// This converter is needed when binding a localized string representation of a double
  /// to a double property because the automatic type conversion for string to double always
  /// uses the default culture in MPF/WPF as the same code path is used to convert string literals
  /// declared in xaml and that needs to be locale neutral so always expects a '.' as the decimal separator.
  /// </remarks>
  public class LocalizedStringToDoubleConverter : IValueConverter
  {
    #region IValueConverter implementation

    public bool Convert(object val, Type targetType, object parameter, CultureInfo culture, out object result)
    {
      result = null;
      if (!(val is string valString))
        return false;
      
      if(!double.TryParse(valString, NumberStyles.Any, culture, out double resultDouble))
        return false;
      result = resultDouble;
      return true;
    }

    public bool ConvertBack(object val, Type targetType, object parameter, CultureInfo culture, out object result)
    {
      result = null;
      if (!(val is double valDouble))
        return false;

      result = valDouble.ToString(culture);
      return true;
    }

    #endregion
  }
}
