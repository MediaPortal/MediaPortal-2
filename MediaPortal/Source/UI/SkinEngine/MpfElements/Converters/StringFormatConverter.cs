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

namespace MediaPortal.UI.SkinEngine.MpfElements.Converters
{
  /// <summary>
  /// Value converter which uses a format string to build a string from a given variable.
  /// </summary>
  /// <remarks>
  /// This converter will often be used in XAML files. Note that in XAML, an attribute beginning with a <c>'{'</c> character
  /// is interpreted as an invocation of a markup extension. So the expression "{0}" must be escaped like this:
  /// <c>"{}{0}"</c>.
  /// </remarks>
  public class StringFormatConverter : AbstractSingleDirectionConverter
  {
    #region IValueConverter implementation

    public override bool Convert(object val, Type targetType, object parameter, System.Globalization.CultureInfo culture, out object result)
    {
      result = null;
      string expression = parameter as string;
      if (string.IsNullOrEmpty(expression))
        result = val.ToString();
      else
      {
        try
        {
          result = string.Format(culture, expression, val);
        }
        catch (Exception)
        {
          return false;
        }
      }
      return true;
    }

    #endregion
  }
}
