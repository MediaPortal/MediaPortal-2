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
using MediaPortal.Common.Localization;
using MediaPortal.UI.SkinEngine.MarkupExtensions;

namespace MediaPortal.UI.SkinEngine.MpfElements.Converters
{
  /// <summary>
  /// DurationConverter formats TimeSpan values to string. It supports also Double and long values, which are treated as seconds. Int32 values will be treated as minutes.
  /// </summary>
  public class DurationConverter : IValueConverter
  {
    #region IValueConverter implementation

    public bool Convert(object val, Type targetType, object parameter, CultureInfo culture, out object result)
    {
      result = null;
      if (val == null)
        return false;

      TimeSpan? timeSpan = null;
      if (val is TimeSpan)
        timeSpan = (TimeSpan) val;
      if (val is double)
        timeSpan = TimeSpan.FromSeconds((Double) val);
      if (val is long)
        timeSpan = TimeSpan.FromSeconds((long) val);
      if (val is int)
        timeSpan = TimeSpan.FromMinutes((int) val);

      if (!timeSpan.HasValue)
        return false;

      string customFormat = (parameter != null ? parameter.ToString() : null);
      result = customFormat != null ? timeSpan.Value.ToString(customFormat) : FormattingUtils.FormatMediaDuration(timeSpan.Value);
      return true;
    }

    public bool ConvertBack(object val, Type targetType, object parameter, CultureInfo culture, out object result)
    {
      // Back conversion not supported
      result = null;
      return false;
    }

    #endregion
  }
}
