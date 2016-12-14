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
using System.Text.RegularExpressions;
using MediaPortal.UI.SkinEngine.MpfElements.Converters;

namespace MediaPortal.UiComponents.ApolloOne.Converters
{
  public enum StringConversion
  {
    LowerCase,
    UpperCase,
    TitleCase
  }

  public class StringCaseConverter : AbstractSingleDirectionConverter
  {
    private static readonly Regex RE_TITLE_CASE = new Regex(@"\b[a-zA-Z]");

    public override bool Convert(object val, Type targetType, object parameter, CultureInfo culture, out object result)
    {
      result = null;
      string strVal = val as string;
      if (string.IsNullOrWhiteSpace(strVal))
        return false;

      StringConversion conv = (StringConversion)parameter;
      switch (conv)
      {
        case StringConversion.LowerCase:
          result = strVal.ToLower(culture);
          break;
        case StringConversion.UpperCase:
          result = strVal.ToUpper(culture);
          break;
        case StringConversion.TitleCase:
          result = RE_TITLE_CASE.Replace(strVal.ToLower(culture), m => m.Value.ToUpper());
          break;
      }
      return true;
    }
  }
}
