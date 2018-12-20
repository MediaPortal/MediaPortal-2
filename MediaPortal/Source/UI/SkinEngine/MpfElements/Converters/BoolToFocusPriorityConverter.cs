#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.UI.SkinEngine.ScreenManagement;
using System;
using System.Globalization;

namespace MediaPortal.UI.SkinEngine.MpfElements.Converters
{
  /// <summary>
  /// Converter which converts a boolean value to a <see cref="SetFocusPriority"/>.
  /// If the given value is <c>true</c>, the conversion result is the <see cref="SetFocusPriority"/> specified in <paramref name="parameter"/>
  /// or <see cref="SetFocusPriority.Default"/> if parameter is <c>null</c>.
  /// If the given value is <c>false</c> or not boolean, the conversion result is <see cref="SetFocusPriority.None"/>.
  /// </summary>
  public class BoolToFocusPriorityConverter : AbstractSingleDirectionConverter
  {
    #region IValueConverter implementation

    public override bool Convert(object val, Type targetType, object parameter, CultureInfo culture, out object result)
    {
      if (val is bool && (bool)val)
      {
        if (parameter is SetFocusPriority)
        {
          result = parameter;
        }
        else
        {
          string enumString = parameter as string;
          SetFocusPriority focusPriority;
          if (!string.IsNullOrEmpty(enumString) && Enum.TryParse(enumString, out focusPriority))
            result = focusPriority;
          else
            result = SetFocusPriority.Default;
        }
      }
      else
      {
        result = SetFocusPriority.None;
      }
      return true;
    }

    #endregion
  }
}
