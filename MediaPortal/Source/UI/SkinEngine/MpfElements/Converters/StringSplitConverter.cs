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
using System.Linq;

namespace MediaPortal.UI.SkinEngine.MpfElements.Converters
{
  /// <summary>
  /// Value converter which splits the given string into a list of string parts.
  /// </summary>
  public class StringSplitConverter : AbstractSingleDirectionConverter
  {
    #region IValueConverter implementation

    public override bool Convert(object val, Type targetType, object parameter, System.Globalization.CultureInfo culture, out object result)
    {
      result = null;
      string stringVal = val as string;
      if (stringVal == null)
        return false;

      string[] separator = string.IsNullOrEmpty(parameter as string) ? new[] { ",", ";" } : new[] { parameter as string };
      result = stringVal.Split(separator, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
      return true;
    }

    #endregion
  }
}
