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

using System;
using System.Globalization;
using System.Windows.Data;

namespace MP2BootstrapperApp.Converters
{
  public class FileSizeUnitConverter : IValueConverter
  {
    const int KILOBYTE = 1024;
    const int MEGABYTE = KILOBYTE * KILOBYTE;
    const int GIGABYTE = KILOBYTE * KILOBYTE * KILOBYTE;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      double size = System.Convert.ToDouble(value);

      if (size < KILOBYTE)
        return size + " B";

      if (size < MEGABYTE)
        return Math.Round(size / KILOBYTE) + " KB";

      if (size < GIGABYTE)
        return Math.Round(size / MEGABYTE) + " MB";

      return Math.Round(size / GIGABYTE, 1) + " GB";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
