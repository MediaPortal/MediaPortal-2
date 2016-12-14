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

using MediaPortal.UI.SkinEngine.MpfElements.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace MediaPortal.UiComponents.WMCSkin.Converters
{
  public class RoundingFormatConverter : AbstractSingleDirectionConverter
  {
    public override bool Convert(object val, Type targetType, object parameter, CultureInfo culture, out object result)
    {
      result = null;
      string parameterString = parameter as string;
      if (string.IsNullOrEmpty(parameterString))
        return false;
      string[] parameters = parameterString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

      int rounding;
      if (parameters.Length < 1 || !int.TryParse(parameters[0] as string, out rounding))
        return false;

      double number;
      try
      {
        number = System.Convert.ToDouble(val);
      }
      catch
      {
        return false;
      }

      int rounded = (int)Math.Round(number / rounding, MidpointRounding.AwayFromZero) * rounding;
      if (parameters.Length == 1)
      {
        result = rounded;
        return true;
      }

      try
      {
        result = string.Format(parameters[1], rounded);
        return true;
      }
      catch
      {
        return false;
      }
    }
  }
}
